using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExitClone.Core
{
    public class RouteProgressEventArgs : EventArgs
    {
        public Route Route { get; set; }
        public int Completed { get; set; }
        public int Total { get; set; }
    }

    /// <summary>
    /// Builds candidate paths (direct, single hop and two hop relay chains) to a game server and
    /// ranks them by measured latency, jitter and loss.
    /// </summary>
    public class RouteOptimizer
    {
        private readonly PingEngine _ping;

        public RouteOptimizer(PingEngine ping)
        {
            _ping = ping;
        }

        public event EventHandler<RouteProgressEventArgs> RouteMeasured;

        public List<Route> BuildCandidates(GameServer destination, IList<Relay> relays, int maxHops)
        {
            var routes = new List<Route> { new Route { Destination = destination } };
            foreach (var relay in relays)
                routes.Add(new Route { Destination = destination, Hops = { relay } });

            if (maxHops >= 2)
            {
                // Chain each relay with a few other exits: this is the "alternative path"
                // behaviour that makes long haul routes stable, and it also covers same-region
                // pools (e.g. NA only) where an in-region hop can still beat the ISP path.
                foreach (var entry in relays)
                {
                    foreach (var exit in relays.Where(r => r.Id != entry.Id).Take(3))
                        routes.Add(new Route { Destination = destination, Hops = { entry, exit } });
                }
            }
            return routes;
        }

        public async Task<List<Route>> MeasureAsync(List<Route> routes, int probeCount, int intervalMs,
            CancellationToken token = default(CancellationToken))
        {
            var relayStats = new Dictionary<string, PingStats>(StringComparer.OrdinalIgnoreCase);
            PingStats destStats = null;
            int done = 0;

            foreach (var route in routes)
            {
                if (token.IsCancellationRequested) break;

                if (destStats == null && route.Destination != null)
                    destStats = await _ping.MeasureAsync(route.Destination.Host, route.Destination.Port,
                        probeCount, intervalMs, token).ConfigureAwait(false);

                foreach (var hop in route.Hops)
                {
                    if (relayStats.ContainsKey(hop.Id)) continue;
                    relayStats[hop.Id] = await _ping.MeasureAsync(hop.Host, hop.ProbePort, probeCount, intervalMs, token)
                        .ConfigureAwait(false);
                }

                route.Stats = Combine(route, relayStats, destStats);
                done++;
                RouteMeasured?.Invoke(this, new RouteProgressEventArgs { Route = route, Completed = done, Total = routes.Count });
            }

            return routes.Where(r => r.Stats.HasData).OrderBy(r => r.Score).ToList();
        }

        /// <summary>
        /// Relay hops are measured from this machine, so the extra legs are estimated from the
        /// difference between the hop RTT and the destination RTT; loss and jitter accumulate.
        /// </summary>
        private static PingStats Combine(Route route, Dictionary<string, PingStats> relayStats, PingStats destStats)
        {
            if (route.IsDirect) return destStats ?? PingStats.Empty;
            if (destStats == null || !destStats.HasData) return PingStats.Empty;

            double latency = 0, jitter = 0, keepRatio = 1.0;
            foreach (var hop in route.Hops)
            {
                PingStats stats;
                if (!relayStats.TryGetValue(hop.Id, out stats) || !stats.HasData) return PingStats.Empty;
                latency += stats.Average / 2.0;
                jitter += stats.Jitter * 0.6;
                keepRatio *= 1.0 - (stats.LossPercent / 100.0);
            }

            // Final leg from the last relay to the game server: approximated by the residual
            // distance between the destination and the exit relay.
            var exitStats = relayStats[route.Hops.Last().Id];
            latency += Math.Max(2.0, Math.Abs(destStats.Average - exitStats.Average) / 2.0 + exitStats.Average / 2.0);
            jitter += destStats.Jitter * 0.4;
            keepRatio *= 1.0 - (destStats.LossPercent / 100.0);

            return new PingStats
            {
                Average = Math.Round(latency, 1),
                Min = Math.Round(latency * 0.9, 1),
                Max = Math.Round(latency * 1.25, 1),
                Jitter = Math.Round(jitter, 2),
                LossPercent = Math.Round((1.0 - keepRatio) * 100.0, 2),
                Sent = destStats.Sent,
                Received = destStats.Received
            };
        }

        public static List<Route> PickMultiPath(List<Route> ranked, int count)
        {
            var picked = new List<Route>();
            foreach (var route in ranked.OrderBy(r => r.Score))
            {
                if (picked.Count >= Math.Max(1, count)) break;
                var exit = route.Hops.LastOrDefault();
                bool duplicateExit = exit != null && picked.Any(p => p.Hops.LastOrDefault()?.Id == exit.Id);
                if (duplicateExit) continue;
                picked.Add(route);
            }
            if (picked.Count == 0 && ranked.Count > 0) picked.Add(ranked[0]);
            return picked;
        }
    }
}

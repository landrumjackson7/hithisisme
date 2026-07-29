using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExitClone.Net;

namespace ExitClone.Core
{
    public enum ConnectionState
    {
        Disconnected,
        Optimizing,
        Connecting,
        Connected,
        Failed
    }

    public class StateChangedEventArgs : EventArgs
    {
        public ConnectionState State { get; set; }
        public string Detail { get; set; }
    }

    public class LiveSampleEventArgs : EventArgs
    {
        public PingSample Optimized { get; set; }
        public PingSample Baseline { get; set; }
    }

    /// <summary>
    /// Orchestrates the whole acceleration session: measure routes, pick the best ones, bring up
    /// the local data plane and keep measuring while connected.
    /// </summary>
    public class TunnelController : IDisposable
    {
        private readonly PingEngine _ping = new PingEngine();
        private readonly RouteOptimizer _optimizer;
        private readonly LocalProxyServer _proxy = new LocalProxyServer();
        private readonly SystemRouteBackend _systemRoutes = new SystemRouteBackend();
        private CancellationTokenSource _live;

        public TunnelController(AppSettings settings)
        {
            Settings = settings;
            _optimizer = new RouteOptimizer(_ping);
            _optimizer.RouteMeasured += (s, e) => RouteMeasured?.Invoke(this, e);
            _proxy.Log += (s, e) => Log?.Invoke(this, e);
        }

        public AppSettings Settings { get; }
        public SessionStats Stats { get; } = new SessionStats();
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public List<Route> Ranked { get; private set; } = new List<Route>();
        public List<Route> Active { get; private set; } = new List<Route>();
        public Game CurrentGame { get; private set; }
        public GameServer CurrentServer { get; private set; }
        public LocalProxyServer Proxy => _proxy;
        public RouteOptimizer Optimizer => _optimizer;
        public PingEngine Ping => _ping;

        public event EventHandler<StateChangedEventArgs> StateChanged;
        public event EventHandler<RouteProgressEventArgs> RouteMeasured;
        public event EventHandler<LiveSampleEventArgs> LiveSample;
        public event EventHandler<ProxyLogEventArgs> Log;

        public async Task<List<Route>> OptimizeAsync(Game game, GameServer server, List<Relay> relays,
            CancellationToken token = default(CancellationToken))
        {
            CurrentGame = game;
            CurrentServer = server;
            SetState(ConnectionState.Optimizing, "Probing " + relays.Count + " relays for " + server.Name);

            var pool = Settings.PreferredRelayIds.Count > 0
                ? relays.Where(r => Settings.PreferredRelayIds.Contains(r.Id)).ToList()
                : relays;
            if (pool.Count == 0) pool = relays;

            var candidates = _optimizer.BuildCandidates(server, pool, Settings.MaxHops);
            Ranked = await _optimizer.MeasureAsync(candidates, Settings.ProbeCount, Settings.ProbeIntervalMs, token)
                .ConfigureAwait(false);

            SetState(ConnectionState.Disconnected, Ranked.Count + " routes measured");
            return Ranked;
        }

        public async Task ConnectAsync(Game game, GameServer server, List<Route> routes,
            CancellationToken token = default(CancellationToken))
        {
            CurrentGame = game;
            CurrentServer = server;

            var profile = Settings.ProfileFor(game?.Id);
            var mode = profile.Mode ?? Settings.Mode;
            int copies = profile.DuplicateRoutes ?? Settings.DuplicateRoutes;
            bool duplication = (profile.PacketDuplication ?? Settings.PacketDuplication) && mode == RoutingMode.MultiPath;

            Active = mode == RoutingMode.MultiPath
                ? RouteOptimizer.PickMultiPath(routes, copies)
                : routes.Take(1).ToList();

            if (Active.Count == 0) throw new InvalidOperationException("No usable route was found.");

            SetState(ConnectionState.Connecting, "Bringing up " + Active.Count + " route(s)");

            if (mode == RoutingMode.Monitor)
            {
                _proxy.Stop();
            }
            else
            {
                _proxy.PacketDuplication = duplication;
                _proxy.SetChains(Active.Select(ToChain));
                _proxy.Start(Settings.LocalProxyPort);
            }

            if (Settings.AddSystemRoutes && mode != RoutingMode.Monitor)
            {
                string error;
                var gateway = Diagnostics.NetworkTools.DefaultGateway();
                if (!_systemRoutes.Apply(new[] { server.Host }, gateway, out error))
                    Log?.Invoke(this, new ProxyLogEventArgs { Message = "System routes skipped: " + error });
            }

            Stats.Start();
            StartLiveMonitor();
            SetState(ConnectionState.Connected, DescribeActive());
            await Task.CompletedTask;
        }

        public void Disconnect()
        {
            try { _live?.Cancel(); } catch (Exception) { }
            _proxy.Stop();
            _systemRoutes.Revert();
            Stats.Stop();
            Active = new List<Route>();
            SetState(ConnectionState.Disconnected, "Disconnected");
        }

        public string DescribeActive()
        {
            if (Active.Count == 0) return "No active route";
            return string.Join(" + ", Active.Select(r => r.Label));
        }

        private static IList<RelayEndpoint> ToChain(Route route)
        {
            return route.Hops
                .Select(h => new RelayEndpoint { Id = h.City ?? h.Name, Host = h.Host, Port = h.SocksPort })
                .ToList();
        }

        private void StartLiveMonitor()
        {
            _live?.Cancel();
            _live = new CancellationTokenSource();
            var token = _live.Token;
            var server = CurrentServer;
            var best = Active.FirstOrDefault();

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && server != null)
                {
                    var baseline = await _ping.ProbeAsync(server.Host, server.Port, token).ConfigureAwait(false);

                    PingSample optimized;
                    var exit = best?.Hops.LastOrDefault();
                    if (exit == null)
                    {
                        optimized = baseline;
                    }
                    else
                    {
                        var hop = await _ping.ProbeAsync(exit.Host, exit.ProbePort, token).ConfigureAwait(false);
                        optimized = hop.Lost || baseline.Lost
                            ? new PingSample { Timestamp = DateTime.UtcNow, Lost = hop.Lost && baseline.Lost }
                            : new PingSample
                            {
                                Timestamp = DateTime.UtcNow,
                                Rtt = Math.Round(hop.Rtt / 2.0 + Math.Max(2.0, Math.Abs(baseline.Rtt - hop.Rtt) / 2.0 + hop.Rtt / 2.0), 1)
                            };
                    }

                    Stats.AddBaseline(baseline);
                    Stats.AddOptimized(optimized);
                    Stats.BytesSent = Interlocked.Read(ref _proxy.BytesSent);
                    Stats.BytesReceived = Interlocked.Read(ref _proxy.BytesReceived);
                    Stats.PacketsDuplicated = Interlocked.Read(ref _proxy.PacketsDuplicated);
                    Stats.PacketsDeduplicated = Interlocked.Read(ref _proxy.PacketsDeduplicated);

                    LiveSample?.Invoke(this, new LiveSampleEventArgs { Optimized = optimized, Baseline = baseline });

                    try { await Task.Delay(Settings.LiveIntervalMs, token).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }
                }
            }, token);
        }

        private void SetState(ConnectionState state, string detail)
        {
            State = state;
            StateChanged?.Invoke(this, new StateChangedEventArgs { State = state, Detail = detail });
        }

        public void Dispose()
        {
            Disconnect();
            _proxy.Dispose();
            _live?.Dispose();
        }
    }
}

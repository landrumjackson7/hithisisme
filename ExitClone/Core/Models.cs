using System;
using System.Collections.Generic;
using System.Linq;

namespace ExitClone.Core
{
    public enum RelayProtocol
    {
        Tcp,
        Udp
    }

    public class Relay
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Region { get; set; }
        public string Host { get; set; }
        public int ProbePort { get; set; } = 443;
        public int SocksPort { get; set; } = 1080;
        public string Provider { get; set; }

        public override string ToString() => Name;
    }

    public class Game
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Publisher { get; set; }
        public List<string> ProcessNames { get; set; } = new List<string>();
        public List<GameServer> Servers { get; set; } = new List<GameServer>();
        public bool Favorite { get; set; }
        public bool Custom { get; set; }

        public override string ToString() => Name;
    }

    public class GameServer
    {
        public string Name { get; set; }
        public string Region { get; set; }
        public string Host { get; set; }
        public int Port { get; set; } = 443;
        public RelayProtocol Protocol { get; set; } = RelayProtocol.Udp;

        public override string ToString() => Name;
    }

    /// <summary>
    /// A candidate path to a game server: zero, one or two relay hops followed by the destination.
    /// </summary>
    public class Route
    {
        public List<Relay> Hops { get; set; } = new List<Relay>();
        public GameServer Destination { get; set; }
        public PingStats Stats { get; set; } = PingStats.Empty;
        public bool IsDirect => Hops.Count == 0;

        public string Label => IsDirect
            ? "Direct (ISP)"
            : string.Join(" -> ", Hops.Select(h => h.City ?? h.Name));

        public string Key => (IsDirect ? "direct" : string.Join("|", Hops.Select(h => h.Id)))
                             + "#" + (Destination?.Host ?? "-");

        /// <summary>
        /// Lower is better. Latency dominates, jitter and loss are penalised heavily because
        /// they are what players actually feel in game.
        /// </summary>
        public double Score
        {
            get
            {
                if (!Stats.HasData) return double.MaxValue;
                return Stats.Average + (Stats.Jitter * 2.5) + (Stats.LossPercent * 8.0);
            }
        }
    }

    public class PingSample
    {
        public DateTime Timestamp { get; set; }
        public double Rtt { get; set; }
        public bool Lost { get; set; }
    }

    public class PingStats
    {
        public static readonly PingStats Empty = new PingStats();

        public double Average { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Jitter { get; set; }
        public double LossPercent { get; set; }
        public int Sent { get; set; }
        public int Received { get; set; }
        public bool HasData => Received > 0;

        public static PingStats FromSamples(IEnumerable<PingSample> samples)
        {
            var list = samples?.ToList() ?? new List<PingSample>();
            var ok = list.Where(s => !s.Lost).Select(s => s.Rtt).ToList();
            var stats = new PingStats
            {
                Sent = list.Count,
                Received = ok.Count,
                LossPercent = list.Count == 0 ? 0 : (list.Count - ok.Count) * 100.0 / list.Count
            };
            if (ok.Count == 0) return stats;

            stats.Average = ok.Average();
            stats.Min = ok.Min();
            stats.Max = ok.Max();

            // RFC 3550 style mean deviation of consecutive samples.
            double jitter = 0;
            for (int i = 1; i < ok.Count; i++) jitter += Math.Abs(ok[i] - ok[i - 1]);
            stats.Jitter = ok.Count > 1 ? jitter / (ok.Count - 1) : 0;
            return stats;
        }
    }
}

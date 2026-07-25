using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ExitClone.Core
{
    /// <summary>
    /// Rolling window of live measurements used by the graphs and the before/after summary.
    /// </summary>
    public class SessionStats
    {
        private readonly object _sync = new object();
        private readonly List<PingSample> _optimized = new List<PingSample>();
        private readonly List<PingSample> _baseline = new List<PingSample>();
        private const int Capacity = 600;

        public DateTime? StartedUtc { get; private set; }
        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }
        public long PacketsDuplicated { get; set; }
        public long PacketsDeduplicated { get; set; }

        public void Start()
        {
            lock (_sync)
            {
                _optimized.Clear();
                _baseline.Clear();
                BytesSent = BytesReceived = PacketsDuplicated = PacketsDeduplicated = 0;
                StartedUtc = DateTime.UtcNow;
            }
        }

        public void Stop()
        {
            lock (_sync) StartedUtc = null;
        }

        public void AddOptimized(PingSample sample) => Add(_optimized, sample);
        public void AddBaseline(PingSample sample) => Add(_baseline, sample);

        private void Add(List<PingSample> target, PingSample sample)
        {
            lock (_sync)
            {
                target.Add(sample);
                if (target.Count > Capacity) target.RemoveRange(0, target.Count - Capacity);
            }
        }

        public List<PingSample> OptimizedSamples { get { lock (_sync) return _optimized.ToList(); } }
        public List<PingSample> BaselineSamples { get { lock (_sync) return _baseline.ToList(); } }
        public PingStats Optimized => PingStats.FromSamples(OptimizedSamples);
        public PingStats Baseline => PingStats.FromSamples(BaselineSamples);

        public TimeSpan Uptime => StartedUtc.HasValue ? DateTime.UtcNow - StartedUtc.Value : TimeSpan.Zero;

        public double ImprovementPercent
        {
            get
            {
                var b = Baseline;
                var o = Optimized;
                if (!b.HasData || !o.HasData || b.Average <= 0) return 0;
                return (b.Average - o.Average) / b.Average * 100.0;
            }
        }

        public string ExportCsv(string path)
        {
            var opt = OptimizedSamples;
            var baseline = BaselineSamples;
            var sb = new StringBuilder("timestamp_utc,optimized_ms,baseline_ms,optimized_lost,baseline_lost\n");
            for (int i = 0; i < Math.Max(opt.Count, baseline.Count); i++)
            {
                var o = i < opt.Count ? opt[i] : null;
                var b = i < baseline.Count ? baseline[i] : null;
                var ts = (o ?? b)?.Timestamp ?? DateTime.UtcNow;
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0:o},{1},{2},{3},{4}\n",
                    ts,
                    o != null && !o.Lost ? o.Rtt.ToString("0.0", CultureInfo.InvariantCulture) : "",
                    b != null && !b.Lost ? b.Rtt.ToString("0.0", CultureInfo.InvariantCulture) : "",
                    o != null && o.Lost ? 1 : 0,
                    b != null && b.Lost ? 1 : 0);
            }
            File.WriteAllText(path, sb.ToString());
            return path;
        }
    }
}

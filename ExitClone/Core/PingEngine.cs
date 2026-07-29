using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ExitClone.Core
{
    /// <summary>
    /// Latency measurement. ICMP is tried first; when it is filtered (very common for game
    /// servers and cloud edges) the engine falls back to timing a TCP handshake, which is what
    /// most commercial ping tools do.
    /// </summary>
    public class PingEngine
    {
        public int Timeout { get; set; } = 1200;

        private readonly Dictionary<string, bool> _icmpUsable = new Dictionary<string, bool>();
        private readonly object _sync = new object();

        public async Task<PingSample> ProbeAsync(string host, int port, CancellationToken token = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(host))
                return new PingSample { Timestamp = DateTime.UtcNow, Lost = true };

            bool tryIcmp;
            lock (_sync) tryIcmp = !_icmpUsable.ContainsKey(host) || _icmpUsable[host];

            if (tryIcmp)
            {
                var icmp = await IcmpAsync(host).ConfigureAwait(false);
                if (icmp != null)
                {
                    lock (_sync) _icmpUsable[host] = true;
                    return icmp;
                }
                lock (_sync) _icmpUsable[host] = false;
            }

            return await TcpAsync(host, port, token).ConfigureAwait(false);
        }

        public async Task<PingStats> MeasureAsync(string host, int port, int count, int intervalMs = 120,
            CancellationToken token = default(CancellationToken))
        {
            var samples = new List<PingSample>();
            for (int i = 0; i < count && !token.IsCancellationRequested; i++)
            {
                samples.Add(await ProbeAsync(host, port, token).ConfigureAwait(false));
                if (i < count - 1)
                {
                    try { await Task.Delay(intervalMs, token).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }
                }
            }
            return PingStats.FromSamples(samples);
        }

        private async Task<PingSample> IcmpAsync(string host)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(host, Timeout).ConfigureAwait(false);
                    if (reply.Status == IPStatus.Success)
                        return new PingSample { Timestamp = DateTime.UtcNow, Rtt = reply.RoundtripTime };
                    if (reply.Status == IPStatus.TimedOut)
                        return new PingSample { Timestamp = DateTime.UtcNow, Lost = true };
                }
            }
            catch (Exception)
            {
                // ICMP unavailable (no raw socket permission, filtered, DNS failure).
            }
            return null;
        }

        private async Task<PingSample> TcpAsync(string host, int port, CancellationToken token)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using (var client = new TcpClient())
                {
                    var connect = client.ConnectAsync(host, port <= 0 ? 443 : port);
                    var finished = await Task.WhenAny(connect, Task.Delay(Timeout, token)).ConfigureAwait(false);
                    if (finished != connect)
                        return new PingSample { Timestamp = DateTime.UtcNow, Lost = true };
                    await connect.ConfigureAwait(false);
                    sw.Stop();
                    return new PingSample { Timestamp = DateTime.UtcNow, Rtt = sw.Elapsed.TotalMilliseconds };
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
            {
                // The host answered, it simply does not serve this port: still a valid RTT.
                sw.Stop();
                return new PingSample { Timestamp = DateTime.UtcNow, Rtt = sw.Elapsed.TotalMilliseconds };
            }
            catch (Exception)
            {
                return new PingSample { Timestamp = DateTime.UtcNow, Lost = true };
            }
        }

        public static IPAddress ResolveFirst(string host)
        {
            IPAddress direct;
            if (IPAddress.TryParse(host, out direct)) return direct;
            try
            {
                var entries = Dns.GetHostAddresses(host);
                foreach (var e in entries)
                    if (e.AddressFamily == AddressFamily.InterNetwork) return e;
                return entries.Length > 0 ? entries[0] : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

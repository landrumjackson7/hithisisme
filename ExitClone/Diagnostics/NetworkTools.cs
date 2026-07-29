using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExitClone.Diagnostics
{
    public class HopResult
    {
        public int Ttl { get; set; }
        public string Address { get; set; }
        public double Rtt { get; set; }
        public bool TimedOut { get; set; }

        public override string ToString()
        {
            return TimedOut
                ? string.Format("{0,2}  *  request timed out", Ttl)
                : string.Format("{0,2}  {1,-24} {2:0.0} ms", Ttl, Address, Rtt);
        }
    }

    public static class NetworkTools
    {
        public static async Task<List<HopResult>> TracerouteAsync(string host, int maxHops = 30, int timeout = 1500,
            IProgress<HopResult> progress = null, CancellationToken token = default(CancellationToken))
        {
            var hops = new List<HopResult>();
            var payload = Encoding.ASCII.GetBytes(new string('x', 32));

            for (int ttl = 1; ttl <= maxHops && !token.IsCancellationRequested; ttl++)
            {
                var hop = new HopResult { Ttl = ttl };
                try
                {
                    using (var ping = new Ping())
                    {
                        var options = new PingOptions(ttl, true);
                        var sw = Stopwatch.StartNew();
                        var reply = await ping.SendPingAsync(host, timeout, payload, options).ConfigureAwait(false);
                        sw.Stop();

                        if (reply.Status == IPStatus.TimedOut)
                        {
                            hop.TimedOut = true;
                        }
                        else
                        {
                            hop.Address = reply.Address?.ToString() ?? "?";
                            hop.Rtt = reply.RoundtripTime > 0 ? reply.RoundtripTime : sw.Elapsed.TotalMilliseconds;
                        }

                        hops.Add(hop);
                        progress?.Report(hop);
                        if (reply.Status == IPStatus.Success) break;
                    }
                }
                catch (Exception ex)
                {
                    hop.TimedOut = true;
                    hop.Address = ex.GetBaseException().Message;
                    hops.Add(hop);
                    progress?.Report(hop);
                    break;
                }
            }
            return hops;
        }

        /// <summary>Throughput probe against a caller supplied URL (defaults to a small CDN object).</summary>
        public static async Task<double> DownloadSpeedMbpsAsync(string url = null, int seconds = 6,
            CancellationToken token = default(CancellationToken))
        {
            url = string.IsNullOrWhiteSpace(url) ? "https://speed.cloudflare.com/__down?bytes=25000000" : url;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.AllowAutoRedirect = true;
            request.Timeout = 15000;

            long total = 0;
            var sw = Stopwatch.StartNew();
            using (var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false))
            using (var stream = response.GetResponseStream())
            {
                var buffer = new byte[64 * 1024];
                while (stream != null && sw.Elapsed.TotalSeconds < seconds && !token.IsCancellationRequested)
                {
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false);
                    if (read <= 0) break;
                    total += read;
                }
            }
            sw.Stop();
            if (sw.Elapsed.TotalSeconds <= 0) return 0;
            return total * 8.0 / sw.Elapsed.TotalSeconds / 1_000_000.0;
        }

        public static List<string> DescribeAdapters()
        {
            var lines = new List<string>();
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
                             .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                         n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
                {
                    var props = nic.GetIPProperties();
                    var v4 = props.UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    var gateway = props.GatewayAddresses.FirstOrDefault();
                    lines.Add(string.Format("{0} [{1}]  ip={2}  gw={3}  speed={4} Mbps",
                        nic.Name,
                        nic.NetworkInterfaceType,
                        v4?.Address?.ToString() ?? "-",
                        gateway?.Address?.ToString() ?? "-",
                        nic.Speed > 0 ? (nic.Speed / 1_000_000).ToString() : "?"));
                }
            }
            catch (Exception ex)
            {
                lines.Add("Adapter enumeration failed: " + ex.Message);
            }
            return lines;
        }

        public static string DefaultGateway()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up)
                    .SelectMany(n => n.GetIPProperties().GatewayAddresses)
                    .Select(g => g.Address)
                    .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    ?.ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

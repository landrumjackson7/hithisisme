using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using ExitClone.Diagnostics;

namespace ExitClone.UI.Pages
{
    public class ToolsPage : UserControl
    {
        private readonly AppState _state;
        private readonly TextBox _target = new TextBox();
        private readonly TextBox _output = new TextBox();
        private CancellationTokenSource _cts;

        public ToolsPage(AppState state)
        {
            _state = state;
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            Padding = new Padding(18);

            var header = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Theme.Background };
            var caption = Theme.Caption("TARGET HOST");
            caption.Location = new Point(2, 4);
            _target.SetBounds(0, 24, 300, 26);
            _target.BackColor = Theme.SurfaceAlt;
            _target.ForeColor = Theme.Text;
            _target.BorderStyle = BorderStyle.FixedSingle;
            _target.Text = _state.SelectedServer?.Host ?? "1.1.1.1";

            var ping = Theme.GhostButton("Ping test");
            ping.SetBounds(312, 22, 110, 30);
            ping.Click += async (s, e) => await PingAsync();

            var trace = Theme.GhostButton("Traceroute");
            trace.SetBounds(430, 22, 110, 30);
            trace.Click += async (s, e) => await TraceAsync();

            var speed = Theme.GhostButton("Speed test");
            speed.SetBounds(548, 22, 110, 30);
            speed.Click += async (s, e) => await SpeedAsync();

            var adapters = Theme.GhostButton("Adapters");
            adapters.SetBounds(666, 22, 110, 30);
            adapters.Click += (s, e) =>
            {
                Clear();
                foreach (var line in NetworkTools.DescribeAdapters()) Append(line);
                Append("Default gateway: " + (NetworkTools.DefaultGateway() ?? "unknown"));
            };

            var stop = Theme.GhostButton("Stop");
            stop.SetBounds(784, 22, 80, 30);
            stop.Click += (s, e) => _cts?.Cancel();

            header.Controls.AddRange(new Control[] { caption, _target, ping, trace, speed, adapters, stop });

            _output.Multiline = true;
            _output.ReadOnly = true;
            _output.ScrollBars = ScrollBars.Both;
            _output.WordWrap = false;
            _output.BackColor = Theme.Surface;
            _output.ForeColor = Theme.Text;
            _output.Font = Theme.Mono;
            _output.BorderStyle = BorderStyle.None;
            _output.Dock = DockStyle.Fill;

            var card = Theme.Card();
            card.Dock = DockStyle.Fill;
            card.Controls.Add(_output);

            Controls.Add(card);
            Controls.Add(header);
        }

        private async System.Threading.Tasks.Task PingAsync()
        {
            Reset();
            var host = _target.Text.Trim();
            Append("Pinging " + host + " ...");
            var stats = await _state.Controller.Ping.MeasureAsync(host, _state.SelectedServer?.Port ?? 443, 10, 200, _cts.Token);
            Append(string.Format("sent={0} received={1} loss={2:0.0}%", stats.Sent, stats.Received, stats.LossPercent));
            Append(string.Format("min={0:0.0} ms avg={1:0.0} ms max={2:0.0} ms jitter={3:0.0} ms",
                stats.Min, stats.Average, stats.Max, stats.Jitter));
        }

        private async System.Threading.Tasks.Task TraceAsync()
        {
            Reset();
            var host = _target.Text.Trim();
            Append("Tracing route to " + host + " (max 30 hops)");
            var progress = new Progress<HopResult>(hop => Append(hop.ToString()));
            await NetworkTools.TracerouteAsync(host, 30, 1500, progress, _cts.Token);
            Append("Trace complete.");
        }

        private async System.Threading.Tasks.Task SpeedAsync()
        {
            Reset();
            Append("Measuring download throughput ...");
            try
            {
                var mbps = await NetworkTools.DownloadSpeedMbpsAsync(null, 6, _cts.Token);
                Append(string.Format("Download: {0:0.0} Mbps", mbps));
            }
            catch (Exception ex)
            {
                Append("Speed test failed: " + ex.Message);
            }
        }

        private void Reset()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            Clear();
        }

        private void Clear() => _output.Clear();

        private void Append(string line)
        {
            if (_output.IsDisposed) return;
            if (_output.InvokeRequired)
            {
                _output.BeginInvoke((Action)(() => Append(line)));
                return;
            }
            _output.AppendText(line + Environment.NewLine);
        }
    }
}

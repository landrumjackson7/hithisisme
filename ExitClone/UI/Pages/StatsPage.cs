using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using ExitClone.UI.Controls;

namespace ExitClone.UI.Pages
{
    public class StatsPage : UserControl
    {
        private readonly AppState _state;
        private readonly MetricCard _uptime = new MetricCard("Session uptime");
        private readonly MetricCard _avgOpt = new MetricCard("Average ping", "accelerated");
        private readonly MetricCard _avgBase = new MetricCard("Average ping", "direct");
        private readonly MetricCard _gain = new MetricCard("Latency saved");
        private readonly MetricCard _lossOpt = new MetricCard("Loss", "accelerated");
        private readonly MetricCard _lossBase = new MetricCard("Loss", "direct");
        private readonly MetricCard _traffic = new MetricCard("Traffic", "sent / received");
        private readonly MetricCard _dup = new MetricCard("Duplicated", "copies sent / dropped");
        private readonly LineChart _history = new LineChart { Capacity = 300 };
        private readonly Timer _timer = new Timer { Interval = 1000 };

        public StatsPage(AppState state)
        {
            _state = state;
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            Padding = new Padding(18);

            var grid = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 220, BackColor = Theme.Background };
            foreach (var card in new Control[] { _uptime, _avgOpt, _avgBase, _gain, _lossOpt, _lossBase, _traffic, _dup })
            {
                card.Margin = new Padding(0, 0, 12, 12);
                grid.Controls.Add(card);
            }

            var chartCard = Theme.Card();
            chartCard.Dock = DockStyle.Fill;
            _history.Dock = DockStyle.Fill;
            chartCard.Controls.Add(_history);

            var export = Theme.GhostButton("Export session CSV");
            export.Dock = DockStyle.Bottom;
            export.Click += (s, e) => Export();

            Controls.Add(chartCard);
            Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 8, BackColor = Theme.Background });
            Controls.Add(export);
            Controls.Add(grid);

            _timer.Tick += (s, e) => Tick();
            _timer.Start();
        }

        private void Tick()
        {
            var stats = _state.Controller.Stats;
            var opt = stats.Optimized;
            var direct = stats.Baseline;

            _uptime.Set(stats.Uptime.ToString(@"hh\:mm\:ss"));
            _avgOpt.Set(opt.HasData ? opt.Average.ToString("0.0") + " ms" : "--", Theme.Accent);
            _avgBase.Set(direct.HasData ? direct.Average.ToString("0.0") + " ms" : "--");
            var saved = direct.HasData && opt.HasData ? direct.Average - opt.Average : 0;
            _gain.Set(saved.ToString("0.0") + " ms", saved >= 0 ? Theme.Accent : Theme.Danger,
                stats.ImprovementPercent.ToString("0.0") + "%");
            _lossOpt.Set(opt.LossPercent.ToString("0.00") + "%", opt.LossPercent < 1 ? Theme.Accent : Theme.Danger);
            _lossBase.Set(direct.LossPercent.ToString("0.00") + "%");
            _traffic.Set(Human(stats.BytesSent) + " / " + Human(stats.BytesReceived));
            _dup.Set(stats.PacketsDuplicated + " / " + stats.PacketsDeduplicated);

            if (opt.HasData) _history.Push(opt.Average, direct.Average);
        }

        private static string Human(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB" };
            double value = bytes;
            int unit = 0;
            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }
            return value.ToString("0.#") + " " + units[unit];
        }

        private void Export()
        {
            using (var dialog = new SaveFileDialog
            {
                Filter = "CSV file|*.csv",
                FileName = "exitclone-session-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    _state.Controller.Stats.ExportCsv(dialog.FileName);
                    MessageBox.Show("Saved to " + Path.GetFileName(dialog.FileName), "ExitClone");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Export failed: " + ex.Message, "ExitClone");
                }
            }
        }
    }
}

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExitClone.Core;
using ExitClone.UI.Controls;

namespace ExitClone.UI.Pages
{
    public class DashboardPage : UserControl
    {
        private readonly AppState _state;
        private readonly ComboBox _games = new ComboBox();
        private readonly ComboBox _servers = new ComboBox();
        private readonly ComboBox _mode = new ComboBox();
        private readonly Button _optimize;
        private readonly Button _connect;
        private readonly LineChart _chart = new LineChart();
        private readonly TextBox _log = new TextBox();
        private readonly MetricCard _ping = new MetricCard("Ping", "optimized route");
        private readonly MetricCard _jitter = new MetricCard("Jitter", "stability");
        private readonly MetricCard _loss = new MetricCard("Packet loss", "last window");
        private readonly MetricCard _gain = new MetricCard("Improvement", "vs. direct ISP path");
        private readonly Label _routeLabel = new Label();
        private CancellationTokenSource _work;

        public DashboardPage(AppState state)
        {
            _state = state;
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            Padding = new Padding(18);

            _optimize = Theme.GhostButton("Optimize routes");
            _connect = Theme.PrimaryButton("Connect");

            BuildLayout();
            Wire();
            ReloadGames();
            UpdateState(_state.Controller.State, "Idle");
        }

        private void BuildLayout()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = Theme.Background };
            header.Controls.Add(Stack("Game", _games, 0, 320));
            header.Controls.Add(Stack("Server / region", _servers, 336, 250));
            header.Controls.Add(Stack("Routing mode", _mode, 602, 190));

            _optimize.SetBounds(806, 26, 150, 36);
            _connect.SetBounds(966, 26, 150, 36);
            header.Controls.Add(_optimize);
            header.Controls.Add(_connect);

            _routeLabel.SetBounds(2, 4, 900, 18);
            _routeLabel.ForeColor = Theme.TextDim;
            _routeLabel.Font = Theme.Small;
            _routeLabel.Text = "No active route";

            var metrics = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 104,
                BackColor = Theme.Background,
                Padding = new Padding(0, 6, 0, 6)
            };
            foreach (var card in new Control[] { _ping, _jitter, _loss, _gain })
            {
                card.Margin = new Padding(0, 0, 12, 0);
                metrics.Controls.Add(card);
            }

            _chart.Dock = DockStyle.Fill;
            var chartCard = Theme.Card();
            chartCard.Dock = DockStyle.Fill;
            chartCard.Controls.Add(_chart);

            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Vertical;
            _log.BackColor = Theme.Surface;
            _log.ForeColor = Theme.TextDim;
            _log.Font = Theme.Mono;
            _log.BorderStyle = BorderStyle.None;
            _log.Dock = DockStyle.Fill;
            var logCard = Theme.Card();
            logCard.Dock = DockStyle.Bottom;
            logCard.Height = 150;
            logCard.Controls.Add(_log);

            var body = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
            body.Controls.Add(chartCard);
            body.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 10, BackColor = Theme.Background });
            body.Controls.Add(logCard);

            Controls.Add(body);
            Controls.Add(metrics);
            Controls.Add(_routeLabel);
            Controls.Add(header);
            _routeLabel.Dock = DockStyle.Top;
            _routeLabel.Height = 20;
        }

        private Control Stack(string caption, ComboBox combo, int x, int width)
        {
            var panel = new Panel { Location = new Point(x, 12), Size = new Size(width, 56), BackColor = Theme.Background };
            var label = Theme.Caption(caption);
            label.Location = new Point(2, 0);
            combo.SetBounds(0, 20, width, 28);
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.FlatStyle = FlatStyle.Flat;
            combo.BackColor = Theme.SurfaceAlt;
            combo.ForeColor = Theme.Text;
            combo.Font = Theme.Body;
            panel.Controls.Add(label);
            panel.Controls.Add(combo);
            return panel;
        }

        private void Wire()
        {
            _mode.Items.AddRange(new object[] { RoutingMode.Optimized, RoutingMode.MultiPath, RoutingMode.Monitor });
            _mode.SelectedItem = _state.Settings.Mode;
            _mode.SelectedIndexChanged += (s, e) =>
            {
                if (_mode.SelectedItem is RoutingMode mode) _state.Settings.Mode = mode;
            };

            _games.SelectedIndexChanged += (s, e) =>
            {
                if (_games.SelectedItem is Game game && game != _state.SelectedGame) _state.Select(game);
                ReloadServers();
            };
            _servers.SelectedIndexChanged += (s, e) =>
            {
                if (_servers.SelectedItem is GameServer server) _state.SelectServer(server);
            };

            _optimize.Click += async (s, e) => await OptimizeAsync();
            _connect.Click += async (s, e) => await ToggleConnectionAsync();

            _state.SelectionChanged += (s, e) => BeginInvoke((Action)SyncSelection);
            _state.GamesChanged += (s, e) => BeginInvoke((Action)ReloadGames);

            var controller = _state.Controller;
            controller.StateChanged += (s, e) => Safe(() => UpdateState(e.State, e.Detail));
            controller.Log += (s, e) => Safe(() => Append(e.Message));
            controller.LiveSample += (s, e) => Safe(() => OnLiveSample(e));
            controller.RouteMeasured += (s, e) => Safe(() =>
                Append(string.Format("[{0}/{1}] {2}  {3:0.0} ms  jitter {4:0.0}  loss {5:0.0}%",
                    e.Completed, e.Total, e.Route.Label, e.Route.Stats.Average, e.Route.Stats.Jitter, e.Route.Stats.LossPercent)));
        }

        private void Safe(Action action)
        {
            if (!IsHandleCreated || IsDisposed) return;
            try { BeginInvoke(action); } catch (Exception) { }
        }

        private void ReloadGames()
        {
            _games.Items.Clear();
            foreach (var game in _state.Games) _games.Items.Add(game);
            _games.SelectedItem = _state.SelectedGame;
            ReloadServers();
        }

        private void ReloadServers()
        {
            _servers.Items.Clear();
            var game = _games.SelectedItem as Game ?? _state.SelectedGame;
            if (game == null) return;
            foreach (var server in game.Servers) _servers.Items.Add(server);
            _servers.SelectedItem = game.Servers.FirstOrDefault(s => s.Name == _state.SelectedServer?.Name)
                                    ?? game.Servers.FirstOrDefault();
        }

        private void SyncSelection()
        {
            if (!Equals(_games.SelectedItem, _state.SelectedGame)) _games.SelectedItem = _state.SelectedGame;
            if (!Equals(_servers.SelectedItem, _state.SelectedServer)) _servers.SelectedItem = _state.SelectedServer;
        }

        public async Task OptimizeAsync()
        {
            var game = _games.SelectedItem as Game;
            var server = _servers.SelectedItem as GameServer;
            if (game == null || server == null) return;

            _optimize.Enabled = false;
            _work = new CancellationTokenSource();
            Append("Measuring routes to " + server.Name + " (" + server.Host + ")...");
            try
            {
                var ranked = await _state.Controller.OptimizeAsync(game, server, _state.Relays, _work.Token);
                var best = ranked.FirstOrDefault();
                if (best != null)
                {
                    Append("Best route: " + best.Label + " (" + best.Stats.Average.ToString("0.0") + " ms)");
                    UpdateMetrics(best.Stats);
                    _routeLabel.Text = "Best measured route: " + best.Label;
                }
                else
                {
                    Append("No route responded. Check the connection and try again.");
                }
            }
            catch (Exception ex)
            {
                Append("Optimization failed: " + ex.Message);
            }
            finally
            {
                _optimize.Enabled = true;
            }
        }

        private async Task ToggleConnectionAsync()
        {
            var controller = _state.Controller;
            if (controller.State == ConnectionState.Connected)
            {
                controller.Disconnect();
                return;
            }

            var game = _games.SelectedItem as Game;
            var server = _servers.SelectedItem as GameServer;
            if (game == null || server == null) return;

            if (controller.Ranked.Count == 0 || controller.CurrentServer?.Host != server.Host)
                await OptimizeAsync();
            if (controller.Ranked.Count == 0) return;

            try
            {
                _chart.Clear();
                await controller.ConnectAsync(game, server, controller.Ranked);
                _state.Save();
            }
            catch (Exception ex)
            {
                Append("Connect failed: " + ex.Message);
            }
        }

        private void OnLiveSample(LiveSampleEventArgs e)
        {
            _chart.Push(e.Optimized.Lost ? 0 : e.Optimized.Rtt, e.Baseline.Lost ? 0 : e.Baseline.Rtt);
            var stats = _state.Controller.Stats;
            UpdateMetrics(stats.Optimized);
            _gain.Set(stats.ImprovementPercent.ToString("0.0") + "%",
                stats.ImprovementPercent >= 0 ? Theme.Accent : Theme.Danger,
                "direct " + stats.Baseline.Average.ToString("0.0") + " ms");
        }

        private void UpdateMetrics(PingStats stats)
        {
            _ping.Set(stats.HasData ? stats.Average.ToString("0.0") + " ms" : "--",
                stats.Average < 60 ? Theme.Accent : stats.Average < 120 ? Theme.Warning : Theme.Danger);
            _jitter.Set(stats.HasData ? stats.Jitter.ToString("0.0") + " ms" : "--",
                stats.Jitter < 8 ? Theme.Accent : Theme.Warning);
            _loss.Set(stats.HasData ? stats.LossPercent.ToString("0.0") + "%" : "--",
                stats.LossPercent < 1 ? Theme.Accent : Theme.Danger);
        }

        private void UpdateState(ConnectionState state, string detail)
        {
            _connect.Text = state == ConnectionState.Connected ? "Disconnect" : "Connect";
            _connect.BackColor = state == ConnectionState.Connected ? Theme.Danger : Theme.Accent;
            if (state == ConnectionState.Connected) _routeLabel.Text = "Active: " + _state.Controller.DescribeActive();
            if (!string.IsNullOrEmpty(detail)) Append(detail);
            StateChanged?.Invoke(this, new StateChangedEventArgs { State = state, Detail = detail });
        }

        public event EventHandler<StateChangedEventArgs> StateChanged;

        private void Append(string message)
        {
            if (_log.IsDisposed) return;
            _log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + message + Environment.NewLine);
        }
    }
}

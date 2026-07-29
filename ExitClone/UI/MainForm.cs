using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ExitClone.Core;
using ExitClone.UI.Controls;
using ExitClone.UI.Pages;

namespace ExitClone.UI
{
    public class MainForm : Form
    {
        private readonly AppState _state = new AppState();
        private readonly Panel _content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
        private readonly Panel _statusBar = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = Theme.Surface };
        private readonly Label _status = new Label();
        private readonly Label _statusDetail = new Label();
        private readonly List<NavButton> _nav = new List<NavButton>();
        private readonly Dictionary<string, UserControl> _pages = new Dictionary<string, UserControl>();
        private readonly NotifyIcon _tray = new NotifyIcon();
        private readonly GameDetector _detector;
        private DashboardPage _dashboard;
        private RoutesPage _routes;
        private Point _dragOrigin;
        private bool _dragging;

        public MainForm(bool startMinimized)
        {
            Text = "ExitClone";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1200, 780);
            MinimumSize = new Size(1000, 640);
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = Theme.Body;
            DoubleBuffered = true;

            BuildChrome();
            BuildPages();
            BuildStatusBar();
            BuildTray();

            _detector = new GameDetector(() => _state.Games) { Enabled = _state.Settings.DetectGames };
            _detector.GameStarted += OnGameStarted;
            _detector.GameStopped += OnGameStopped;

            FormClosing += OnFormClosing;
            Resize += (s, e) =>
            {
                if (WindowState == FormWindowState.Minimized && _state.Settings.MinimizeToTray) HideToTray();
            };

            if (startMinimized || _state.Settings.StartMinimized)
            {
                WindowState = FormWindowState.Minimized;
                if (_state.Settings.MinimizeToTray) Shown += (s, e) => HideToTray();
            }
        }

        private void BuildChrome()
        {
            var title = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.Surface };
            var brand = new Label
            {
                Text = "EXITCLONE",
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI Semibold", 12f),
                AutoSize = true,
                Location = new Point(16, 10)
            };
            var subtitle = new Label
            {
                Text = "route optimizer",
                ForeColor = Theme.TextDim,
                Font = Theme.Small,
                AutoSize = true,
                Location = new Point(122, 15)
            };

            var close = ChromeButton("X", Theme.Danger);
            close.Click += (s, e) => Close();
            var minimize = ChromeButton("-", Theme.TextDim);
            minimize.Click += (s, e) => WindowState = FormWindowState.Minimized;
            var maximize = ChromeButton("[]", Theme.TextDim);
            maximize.Click += (s, e) =>
                WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;

            title.Resize += (s, e) =>
            {
                close.Location = new Point(title.Width - 44, 6);
                maximize.Location = new Point(title.Width - 88, 6);
                minimize.Location = new Point(title.Width - 132, 6);
            };

            title.MouseDown += (s, e) =>
            {
                _dragging = true;
                _dragOrigin = e.Location;
            };
            title.MouseMove += (s, e) =>
            {
                if (!_dragging) return;
                Location = new Point(Location.X + e.X - _dragOrigin.X, Location.Y + e.Y - _dragOrigin.Y);
            };
            title.MouseUp += (s, e) => _dragging = false;

            title.Controls.AddRange(new Control[] { brand, subtitle, close, maximize, minimize });

            var sidebar = new Panel { Dock = DockStyle.Left, Width = 210, BackColor = Theme.Background };
            foreach (var name in new[] { "Settings", "Tools", "Statistics", "Routes", "Games", "Dashboard" })
            {
                var button = new NavButton(name);
                var target = name;
                button.Click += (s, e) => ShowPage(target);
                sidebar.Controls.Add(button);
                _nav.Add(button);
            }

            var account = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                ForeColor = Theme.TextDim,
                Font = Theme.Small,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Text = "Signed in as " + (string.IsNullOrEmpty(_state.Settings.AccountName) ? "guest" : _state.Settings.AccountName)
            };
            sidebar.Controls.Add(account);

            Controls.Add(_content);
            Controls.Add(sidebar);
            Controls.Add(title);
        }

        private Button ChromeButton(string text, Color color)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(36, 28),
                FlatStyle = FlatStyle.Flat,
                ForeColor = color,
                BackColor = Theme.Surface,
                Font = Theme.Body,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private void BuildPages()
        {
            _dashboard = new DashboardPage(_state);
            _routes = new RoutesPage(_state);
            var games = new GamesPage(_state);
            games.GameActivated += (s, e) => ShowPage("Dashboard");
            _routes.OptimizeRequested += async (s, e) =>
            {
                ShowPage("Dashboard");
                await _dashboard.OptimizeAsync();
                _routes.Refresh();
            };
            _dashboard.StateChanged += (s, e) => UpdateStatus(e.State, e.Detail);

            _pages["Dashboard"] = _dashboard;
            _pages["Games"] = games;
            _pages["Routes"] = _routes;
            _pages["Statistics"] = new StatsPage(_state);
            _pages["Tools"] = new ToolsPage(_state);
            _pages["Settings"] = new SettingsPage(_state);

            ShowPage("Dashboard");
        }

        private void BuildStatusBar()
        {
            _status.Dock = DockStyle.Left;
            _status.Width = 240;
            _status.TextAlign = ContentAlignment.MiddleLeft;
            _status.Padding = new Padding(16, 0, 0, 0);
            _status.ForeColor = Theme.TextDim;
            _status.Text = "Disconnected";

            _statusDetail.Dock = DockStyle.Fill;
            _statusDetail.TextAlign = ContentAlignment.MiddleLeft;
            _statusDetail.ForeColor = Theme.TextDim;
            _statusDetail.Font = Theme.Small;

            _statusBar.Controls.Add(_statusDetail);
            _statusBar.Controls.Add(_status);
            Controls.Add(_statusBar);
        }

        private void BuildTray()
        {
            _tray.Icon = Icon ?? SystemIcons.Application;
            _tray.Text = "ExitClone";
            _tray.Visible = true;
            _tray.DoubleClick += (s, e) => RestoreFromTray();

            var menu = new ContextMenuStrip();
            menu.Items.Add("Open", null, (s, e) => RestoreFromTray());
            menu.Items.Add("Disconnect", null, (s, e) => _state.Controller.Disconnect());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => Close());
            _tray.ContextMenuStrip = menu;
        }

        private void ShowPage(string name)
        {
            UserControl page;
            if (!_pages.TryGetValue(name, out page)) return;
            _content.Controls.Clear();
            _content.Controls.Add(page);
            page.BringToFront();
            foreach (var button in _nav) button.Active = button.Text == name;
            if (name == "Routes") _routes.Refresh();
        }

        private void UpdateStatus(ConnectionState state, string detail)
        {
            switch (state)
            {
                case ConnectionState.Connected:
                    _status.Text = "Connected";
                    _status.ForeColor = Theme.Accent;
                    break;
                case ConnectionState.Optimizing:
                case ConnectionState.Connecting:
                    _status.Text = state.ToString();
                    _status.ForeColor = Theme.Warning;
                    break;
                case ConnectionState.Failed:
                    _status.Text = "Failed";
                    _status.ForeColor = Theme.Danger;
                    break;
                default:
                    _status.Text = "Disconnected";
                    _status.ForeColor = Theme.TextDim;
                    break;
            }
            _statusDetail.Text = detail ?? "";
            _tray.Text = ("ExitClone - " + _status.Text).Substring(0, Math.Min(63, ("ExitClone - " + _status.Text).Length));
        }

        private async void OnGameStarted(object sender, GameEventArgs e)
        {
            if (!_state.Settings.DetectGames) return;
            UiDispatch.Post(this, () =>
            {
                _tray.ShowBalloonTip(4000, "ExitClone", e.Game.Name + " detected", ToolTipIcon.Info);
                _state.Select(e.Game);
            });

            if (!_state.Settings.AutoConnectOnGameLaunch) return;

            try
            {
                var server = _state.SelectedServer ?? e.Game.Servers.FirstOrDefault();
                if (server == null) return;
                var ranked = await _state.Controller.OptimizeAsync(e.Game, server, _state.Relays);
                if (ranked.Count > 0) await _state.Controller.ConnectAsync(e.Game, server, ranked);
            }
            catch (Exception)
            {
                // Auto-connect is best effort; the user can retry from the dashboard.
            }
        }

        private void OnGameStopped(object sender, GameEventArgs e)
        {
            if (_state.Controller.State == ConnectionState.Connected && _state.Settings.AutoConnectOnGameLaunch)
                _state.Controller.Disconnect();
        }

        private void HideToTray()
        {
            Hide();
            ShowInTaskbar = false;
            _tray.ShowBalloonTip(2000, "ExitClone", "Still running in the tray", ToolTipIcon.Info);
        }

        private void RestoreFromTray()
        {
            base.Show();
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            _state.Save();
            _detector.Dispose();
            _state.Controller.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
        }
    }
}

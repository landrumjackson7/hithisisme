using System;
using System.Drawing;
using System.Windows.Forms;
using ExitClone.Core;
using ExitClone.UI.Controls;

namespace ExitClone.UI.Pages
{
    public class SettingsPage : UserControl
    {
        private readonly AppState _state;
        private readonly FlowLayoutPanel _flow = new FlowLayoutPanel();
        private int _row;

        public SettingsPage(AppState state)
        {
            _state = state;
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            Padding = new Padding(18);

            _flow.Dock = DockStyle.Fill;
            _flow.FlowDirection = FlowDirection.TopDown;
            _flow.WrapContents = false;
            _flow.AutoScroll = true;
            _flow.BackColor = Theme.Background;
            Controls.Add(_flow);

            var settings = _state.Settings;

            Section("Startup");
            Toggle("Start with Windows", settings.StartWithWindows, v =>
            {
                settings.StartWithWindows = v;
                AutoStart.Set(v);
            });
            Toggle("Start minimized", settings.StartMinimized, v => settings.StartMinimized = v);
            Toggle("Minimize to system tray", settings.MinimizeToTray, v => settings.MinimizeToTray = v);

            Section("Automation");
            Toggle("Detect running games", settings.DetectGames, v => settings.DetectGames = v);
            Toggle("Auto-connect when a game launches", settings.AutoConnectOnGameLaunch,
                v => settings.AutoConnectOnGameLaunch = v);

            Section("Acceleration");
            Choice("Routing mode", new object[] { RoutingMode.Optimized, RoutingMode.MultiPath, RoutingMode.Monitor },
                settings.Mode, v => settings.Mode = (RoutingMode)v);
            Toggle("Packet duplication (multi-path only)", settings.PacketDuplication, v => settings.PacketDuplication = v);
            Number("Simultaneous routes", settings.DuplicateRoutes, 1, 5, v => settings.DuplicateRoutes = v);
            Number("Maximum relay hops", settings.MaxHops, 1, 2, v => settings.MaxHops = v);
            Toggle("Anti-DDoS filtering on relays", settings.AntiDdos, v => settings.AntiDdos = v);
            Toggle("Disable Nagle (TCP_NODELAY)", settings.DisableNagle, v => settings.DisableNagle = v);
            Number("MTU", settings.Mtu, 576, 1500, v => settings.Mtu = v);
            Number("Local SOCKS5 port", settings.LocalProxyPort, 1024, 65535, v => settings.LocalProxyPort = v);
            Toggle("Add Windows routes for game servers (needs admin)", settings.AddSystemRoutes,
                v => settings.AddSystemRoutes = v);

            Section("Measurement");
            Number("Probes per route", settings.ProbeCount, 3, 30, v => settings.ProbeCount = v);
            Number("Probe interval (ms)", settings.ProbeIntervalMs, 50, 1000, v => settings.ProbeIntervalMs = v);
            Number("Live sample interval (ms)", settings.LiveIntervalMs, 250, 5000, v => settings.LiveIntervalMs = v);

            Section("Data");
            var open = Theme.GhostButton("Open configuration folder");
            open.Width = 240;
            open.Click += (s, e) =>
            {
                try
                {
                    System.IO.Directory.CreateDirectory(SettingsStore.DataDirectory);
                    System.Diagnostics.Process.Start(SettingsStore.DataDirectory);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "ExitClone");
                }
            };
            _flow.Controls.Add(open);

            var save = Theme.PrimaryButton("Save settings");
            save.Width = 240;
            save.Margin = new Padding(0, 12, 0, 0);
            save.Click += (s, e) =>
            {
                _state.Save();
                MessageBox.Show("Settings saved to " + SettingsStore.SettingsPath, "ExitClone");
            };
            _flow.Controls.Add(save);
        }

        private void Section(string title)
        {
            var label = new Label
            {
                Text = title.ToUpperInvariant(),
                ForeColor = Theme.Accent,
                Font = Theme.Small,
                AutoSize = true,
                Margin = new Padding(0, _row++ == 0 ? 0 : 16, 0, 6)
            };
            _flow.Controls.Add(label);
        }

        private void Toggle(string caption, bool value, Action<bool> apply)
        {
            var row = Row(caption);
            var toggle = new ToggleSwitch { Checked = value, Location = new Point(430, 4) };
            toggle.CheckedChanged += (s, e) =>
            {
                apply(toggle.Checked);
                _state.Save();
            };
            row.Controls.Add(toggle);
        }

        private void Number(string caption, int value, int min, int max, Action<int> apply)
        {
            var row = Row(caption);
            var spinner = new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = Math.Min(Math.Max(value, min), max),
                Location = new Point(430, 2),
                Width = 90,
                BackColor = Theme.SurfaceAlt,
                ForeColor = Theme.Text,
                BorderStyle = BorderStyle.FixedSingle
            };
            spinner.ValueChanged += (s, e) =>
            {
                apply((int)spinner.Value);
                _state.Save();
            };
            row.Controls.Add(spinner);
        }

        private void Choice(string caption, object[] options, object value, Action<object> apply)
        {
            var row = Row(caption);
            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(430, 2),
                Width = 160,
                BackColor = Theme.SurfaceAlt,
                ForeColor = Theme.Text
            };
            combo.Items.AddRange(options);
            combo.SelectedItem = value;
            combo.SelectedIndexChanged += (s, e) =>
            {
                apply(combo.SelectedItem);
                _state.Save();
            };
            row.Controls.Add(combo);
        }

        private Panel Row(string caption)
        {
            var panel = new Panel { Size = new Size(600, 32), BackColor = Theme.Background, Margin = new Padding(0, 2, 0, 2) };
            var label = new Label
            {
                Text = caption,
                ForeColor = Theme.Text,
                Font = Theme.Body,
                AutoSize = true,
                Location = new Point(2, 7)
            };
            panel.Controls.Add(label);
            _flow.Controls.Add(panel);
            return panel;
        }
    }
}

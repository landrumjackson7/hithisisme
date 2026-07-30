using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FrameSight
{
    /// <summary>
    /// The configuration window. It owns the sampling timer and pushes readings into
    /// the overlay, so closing this window to the tray keeps the overlay alive.
    /// </summary>
    public class ControlForm : Form
    {
        private static readonly Color Background = Color.FromArgb(24, 26, 32);
        private static readonly Color Panel = Color.FromArgb(33, 36, 44);
        private static readonly Color Foreground = Color.FromArgb(228, 232, 240);

        private readonly OverlaySettings _settings;
        private readonly SensorHub _sensors = new SensorHub();
        private readonly OverlayForm _overlay;
        private readonly HotkeyWindow _hotkey = new HotkeyWindow();
        private readonly Timer _timer = new Timer();
        private readonly NotifyIcon _tray = new NotifyIcon();

        private readonly CheckedListBox _sensorList = new CheckedListBox();
        private readonly ComboBox _cornerBox = new ComboBox();
        private readonly NumericUpDown _interval = new NumericUpDown();
        private readonly NumericUpDown _fontSize = new NumericUpDown();
        private readonly TrackBar _alpha = new TrackBar();
        private readonly CheckBox _clickThrough = new CheckBox();
        private readonly CheckBox _showBars = new CheckBox();
        private readonly CheckBox _showLabels = new CheckBox();
        private readonly Button _accentButton = new Button();
        private readonly Button _logButton = new Button();
        private readonly Button _overlayButton = new Button();
        private readonly Label _logStatus = new Label();
        private readonly Label _hotkeyStatus = new Label();

        private SessionRecorder _recorder;
        private DateTime _lastLogUtc = DateTime.MinValue;
        private bool _suppressEvents;
        private bool _exiting;

        public ControlForm()
        {
            _settings = OverlaySettings.Load();
            _overlay = new OverlayForm(_settings);

            Text = "FrameSight — hardware overlay";
            BackColor = Background;
            ForeColor = Foreground;
            Font = new Font("Segoe UI", 9f);
            ClientSize = new Size(720, 520);
            MinimumSize = new Size(700, 480);
            StartPosition = FormStartPosition.CenterScreen;

            BuildLayout();
            LoadSettingsIntoUi();

            _timer.Interval = _settings.UpdateIntervalMs;
            _timer.Tick += (s, e) => Sample();
            _timer.Start();

            _hotkey.Pressed += ToggleOverlay;
            var registered = _hotkey.Register(_settings.HotkeyVirtualKey);
            _hotkeyStatus.Text = registered
                ? "Show/hide hotkey: " + (Keys)_settings.HotkeyVirtualKey
                : "Hotkey " + (Keys)_settings.HotkeyVirtualKey + " is taken by another app";

            SetUpTray();

            if (_settings.OverlayVisible)
                _overlay.Show();
            UpdateOverlayButton();

            Sample();
        }

        private void BuildLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(12),
                BackColor = Background
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            layout.Controls.Add(BuildSensorPanel(), 0, 0);
            layout.Controls.Add(BuildAppearancePanel(), 1, 0);

            var footer = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, BackColor = Background };
            _hotkeyStatus.AutoSize = true;
            _hotkeyStatus.ForeColor = Color.FromArgb(150, 158, 176);
            _hotkeyStatus.Margin = new Padding(4, 10, 12, 0);
            footer.Controls.Add(_hotkeyStatus);
            layout.Controls.Add(footer, 0, 1);
            layout.SetColumnSpan(footer, 2);

            Controls.Add(layout);
        }

        private Control BuildSensorPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Panel, Padding = new Padding(10) };

            var header = new Label
            {
                Text = "Sensors to display",
                Dock = DockStyle.Top,
                Height = 26,
                ForeColor = Foreground,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };

            _sensorList.Dock = DockStyle.Fill;
            _sensorList.BackColor = Color.FromArgb(44, 48, 58);
            _sensorList.ForeColor = Foreground;
            _sensorList.BorderStyle = BorderStyle.None;
            _sensorList.CheckOnClick = true;
            _sensorList.IntegralHeight = false;

            foreach (SensorKind kind in Enum.GetValues(typeof(SensorKind)))
            {
                var supported = _sensors.IsSupported(kind);
                _sensorList.Items.Add(new SensorEntry(kind, supported), supported && _settings.Sensors.Contains(kind));
            }

            // Attached after populating: Items.Add(item, checked) raises ItemCheck too.
            _sensorList.ItemCheck += (s, e) => BeginInvoke((Action)CommitSensors);

            panel.Controls.Add(_sensorList);
            panel.Controls.Add(header);
            return panel;
        }

        private Control BuildAppearancePanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Panel,
                Padding = new Padding(14),
                Margin = new Padding(12, 0, 0, 0),
                AutoScroll = true
            };

            panel.Controls.Add(SectionLabel("Overlay"));

            _overlayButton.Size = new Size(400, 34);
            _overlayButton.FlatStyle = FlatStyle.Flat;
            _overlayButton.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            _overlayButton.Click += (s, e) => ToggleOverlay();
            panel.Controls.Add(_overlayButton);

            panel.Controls.Add(FieldLabel("Corner"));
            _cornerBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _cornerBox.Width = 400;
            foreach (OverlayCorner corner in Enum.GetValues(typeof(OverlayCorner)))
                _cornerBox.Items.Add(corner);
            _cornerBox.SelectedIndexChanged += (s, e) =>
            {
                if (_suppressEvents) return;
                _settings.Corner = (OverlayCorner)_cornerBox.SelectedItem;
                Persist();
            };
            panel.Controls.Add(_cornerBox);

            panel.Controls.Add(FieldLabel("Background opacity"));
            _alpha.Minimum = 0;
            _alpha.Maximum = 255;
            _alpha.TickFrequency = 32;
            _alpha.Width = 400;
            _alpha.Scroll += (s, e) =>
            {
                if (_suppressEvents) return;
                _settings.BackgroundAlpha = _alpha.Value;
                Persist();
            };
            panel.Controls.Add(_alpha);

            panel.Controls.Add(FieldLabel("Font size"));
            _fontSize.Minimum = 7;
            _fontSize.Maximum = 28;
            _fontSize.Width = 90;
            _fontSize.ValueChanged += (s, e) =>
            {
                if (_suppressEvents) return;
                _settings.FontSize = (float)_fontSize.Value;
                Persist();
            };
            panel.Controls.Add(_fontSize);

            panel.Controls.Add(FieldLabel("Update interval (ms)"));
            _interval.Minimum = 100;
            _interval.Maximum = 5000;
            _interval.Increment = 100;
            _interval.Width = 90;
            _interval.ValueChanged += (s, e) =>
            {
                if (_suppressEvents) return;
                _settings.UpdateIntervalMs = (int)_interval.Value;
                _timer.Interval = _settings.UpdateIntervalMs;
                Persist();
            };
            panel.Controls.Add(_interval);

            _clickThrough.Text = "Click-through (mouse passes to the game)";
            _clickThrough.AutoSize = true;
            _clickThrough.ForeColor = Foreground;
            _clickThrough.CheckedChanged += (s, e) =>
            {
                if (_suppressEvents) return;
                _settings.ClickThrough = _clickThrough.Checked;
                _overlay.RefreshInteractivity();
                Persist();
            };
            panel.Controls.Add(_clickThrough);

            _showBars.Text = "Show load bars";
            _showBars.AutoSize = true;
            _showBars.ForeColor = Foreground;
            _showBars.CheckedChanged += (s, e) =>
            {
                if (_suppressEvents) return;
                _settings.ShowBars = _showBars.Checked;
                Persist();
            };
            panel.Controls.Add(_showBars);

            _showLabels.Text = "Show sensor names";
            _showLabels.AutoSize = true;
            _showLabels.ForeColor = Foreground;
            _showLabels.CheckedChanged += (s, e) =>
            {
                if (_suppressEvents) return;
                _settings.ShowLabels = _showLabels.Checked;
                Persist();
            };
            panel.Controls.Add(_showLabels);

            _accentButton.Text = "Accent colour\u2026";
            _accentButton.Size = new Size(160, 28);
            _accentButton.FlatStyle = FlatStyle.Flat;
            _accentButton.ForeColor = Foreground;
            _accentButton.Click += (s, e) => PickAccent();
            panel.Controls.Add(_accentButton);

            panel.Controls.Add(SectionLabel("CSV logging"));

            _logButton.Size = new Size(400, 32);
            _logButton.FlatStyle = FlatStyle.Flat;
            _logButton.ForeColor = Foreground;
            _logButton.Click += (s, e) => ToggleLogging();
            panel.Controls.Add(_logButton);

            _logStatus.AutoSize = false;
            _logStatus.Size = new Size(400, 34);
            _logStatus.ForeColor = Color.FromArgb(150, 158, 176);
            panel.Controls.Add(_logStatus);

            var openFolder = new Button
            {
                Text = "Open log folder",
                Size = new Size(160, 28),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Foreground
            };
            openFolder.Click += (s, e) =>
            {
                Directory.CreateDirectory(_settings.LogDirectory);
                Process.Start("explorer.exe", "\"" + _settings.LogDirectory + "\"");
            };
            panel.Controls.Add(openFolder);

            return panel;
        }

        private void LoadSettingsIntoUi()
        {
            _suppressEvents = true;
            _cornerBox.SelectedItem = _settings.Corner;
            _alpha.Value = _settings.BackgroundAlpha;
            _fontSize.Value = (decimal)_settings.FontSize;
            _interval.Value = _settings.UpdateIntervalMs;
            _clickThrough.Checked = _settings.ClickThrough;
            _showBars.Checked = _settings.ShowBars;
            _showLabels.Checked = _settings.ShowLabels;
            _accentButton.BackColor = _settings.AccentColor;
            _suppressEvents = false;
            UpdateLogButton();
        }

        private void CommitSensors()
        {
            if (_suppressEvents)
                return;

            _settings.Sensors = _sensorList.CheckedItems
                .OfType<SensorEntry>()
                .Select(entry => entry.Kind)
                .ToList();

            // An unsupported sensor would only ever read zero, so refuse the tick.
            var unsupported = _sensorList.CheckedItems.OfType<SensorEntry>().Where(e => !e.Supported).ToList();
            foreach (var entry in unsupported)
            {
                _sensorList.SetItemChecked(_sensorList.Items.IndexOf(entry), false);
                _settings.Sensors.Remove(entry.Kind);
            }

            Persist();
        }

        private void Persist()
        {
            _settings.Save();
            Sample();
        }

        private void Sample()
        {
            var readings = new List<Reading>();
            foreach (var kind in _settings.Sensors)
            {
                try
                {
                    readings.Add(_sensors.Read(kind));
                }
                catch (Exception)
                {
                    // A sensor that fails mid-session is skipped for this tick.
                }
            }

            if (readings.Count > 0)
                _overlay.Apply(readings);

            if (_recorder != null && (DateTime.UtcNow - _lastLogUtc).TotalMilliseconds >= _settings.LogIntervalMs)
            {
                _recorder.Write(readings);
                _lastLogUtc = DateTime.UtcNow;
                UpdateLogButton();
            }
        }

        private void ToggleOverlay()
        {
            if (_overlay.Visible)
                _overlay.Hide();
            else
                _overlay.Show();

            _settings.OverlayVisible = _overlay.Visible;
            _settings.Save();
            UpdateOverlayButton();
        }

        private void UpdateOverlayButton()
        {
            var visible = _overlay.Visible;
            _overlayButton.Text = visible ? "Hide overlay" : "Show overlay";
            _overlayButton.BackColor = visible ? Color.FromArgb(94, 214, 158) : Color.FromArgb(56, 60, 72);
            _overlayButton.ForeColor = visible ? Color.FromArgb(12, 24, 20) : Foreground;
        }

        private void ToggleLogging()
        {
            if (_recorder != null)
            {
                var file = _recorder.CurrentFile;
                var rows = _recorder.RowCount;
                _recorder.Dispose();
                _recorder = null;
                _settings.LoggingEnabled = false;
                _settings.Save();
                _logStatus.Text = string.Format("Saved {0} rows to {1}", rows, Path.GetFileName(file));
                UpdateLogButton();
                return;
            }

            _recorder = new SessionRecorder(_settings.LogDirectory);
            _lastLogUtc = DateTime.MinValue;
            _settings.LoggingEnabled = true;
            _settings.Save();
            UpdateLogButton();
        }

        private void UpdateLogButton()
        {
            if (_recorder == null)
            {
                _logButton.Text = "Start logging to CSV";
                _logButton.BackColor = Color.FromArgb(56, 60, 72);
                return;
            }

            _logButton.Text = "Stop logging";
            _logButton.BackColor = Color.FromArgb(214, 118, 94);
            _logStatus.Text = string.Format("Recording {0} row(s) to {1}",
                _recorder.RowCount, Path.GetFileName(_recorder.CurrentFile ?? "\u2014"));
        }

        private void PickAccent()
        {
            using (var dialog = new ColorDialog { Color = _settings.AccentColor, FullOpen = true })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                _settings.AccentHtmlColor = ColorTranslator.ToHtml(dialog.Color);
                _accentButton.BackColor = dialog.Color;
                Persist();
            }
        }

        private void SetUpTray()
        {
            _tray.Icon = SystemIcons.Information;
            _tray.Text = "FrameSight";
            _tray.Visible = true;

            var menu = new ContextMenuStrip();
            menu.Items.Add("Show/hide overlay", null, (s, e) => ToggleOverlay());
            menu.Items.Add("Settings", null, (s, e) =>
            {
                Show();
                WindowState = FormWindowState.Normal;
                Activate();
            });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) =>
            {
                _exiting = true;
                Close();
            });
            _tray.ContextMenuStrip = menu;
            _tray.DoubleClick += (s, e) => ToggleOverlay();
        }

        /// <summary>Closing the settings window leaves the overlay running in the tray.</summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_exiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                _tray.ShowBalloonTip(2000, "FrameSight", "Still running — use the tray icon to exit.", ToolTipIcon.Info);
                return;
            }

            _timer.Stop();
            _settings.Save();
            if (_recorder != null)
                _recorder.Dispose();
            _hotkey.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            _sensors.Dispose();
            _overlay.Dispose();
            base.OnFormClosing(e);
        }

        private static Label SectionLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Foreground,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Margin = new Padding(0, 12, 0, 6)
            };
        }

        private static Label FieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Color.FromArgb(170, 178, 196),
                Margin = new Padding(0, 10, 0, 2)
            };
        }

        /// <summary>List entry that annotates sensors this machine cannot report.</summary>
        private class SensorEntry
        {
            public SensorEntry(SensorKind kind, bool supported)
            {
                Kind = kind;
                Supported = supported;
            }

            public SensorKind Kind { get; private set; }
            public bool Supported { get; private set; }

            public override string ToString()
            {
                return Describe(Kind) + (Supported ? string.Empty : "  (not available)");
            }

            private static string Describe(SensorKind kind)
            {
                switch (kind)
                {
                    case SensorKind.CpuLoad: return "CPU load";
                    case SensorKind.CpuClock: return "CPU clock";
                    case SensorKind.RamUsedGb: return "RAM used (GB)";
                    case SensorKind.RamPercent: return "RAM used (%)";
                    case SensorKind.GpuLoad: return "GPU load";
                    case SensorKind.GpuMemoryMb: return "GPU memory";
                    case SensorKind.DiskActivity: return "Disk activity";
                    case SensorKind.NetworkDown: return "Network down";
                    case SensorKind.NetworkUp: return "Network up";
                    case SensorKind.ProcessCount: return "Process count";
                    case SensorKind.Uptime: return "Uptime";
                    case SensorKind.ForegroundApp: return "Foreground app";
                    default: return "Clock";
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GameBoost
{
    public class MainForm : Form
    {
        private static readonly Color Background = Color.FromArgb(22, 24, 30);
        private static readonly Color Panel = Color.FromArgb(31, 34, 42);
        private static readonly Color Foreground = Color.FromArgb(228, 232, 240);
        private static readonly Color Accent = Color.FromArgb(255, 156, 74);
        private static readonly Color Danger = Color.FromArgb(233, 118, 118);

        private readonly BoostEngine _engine = new BoostEngine();
        private readonly BoostSettings _settings;
        private readonly Timer _loadTimer = new Timer { Interval = 2000 };

        private readonly ListView _processList = new ListView();
        private readonly ListView _serviceList = new ListView();
        private readonly TextBox _log = new TextBox();
        private readonly TextBox _gameName = new TextBox();
        private readonly Button _boostButton = new Button();
        private readonly Label _loadLabel = new Label();
        private readonly Dictionary<string, CheckBox> _options = new Dictionary<string, CheckBox>();

        private bool _suppressEvents;

        public MainForm()
        {
            _settings = BoostSettings.Load();

            Text = "GameBoost — one-click gaming mode";
            BackColor = Background;
            ForeColor = Foreground;
            Font = new Font("Segoe UI", 9f);
            ClientSize = new Size(1020, 720);
            MinimumSize = new Size(940, 640);
            StartPosition = FormStartPosition.CenterScreen;

            BuildLayout();
            LoadProcesses();
            LoadServices();
            ApplySettingsToUi();

            _engine.Logged += Append;
            _loadTimer.Tick += (s, e) => RefreshLoad();
            _loadTimer.Start();
            RefreshLoad();

            if (!Elevation.IsElevated)
                Append("Running without administrator rights: services, power plan and standby memory will be skipped.");
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(12),
                BackColor = Background
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));

            var header = BuildHeader();
            root.Controls.Add(header, 0, 0);
            root.SetColumnSpan(header, 2);

            root.Controls.Add(BuildProcessPanel(), 0, 1);
            root.Controls.Add(BuildRightPanel(), 1, 1);

            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Vertical;
            _log.BackColor = Panel;
            _log.ForeColor = Foreground;
            _log.BorderStyle = BorderStyle.None;
            _log.Dock = DockStyle.Fill;
            _log.Margin = new Padding(0, 10, 0, 0);
            root.Controls.Add(_log, 0, 2);
            root.SetColumnSpan(_log, 2);

            Controls.Add(root);
        }

        private Control BuildHeader()
        {
            var header = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Panel,
                Padding = new Padding(12, 10, 12, 10),
                Margin = new Padding(0, 0, 0, 10)
            };

            _boostButton.Text = "Boost now";
            _boostButton.Size = new Size(160, 40);
            _boostButton.FlatStyle = FlatStyle.Flat;
            _boostButton.BackColor = Accent;
            _boostButton.ForeColor = Color.FromArgb(24, 18, 10);
            _boostButton.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            _boostButton.Click += (s, e) => Toggle();
            header.Controls.Add(_boostButton);

            _loadLabel.AutoSize = true;
            _loadLabel.Margin = new Padding(16, 12, 12, 0);
            _loadLabel.ForeColor = Foreground;
            header.Controls.Add(_loadLabel);

            header.Controls.Add(MakeLabel("Game exe"));
            _gameName.Width = 150;
            _gameName.BackColor = Color.FromArgb(44, 48, 58);
            _gameName.ForeColor = Foreground;
            _gameName.BorderStyle = BorderStyle.FixedSingle;
            _gameName.Margin = new Padding(4, 9, 4, 0);
            _gameName.TextChanged += (s, e) =>
            {
                if (!_suppressEvents)
                    _settings.GameProcessName = _gameName.Text;
            };
            header.Controls.Add(_gameName);

            header.Controls.Add(MakeButton("Refresh", () => { LoadProcesses(); LoadServices(); }));
            header.Controls.Add(MakeButton("Recommended", SelectRecommended));
            header.Controls.Add(MakeButton("Save settings", () =>
            {
                CaptureSelections();
                _settings.Save();
                Append("Settings saved.");
            }));

            return header;
        }

        private Control BuildProcessPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Background,
                Margin = new Padding(0, 0, 8, 0)
            };
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            panel.Controls.Add(MakeHeading("Background apps to pause while you play"), 0, 0);

            _processList.Dock = DockStyle.Fill;
            _processList.View = View.Details;
            _processList.CheckBoxes = true;
            _processList.FullRowSelect = true;
            _processList.BackColor = Panel;
            _processList.ForeColor = Foreground;
            _processList.BorderStyle = BorderStyle.None;
            _processList.Columns.Add("Process", 200);
            _processList.Columns.Add("Memory", 90, HorizontalAlignment.Right);
            _processList.Columns.Add("Window title", 250);
            panel.Controls.Add(_processList, 0, 1);

            return panel;
        }

        private Control BuildRightPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Background
            };
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            panel.Controls.Add(MakeHeading("What a boost does"), 0, 0);

            var options = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Panel,
                Padding = new Padding(10, 8, 10, 8)
            };
            AddOption(options, "suspend", "Pause the selected background apps");
            AddOption(options, "services", "Stop the selected Windows services");
            AddOption(options, "power", "Switch to the High performance power plan");
            AddOption(options, "trim", "Trim paused apps' working sets");
            AddOption(options, "standby", "Purge the standby memory list");
            AddOption(options, "temp", "Delete temp files");
            panel.Controls.Add(options, 0, 1);

            panel.Controls.Add(MakeHeading("Services (restored when you stop the boost)"), 0, 2);

            _serviceList.Dock = DockStyle.Fill;
            _serviceList.View = View.Details;
            _serviceList.CheckBoxes = true;
            _serviceList.FullRowSelect = true;
            _serviceList.BackColor = Panel;
            _serviceList.ForeColor = Foreground;
            _serviceList.BorderStyle = BorderStyle.None;
            _serviceList.Columns.Add("Service", 170);
            _serviceList.Columns.Add("State", 70);
            _serviceList.Columns.Add("What you lose", 260);
            panel.Controls.Add(_serviceList, 0, 3);

            return panel;
        }

        private void AddOption(Control parent, string key, string caption)
        {
            var box = new CheckBox
            {
                Text = caption,
                AutoSize = true,
                ForeColor = Foreground,
                Checked = true
            };
            _options[key] = box;
            parent.Controls.Add(box);
        }

        private void LoadProcesses()
        {
            var selected = new HashSet<string>(SelectedProcessNames(), StringComparer.OrdinalIgnoreCase);
            if (selected.Count == 0)
                selected = new HashSet<string>(_settings.SuspendList, StringComparer.OrdinalIgnoreCase);

            _processList.BeginUpdate();
            _processList.Items.Clear();
            foreach (var process in BoostEngine.CandidateProcesses())
            {
                var item = new ListViewItem(process.ProcessName) { Tag = process.ProcessName };
                item.SubItems.Add((BoostEngine.WorkingSet(process) / 1024.0 / 1024.0).ToString("0") + " MB");
                item.SubItems.Add(SafeTitle(process));
                item.Checked = selected.Contains(process.ProcessName);
                _processList.Items.Add(item);
            }
            _processList.EndUpdate();
        }

        private void LoadServices()
        {
            var selected = new HashSet<string>(SelectedServiceNames(), StringComparer.OrdinalIgnoreCase);
            if (selected.Count == 0)
                selected = new HashSet<string>(_settings.ServiceList, StringComparer.OrdinalIgnoreCase);

            _serviceList.BeginUpdate();
            _serviceList.Items.Clear();
            foreach (var candidate in ServiceCatalog.Enumerate())
            {
                var item = new ListViewItem(candidate.DisplayName) { Tag = candidate.Name };
                item.SubItems.Add(candidate.Running ? "Running" : "Stopped");
                item.SubItems.Add(candidate.Impact);
                item.Checked = selected.Contains(candidate.Name);
                _serviceList.Items.Add(item);
            }
            _serviceList.EndUpdate();
        }

        private void ApplySettingsToUi()
        {
            _suppressEvents = true;
            _options["suspend"].Checked = _settings.SuspendProcesses;
            _options["services"].Checked = _settings.StopServices;
            _options["power"].Checked = _settings.HighPerformancePowerPlan;
            _options["trim"].Checked = _settings.TrimWorkingSets;
            _options["standby"].Checked = _settings.PurgeStandbyMemory;
            _options["temp"].Checked = _settings.ClearTempFiles;
            _gameName.Text = _settings.GameProcessName;
            _suppressEvents = false;
        }

        private void CaptureSelections()
        {
            _settings.SuspendProcesses = _options["suspend"].Checked;
            _settings.StopServices = _options["services"].Checked;
            _settings.HighPerformancePowerPlan = _options["power"].Checked;
            _settings.TrimWorkingSets = _options["trim"].Checked;
            _settings.PurgeStandbyMemory = _options["standby"].Checked;
            _settings.ClearTempFiles = _options["temp"].Checked;
            _settings.SuspendList = SelectedProcessNames().ToList();
            _settings.ServiceList = SelectedServiceNames().ToList();
            _settings.GameProcessName = _gameName.Text;
        }

        private void SelectRecommended()
        {
            var recommended = Process.GetProcesses()
                .Where(p =>
                {
                    try
                    {
                        return SafeList.IsRecommended(p) && !SafeList.IsCritical(p);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                })
                .Select(p => p.ProcessName)
                .ToList();

            var set = new HashSet<string>(recommended, StringComparer.OrdinalIgnoreCase);
            foreach (ListViewItem item in _processList.Items)
                item.Checked = set.Contains((string)item.Tag);

            var defaults = new HashSet<string>(ServiceCatalog.DefaultNames, StringComparer.OrdinalIgnoreCase);
            foreach (ListViewItem item in _serviceList.Items)
                item.Checked = defaults.Contains((string)item.Tag);

            Append("Selected " + set.Count + " known resource hog(s) and the default service list.");
        }

        private void Toggle()
        {
            if (_engine.IsBoosted)
            {
                _engine.Restore();
                _boostButton.Text = "Boost now";
                _boostButton.BackColor = Accent;
                SetListsEnabled(true);
                LoadProcesses();
                LoadServices();
                return;
            }

            CaptureSelections();
            _settings.Save();

            if (_settings.SuspendList.Count == 0 && !_settings.PurgeStandbyMemory && !_settings.HighPerformancePowerPlan)
            {
                Append("Nothing selected to do.");
                return;
            }

            _log.Clear();
            _engine.Boost(_settings);
            _boostButton.Text = "Restore";
            _boostButton.BackColor = Danger;
            SetListsEnabled(false);
        }

        private void SetListsEnabled(bool enabled)
        {
            _processList.Enabled = enabled;
            _serviceList.Enabled = enabled;
            foreach (var option in _options.Values)
                option.Enabled = enabled;
        }

        private IEnumerable<string> SelectedProcessNames()
        {
            return _processList.CheckedItems.Cast<ListViewItem>().Select(i => (string)i.Tag);
        }

        private IEnumerable<string> SelectedServiceNames()
        {
            return _serviceList.CheckedItems.Cast<ListViewItem>().Select(i => (string)i.Tag);
        }

        private void RefreshLoad()
        {
            var load = SystemInfo.Read();
            _loadLabel.Text = string.Format("{0:0.0} GB free of {1:0.0} GB  •  {2:0}% used  •  {3} processes",
                load.AvailableGigabytes,
                load.TotalPhysicalBytes / 1024.0 / 1024.0 / 1024.0,
                load.MemoryUsedPercent,
                load.ProcessCount);
        }

        private void Append(string message)
        {
            _log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + message + Environment.NewLine);
        }

        private static string SafeTitle(Process process)
        {
            try
            {
                return process.MainWindowTitle;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static Label MakeHeading(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Color.FromArgb(160, 168, 186),
                Margin = new Padding(2, 4, 2, 6)
            };
        }

        private static Label MakeLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Foreground,
                Margin = new Padding(10, 13, 2, 0)
            };
        }

        private Button MakeButton(string text, Action onClick)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 50, 62),
                ForeColor = Foreground,
                Margin = new Padding(6, 8, 0, 8)
            };
            button.Click += (s, e) => onClick();
            return button;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_engine.IsBoosted)
            {
                var answer = MessageBox.Show(
                    "A boost is still active. Restore everything before closing?",
                    "GameBoost", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (answer == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                if (answer == DialogResult.Yes)
                    _engine.Restore();
            }

            _loadTimer.Stop();
            CaptureSelections();
            _settings.Save();
            base.OnFormClosing(e);
        }
    }
}

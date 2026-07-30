using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace OpenWASD
{
    public class MainForm : Form
    {
        private static readonly Color Background = Color.FromArgb(24, 26, 33);
        private static readonly Color Panel = Color.FromArgb(32, 35, 44);
        private static readonly Color Foreground = Color.FromArgb(226, 230, 240);
        private static readonly Color Accent = Color.FromArgb(94, 214, 158);

        private readonly MappingEngine _engine = new MappingEngine();
        private readonly Timer _previewTimer = new Timer { Interval = 40 };
        private readonly List<MappingProfile> _profiles;

        private readonly ComboBox _controllerBox = new ComboBox();
        private readonly ComboBox _profileBox = new ComboBox();
        private readonly ComboBox _leftStickBox = new ComboBox();
        private readonly ComboBox _rightStickBox = new ComboBox();
        private readonly ComboBox _kindBox = new ComboBox();
        private readonly ComboBox _keyBox = new ComboBox();
        private readonly ComboBox _mouseBox = new ComboBox();
        private readonly CheckBox _turboBox = new CheckBox();
        private readonly CheckBox _invertYBox = new CheckBox();
        private readonly CheckBox _startupBox = new CheckBox();
        private readonly Button _toggleButton = new Button();
        private readonly Button _captureButton = new Button();
        private readonly Label _statusLabel = new Label();
        private readonly Label _bindingHint = new Label();
        private readonly ListView _bindingList = new ListView();
        private readonly ControllerVisualizer _visualizer = new ControllerVisualizer();
        private readonly TrackBar _deadzone = new TrackBar();
        private readonly TrackBar _sensitivity = new TrackBar();
        private readonly TrackBar _acceleration = new TrackBar();
        private readonly TrackBar _triggerThreshold = new TrackBar();
        private readonly TrackBar _pollInterval = new TrackBar();
        private readonly Label _deadzoneValue = new Label();
        private readonly Label _sensitivityValue = new Label();
        private readonly Label _accelerationValue = new Label();
        private readonly Label _triggerValue = new Label();
        private readonly Label _pollValue = new Label();

        private bool _suppressEvents;
        private bool _capturingKey;

        public MainForm(bool startMinimized)
        {
            _profiles = ProfileStore.LoadAll();

            Text = "OpenWASD — gamepad to keyboard & mouse";
            BackColor = Background;
            ForeColor = Foreground;
            Font = new Font("Segoe UI", 9f);
            MinimumSize = new Size(940, 660);
            ClientSize = new Size(1000, 700);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;

            BuildLayout();
            PopulateStaticChoices();

            _engine.Polled += snapshot => _visualizer.Snapshot = snapshot;
            _previewTimer.Tick += (s, e) =>
            {
                if (!_engine.IsRunning)
                    _visualizer.Snapshot = _engine.Read();
                RefreshControllerLabels();
            };
            _previewTimer.Start();

            SelectProfile(_profiles[0]);

            if (!XInput.IsAvailable)
                SetStatus("XInput runtime not found — controller input is unavailable.", Color.FromArgb(233, 118, 118));
            else
                SetStatus("Idle. Pick a profile and press Start.", Foreground);

            if (startMinimized)
            {
                WindowState = FormWindowState.Minimized;
                Shown += (s, e) => StartEngine();
            }
        }

        private MappingProfile Current
        {
            get { return _profileBox.SelectedItem as MappingProfile; }
        }

        private ControlId SelectedControl
        {
            get
            {
                if (_bindingList.SelectedItems.Count == 0)
                    return ControlId.A;
                return (ControlId)_bindingList.SelectedItems[0].Tag;
            }
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
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var header = BuildHeader();
            root.Controls.Add(header, 0, 0);
            root.SetColumnSpan(header, 2);

            root.Controls.Add(BuildLeftColumn(), 0, 1);
            root.Controls.Add(BuildBindingColumn(), 1, 1);

            _statusLabel.AutoSize = true;
            _statusLabel.Margin = new Padding(4, 8, 4, 0);
            root.Controls.Add(_statusLabel, 0, 2);
            root.SetColumnSpan(_statusLabel, 2);

            Controls.Add(root);
        }

        private Control BuildHeader()
        {
            var header = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Panel,
                Padding = new Padding(10, 8, 10, 8),
                Margin = new Padding(0, 0, 0, 10)
            };

            header.Controls.Add(MakeLabel("Controller"));
            _controllerBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _controllerBox.Width = 150;
            _controllerBox.SelectedIndexChanged += (s, e) =>
            {
                if (_controllerBox.SelectedIndex >= 0)
                    _engine.ControllerIndex = _controllerBox.SelectedIndex;
            };
            header.Controls.Add(_controllerBox);

            header.Controls.Add(MakeLabel("Profile"));
            _profileBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _profileBox.Width = 210;
            _profileBox.DisplayMember = "Name";
            _profileBox.SelectedIndexChanged += (s, e) => OnProfileChanged();
            header.Controls.Add(_profileBox);

            header.Controls.Add(MakeButton("New", NewProfile));
            header.Controls.Add(MakeButton("Rename", RenameProfile));
            header.Controls.Add(MakeButton("Save", SaveProfile));
            header.Controls.Add(MakeButton("Duplicate", DuplicateProfile));
            header.Controls.Add(MakeButton("Delete", DeleteProfile));
            header.Controls.Add(MakeButton("Import", ImportProfile));
            header.Controls.Add(MakeButton("Export", ExportProfile));

            _toggleButton.Text = "Start remapping";
            _toggleButton.Width = 150;
            _toggleButton.Height = 30;
            _toggleButton.FlatStyle = FlatStyle.Flat;
            _toggleButton.BackColor = Accent;
            _toggleButton.ForeColor = Color.FromArgb(16, 20, 26);
            _toggleButton.Margin = new Padding(16, 2, 4, 2);
            _toggleButton.Click += (s, e) =>
            {
                if (_engine.IsRunning)
                    StopEngine();
                else
                    StartEngine();
            };
            header.Controls.Add(_toggleButton);

            return header;
        }

        private Control BuildLeftColumn()
        {
            var column = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Background,
                Margin = new Padding(0, 0, 8, 0)
            };
            column.RowStyles.Add(new RowStyle(SizeType.Percent, 46));
            column.RowStyles.Add(new RowStyle(SizeType.Percent, 54));

            _visualizer.Dock = DockStyle.Fill;
            _visualizer.Margin = new Padding(0, 0, 0, 8);
            column.Controls.Add(_visualizer, 0, 0);
            column.Controls.Add(BuildTuningPanel(), 0, 1);
            return column;
        }

        private Control BuildTuningPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                BackColor = Panel,
                Padding = new Padding(10),
                AutoScroll = true
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56));

            AddStickRow(panel, "Left stick", _leftStickBox, mode =>
            {
                if (Current != null) Current.LeftStickMode = mode;
                RefreshBindingList();
            });
            AddStickRow(panel, "Right stick", _rightStickBox, mode =>
            {
                if (Current != null) Current.RightStickMode = mode;
                RefreshBindingList();
            });

            AddSliderRow(panel, "Deadzone", _deadzone, _deadzoneValue, 0, 90, value =>
            {
                if (Current != null) Current.Deadzone = value / 100.0;
                _deadzoneValue.Text = value + "%";
            });
            AddSliderRow(panel, "Mouse speed", _sensitivity, _sensitivityValue, 1, 60, value =>
            {
                if (Current != null) Current.MouseSensitivity = value;
                _sensitivityValue.Text = value.ToString(CultureInfo.CurrentCulture);
            });
            AddSliderRow(panel, "Mouse curve", _acceleration, _accelerationValue, 100, 300, value =>
            {
                if (Current != null) Current.MouseAcceleration = value / 100.0;
                _accelerationValue.Text = (value / 100.0).ToString("0.00", CultureInfo.CurrentCulture);
            });
            AddSliderRow(panel, "Trigger point", _triggerThreshold, _triggerValue, 5, 95, value =>
            {
                if (Current != null) Current.TriggerThreshold = value / 100.0;
                _triggerValue.Text = value + "%";
            });
            AddSliderRow(panel, "Poll interval", _pollInterval, _pollValue,
                MappingProfile.MinPollIntervalMs, MappingProfile.MaxPollIntervalMs, value =>
            {
                if (Current != null) Current.PollIntervalMs = value;
                _pollValue.Text = value + " ms";
                if (_engine.IsRunning && Current != null)
                    _engine.Profile = Current;
            });

            _invertYBox.Text = "Invert mouse Y";
            _invertYBox.AutoSize = true;
            _invertYBox.ForeColor = Foreground;
            _invertYBox.CheckedChanged += (s, e) =>
            {
                if (!_suppressEvents && Current != null)
                    Current.InvertMouseY = _invertYBox.Checked;
            };
            panel.Controls.Add(_invertYBox);
            panel.SetColumnSpan(_invertYBox, 3);

            _startupBox.Text = "Launch with Windows (starts remapping automatically)";
            _startupBox.AutoSize = true;
            _startupBox.ForeColor = Foreground;
            _startupBox.Checked = Startup.IsEnabled();
            _startupBox.CheckedChanged += (s, e) =>
            {
                if (_suppressEvents)
                    return;
                try
                {
                    Startup.SetEnabled(_startupBox.Checked);
                }
                catch (Exception ex)
                {
                    SetStatus("Could not update startup entry: " + ex.Message, Color.FromArgb(233, 118, 118));
                }
            };
            panel.Controls.Add(_startupBox);
            panel.SetColumnSpan(_startupBox, 3);

            return panel;
        }

        private Control BuildBindingColumn()
        {
            var column = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Background
            };
            column.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            column.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _bindingList.Dock = DockStyle.Fill;
            _bindingList.View = View.Details;
            _bindingList.FullRowSelect = true;
            _bindingList.MultiSelect = false;
            _bindingList.HideSelection = false;
            _bindingList.BackColor = Panel;
            _bindingList.ForeColor = Foreground;
            _bindingList.BorderStyle = BorderStyle.None;
            _bindingList.Columns.Add("Control", 190);
            _bindingList.Columns.Add("Output", 220);
            _bindingList.Columns.Add("Notes", 120);
            _bindingList.SelectedIndexChanged += (s, e) => LoadSelectedBinding();
            _bindingList.Margin = new Padding(0, 0, 0, 8);
            column.Controls.Add(_bindingList, 0, 0);

            column.Controls.Add(BuildEditorPanel(), 0, 1);
            return column;
        }

        private Control BuildEditorPanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                BackColor = Panel,
                Padding = new Padding(10, 8, 10, 8)
            };

            panel.Controls.Add(MakeLabel("Output type"));
            _kindBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _kindBox.Width = 130;
            _kindBox.SelectedIndexChanged += (s, e) => ApplyEditorToBinding();
            panel.Controls.Add(_kindBox);

            panel.Controls.Add(MakeLabel("Key"));
            _keyBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _keyBox.Width = 140;
            _keyBox.SelectedIndexChanged += (s, e) => ApplyEditorToBinding();
            panel.Controls.Add(_keyBox);

            _captureButton.Text = "Press a key\u2026";
            _captureButton.Width = 100;
            _captureButton.FlatStyle = FlatStyle.Flat;
            _captureButton.Click += (s, e) => BeginCapture();
            panel.Controls.Add(_captureButton);

            panel.Controls.Add(MakeLabel("Mouse"));
            _mouseBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _mouseBox.Width = 110;
            _mouseBox.SelectedIndexChanged += (s, e) => ApplyEditorToBinding();
            panel.Controls.Add(_mouseBox);

            _turboBox.Text = "Turbo";
            _turboBox.AutoSize = true;
            _turboBox.ForeColor = Foreground;
            _turboBox.Margin = new Padding(8, 6, 4, 0);
            _turboBox.CheckedChanged += (s, e) => ApplyEditorToBinding();
            panel.Controls.Add(_turboBox);

            panel.Controls.Add(MakeButton("Clear", ClearBinding));
            panel.Controls.Add(MakeButton("Reset to FPS defaults", ResetToFpsDefaults));

            _bindingHint.AutoSize = true;
            _bindingHint.ForeColor = Color.FromArgb(150, 158, 176);
            _bindingHint.Margin = new Padding(4, 8, 4, 0);
            panel.Controls.Add(_bindingHint);

            return panel;
        }

        private void PopulateStaticChoices()
        {
            _suppressEvents = true;

            for (var i = 0; i < XInput.MaxControllers; i++)
                _controllerBox.Items.Add("Slot " + (i + 1));
            _controllerBox.SelectedIndex = 0;

            foreach (var box in new[] { _leftStickBox, _rightStickBox })
            {
                box.Items.Add(new ChoiceItem<StickMode>(StickMode.DigitalDirections, "Digital directions"));
                box.Items.Add(new ChoiceItem<StickMode>(StickMode.MouseCursor, "Mouse cursor"));
                box.Items.Add(new ChoiceItem<StickMode>(StickMode.ScrollWheel, "Scroll wheel"));
            }

            _kindBox.Items.Add(new ChoiceItem<BindingKind>(BindingKind.None, "Unbound"));
            _kindBox.Items.Add(new ChoiceItem<BindingKind>(BindingKind.Key, "Keyboard key"));
            _kindBox.Items.Add(new ChoiceItem<BindingKind>(BindingKind.Mouse, "Mouse button"));
            _kindBox.Items.Add(new ChoiceItem<BindingKind>(BindingKind.ScrollUp, "Scroll up"));
            _kindBox.Items.Add(new ChoiceItem<BindingKind>(BindingKind.ScrollDown, "Scroll down"));

            foreach (var virtualKey in KeyNames.Selectable().OrderBy(KeyNames.Describe, StringComparer.CurrentCultureIgnoreCase))
                _keyBox.Items.Add(new ChoiceItem<ushort>(virtualKey, KeyNames.Describe(virtualKey)));

            foreach (MouseButton button in Enum.GetValues(typeof(MouseButton)))
                _mouseBox.Items.Add(new ChoiceItem<MouseButton>(button, button == MouseButton.None ? "—" : button.ToString()));

            foreach (var profile in _profiles)
                _profileBox.Items.Add(profile);

            _suppressEvents = false;
        }

        private void SelectProfile(MappingProfile profile)
        {
            _profileBox.SelectedItem = profile;
            OnProfileChanged();
        }

        private void OnProfileChanged()
        {
            var profile = Current;
            if (profile == null)
                return;

            _engine.Profile = profile;

            _suppressEvents = true;
            SelectChoice(_leftStickBox, profile.LeftStickMode);
            SelectChoice(_rightStickBox, profile.RightStickMode);
            _deadzone.Value = Clamp((int)Math.Round(profile.Deadzone * 100), _deadzone);
            _sensitivity.Value = Clamp((int)Math.Round(profile.MouseSensitivity), _sensitivity);
            _acceleration.Value = Clamp((int)Math.Round(profile.MouseAcceleration * 100), _acceleration);
            _triggerThreshold.Value = Clamp((int)Math.Round(profile.TriggerThreshold * 100), _triggerThreshold);
            _pollInterval.Value = Clamp(profile.PollIntervalMs, _pollInterval);
            _invertYBox.Checked = profile.InvertMouseY;
            _deadzoneValue.Text = _deadzone.Value + "%";
            _sensitivityValue.Text = _sensitivity.Value.ToString(CultureInfo.CurrentCulture);
            _accelerationValue.Text = (_acceleration.Value / 100.0).ToString("0.00", CultureInfo.CurrentCulture);
            _triggerValue.Text = _triggerThreshold.Value + "%";
            _pollValue.Text = _pollInterval.Value + " ms";
            _suppressEvents = false;

            RefreshBindingList();
        }

        private void RefreshBindingList()
        {
            var profile = Current;
            if (profile == null)
                return;

            var previous = _bindingList.SelectedItems.Count > 0 ? (ControlId?)SelectedControl : null;

            _bindingList.BeginUpdate();
            _bindingList.Items.Clear();
            foreach (ControlId control in Enum.GetValues(typeof(ControlId)))
            {
                var binding = profile[control];
                var item = new ListViewItem(DescribeControl(control)) { Tag = control };
                item.SubItems.Add(binding.Describe());
                item.SubItems.Add(NotesFor(profile, control));
                if (previous.HasValue && previous.Value == control)
                    item.Selected = true;
                _bindingList.Items.Add(item);
            }
            if (_bindingList.SelectedItems.Count == 0 && _bindingList.Items.Count > 0)
                _bindingList.Items[0].Selected = true;
            _bindingList.EndUpdate();

            LoadSelectedBinding();
        }

        private void LoadSelectedBinding()
        {
            var profile = Current;
            if (profile == null || _bindingList.SelectedItems.Count == 0)
                return;

            var control = SelectedControl;
            var binding = profile[control];

            _suppressEvents = true;
            SelectChoice(_kindBox, binding.Kind);
            SelectChoice(_keyBox, binding.VirtualKey);
            SelectChoice(_mouseBox, binding.Mouse);
            _turboBox.Checked = binding.Turbo;
            var overridden = NotesFor(profile, control);
            _bindingHint.Text = overridden.Length > 0
                ? DescribeControl(control) + " is driven by the stick mode instead."
                : string.Empty;
            _keyBox.Enabled = binding.Kind == BindingKind.Key;
            _captureButton.Enabled = binding.Kind == BindingKind.Key;
            _mouseBox.Enabled = binding.Kind == BindingKind.Mouse;
            _turboBox.Enabled = binding.Kind == BindingKind.Key || binding.Kind == BindingKind.Mouse;
            _suppressEvents = false;
        }

        private void ApplyEditorToBinding()
        {
            if (_suppressEvents)
                return;

            var profile = Current;
            if (profile == null || _bindingList.SelectedItems.Count == 0)
                return;

            var binding = profile[SelectedControl];
            binding.Kind = SelectedChoice<BindingKind>(_kindBox);
            binding.VirtualKey = SelectedChoice<ushort>(_keyBox);
            binding.Mouse = SelectedChoice<MouseButton>(_mouseBox);
            binding.Turbo = _turboBox.Checked;

            UpdateSelectedRow(profile, binding);
            LoadSelectedBinding();
        }

        private void UpdateSelectedRow(MappingProfile profile, Binding binding)
        {
            var item = _bindingList.SelectedItems[0];
            item.SubItems[1].Text = binding.Describe();
            item.SubItems[2].Text = NotesFor(profile, (ControlId)item.Tag);
        }

        private void BeginCapture()
        {
            _capturingKey = true;
            _captureButton.Text = "Listening\u2026";
            SetStatus("Press any key to bind it to " + DescribeControl(SelectedControl) + ".", Accent);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_capturingKey)
            {
                var virtualKey = KeyNames.FromKeyEvent(e);
                _capturingKey = false;
                _captureButton.Text = "Press a key\u2026";

                _suppressEvents = true;
                SelectChoice(_kindBox, BindingKind.Key);
                if (!SelectChoice(_keyBox, virtualKey))
                {
                    _keyBox.Items.Add(new ChoiceItem<ushort>(virtualKey, KeyNames.Describe(virtualKey)));
                    _keyBox.SelectedIndex = _keyBox.Items.Count - 1;
                }
                _suppressEvents = false;

                ApplyEditorToBinding();
                SetStatus("Bound " + DescribeControl(SelectedControl) + " to " + KeyNames.Describe(virtualKey) + ".", Accent);
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
            base.OnKeyDown(e);
        }

        private void StartEngine()
        {
            var profile = Current;
            if (profile == null)
                return;

            if (!XInput.IsAvailable)
            {
                SetStatus("XInput runtime not found — cannot start.", Color.FromArgb(233, 118, 118));
                return;
            }

            _engine.Profile = profile;
            _engine.Start();
            _toggleButton.Text = "Stop remapping";
            _toggleButton.BackColor = Color.FromArgb(233, 118, 118);
            SetStatus("Remapping slot " + (_engine.ControllerIndex + 1) + " with \"" + profile.Name + "\".", Accent);
        }

        private void StopEngine()
        {
            _engine.Stop();
            _toggleButton.Text = "Start remapping";
            _toggleButton.BackColor = Accent;
            SetStatus("Stopped. All emulated keys released.", Foreground);
        }

        private void NewProfile()
        {
            var name = Prompt("Name the new profile", "Profile " + (_profiles.Count + 1));
            if (name == null)
                return;

            var profile = new MappingProfile { Name = name };
            AddProfile(profile);
            SetStatus("Created \"" + profile.Name + "\".", Accent);
        }

        private void DuplicateProfile()
        {
            var source = Current;
            if (source == null)
                return;

            var name = Prompt("Name for the copy", source.Name + " copy");
            if (name == null)
                return;

            var copy = source.Clone();
            copy.Name = name;
            AddProfile(copy);
            SetStatus("Duplicated into \"" + copy.Name + "\".", Accent);
        }

        private void RenameProfile()
        {
            var profile = Current;
            if (profile == null)
                return;

            var name = Prompt("New name", profile.Name);
            if (name == null)
                return;

            ProfileStore.Delete(profile);
            profile.Name = name;
            ProfileStore.Save(profile);
            _profileBox.Items[_profileBox.SelectedIndex] = profile; // refresh display text
            SetStatus("Renamed to \"" + profile.Name + "\".", Accent);
        }

        private void SaveProfile()
        {
            var profile = Current;
            if (profile == null)
                return;

            try
            {
                var path = ProfileStore.Save(profile);
                SetStatus("Saved to " + path, Accent);
            }
            catch (Exception ex)
            {
                SetStatus("Save failed: " + ex.Message, Color.FromArgb(233, 118, 118));
            }
        }

        private void DeleteProfile()
        {
            var profile = Current;
            if (profile == null || _profiles.Count == 1)
            {
                SetStatus("At least one profile must remain.", Color.FromArgb(233, 118, 118));
                return;
            }

            if (MessageBox.Show("Delete \"" + profile.Name + "\"?", "OpenWASD",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            ProfileStore.Delete(profile);
            _profiles.Remove(profile);
            _profileBox.Items.Remove(profile);
            _profileBox.SelectedIndex = 0;
            SetStatus("Deleted \"" + profile.Name + "\".", Foreground);
        }

        private void ImportProfile()
        {
            using (var dialog = new OpenFileDialog { Filter = "OpenWASD profile (*.json)|*.json", Title = "Import profile" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                var profile = ProfileStore.TryLoad(dialog.FileName);
                if (profile == null)
                {
                    SetStatus("That file is not a valid profile.", Color.FromArgb(233, 118, 118));
                    return;
                }

                ProfileStore.Save(profile);
                AddProfile(profile);
                SetStatus("Imported \"" + profile.Name + "\".", Accent);
            }
        }

        private void ExportProfile()
        {
            var profile = Current;
            if (profile == null)
                return;

            using (var dialog = new SaveFileDialog
            {
                Filter = "OpenWASD profile (*.json)|*.json",
                FileName = profile.Name + ".json",
                Title = "Export profile"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    ProfileStore.Export(profile, dialog.FileName);
                    SetStatus("Exported to " + dialog.FileName, Accent);
                }
                catch (Exception ex)
                {
                    SetStatus("Export failed: " + ex.Message, Color.FromArgb(233, 118, 118));
                }
            }
        }

        private void ClearBinding()
        {
            var profile = Current;
            if (profile == null || _bindingList.SelectedItems.Count == 0)
                return;

            var binding = profile[SelectedControl];
            binding.Kind = BindingKind.None;
            binding.VirtualKey = 0;
            binding.Mouse = MouseButton.None;
            binding.Turbo = false;
            UpdateSelectedRow(profile, binding);
            LoadSelectedBinding();
        }

        private void ResetToFpsDefaults()
        {
            var profile = Current;
            if (profile == null)
                return;

            var defaults = MappingProfile.CreateFpsDefault();
            profile.Bindings = defaults.Bindings;
            profile.LeftStickMode = defaults.LeftStickMode;
            profile.RightStickMode = defaults.RightStickMode;
            OnProfileChanged();
            SetStatus("Restored the FPS defaults in \"" + profile.Name + "\".", Accent);
        }

        private void AddProfile(MappingProfile profile)
        {
            ProfileStore.Save(profile);
            _profiles.Add(profile);
            _profileBox.Items.Add(profile);
            SelectProfile(profile);
        }

        private void RefreshControllerLabels()
        {
            for (var i = 0; i < XInput.MaxControllers; i++)
            {
                XInputState state;
                var connected = XInput.TryGetState(i, out state);
                var label = "Slot " + (i + 1) + (connected ? " (connected)" : string.Empty);
                if (!Equals(_controllerBox.Items[i], label))
                {
                    var selected = _controllerBox.SelectedIndex;
                    _suppressEvents = true;
                    _controllerBox.Items[i] = label;
                    _controllerBox.SelectedIndex = selected;
                    _suppressEvents = false;
                }
            }
        }

        private void SetStatus(string text, Color color)
        {
            _statusLabel.Text = text;
            _statusLabel.ForeColor = color;
        }

        private static string NotesFor(MappingProfile profile, ControlId control)
        {
            var mode = IsLeftStickDirection(control) ? profile.LeftStickMode
                : IsRightStickDirection(control) ? profile.RightStickMode
                : StickMode.DigitalDirections;

            if (mode == StickMode.MouseCursor)
                return "mouse cursor";
            if (mode == StickMode.ScrollWheel)
                return "scroll wheel";
            return string.Empty;
        }

        private static bool IsLeftStickDirection(ControlId control)
        {
            return control == ControlId.LeftStickUp || control == ControlId.LeftStickDown
                || control == ControlId.LeftStickLeft || control == ControlId.LeftStickRight;
        }

        private static bool IsRightStickDirection(ControlId control)
        {
            return control == ControlId.RightStickUp || control == ControlId.RightStickDown
                || control == ControlId.RightStickLeft || control == ControlId.RightStickRight;
        }

        private static string DescribeControl(ControlId control)
        {
            switch (control)
            {
                case ControlId.LeftShoulder: return "LB (left bumper)";
                case ControlId.RightShoulder: return "RB (right bumper)";
                case ControlId.LeftTrigger: return "LT (left trigger)";
                case ControlId.RightTrigger: return "RT (right trigger)";
                case ControlId.LeftThumbClick: return "Left stick click";
                case ControlId.RightThumbClick: return "Right stick click";
                case ControlId.DPadUp: return "D-pad up";
                case ControlId.DPadDown: return "D-pad down";
                case ControlId.DPadLeft: return "D-pad left";
                case ControlId.DPadRight: return "D-pad right";
                case ControlId.LeftStickUp: return "Left stick up";
                case ControlId.LeftStickDown: return "Left stick down";
                case ControlId.LeftStickLeft: return "Left stick left";
                case ControlId.LeftStickRight: return "Left stick right";
                case ControlId.RightStickUp: return "Right stick up";
                case ControlId.RightStickDown: return "Right stick down";
                case ControlId.RightStickLeft: return "Right stick left";
                case ControlId.RightStickRight: return "Right stick right";
                default: return control.ToString();
            }
        }

        private void AddStickRow(TableLayoutPanel panel, string caption, ComboBox box, Action<StickMode> onChanged)
        {
            panel.Controls.Add(MakeLabel(caption));
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.Dock = DockStyle.Left;
            box.Width = 170;
            box.SelectedIndexChanged += (s, e) =>
            {
                if (_suppressEvents)
                    return;
                onChanged(SelectedChoice<StickMode>(box));
            };
            panel.Controls.Add(box);
            panel.Controls.Add(new Label { Text = string.Empty, AutoSize = true });
        }

        private void AddSliderRow(TableLayoutPanel panel, string caption, TrackBar bar, Label value,
            int min, int max, Action<int> onChanged)
        {
            panel.Controls.Add(MakeLabel(caption));

            bar.Minimum = min;
            bar.Maximum = max;
            bar.TickStyle = TickStyle.None;
            bar.Dock = DockStyle.Top;
            bar.BackColor = Panel;
            bar.ValueChanged += (s, e) =>
            {
                if (_suppressEvents)
                    return;
                onChanged(bar.Value);
            };
            panel.Controls.Add(bar);

            value.AutoSize = true;
            value.ForeColor = Foreground;
            value.Margin = new Padding(4, 8, 4, 0);
            panel.Controls.Add(value);
        }

        private static int Clamp(int value, TrackBar bar)
        {
            return Math.Min(bar.Maximum, Math.Max(bar.Minimum, value));
        }

        private static Label MakeLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Foreground,
                Margin = new Padding(4, 8, 4, 0)
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
                Margin = new Padding(4, 3, 0, 3)
            };
            button.Click += (s, e) => onClick();
            return button;
        }

        private static bool SelectChoice<T>(ComboBox box, T value)
        {
            foreach (var item in box.Items)
            {
                var choice = item as ChoiceItem<T>;
                if (choice != null && EqualityComparer<T>.Default.Equals(choice.Value, value))
                {
                    box.SelectedItem = item;
                    return true;
                }
            }
            return false;
        }

        private static T SelectedChoice<T>(ComboBox box)
        {
            var choice = box.SelectedItem as ChoiceItem<T>;
            return choice == null ? default(T) : choice.Value;
        }

        private static string Prompt(string caption, string initial)
        {
            using (var dialog = new Form
            {
                Text = "OpenWASD",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(360, 120),
                MinimizeBox = false,
                MaximizeBox = false,
                BackColor = Background,
                ForeColor = Foreground
            })
            {
                var label = new Label { Text = caption, AutoSize = true, Location = new Point(12, 14) };
                var input = new TextBox { Text = initial, Location = new Point(12, 40), Width = 330 };
                var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(186, 74), FlatStyle = FlatStyle.Flat };
                var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(267, 74), FlatStyle = FlatStyle.Flat };

                dialog.Controls.AddRange(new Control[] { label, input, ok, cancel });
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;

                if (dialog.ShowDialog() != DialogResult.OK)
                    return null;

                var name = input.Text.Trim();
                return name.Length == 0 ? null : name;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _previewTimer.Stop();
            _engine.Dispose();
            base.OnFormClosing(e);
        }

        /// <summary>Combo box entry that pairs a value with its display text.</summary>
        private class ChoiceItem<T>
        {
            private readonly string _text;

            public ChoiceItem(T value, string text)
            {
                Value = value;
                _text = text;
            }

            public T Value { get; private set; }

            public override string ToString()
            {
                return _text;
            }
        }
    }
}

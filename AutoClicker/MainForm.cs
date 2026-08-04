using System;
using System.Drawing;
using System.Windows.Forms;

namespace AstryxAutoClicker
{
    public sealed class MainForm : Form
    {
        private const int HotkeyId = 0xA5C1;

        private static readonly Color Bg = Color.FromArgb(8, 12, 22);
        private static readonly Color Panel = Color.FromArgb(15, 32, 62);
        private static readonly Color Acc = Color.FromArgb(56, 132, 255);
        private static readonly Color Txt = Color.FromArgb(244, 246, 255);
        private static readonly Color Muted = Color.FromArgb(132, 143, 175);
        private static readonly Color Grn = Color.FromArgb(38, 201, 132);
        private static readonly Color Warn = Color.FromArgb(255, 172, 54);

        private readonly ClickEngine engine = new ClickEngine();

        private NumericUpDown cpsInput;
        private ComboBox buttonBox;
        private ComboBox hotkeyBox;
        private Button toggleButton;
        private Label statusLabel;
        private bool hotkeyRegistered;

        private static readonly Keys[] HotkeyChoices =
        {
            Keys.F6, Keys.F7, Keys.F8, Keys.F9, Keys.F10
        };

        public MainForm()
        {
            Text = "Astryx AutoClicker";
            BackColor = Bg;
            ForeColor = Txt;
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(360, 340);

            BuildUi();
        }

        private void BuildUi()
        {
            var title = new Label
            {
                Text = "Astryx AutoClicker",
                Font = new Font("Segoe UI Semibold", 13f),
                ForeColor = Txt,
                AutoSize = true,
                Location = new Point(20, 18)
            };
            Controls.Add(title);

            var subtitle = new Label
            {
                Text = "Clicks per second: 1 - 10000",
                ForeColor = Muted,
                AutoSize = true,
                Location = new Point(22, 46)
            };
            Controls.Add(subtitle);

            Controls.Add(MakeLabel("Target CPS", 20, 84));
            cpsInput = new NumericUpDown
            {
                Location = new Point(150, 82),
                Size = new Size(180, 24),
                Minimum = 1,
                Maximum = 10000,
                Value = 20,
                BackColor = Panel,
                ForeColor = Txt,
                BorderStyle = BorderStyle.FixedSingle
            };
            cpsInput.ValueChanged += (s, e) => engine.TargetCps = (int)cpsInput.Value;
            Controls.Add(cpsInput);

            Controls.Add(MakeLabel("Mouse button", 20, 124));
            buttonBox = new ComboBox
            {
                Location = new Point(150, 122),
                Size = new Size(180, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Panel,
                ForeColor = Txt,
                FlatStyle = FlatStyle.Flat
            };
            buttonBox.Items.AddRange(new object[] { "Left", "Right", "Middle" });
            buttonBox.SelectedIndex = 0;
            buttonBox.SelectedIndexChanged += (s, e) => engine.Button = (MouseButtonKind)buttonBox.SelectedIndex;
            Controls.Add(buttonBox);

            Controls.Add(MakeLabel("Start/stop hotkey", 20, 164));
            hotkeyBox = new ComboBox
            {
                Location = new Point(150, 162),
                Size = new Size(180, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Panel,
                ForeColor = Txt,
                FlatStyle = FlatStyle.Flat
            };
            foreach (var key in HotkeyChoices)
            {
                hotkeyBox.Items.Add(key.ToString());
            }
            hotkeyBox.SelectedIndex = 0;
            hotkeyBox.SelectedIndexChanged += (s, e) => RegisterCurrentHotkey();
            Controls.Add(hotkeyBox);

            toggleButton = new Button
            {
                Text = "Start (F6)",
                Location = new Point(20, 210),
                Size = new Size(310, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Acc,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11f)
            };
            toggleButton.FlatAppearance.BorderSize = 0;
            toggleButton.Click += (s, e) => ToggleClicking();
            Controls.Add(toggleButton);

            statusLabel = new Label
            {
                Location = new Point(20, 262),
                Size = new Size(320, 60),
                ForeColor = Grn,
                Text = "Idle. Point the cursor where you want to click, then press Start."
            };
            Controls.Add(statusLabel);
        }

        private static Label MakeLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                ForeColor = Muted,
                AutoSize = true,
                Location = new Point(x, y + 2)
            };
        }

        private Keys SelectedHotkey
        {
            get { return HotkeyChoices[hotkeyBox.SelectedIndex]; }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RegisterCurrentHotkey();
        }

        private void RegisterCurrentHotkey()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            if (hotkeyRegistered)
            {
                NativeMethods.UnregisterHotKey(Handle, HotkeyId);
                hotkeyRegistered = false;
            }

            var key = SelectedHotkey;
            hotkeyRegistered = NativeMethods.RegisterHotKey(Handle, HotkeyId, NativeMethods.MOD_NONE, (uint)key);
            toggleButton.Text = (engine.IsRunning ? "Stop (" : "Start (") + key + ")";

            if (!hotkeyRegistered)
            {
                statusLabel.ForeColor = Warn;
                statusLabel.Text = key + " could not be registered (in use by another app). Use the button instead.";
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
            {
                ToggleClicking();
                return;
            }

            base.WndProc(ref m);
        }

        private void ToggleClicking()
        {
            engine.TargetCps = (int)cpsInput.Value;
            engine.Button = (MouseButtonKind)buttonBox.SelectedIndex;
            engine.Toggle();

            var key = SelectedHotkey;
            if (engine.IsRunning)
            {
                toggleButton.Text = "Stop (" + key + ")";
                toggleButton.BackColor = Color.FromArgb(214, 69, 69);
                statusLabel.ForeColor = Warn;
                statusLabel.Text = "Clicking at " + engine.TargetCps + " CPS (" + buttonBox.SelectedItem +
                    "). Press " + key + " to stop.";
            }
            else
            {
                toggleButton.Text = "Start (" + key + ")";
                toggleButton.BackColor = Acc;
                statusLabel.ForeColor = Grn;
                statusLabel.Text = "Stopped. Adjust settings and press " + key + " to start again.";
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            engine.Stop();
            if (hotkeyRegistered)
            {
                NativeMethods.UnregisterHotKey(Handle, HotkeyId);
                hotkeyRegistered = false;
            }

            base.OnFormClosing(e);
        }
    }
}

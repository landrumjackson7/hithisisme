using System;
using System.Drawing;
using System.Windows.Forms;
using ExitClone.Core;

namespace ExitClone.UI
{
    /// <summary>
    /// Local profile picker shown on first run. There is no remote account service, the name is
    /// only used to label the session and to keep per-profile settings apart.
    /// </summary>
    public class LoginForm : Form
    {
        private readonly TextBox _name = new TextBox();

        public string AccountName { get; private set; }

        public LoginForm()
        {
            Text = "ExitClone";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(420, 260);
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = Theme.Body;

            var brand = new Label
            {
                Text = "EXITCLONE",
                ForeColor = Theme.Accent,
                Font = new Font("Segoe UI Semibold", 20f),
                AutoSize = true,
                Location = new Point(30, 36)
            };
            var tagline = new Label
            {
                Text = "Multi-path route optimizer for online games",
                ForeColor = Theme.TextDim,
                Font = Theme.Small,
                AutoSize = true,
                Location = new Point(32, 76)
            };

            var caption = Theme.Caption("PROFILE NAME");
            caption.Location = new Point(32, 116);

            _name.SetBounds(32, 136, 356, 26);
            _name.BackColor = Theme.SurfaceAlt;
            _name.ForeColor = Theme.Text;
            _name.BorderStyle = BorderStyle.FixedSingle;
            _name.Text = Environment.UserName;

            var start = Theme.PrimaryButton("Continue");
            start.SetBounds(32, 180, 356, 38);
            start.Click += (s, e) =>
            {
                AccountName = string.IsNullOrWhiteSpace(_name.Text) ? "guest" : _name.Text.Trim();
                DialogResult = DialogResult.OK;
            };

            var exit = Theme.GhostButton("Exit");
            exit.SetBounds(32, 224, 356, 24);
            exit.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] { brand, tagline, caption, _name, start, exit });
            AcceptButton = start;
        }
    }
}

using System.Drawing;
using System.Windows.Forms;

namespace ExitClone.UI
{
    public static class Theme
    {
        public static readonly Color Background = Color.FromArgb(16, 18, 24);
        public static readonly Color Surface = Color.FromArgb(23, 26, 34);
        public static readonly Color SurfaceAlt = Color.FromArgb(30, 34, 45);
        public static readonly Color Border = Color.FromArgb(44, 49, 63);
        public static readonly Color Accent = Color.FromArgb(0, 214, 143);
        public static readonly Color AccentDim = Color.FromArgb(0, 150, 100);
        public static readonly Color Danger = Color.FromArgb(232, 74, 95);
        public static readonly Color Warning = Color.FromArgb(240, 180, 41);
        public static readonly Color Text = Color.FromArgb(235, 238, 245);
        public static readonly Color TextDim = Color.FromArgb(140, 148, 166);

        public static readonly Font H1 = new Font("Segoe UI Semibold", 16f);
        public static readonly Font H2 = new Font("Segoe UI Semibold", 11.5f);
        public static readonly Font Body = new Font("Segoe UI", 9.5f);
        public static readonly Font Small = new Font("Segoe UI", 8.5f);
        public static readonly Font Mono = new Font("Consolas", 9f);
        public static readonly Font Metric = new Font("Segoe UI Semibold", 20f);

        public static Label Caption(string text)
        {
            return new Label
            {
                Text = text,
                ForeColor = TextDim,
                Font = Small,
                AutoSize = true,
                BackColor = Color.Transparent
            };
        }

        public static Button PrimaryButton(string text)
        {
            var button = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = Accent,
                ForeColor = Color.FromArgb(10, 14, 18),
                Font = new Font("Segoe UI Semibold", 10f),
                Height = 36,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        public static Button GhostButton(string text)
        {
            var button = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = SurfaceAlt,
                ForeColor = Text,
                Font = Body,
                Height = 32,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = Border;
            return button;
        }

        public static Panel Card()
        {
            return new Panel { BackColor = Surface, Padding = new Padding(14) };
        }
    }
}

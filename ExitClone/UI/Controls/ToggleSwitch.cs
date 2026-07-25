using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ExitClone.UI.Controls
{
    public class ToggleSwitch : Control
    {
        private bool _checked;

        public event EventHandler CheckedChanged;

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Size = new Size(46, 24);
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var track = new Rectangle(0, 2, Width - 1, Height - 5);

            using (var path = Rounded(track, track.Height))
            using (var brush = new SolidBrush(_checked ? Theme.Accent : Theme.Border))
                g.FillPath(brush, path);

            int diameter = track.Height - 6;
            int x = _checked ? track.Right - diameter - 3 : track.Left + 3;
            using (var knob = new SolidBrush(_checked ? Color.FromArgb(12, 16, 20) : Theme.TextDim))
                g.FillEllipse(knob, x, track.Top + 3, diameter, diameter);
        }

        private static GraphicsPath Rounded(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 90, 180);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 180);
            path.CloseFigure();
            return path;
        }
    }
}

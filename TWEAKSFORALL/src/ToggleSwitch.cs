using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TweaksForAll
{
    // Rounded animated on/off switch in the blue accent colour.
    internal sealed class ToggleSwitch : Control
    {
        private bool _on;
        private float _pos; // 0..1 knob position
        private readonly Timer _anim;

        public event EventHandler Toggled;

        public ToggleSwitch()
        {
            DoubleBuffered = true;
            Size = new Size(46, 24);
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            _anim = new Timer { Interval = 12 };
            _anim.Tick += Animate;
        }

        public bool On
        {
            get => _on;
            set
            {
                if (_on == value) return;
                _on = value;
                _anim.Start();
                Invalidate();
            }
        }

        // Set state without raising the Toggled event (used when building pages).
        public void SetSilent(bool value)
        {
            _on = value;
            _pos = value ? 1f : 0f;
            Invalidate();
        }

        private void Animate(object sender, EventArgs e)
        {
            float target = _on ? 1f : 0f;
            _pos += (target - _pos) * 0.35f;
            if (Math.Abs(target - _pos) < 0.01f) { _pos = target; _anim.Stop(); }
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            _on = !_on;
            _anim.Start();
            Toggled?.Invoke(this, EventArgs.Empty);
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, (Height - 22) / 2, 44, 22);
            Color track = Blend(Theme.Border, Theme.Accent, _pos);
            using (var b = new SolidBrush(track))
            using (var path = Rounded(r, r.Height))
                g.FillPath(b, path);
            if (_pos > 0.02f)
            {
                using (var glow = new SolidBrush(Color.FromArgb((int)(70 * _pos), Theme.AccentBright)))
                using (var path = Rounded(new Rectangle(r.X - 1, r.Y - 1, r.Width + 2, r.Height + 2), r.Height))
                    g.FillPath(glow, path);
            }
            int knob = 18;
            int x = r.X + 2 + (int)((r.Width - knob - 4) * _pos);
            using (var b = new SolidBrush(Color.White))
                g.FillEllipse(b, x, r.Y + 2, knob, knob);
        }

        private static Color Blend(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        private static GraphicsPath Rounded(Rectangle r, int d)
        {
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 90, 180);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 180);
            p.CloseFigure();
            return p;
        }
    }
}

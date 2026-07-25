using System;
using System.Drawing;
using System.Windows.Forms;

namespace ExitClone.UI.Controls
{
    public class NavButton : Control
    {
        private bool _hover;
        private bool _active;

        public NavButton(string text)
        {
            Text = text;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Height = 42;
            Dock = DockStyle.Top;
            Cursor = Cursors.Hand;
            BackColor = Theme.Background;
        }

        public bool Active
        {
            get { return _active; }
            set
            {
                _active = value;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(_active ? Theme.SurfaceAlt : (_hover ? Theme.Surface : BackColor));

            if (_active)
                using (var accent = new SolidBrush(Theme.Accent))
                    g.FillRectangle(accent, 0, 6, 3, Height - 12);

            using (var brush = new SolidBrush(_active ? Theme.Text : Theme.TextDim))
                g.DrawString(Text, _active ? Theme.H2 : Theme.Body, brush, 20, Height / 2f - 9);
        }
    }
}

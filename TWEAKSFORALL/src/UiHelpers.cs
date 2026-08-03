using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TweaksForAll
{
    // A panel with rounded corners and optional border, painted in theme colours.
    internal class Card : Panel
    {
        public int Radius = 12;
        public Color Fill = Theme.Card;
        public Color Stroke = Theme.Border;
        public bool DrawBorder = true;

        public Card()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Round(r, Radius))
            {
                using (var b = new SolidBrush(Fill)) g.FillPath(b, path);
                if (DrawBorder) using (var p = new Pen(Stroke)) g.DrawPath(p, path);
            }
            base.OnPaint(e);
        }

        public static GraphicsPath Round(Rectangle r, int d)
        {
            var p = new GraphicsPath();
            if (d <= 0) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // Filled/outlined pill button with hover feedback in the blue accent.
    internal class PillButton : Button
    {
        public bool Filled = true;
        public int Radius = 9;
        public Color Accent = Theme.Accent;
        private bool _hover;

        public PillButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            BackColor = Color.Transparent;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Card.Round(r, Radius))
            {
                if (Filled)
                {
                    Color top = _hover ? Theme.AccentBright : Accent;
                    Color bot = _hover ? Accent : Theme.AccentDim;
                    using (var b = new LinearGradientBrush(r, top, bot, 90f)) g.FillPath(b, path);
                }
                else
                {
                    using (var b = new SolidBrush(_hover ? Theme.AccentGlow : Theme.Panel)) g.FillPath(b, path);
                    using (var p = new Pen(_hover ? Accent : Theme.Border, 1.4f)) g.DrawPath(p, path);
                }
            }
            var tc = Filled ? Color.White : (_hover ? Theme.AccentBright : Theme.Text);
            TextRenderer.DrawText(g, Text, Font, r, tc,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
}

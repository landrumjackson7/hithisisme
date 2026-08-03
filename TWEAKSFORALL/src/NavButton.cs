using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TweaksForAll
{
    // A sidebar navigation item with a Segoe MDL2 glyph and selected/hover states.
    internal sealed class NavButton : Control
    {
        public string Glyph;
        public string Label;
        private bool _selected;
        private bool _hover;

        private static readonly Font GlyphFont = MakeGlyphFont();
        private static Font MakeGlyphFont()
        {
            try { return new Font("Segoe MDL2 Assets", 12f); } catch { return new Font("Segoe UI", 12f); }
        }

        public NavButton(string glyph, string label)
        {
            Glyph = glyph;
            Label = label;
            Height = 40;
            Dock = DockStyle.Top;
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
            BackColor = Theme.Sidebar;
            Margin = new Padding(0);
        }

        public bool Selected { get => _selected; set { _selected = value; Invalidate(); } }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Sidebar);

            var r = new Rectangle(10, 3, Width - 18, Height - 6);
            if (_selected)
            {
                using (var b = new LinearGradientBrush(r, Theme.AccentGlow, Theme.Panel, 0f))
                using (var path = Card.Round(r, 9))
                    g.FillPath(b, path);
                using (var accent = new SolidBrush(Theme.Accent))
                using (var bar = Card.Round(new Rectangle(2, 8, 4, Height - 16), 2))
                    g.FillPath(accent, bar);
            }
            else if (_hover)
            {
                using (var b = new SolidBrush(Theme.Panel))
                using (var path = Card.Round(r, 9))
                    g.FillPath(b, path);
            }

            Color fg = _selected ? Theme.AccentBright : (_hover ? Theme.Text : Theme.SubText);
            TextRenderer.DrawText(g, Glyph, GlyphFont, new Rectangle(20, 0, 24, Height), fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            TextRenderer.DrawText(g, Label, Theme.NavFont, new Rectangle(50, 0, Width - 54, Height), fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }
    }
}

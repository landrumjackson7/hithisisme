using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LaunchDeck
{
    /// <summary>
    /// A box-art tile. Art is loaded lazily and cached per tile; when a game has no
    /// art assigned, a deterministic colour block with the title initials is drawn so
    /// the grid still reads as a grid.
    /// </summary>
    public class GameTile : Control
    {
        public const int TileWidth = 168;
        public const int TileHeight = 250;

        private static readonly Color Border = Color.FromArgb(58, 62, 76);
        private static readonly Color SelectedBorder = Color.FromArgb(120, 190, 255);
        private static readonly Color Caption = Color.FromArgb(226, 230, 240);
        private static readonly Color Sub = Color.FromArgb(150, 158, 176);

        private Image _art;
        private bool _artLoadFailed;
        private bool _selected;
        private bool _hovered;

        public GameTile(Game game)
        {
            Game = game;
            Size = new Size(TileWidth, TileHeight);
            Margin = new Padding(8);
            BackColor = Color.FromArgb(31, 34, 42);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        public Game Game { get; private set; }

        public bool Selected
        {
            get { return _selected; }
            set
            {
                if (_selected == value)
                    return;
                _selected = value;
                Invalidate();
            }
        }

        /// <summary>Drops the cached art so the next paint reloads it.</summary>
        public void InvalidateArt()
        {
            if (_art != null)
            {
                _art.Dispose();
                _art = null;
            }
            _artLoadFailed = false;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.Clear(BackColor);

            var artBounds = new Rectangle(6, 6, Width - 12, Height - 62);
            DrawArt(g, artBounds);

            using (var titleFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 7.5f))
            using (var titleBrush = new SolidBrush(Caption))
            using (var subBrush = new SolidBrush(Sub))
            using (var format = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Near,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
            {
                g.DrawString(Game.Title, titleFont, titleBrush,
                    new RectangleF(8, Height - 52, Width - 16, 18), format);
                g.DrawString(Game.Platform + "  •  " + Game.DescribePlaytime(), subFont, subBrush,
                    new RectangleF(8, Height - 32, Width - 16, 16), format);
            }

            if (Game.Favorite)
            {
                using (var brush = new SolidBrush(Color.FromArgb(255, 196, 84)))
                using (var font = new Font("Segoe UI", 11f, FontStyle.Bold))
                    g.DrawString("\u2605", font, brush, Width - 26, 8);
            }

            var borderColor = Selected ? SelectedBorder : _hovered ? Color.FromArgb(88, 96, 116) : Border;
            using (var pen = new Pen(borderColor, Selected ? 2f : 1f))
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        private void DrawArt(Graphics g, Rectangle bounds)
        {
            if (_art == null && !_artLoadFailed && !string.IsNullOrEmpty(Game.ArtPath) && File.Exists(Game.ArtPath))
            {
                try
                {
                    // Copy through a stream so the file is not locked while the tile lives.
                    using (var stream = new MemoryStream(File.ReadAllBytes(Game.ArtPath)))
                        _art = Image.FromStream(stream);
                }
                catch (Exception)
                {
                    _artLoadFailed = true;
                }
            }

            if (_art != null)
            {
                g.DrawImage(_art, bounds);
                return;
            }

            using (var brush = new SolidBrush(PlaceholderColor(Game.Title)))
                g.FillRectangle(brush, bounds);

            using (var font = new Font("Segoe UI", 28f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(230, 255, 255, 255)))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(Initials(Game.Title), font, brush, bounds, format);
        }

        /// <summary>Stable pseudo-random hue from the title so a game keeps its colour between runs.</summary>
        private static Color PlaceholderColor(string title)
        {
            var hash = 17;
            foreach (var character in title ?? string.Empty)
                hash = hash * 31 + character;

            var hue = Math.Abs(hash) % 360;
            return FromHsv(hue, 0.45, 0.42);
        }

        private static string Initials(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return "?";

            var words = title.Split(new[] { ' ', ':', '-' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(word => char.IsLetterOrDigit(word[0]))
                .ToArray();
            if (words.Length == 0)
                return "?";
            if (words.Length == 1)
                return words[0].Substring(0, Math.Min(2, words[0].Length)).ToUpperInvariant();
            return (words[0].Substring(0, 1) + words[1].Substring(0, 1)).ToUpperInvariant();
        }

        private static Color FromHsv(double hue, double saturation, double value)
        {
            var sector = (int)(hue / 60) % 6;
            var fraction = hue / 60 - Math.Floor(hue / 60);

            var v = (int)(value * 255);
            var p = (int)(value * (1 - saturation) * 255);
            var q = (int)(value * (1 - fraction * saturation) * 255);
            var t = (int)(value * (1 - (1 - fraction) * saturation) * 255);

            switch (sector)
            {
                case 0: return Color.FromArgb(v, t, p);
                case 1: return Color.FromArgb(q, v, p);
                case 2: return Color.FromArgb(p, v, t);
                case 3: return Color.FromArgb(p, q, v);
                case 4: return Color.FromArgb(t, p, v);
                default: return Color.FromArgb(v, p, q);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _art != null)
            {
                _art.Dispose();
                _art = null;
            }
            base.Dispose(disposing);
        }
    }
}

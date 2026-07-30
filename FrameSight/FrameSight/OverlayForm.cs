using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FrameSight
{
    /// <summary>
    /// The always-on-top readout. Borderless, non-activating and optionally
    /// click-through, so it never steals focus from a fullscreen-windowed game.
    /// </summary>
    public class OverlayForm : Form
    {
        private const int WsExTransparent = 0x00000020;
        private const int WsExLayered = 0x00080000;
        private const int WsExToolWindow = 0x00000080;
        private const int WsExNoActivate = 0x08000000;
        private const int Margin = 12;
        private const int RowHeight = 22;
        private const int BarWidth = 68;

        private readonly OverlaySettings _settings;
        private List<Reading> _readings = new List<Reading>();

        public OverlayForm(OverlaySettings settings)
        {
            _settings = settings;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;
            DoubleBuffered = true;
            Size = new Size(260, 200);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                parameters.ExStyle |= WsExLayered | WsExToolWindow | WsExNoActivate;
                // The base Form constructor reads CreateParams before _settings is assigned.
                if (_settings != null && _settings.ClickThrough)
                    parameters.ExStyle |= WsExTransparent;
                return parameters;
            }
        }

        /// <summary>
        /// A layered window shows nothing until its attributes are set. Black is keyed out,
        /// so painting the background as alpha-over-black gives a translucent-looking panel
        /// with crisp text, and an alpha of 0 leaves only the text floating.
        /// </summary>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SetLayeredWindowAttributes(Handle, 0, 255, LwaColorKey | LwaAlpha);
        }

        /// <summary>Never take focus, even if something tries to activate the overlay.</summary>
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        public void Apply(IList<Reading> readings)
        {
            _readings = new List<Reading>(readings);
            Resize(readings.Count);
            Reposition();
            Invalidate();
        }

        /// <summary>Recreates the window handle so a click-through change takes effect.</summary>
        public void RefreshInteractivity()
        {
            RecreateHandle();
            TopMost = true;
        }

        private void Resize(int rowCount)
        {
            using (var font = CreateFont())
            {
                var rowHeight = (int)Math.Max(RowHeight, font.GetHeight() + 6);
                var width = _settings.ShowBars ? 300 : 210;
                width = (int)(width * (_settings.FontSize / 11f));
                Size = new Size(Math.Max(160, width), rowCount * rowHeight + Margin * 2);
            }
        }

        private void Reposition()
        {
            var screen = Screen.PrimaryScreen.WorkingArea;
            switch (_settings.Corner)
            {
                case OverlayCorner.TopRight:
                    Location = new Point(screen.Right - Width - 16, screen.Top + 16);
                    break;
                case OverlayCorner.BottomLeft:
                    Location = new Point(screen.Left + 16, screen.Bottom - Height - 16);
                    break;
                case OverlayCorner.BottomRight:
                    Location = new Point(screen.Right - Width - 16, screen.Bottom - Height - 16);
                    break;
                case OverlayCorner.Custom:
                    Location = new Point(_settings.CustomX, _settings.CustomY);
                    break;
                default:
                    Location = new Point(screen.Left + 16, screen.Top + 16);
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Color.Black);

            using (var background = new SolidBrush(Color.FromArgb(_settings.BackgroundAlpha, 12, 14, 18)))
                g.FillRectangle(background, ClientRectangle);

            using (var font = CreateFont())
            using (var labelBrush = new SolidBrush(Color.FromArgb(178, 186, 204)))
            using (var valueBrush = new SolidBrush(Color.White))
            using (var accent = new SolidBrush(_settings.AccentColor))
            using (var track = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
            {
                var rowHeight = (int)Math.Max(RowHeight, font.GetHeight() + 6);
                var y = Margin;

                foreach (var reading in _readings)
                {
                    var x = Margin;
                    if (_settings.ShowLabels)
                    {
                        g.DrawString(reading.Label, font, labelBrush, x, y);
                        x += (int)(92 * (_settings.FontSize / 11f));
                    }

                    g.DrawString(reading.Value, font, valueBrush, x, y);

                    if (_settings.ShowBars && reading.HasBar)
                    {
                        var barHeight = Math.Max(4, (int)(font.GetHeight() * 0.35f));
                        var barY = y + (int)(font.GetHeight() / 2) - barHeight / 2;
                        var barX = Width - Margin - BarWidth;
                        g.FillRectangle(track, barX, barY, BarWidth, barHeight);
                        g.FillRectangle(accent, barX, barY, (int)(BarWidth * reading.Fraction), barHeight);
                    }

                    y += rowHeight;
                }
            }
        }

        private Font CreateFont()
        {
            return new Font("Consolas", _settings.FontSize, FontStyle.Bold);
        }

        /// <summary>Lets the user drag the panel when click-through is off.</summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!_settings.ClickThrough && e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WmNcLButtonDown, HtCaption, 0);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            if (_settings.Corner != OverlayCorner.Custom || !Visible)
                return;
            _settings.CustomX = Location.X;
            _settings.CustomY = Location.Y;
        }

        private const int WmNcLButtonDown = 0x00A1;
        private const int HtCaption = 0x2;
        private const int LwaColorKey = 0x1;
        private const int LwaAlpha = 0x2;

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr window, uint colorKey, byte alpha, int flags);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, int message, int wParam, int lParam);
    }
}

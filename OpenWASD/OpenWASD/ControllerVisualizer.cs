using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OpenWASD
{
    /// <summary>Owner-drawn live view of the controller: buttons, sticks and triggers.</summary>
    public class ControllerVisualizer : Control
    {
        private static readonly Color Idle = Color.FromArgb(58, 62, 74);
        private static readonly Color Lit = Color.FromArgb(94, 214, 158);
        private static readonly Color Outline = Color.FromArgb(88, 94, 110);
        private static readonly Color Label = Color.FromArgb(198, 204, 218);

        private ControllerSnapshot _snapshot = new ControllerSnapshot();

        public ControllerVisualizer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Color.FromArgb(30, 32, 40);
            MinimumSize = new Size(320, 220);
        }

        /// <summary>Latest controller state to render.</summary>
        public ControllerSnapshot Snapshot
        {
            get { return _snapshot; }
            set
            {
                _snapshot = value ?? new ControllerSnapshot();
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(BackColor);

            if (!_snapshot.Connected)
            {
                using (var font = new Font(Font.FontFamily, 10, FontStyle.Regular))
                using (var brush = new SolidBrush(Label))
                {
                    var text = "No controller in this slot";
                    var size = g.MeasureString(text, font);
                    g.DrawString(text, font, brush,
                        (Width - size.Width) / 2, (Height - size.Height) / 2);
                }
                return;
            }

            DrawTrigger(g, new RectangleF(14, 12, 60, 14), _snapshot.LeftTrigger, "LT");
            DrawTrigger(g, new RectangleF(Width - 74, 12, 60, 14), _snapshot.RightTrigger, "RT");

            DrawButton(g, new RectangleF(14, 34, 60, 16), _snapshot.IsDown(GamepadButton.LeftShoulder), "LB");
            DrawButton(g, new RectangleF(Width - 74, 34, 60, 16), _snapshot.IsDown(GamepadButton.RightShoulder), "RB");

            var stickRadius = 42f;
            DrawStick(g, new PointF(70, 118), stickRadius, _snapshot.LeftX, _snapshot.LeftY,
                _snapshot.IsDown(GamepadButton.LeftThumb), "L");
            DrawStick(g, new PointF(Width - 70, 118), stickRadius, _snapshot.RightX, _snapshot.RightY,
                _snapshot.IsDown(GamepadButton.RightThumb), "R");

            var centerX = Width / 2f;
            DrawButton(g, new RectangleF(centerX - 46, 62, 40, 18), _snapshot.IsDown(GamepadButton.Back), "Back");
            DrawButton(g, new RectangleF(centerX + 6, 62, 40, 18), _snapshot.IsDown(GamepadButton.Start), "Start");

            DrawDiamond(g, new PointF(centerX, 130));
            DrawDPad(g, new PointF(centerX, 190));
        }

        private void DrawDiamond(Graphics g, PointF center)
        {
            const float spacing = 22f;
            const float size = 20f;
            DrawButton(g, Square(center.X, center.Y - spacing, size), _snapshot.IsDown(GamepadButton.Y), "Y");
            DrawButton(g, Square(center.X, center.Y + spacing, size), _snapshot.IsDown(GamepadButton.A), "A");
            DrawButton(g, Square(center.X - spacing, center.Y, size), _snapshot.IsDown(GamepadButton.X), "X");
            DrawButton(g, Square(center.X + spacing, center.Y, size), _snapshot.IsDown(GamepadButton.B), "B");
        }

        private void DrawDPad(Graphics g, PointF center)
        {
            const float spacing = 18f;
            const float size = 16f;
            DrawButton(g, Square(center.X, center.Y - spacing, size), _snapshot.IsDown(GamepadButton.DPadUp), "\u25B2");
            DrawButton(g, Square(center.X, center.Y + spacing, size), _snapshot.IsDown(GamepadButton.DPadDown), "\u25BC");
            DrawButton(g, Square(center.X - spacing, center.Y, size), _snapshot.IsDown(GamepadButton.DPadLeft), "\u25C0");
            DrawButton(g, Square(center.X + spacing, center.Y, size), _snapshot.IsDown(GamepadButton.DPadRight), "\u25B6");
        }

        private static RectangleF Square(float centerX, float centerY, float size)
        {
            return new RectangleF(centerX - size / 2, centerY - size / 2, size, size);
        }

        private void DrawButton(Graphics g, RectangleF bounds, bool pressed, string caption)
        {
            using (var fill = new SolidBrush(pressed ? Lit : Idle))
            using (var pen = new Pen(Outline))
            {
                g.FillEllipse(fill, bounds);
                g.DrawEllipse(pen, bounds);
            }

            using (var font = new Font(Font.FontFamily, 7.5f, FontStyle.Bold))
            using (var brush = new SolidBrush(pressed ? Color.FromArgb(18, 22, 28) : Label))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(caption, font, brush, bounds, format);
            }
        }

        private void DrawTrigger(Graphics g, RectangleF bounds, double value, string caption)
        {
            using (var back = new SolidBrush(Idle))
            using (var pen = new Pen(Outline))
            {
                g.FillRectangle(back, bounds);
                var filled = new RectangleF(bounds.X, bounds.Y, (float)(bounds.Width * value), bounds.Height);
                using (var fill = new SolidBrush(Lit))
                    g.FillRectangle(fill, filled);
                g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }

            using (var font = new Font(Font.FontFamily, 7f, FontStyle.Bold))
            using (var brush = new SolidBrush(Label))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(caption, font, brush, bounds, format);
            }
        }

        private void DrawStick(Graphics g, PointF center, float radius, double x, double y, bool pressed, string caption)
        {
            var bounds = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
            using (var pen = new Pen(Outline))
            using (var back = new SolidBrush(Color.FromArgb(38, 41, 51)))
            {
                g.FillEllipse(back, bounds);
                g.DrawEllipse(pen, bounds);
            }

            var knobRadius = radius * 0.36f;
            var travel = radius - knobRadius - 2;
            var knobCenter = new PointF(
                center.X + (float)(x * travel),
                center.Y - (float)(y * travel));

            using (var fill = new SolidBrush(pressed ? Lit : Color.FromArgb(120, 128, 148)))
            {
                g.FillEllipse(fill,
                    knobCenter.X - knobRadius, knobCenter.Y - knobRadius, knobRadius * 2, knobRadius * 2);
            }

            using (var font = new Font(Font.FontFamily, 7f, FontStyle.Bold))
            using (var brush = new SolidBrush(Label))
            {
                g.DrawString(caption, font, brush, center.X - radius, center.Y + radius + 2);
            }
        }
    }
}

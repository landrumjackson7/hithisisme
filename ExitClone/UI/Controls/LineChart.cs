using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace ExitClone.UI.Controls
{
    /// <summary>Live latency graph with two series (optimized vs. unaccelerated).</summary>
    public class LineChart : Control
    {
        private readonly List<double> _primary = new List<double>();
        private readonly List<double> _secondary = new List<double>();
        private readonly object _sync = new object();

        public int Capacity { get; set; } = 120;
        public string PrimaryLabel { get; set; } = "Optimized";
        public string SecondaryLabel { get; set; } = "Direct";
        public Color PrimaryColor { get; set; } = Theme.Accent;
        public Color SecondaryColor { get; set; } = Color.FromArgb(120, 130, 150);
        public string Unit { get; set; } = "ms";

        public LineChart()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Surface;
        }

        public void Push(double primary, double secondary)
        {
            lock (_sync)
            {
                _primary.Add(primary);
                _secondary.Add(secondary);
                Trim(_primary);
                Trim(_secondary);
            }
            UiDispatch.Post(this, Invalidate);
        }

        public void Clear()
        {
            lock (_sync)
            {
                _primary.Clear();
                _secondary.Clear();
            }
            Invalidate();
        }

        private void Trim(List<double> series)
        {
            if (series.Count > Capacity) series.RemoveRange(0, series.Count - Capacity);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            List<double> primary, secondary;
            lock (_sync)
            {
                primary = _primary.ToList();
                secondary = _secondary.ToList();
            }

            var plot = new Rectangle(46, 12, Math.Max(10, Width - 60), Math.Max(10, Height - 40));
            double max = Math.Max(20, Math.Max(primary.DefaultIfEmpty(0).Max(), secondary.DefaultIfEmpty(0).Max()) * 1.25);

            using (var grid = new Pen(Theme.Border))
            using (var brush = new SolidBrush(Theme.TextDim))
            {
                for (int i = 0; i <= 4; i++)
                {
                    int y = plot.Bottom - (int)(plot.Height * i / 4.0);
                    g.DrawLine(grid, plot.Left, y, plot.Right, y);
                    var label = ((int)(max * i / 4.0)) + Unit;
                    g.DrawString(label, Theme.Small, brush, 4, y - 8);
                }
            }

            DrawSeries(g, secondary, plot, max, SecondaryColor, false);
            DrawSeries(g, primary, plot, max, PrimaryColor, true);
            DrawLegend(g, plot);
        }

        private void DrawSeries(Graphics g, List<double> values, Rectangle plot, double max, Color color, bool fill)
        {
            if (values.Count < 2) return;
            var points = new PointF[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                float x = plot.Left + plot.Width * i / (float)Math.Max(1, Capacity - 1);
                float y = plot.Bottom - (float)(plot.Height * Math.Min(values[i], max) / max);
                points[i] = new PointF(x, y);
            }

            if (fill)
            {
                var area = points.ToList();
                area.Add(new PointF(points.Last().X, plot.Bottom));
                area.Add(new PointF(points.First().X, plot.Bottom));
                using (var brush = new SolidBrush(Color.FromArgb(40, color)))
                    g.FillPolygon(brush, area.ToArray());
            }

            using (var pen = new Pen(color, 2f))
                g.DrawLines(pen, points);
        }

        private void DrawLegend(Graphics g, Rectangle plot)
        {
            using (var text = new SolidBrush(Theme.TextDim))
            using (var primary = new SolidBrush(PrimaryColor))
            using (var secondary = new SolidBrush(SecondaryColor))
            {
                int y = plot.Bottom + 8;
                g.FillRectangle(primary, plot.Left, y + 4, 10, 3);
                g.DrawString(PrimaryLabel, Theme.Small, text, plot.Left + 14, y);
                int offset = plot.Left + 24 + (int)g.MeasureString(PrimaryLabel, Theme.Small).Width;
                g.FillRectangle(secondary, offset, y + 4, 10, 3);
                g.DrawString(SecondaryLabel, Theme.Small, text, offset + 14, y);
            }
        }
    }
}

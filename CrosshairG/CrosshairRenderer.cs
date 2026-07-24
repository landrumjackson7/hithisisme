using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Brush = System.Windows.Media.Brush;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using PenLineCap = System.Windows.Media.PenLineCap;
using Point = System.Windows.Point;

namespace CrosshairG;

/// <summary>
/// Draws the crosshair described by <see cref="CrosshairSettings"/> onto a
/// <see cref="Canvas"/>, centred at (cx, cy). Shared by the on-screen overlay,
/// the designer live preview and the browse-gallery thumbnails so everything
/// looks identical.
/// </summary>
public static class CrosshairRenderer
{
    public static void Render(Canvas canvas, CrosshairSettings s, double cx, double cy)
    {
        canvas.Children.Clear();
        canvas.Opacity = Math.Clamp(s.Opacity, 0, 1);

        var main = new SolidColorBrush(s.Color);
        main.Freeze();

        double thickness = Math.Max(0.5, s.Thickness);
        double gap = Math.Max(0, s.Gap);
        double size = Math.Max(0, s.Size);

        bool drawArms = s.Style is CrosshairStyle.Cross or CrosshairStyle.CrossDot or CrosshairStyle.TShape;
        bool drawDot = s.Style is CrosshairStyle.Dot or CrosshairStyle.CrossDot;
        bool drawCircle = s.Style is CrosshairStyle.Circle;
        bool drawArrow = s.Style is CrosshairStyle.Arrow;

        if (drawArms)
        {
            // Up, down (down omitted for the T shape), left, right.
            AddArm(canvas, s, cx, cy - gap, cx, cy - gap - size, thickness, main);
            if (s.Style != CrosshairStyle.TShape)
                AddArm(canvas, s, cx, cy + gap, cx, cy + gap + size, thickness, main);
            AddArm(canvas, s, cx - gap, cy, cx - gap - size, cy, thickness, main);
            AddArm(canvas, s, cx + gap, cy, cx + gap + size, cy, thickness, main);
        }

        if (drawArrow)
        {
            // Four chevrons, one per side, each pointing toward the centre.
            double w = Math.Max(2, size * 0.7);
            double h = Math.Max(2, size);
            // top (tip points down)
            AddChevron(canvas, s, thickness, main, new Point(cx, cy - gap), new Point(cx - w, cy - gap - h), new Point(cx + w, cy - gap - h));
            // bottom (tip points up)
            AddChevron(canvas, s, thickness, main, new Point(cx, cy + gap), new Point(cx - w, cy + gap + h), new Point(cx + w, cy + gap + h));
            // left (tip points right)
            AddChevron(canvas, s, thickness, main, new Point(cx - gap, cy), new Point(cx - gap - h, cy - w), new Point(cx - gap - h, cy + w));
            // right (tip points left)
            AddChevron(canvas, s, thickness, main, new Point(cx + gap, cy), new Point(cx + gap + h, cy - w), new Point(cx + gap + h, cy + w));
        }

        if (drawCircle)
        {
            double r = Math.Max(1, s.CircleRadius);
            AddCircle(canvas, s, cx, cy, r, thickness, main);
        }

        if (drawDot)
        {
            double r = Math.Max(0.5, s.DotSize);
            AddDot(canvas, s, cx, cy, r, main);
        }
    }

    // ----- arms (straight or wavy) -----

    private static void AddArm(Canvas c, CrosshairSettings s, double x0, double y0, double x1, double y1, double thickness, Brush main)
    {
        if (s.LineStyle == LineStyle.Wavy)
        {
            var pts = WavyPoints(x0, y0, x1, y1, s.WaveAmplitude, s.WaveFrequency);
            if (s.OutlineEnabled && s.OutlineThickness > 0)
                c.Children.Add(WavyPolyline(pts, OutlineBrush(s), thickness + s.OutlineThickness * 2));
            c.Children.Add(WavyPolyline(pts, main, thickness));
        }
        else
        {
            AddStraight(c, s, x0, y0, x1, y1, thickness, main);
        }
    }

    private static void AddStraight(Canvas c, CrosshairSettings s, double x0, double y0, double x1, double y1, double thickness, Brush main)
    {
        if (s.OutlineEnabled && s.OutlineThickness > 0)
            c.Children.Add(StraightLine(x0, y0, x1, y1, OutlineBrush(s), thickness + s.OutlineThickness * 2));
        c.Children.Add(StraightLine(x0, y0, x1, y1, main, thickness));
    }

    private static Line StraightLine(double x0, double y0, double x1, double y1, Brush stroke, double thickness) => new()
    {
        X1 = x0, Y1 = y0, X2 = x1, Y2 = y1,
        Stroke = stroke,
        StrokeThickness = thickness,
        StrokeStartLineCap = PenLineCap.Square,
        StrokeEndLineCap = PenLineCap.Square,
        SnapsToDevicePixels = true,
    };

    private static PointCollection WavyPoints(double x0, double y0, double x1, double y1, double amplitude, double frequency)
    {
        double dx = x1 - x0, dy = y1 - y0;
        double len = Math.Sqrt(dx * dx + dy * dy);
        var pts = new PointCollection();
        if (len < 0.001) { pts.Add(new Point(x0, y0)); return pts; }
        double ux = dx / len, uy = dy / len;   // direction
        double px = -uy, py = ux;               // perpendicular
        int steps = Math.Max(8, (int)(len / 2));
        for (int i = 0; i <= steps; i++)
        {
            double t = (double)i / steps;
            double along = len * t;
            double wave = amplitude * Math.Sin(2 * Math.PI * frequency * t);
            pts.Add(new Point(x0 + ux * along + px * wave, y0 + uy * along + py * wave));
        }
        return pts;
    }

    private static Polyline WavyPolyline(PointCollection pts, Brush stroke, double thickness) => new()
    {
        Points = pts,
        Stroke = stroke,
        StrokeThickness = thickness,
        StrokeLineJoin = PenLineJoin.Round,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
    };

    // ----- chevron (arrow) -----

    private static void AddChevron(Canvas c, CrosshairSettings s, double thickness, Brush main, Point tip, Point wingA, Point wingB)
    {
        var pts = new PointCollection { wingA, tip, wingB };
        if (s.OutlineEnabled && s.OutlineThickness > 0)
            c.Children.Add(Chevron(pts, OutlineBrush(s), thickness + s.OutlineThickness * 2));
        c.Children.Add(Chevron(pts, main, thickness));
    }

    private static Polyline Chevron(PointCollection pts, Brush stroke, double thickness) => new()
    {
        Points = pts,
        Stroke = stroke,
        StrokeThickness = thickness,
        StrokeLineJoin = PenLineJoin.Round,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
    };

    // ----- circle & dot -----

    private static void AddCircle(Canvas c, CrosshairSettings s, double cx, double cy, double r, double thickness, Brush main)
    {
        if (s.OutlineEnabled && s.OutlineThickness > 0)
            AddEllipse(c, cx, cy, r, OutlineBrush(s), thickness + s.OutlineThickness * 2);
        AddEllipse(c, cx, cy, r, main, thickness);
    }

    private static void AddEllipse(Canvas c, double cx, double cy, double r, Brush stroke, double thickness)
    {
        var e = new Ellipse
        {
            Width = r * 2,
            Height = r * 2,
            Stroke = stroke,
            StrokeThickness = thickness,
        };
        Canvas.SetLeft(e, cx - r);
        Canvas.SetTop(e, cy - r);
        c.Children.Add(e);
    }

    private static void AddDot(Canvas c, CrosshairSettings s, double cx, double cy, double r, Brush main)
    {
        if (s.OutlineEnabled && s.OutlineThickness > 0)
        {
            double or = r + s.OutlineThickness;
            var outer = new Ellipse { Width = or * 2, Height = or * 2, Fill = OutlineBrush(s) };
            Canvas.SetLeft(outer, cx - or);
            Canvas.SetTop(outer, cy - or);
            c.Children.Add(outer);
        }
        var dot = new Ellipse { Width = r * 2, Height = r * 2, Fill = main };
        Canvas.SetLeft(dot, cx - r);
        Canvas.SetTop(dot, cy - r);
        c.Children.Add(dot);
    }

    private static Brush OutlineBrush(CrosshairSettings s)
    {
        var b = new SolidColorBrush(s.OutlineColor);
        b.Freeze();
        return b;
    }
}

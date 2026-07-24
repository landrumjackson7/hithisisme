using System.Windows.Controls;
using System.Windows.Shapes;
using Brush = System.Windows.Media.Brush;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using PenLineCap = System.Windows.Media.PenLineCap;

namespace CrosshairG;

/// <summary>
/// Draws the crosshair described by <see cref="CrosshairSettings"/> onto a
/// <see cref="Canvas"/>, centred at (cx, cy). Shared by the on-screen overlay
/// and the settings-window live preview so both always look identical.
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

        if (drawArms)
        {
            // Up, down (down omitted for the T shape), left, right.
            AddLine(canvas, s, cx, cy - gap, cx, cy - gap - size, thickness, main);
            if (s.Style != CrosshairStyle.TShape)
                AddLine(canvas, s, cx, cy + gap, cx, cy + gap + size, thickness, main);
            AddLine(canvas, s, cx - gap, cy, cx - gap - size, cy, thickness, main);
            AddLine(canvas, s, cx + gap, cy, cx + gap + size, cy, thickness, main);
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

    private static void AddLine(Canvas c, CrosshairSettings s, double x0, double y0, double x1, double y1, double thickness, Brush main)
    {
        if (s.OutlineEnabled && s.OutlineThickness > 0)
        {
            var ob = new SolidColorBrush(s.OutlineColor); ob.Freeze();
            c.Children.Add(new Line
            {
                X1 = x0, Y1 = y0, X2 = x1, Y2 = y1,
                Stroke = ob,
                StrokeThickness = thickness + s.OutlineThickness * 2,
                StrokeStartLineCap = PenLineCap.Square,
                StrokeEndLineCap = PenLineCap.Square,
                SnapsToDevicePixels = true,
            });
        }
        c.Children.Add(new Line
        {
            X1 = x0, Y1 = y0, X2 = x1, Y2 = y1,
            Stroke = main,
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Square,
            StrokeEndLineCap = PenLineCap.Square,
            SnapsToDevicePixels = true,
        });
    }

    private static void AddCircle(Canvas c, CrosshairSettings s, double cx, double cy, double r, double thickness, Brush main)
    {
        if (s.OutlineEnabled && s.OutlineThickness > 0)
        {
            var ob = new SolidColorBrush(s.OutlineColor); ob.Freeze();
            AddEllipse(c, cx, cy, r, ob, thickness + s.OutlineThickness * 2);
        }
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
            var ob = new SolidColorBrush(s.OutlineColor); ob.Freeze();
            double or = r + s.OutlineThickness;
            var outer = new Ellipse { Width = or * 2, Height = or * 2, Fill = ob };
            Canvas.SetLeft(outer, cx - or);
            Canvas.SetTop(outer, cy - or);
            c.Children.Add(outer);
        }
        var dot = new Ellipse { Width = r * 2, Height = r * 2, Fill = main };
        Canvas.SetLeft(dot, cx - r);
        Canvas.SetTop(dot, cy - r);
        c.Children.Add(dot);
    }
}

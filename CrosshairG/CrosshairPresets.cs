namespace CrosshairG;

/// <summary>A named, ready-made crosshair shown in the Browse gallery.</summary>
public sealed class CrosshairPreset
{
    public required string Name { get; init; }
    public required string Category { get; init; }
    public required CrosshairSettings Settings { get; init; }
}

/// <summary>
/// Built-in crosshairs grouped by category, mirroring the Browse tab layout.
/// </summary>
public static class CrosshairPresets
{
    public static readonly string[] Categories =
    {
        "Classic", "Dot", "Cross + Dot", "Circle", "T-Shape", "Arrow",
    };

    public static IReadOnlyList<CrosshairPreset> All { get; } = Build();

    private static CrosshairSettings S(
        CrosshairStyle style,
        double size, double thickness, double gap,
        byte r, byte g, byte b,
        double dot = 3, double circle = 16, bool outline = true,
        LineStyle line = LineStyle.Solid, double opacity = 1.0)
        => new()
        {
            Style = style,
            Size = size,
            Thickness = thickness,
            Gap = gap,
            DotSize = dot,
            CircleRadius = circle,
            OutlineEnabled = outline,
            LineStyle = line,
            Opacity = opacity,
            R = r, G = g, B = b, A = 255,
        };

    private static List<CrosshairPreset> Build()
    {
        var list = new List<CrosshairPreset>();

        void Add(string name, string cat, CrosshairSettings s)
        {
            s.Name = name;
            list.Add(new CrosshairPreset { Name = name, Category = cat, Settings = s });
        }

        // Classic (cross only)
        Add("Default", "Classic", S(CrosshairStyle.Cross, 10, 2, 4, 255, 255, 255));
        Add("Compact", "Classic", S(CrosshairStyle.Cross, 6, 2, 2, 255, 255, 255));
        Add("Long", "Classic", S(CrosshairStyle.Cross, 20, 2, 4, 200, 200, 200));
        Add("Chunky", "Classic", S(CrosshairStyle.Cross, 9, 5, 3, 255, 255, 255));
        Add("Open", "Classic", S(CrosshairStyle.Cross, 12, 2, 10, 220, 220, 220));
        Add("Fine Green", "Classic", S(CrosshairStyle.Cross, 10, 1.5, 4, 0, 220, 0));
        Add("Micro", "Classic", S(CrosshairStyle.Cross, 4, 2, 2, 0, 220, 220));
        Add("Wavy Cross", "Classic", S(CrosshairStyle.Cross, 16, 2, 5, 0, 220, 120, line: LineStyle.Wavy));

        // Dot
        Add("Tiny Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 0, 220, 0, dot: 2));
        Add("Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 0, 220, 0, dot: 3));
        Add("Big Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 255, 255, 255, dot: 5));
        Add("Red Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 230, 20, 20, dot: 3));
        Add("Cyan Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 0, 220, 220, dot: 4));

        // Cross + Dot
        Add("Classic + Dot", "Cross + Dot", S(CrosshairStyle.CrossDot, 10, 2, 5, 0, 220, 0, dot: 2));
        Add("Compact + Dot", "Cross + Dot", S(CrosshairStyle.CrossDot, 6, 2, 3, 255, 255, 255, dot: 2));
        Add("Pro", "Cross + Dot", S(CrosshairStyle.CrossDot, 8, 2, 4, 0, 220, 220, dot: 2));
        Add("Bold + Dot", "Cross + Dot", S(CrosshairStyle.CrossDot, 10, 4, 4, 255, 255, 255, dot: 3));
        Add("Precision", "Cross + Dot", S(CrosshairStyle.CrossDot, 14, 1.5, 6, 0, 220, 0, dot: 1.5));

        // Circle
        Add("Ring", "Circle", S(CrosshairStyle.Circle, 0, 2, 0, 255, 255, 255, circle: 14));
        Add("Big Ring", "Circle", S(CrosshairStyle.Circle, 0, 2, 0, 0, 220, 0, circle: 22));
        Add("Thin Ring", "Circle", S(CrosshairStyle.Circle, 0, 1, 0, 0, 220, 220, circle: 16));
        Add("Bold Ring", "Circle", S(CrosshairStyle.Circle, 0, 4, 0, 230, 20, 20, circle: 18));

        // T-Shape
        Add("T", "T-Shape", S(CrosshairStyle.TShape, 10, 2, 4, 255, 255, 255));
        Add("T Compact", "T-Shape", S(CrosshairStyle.TShape, 6, 2, 2, 0, 220, 0));
        Add("T Long", "T-Shape", S(CrosshairStyle.TShape, 18, 2, 4, 0, 220, 220));
        Add("T Bold", "T-Shape", S(CrosshairStyle.TShape, 10, 4, 3, 255, 255, 255));

        // Arrow
        Add("Arrow", "Arrow", S(CrosshairStyle.Arrow, 8, 2, 4, 0, 220, 0));
        Add("Arrow Wide", "Arrow", S(CrosshairStyle.Arrow, 12, 2, 6, 255, 255, 255));
        Add("Arrow Bold", "Arrow", S(CrosshairStyle.Arrow, 9, 4, 4, 0, 220, 220));
        Add("Arrow Red", "Arrow", S(CrosshairStyle.Arrow, 10, 2, 5, 230, 20, 20));

        return list;
    }
}

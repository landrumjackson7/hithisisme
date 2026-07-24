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
        Add("Hairline", "Classic", S(CrosshairStyle.Cross, 12, 1, 3, 255, 255, 255));
        Add("Wide Open", "Classic", S(CrosshairStyle.Cross, 8, 2, 16, 0, 220, 0));
        Add("Closed", "Classic", S(CrosshairStyle.Cross, 12, 2, 0, 255, 255, 255));
        Add("Heavy", "Classic", S(CrosshairStyle.Cross, 12, 7, 4, 255, 255, 255));
        Add("Cyan Fine", "Classic", S(CrosshairStyle.Cross, 10, 1.5, 4, 0, 220, 220));
        Add("Red Cross", "Classic", S(CrosshairStyle.Cross, 10, 2, 4, 230, 20, 20));
        Add("Yellow Cross", "Classic", S(CrosshairStyle.Cross, 10, 2, 4, 240, 220, 0));
        Add("Magenta Cross", "Classic", S(CrosshairStyle.Cross, 10, 2, 4, 230, 0, 230));
        Add("Sniper", "Classic", S(CrosshairStyle.Cross, 28, 1, 6, 0, 220, 0));
        Add("Ghost", "Classic", S(CrosshairStyle.Cross, 12, 2, 4, 255, 255, 255, opacity: 0.5));
        Add("Wavy White", "Classic", S(CrosshairStyle.Cross, 14, 2, 4, 255, 255, 255, line: LineStyle.Wavy));
        Add("Wavy Red", "Classic", S(CrosshairStyle.Cross, 16, 2, 5, 230, 20, 20, line: LineStyle.Wavy));

        // Dot
        Add("Tiny Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 0, 220, 0, dot: 2));
        Add("Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 0, 220, 0, dot: 3));
        Add("Big Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 255, 255, 255, dot: 5));
        Add("Red Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 230, 20, 20, dot: 3));
        Add("Cyan Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 0, 220, 220, dot: 4));
        Add("White Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 255, 255, 255, dot: 3));
        Add("Magenta Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 230, 0, 230, dot: 3));
        Add("Yellow Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 240, 220, 0, dot: 3));
        Add("Huge Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 0, 220, 0, dot: 7));
        Add("Pin Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 255, 255, 255, dot: 1.5));
        Add("Faint Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 0, 220, 0, dot: 3, opacity: 0.5));
        Add("No-Outline Dot", "Dot", S(CrosshairStyle.Dot, 0, 2, 0, 0, 220, 0, dot: 3, outline: false));

        // Cross + Dot
        Add("Classic + Dot", "Cross + Dot", S(CrosshairStyle.CrossDot, 10, 2, 5, 0, 220, 0, dot: 2));
        Add("Compact + Dot", "Cross + Dot", S(CrosshairStyle.CrossDot, 6, 2, 3, 255, 255, 255, dot: 2));
        Add("Pro", "Cross + Dot", S(CrosshairStyle.CrossDot, 8, 2, 4, 0, 220, 220, dot: 2));
        Add("Bold + Dot", "Cross + Dot", S(CrosshairStyle.CrossDot, 10, 4, 4, 255, 255, 255, dot: 3));
        Add("Precision", "Cross + Dot", S(CrosshairStyle.CrossDot, 14, 1.5, 6, 0, 220, 0, dot: 1.5));
        Add("Cyan + Dot", "Cross + Dot", S(CrosshairStyle.CrossDot, 10, 2, 5, 0, 220, 220, dot: 2));
        Add("Red + Dot", "Cross + Dot", S(CrosshairStyle.CrossDot, 10, 2, 5, 230, 20, 20, dot: 2));
        Add("Yellow + Dot", "Cross + Dot", S(CrosshairStyle.CrossDot, 10, 2, 5, 240, 220, 0, dot: 2));
        Add("Long + Dot", "Cross + Dot", S(CrosshairStyle.CrossDot, 18, 2, 6, 255, 255, 255, dot: 2));
        Add("Micro + Dot", "Cross + Dot", S(CrosshairStyle.CrossDot, 5, 2, 2, 0, 220, 0, dot: 1.5));
        Add("Wavy + Dot", "Cross + Dot", S(CrosshairStyle.CrossDot, 14, 2, 5, 0, 220, 120, dot: 2, line: LineStyle.Wavy));
        Add("Big Dot Cross", "Cross + Dot", S(CrosshairStyle.CrossDot, 10, 2, 6, 255, 255, 255, dot: 4));

        // Circle
        Add("Ring", "Circle", S(CrosshairStyle.Circle, 0, 2, 0, 255, 255, 255, circle: 14));
        Add("Big Ring", "Circle", S(CrosshairStyle.Circle, 0, 2, 0, 0, 220, 0, circle: 22));
        Add("Thin Ring", "Circle", S(CrosshairStyle.Circle, 0, 1, 0, 0, 220, 220, circle: 16));
        Add("Bold Ring", "Circle", S(CrosshairStyle.Circle, 0, 4, 0, 230, 20, 20, circle: 18));
        Add("Green Ring", "Circle", S(CrosshairStyle.Circle, 0, 2, 0, 0, 220, 0, circle: 16));
        Add("Yellow Ring", "Circle", S(CrosshairStyle.Circle, 0, 2, 0, 240, 220, 0, circle: 16));
        Add("Magenta Ring", "Circle", S(CrosshairStyle.Circle, 0, 2, 0, 230, 0, 230, circle: 16));
        Add("Med Ring", "Circle", S(CrosshairStyle.Circle, 0, 2, 0, 0, 220, 0, circle: 16));
        Add("Small Ring", "Circle", S(CrosshairStyle.Circle, 0, 2, 0, 255, 255, 255, circle: 8));
        Add("Huge Ring", "Circle", S(CrosshairStyle.Circle, 0, 2, 0, 0, 220, 220, circle: 30));
        Add("Faint Ring", "Circle", S(CrosshairStyle.Circle, 0, 2, 0, 255, 255, 255, circle: 16, opacity: 0.5));

        // T-Shape
        Add("T", "T-Shape", S(CrosshairStyle.TShape, 10, 2, 4, 255, 255, 255));
        Add("T Compact", "T-Shape", S(CrosshairStyle.TShape, 6, 2, 2, 0, 220, 0));
        Add("T Long", "T-Shape", S(CrosshairStyle.TShape, 18, 2, 4, 0, 220, 220));
        Add("T Bold", "T-Shape", S(CrosshairStyle.TShape, 10, 4, 3, 255, 255, 255));
        Add("T Red", "T-Shape", S(CrosshairStyle.TShape, 10, 2, 4, 230, 20, 20));
        Add("T Yellow", "T-Shape", S(CrosshairStyle.TShape, 10, 2, 4, 240, 220, 0));
        Add("T Fine", "T-Shape", S(CrosshairStyle.TShape, 12, 1, 4, 255, 255, 255));
        Add("T Open", "T-Shape", S(CrosshairStyle.TShape, 10, 2, 12, 0, 220, 0));
        Add("T Wavy", "T-Shape", S(CrosshairStyle.TShape, 14, 2, 4, 0, 220, 120, line: LineStyle.Wavy));

        // Arrow
        Add("Arrow", "Arrow", S(CrosshairStyle.Arrow, 8, 2, 4, 0, 220, 0));
        Add("Arrow Wide", "Arrow", S(CrosshairStyle.Arrow, 12, 2, 6, 255, 255, 255));
        Add("Arrow Bold", "Arrow", S(CrosshairStyle.Arrow, 9, 4, 4, 0, 220, 220));
        Add("Arrow Red", "Arrow", S(CrosshairStyle.Arrow, 10, 2, 5, 230, 20, 20));
        Add("Arrow Yellow", "Arrow", S(CrosshairStyle.Arrow, 10, 2, 5, 240, 220, 0));
        Add("Arrow Magenta", "Arrow", S(CrosshairStyle.Arrow, 10, 2, 5, 230, 0, 230));
        Add("Arrow Tight", "Arrow", S(CrosshairStyle.Arrow, 7, 2, 2, 0, 220, 0));
        Add("Arrow Fine", "Arrow", S(CrosshairStyle.Arrow, 10, 1, 5, 255, 255, 255));
        Add("Arrow Sharp", "Arrow", S(CrosshairStyle.Arrow, 9, 2, 3, 0, 220, 0));

        return list;
    }
}

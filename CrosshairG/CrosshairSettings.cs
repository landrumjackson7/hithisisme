using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Color = System.Windows.Media.Color;

namespace CrosshairG;

public enum CrosshairStyle
{
    Cross,
    Dot,
    CrossDot,
    Circle,
    TShape,
    Arrow,
}

public enum LineStyle
{
    Solid,
    Wavy,
}

/// <summary>
/// All user-configurable crosshair options. Persisted as JSON to
/// %APPDATA%\CrosshairG\settings.json and raises change notifications so the
/// overlay and the settings preview update live.
/// </summary>
public sealed class CrosshairSettings : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        // Colour components also affect the derived brushes used for rendering.
        if (name is "R" or "G" or "B" or "A")
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
        if (name is "OR" or "OG" or "OB" or "OA")
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OutlineColor)));
    }

    private string _name = "Custom";
    public string Name { get => _name; set => Set(ref _name, value); }

    private CrosshairStyle _style = CrosshairStyle.CrossDot;
    public CrosshairStyle Style { get => _style; set => Set(ref _style, value); }

    private LineStyle _lineStyle = LineStyle.Solid;
    public LineStyle LineStyle { get => _lineStyle; set => Set(ref _lineStyle, value); }

    private double _waveAmplitude = 3;   // wavy: peak displacement
    public double WaveAmplitude { get => _waveAmplitude; set => Set(ref _waveAmplitude, value); }

    private double _waveFrequency = 3;    // wavy: number of humps along an arm
    public double WaveFrequency { get => _waveFrequency; set => Set(ref _waveFrequency, value); }

    private double _size = 12;      // length of each arm (from the gap outward)
    public double Size { get => _size; set => Set(ref _size, value); }

    private double _thickness = 3;  // arm thickness
    public double Thickness { get => _thickness; set => Set(ref _thickness, value); }

    private double _gap = 5;        // gap between centre and start of each arm
    public double Gap { get => _gap; set => Set(ref _gap, value); }

    private double _dotSize = 3;    // radius of the centre dot
    public double DotSize { get => _dotSize; set => Set(ref _dotSize, value); }

    private double _circleRadius = 16;
    public double CircleRadius { get => _circleRadius; set => Set(ref _circleRadius, value); }

    private double _opacity = 1.0;  // 0..1
    public double Opacity { get => _opacity; set => Set(ref _opacity, value); }

    private double _offsetX = 0;
    public double OffsetX { get => _offsetX; set => Set(ref _offsetX, value); }

    private double _offsetY = 0;
    public double OffsetY { get => _offsetY; set => Set(ref _offsetY, value); }

    private bool _outlineEnabled = true;
    public bool OutlineEnabled { get => _outlineEnabled; set => Set(ref _outlineEnabled, value); }

    private double _outlineThickness = 1;
    public double OutlineThickness { get => _outlineThickness; set => Set(ref _outlineThickness, value); }

    // Main colour (default: bright green, fully opaque).
    private byte _r = 0;   public byte R { get => _r; set => Set(ref _r, value); }
    private byte _g = 220; public byte G { get => _g; set => Set(ref _g, value); }
    private byte _b = 0;   public byte B { get => _b; set => Set(ref _b, value); }
    private byte _a = 255; public byte A { get => _a; set => Set(ref _a, value); }

    // Outline colour (default: black, fully opaque).
    private byte _or = 0;   public byte OR { get => _or; set => Set(ref _or, value); }
    private byte _og = 0;   public byte OG { get => _og; set => Set(ref _og, value); }
    private byte _ob = 0;   public byte OB { get => _ob; set => Set(ref _ob, value); }
    private byte _oa = 255; public byte OA { get => _oa; set => Set(ref _oa, value); }

    private bool _overlayEnabled = true;
    public bool OverlayEnabled { get => _overlayEnabled; set => Set(ref _overlayEnabled, value); }

    [JsonIgnore] public Color Color => Color.FromArgb(A, R, G, B);
    [JsonIgnore] public Color OutlineColor => Color.FromArgb(OA, OR, OG, OB);

    // ----- persistence -----

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [JsonIgnore]
    public static string SettingsPath
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CrosshairG");
            return Path.Combine(dir, "settings.json");
        }
    }

    public static CrosshairSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                var loaded = JsonSerializer.Deserialize<CrosshairSettings>(json, JsonOptions);
                if (loaded != null) return loaded;
            }
        }
        catch
        {
            // Fall back to defaults on any read/parse error.
        }
        return new CrosshairSettings();
    }

    public void Save()
    {
        try
        {
            string path = SettingsPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // Best-effort; ignore write failures (e.g. locked file).
        }
    }

    public CrosshairSettings Clone()
    {
        var c = new CrosshairSettings();
        c.CopyFrom(this);
        c.Name = Name;
        return c;
    }

    public void CopyFrom(CrosshairSettings other)
    {
        Name = other.Name;
        Style = other.Style;
        LineStyle = other.LineStyle;
        WaveAmplitude = other.WaveAmplitude;
        WaveFrequency = other.WaveFrequency;
        Size = other.Size;
        Thickness = other.Thickness;
        Gap = other.Gap;
        DotSize = other.DotSize;
        CircleRadius = other.CircleRadius;
        Opacity = other.Opacity;
        OffsetX = other.OffsetX;
        OffsetY = other.OffsetY;
        OutlineEnabled = other.OutlineEnabled;
        OutlineThickness = other.OutlineThickness;
        R = other.R; G = other.G; B = other.B; A = other.A;
        OR = other.OR; OG = other.OG; OB = other.OB; OA = other.OA;
        OverlayEnabled = other.OverlayEnabled;
    }
}

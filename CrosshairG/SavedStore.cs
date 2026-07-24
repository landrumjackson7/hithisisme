using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrosshairG;

/// <summary>
/// Loads/saves the user's saved crosshairs to
/// %APPDATA%\CrosshairG\saved.json.
/// </summary>
public static class SavedStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string SavedPath
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CrosshairG");
            return Path.Combine(dir, "saved.json");
        }
    }

    public static List<CrosshairSettings> Load()
    {
        try
        {
            if (File.Exists(SavedPath))
            {
                var list = JsonSerializer.Deserialize<List<CrosshairSettings>>(
                    File.ReadAllText(SavedPath), JsonOptions);
                if (list != null) return list;
            }
        }
        catch
        {
            // ignore corrupt file
        }
        return new List<CrosshairSettings>();
    }

    public static void Save(IEnumerable<CrosshairSettings> items)
    {
        try
        {
            string path = SavedPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(items, JsonOptions));
        }
        catch
        {
            // best effort
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace LaunchDeck
{
    /// <summary>Finds installed games for one storefront.</summary>
    public interface IGameScanner
    {
        GamePlatform Platform { get; }
        string Name { get; }
        IEnumerable<Game> Scan();
    }

    /// <summary>
    /// Minimal reader for Valve's KeyValues text format (.vdf / .acf).
    /// Only flat key/value pairs are needed, so nesting is ignored rather than modelled.
    /// </summary>
    internal static class KeyValues
    {
        private static readonly Regex Pair = new Regex("\"(?<key>[^\"]+)\"\\s+\"(?<value>[^\"]*)\"", RegexOptions.Compiled);

        public static Dictionary<string, string> ReadFlat(string path)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadLines(path))
            {
                var match = Pair.Match(line);
                if (match.Success)
                    values[match.Groups["key"].Value] = match.Groups["value"].Value;
            }
            return values;
        }

        /// <summary>Returns every value whose key matches the predicate, in file order.</summary>
        public static List<string> ReadValues(string path, Func<string, bool> keyPredicate)
        {
            var results = new List<string>();
            foreach (var line in File.ReadLines(path))
            {
                var match = Pair.Match(line);
                if (match.Success && keyPredicate(match.Groups["key"].Value))
                    results.Add(match.Groups["value"].Value);
            }
            return results;
        }
    }

    public class SteamScanner : IGameScanner
    {
        public GamePlatform Platform { get { return GamePlatform.Steam; } }
        public string Name { get { return "Steam"; } }

        public IEnumerable<Game> Scan()
        {
            var root = FindSteamRoot();
            if (root == null)
                return Enumerable.Empty<Game>();

            var games = new List<Game>();
            foreach (var library in LibraryFolders(root))
            {
                var apps = Path.Combine(library, "steamapps");
                if (!Directory.Exists(apps))
                    continue;

                foreach (var manifest in Directory.GetFiles(apps, "appmanifest_*.acf"))
                {
                    Game game;
                    if (TryReadManifest(manifest, apps, out game))
                        games.Add(game);
                }
            }

            return games;
        }

        private static bool TryReadManifest(string manifestPath, string appsDirectory, out Game game)
        {
            game = null;
            try
            {
                var values = KeyValues.ReadFlat(manifestPath);
                string appId, name;
                if (!values.TryGetValue("appid", out appId) || !values.TryGetValue("name", out name))
                    return false;

                // Steamworks redistributables and tools install as apps but are not games.
                if (name.IndexOf("Steamworks", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Proton", StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;

                string installDir;
                values.TryGetValue("installdir", out installDir);

                game = new Game
                {
                    Title = name,
                    Platform = GamePlatform.Steam,
                    LaunchTarget = "steam://rungameid/" + appId,
                    InstallDirectory = installDir == null
                        ? null
                        : Path.Combine(appsDirectory, "common", installDir)
                };
                game.TrackProcessName = GuessProcessName(game.InstallDirectory);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string FindSteamRoot()
        {
            foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view)
                    .OpenSubKey(@"SOFTWARE\Valve\Steam"))
                {
                    var path = key == null ? null : key.GetValue("InstallPath") as string;
                    if (path != null && Directory.Exists(path))
                        return path;
                }
            }

            using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam"))
            {
                var path = key == null ? null : key.GetValue("SteamPath") as string;
                return path != null && Directory.Exists(path) ? path : null;
            }
        }

        /// <summary>Steam spreads games across drives; libraryfolders.vdf lists them.</summary>
        private static IEnumerable<string> LibraryFolders(string steamRoot)
        {
            var folders = new List<string> { steamRoot };
            var vdf = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf))
                return folders;

            try
            {
                folders.AddRange(KeyValues.ReadValues(vdf, key =>
                        string.Equals(key, "path", StringComparison.OrdinalIgnoreCase) || IsNumeric(key))
                    .Where(Directory.Exists));
            }
            catch (Exception)
            {
                // Unreadable file: fall back to the default library only.
            }

            return folders.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsNumeric(string text)
        {
            int ignored;
            return int.TryParse(text, out ignored);
        }

        internal static string GuessProcessName(string installDirectory)
        {
            if (string.IsNullOrEmpty(installDirectory) || !Directory.Exists(installDirectory))
                return null;

            try
            {
                var candidate = new DirectoryInfo(installDirectory)
                    .GetFiles("*.exe", SearchOption.TopDirectoryOnly)
                    .Where(f => !LooksLikeHelper(f.Name))
                    .OrderByDescending(f => f.Length)
                    .FirstOrDefault();

                return candidate == null ? null : Path.GetFileNameWithoutExtension(candidate.Name);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool LooksLikeHelper(string fileName)
        {
            var lower = fileName.ToLowerInvariant();
            return lower.Contains("unins") || lower.Contains("crash") || lower.Contains("setup")
                || lower.Contains("redist") || lower.Contains("vcredist") || lower.Contains("launcher_helper")
                || lower.Contains("dxsetup") || lower.Contains("touchup");
        }
    }

    public class EpicScanner : IGameScanner
    {
        public GamePlatform Platform { get { return GamePlatform.Epic; } }
        public string Name { get { return "Epic Games"; } }

        public IEnumerable<Game> Scan()
        {
            var manifests = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Epic", "EpicGamesLauncher", "Data", "Manifests");

            if (!Directory.Exists(manifests))
                return Enumerable.Empty<Game>();

            var games = new List<Game>();
            foreach (var file in Directory.GetFiles(manifests, "*.item"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var name = JsonPeek.String(json, "DisplayName");
                    var location = JsonPeek.String(json, "InstallLocation");
                    var executable = JsonPeek.String(json, "LaunchExecutable");
                    var appName = JsonPeek.String(json, "AppName");
                    var namespaceId = JsonPeek.String(json, "CatalogNamespace");
                    var itemId = JsonPeek.String(json, "CatalogItemId");

                    if (name == null || location == null)
                        continue;

                    var game = new Game
                    {
                        Title = name,
                        Platform = GamePlatform.Epic,
                        InstallDirectory = location
                    };

                    // The launcher URI keeps overlay and cloud saves working; the exe is only a fallback.
                    game.LaunchTarget = appName != null && namespaceId != null && itemId != null
                        ? string.Format("com.epicgames.launcher://apps/{0}%3A{1}%3A{2}?action=launch&silent=true",
                            namespaceId, itemId, appName)
                        : Path.Combine(location, executable ?? string.Empty);

                    game.TrackProcessName = executable != null
                        ? Path.GetFileNameWithoutExtension(executable)
                        : SteamScanner.GuessProcessName(location);

                    games.Add(game);
                }
                catch (Exception)
                {
                    // Skip malformed manifests.
                }
            }

            return games;
        }
    }

    public class GogScanner : IGameScanner
    {
        public GamePlatform Platform { get { return GamePlatform.Gog; } }
        public string Name { get { return "GOG"; } }

        public IEnumerable<Game> Scan()
        {
            var games = new List<Game>();
            foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                using (var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view)
                    .OpenSubKey(@"SOFTWARE\GOG.com\Games"))
                {
                    if (root == null)
                        continue;

                    foreach (var subKeyName in root.GetSubKeyNames())
                    {
                        using (var key = root.OpenSubKey(subKeyName))
                        {
                            if (key == null)
                                continue;

                            var name = key.GetValue("gameName") as string;
                            var path = key.GetValue("path") as string;
                            var exe = key.GetValue("exe") as string;
                            if (name == null || path == null)
                                continue;

                            var target = exe ?? key.GetValue("launchCommand") as string;
                            games.Add(new Game
                            {
                                Title = name,
                                Platform = GamePlatform.Gog,
                                InstallDirectory = path,
                                LaunchTarget = target ?? path,
                                TrackProcessName = target != null
                                    ? Path.GetFileNameWithoutExtension(target)
                                    : SteamScanner.GuessProcessName(path)
                            });
                        }
                    }
                }
            }

            return games
                .GroupBy(g => g.LaunchTarget, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First());
        }
    }

    /// <summary>
    /// Pulls single string properties out of small JSON documents. Epic manifests are
    /// flat and shallow, so a targeted regex avoids dragging in a JSON dependency.
    /// </summary>
    internal static class JsonPeek
    {
        public static string String(string json, string property)
        {
            var match = Regex.Match(json,
                "\"" + Regex.Escape(property) + "\"\\s*:\\s*\"(?<value>(\\\\.|[^\"\\\\])*)\"");
            if (!match.Success)
                return null;

            return match.Groups["value"].Value
                .Replace("\\\\", "\\")
                .Replace("\\\"", "\"")
                .Replace("\\/", "/");
        }
    }
}

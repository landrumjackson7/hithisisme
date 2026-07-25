using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ExitClone.Core
{
    public enum RoutingMode
    {
        /// <summary>Single best route.</summary>
        Optimized,
        /// <summary>Best route plus redundant copies of every packet on backup routes.</summary>
        MultiPath,
        /// <summary>No relay, only measurement.</summary>
        Monitor
    }

    public class AppSettings
    {
        public string AccountName { get; set; } = "";
        public bool StartWithWindows { get; set; }
        public bool StartMinimized { get; set; }
        public bool MinimizeToTray { get; set; } = true;
        public bool AutoConnectOnGameLaunch { get; set; } = true;
        public bool DetectGames { get; set; } = true;

        public RoutingMode Mode { get; set; } = RoutingMode.Optimized;
        public int DuplicateRoutes { get; set; } = 2;
        public bool PacketDuplication { get; set; } = true;
        public bool AntiDdos { get; set; } = true;
        public bool DisableNagle { get; set; } = true;
        public int Mtu { get; set; } = 1400;
        public int LocalProxyPort { get; set; } = 1080;
        public bool AddSystemRoutes { get; set; }

        public int ProbeCount { get; set; } = 8;
        public int ProbeIntervalMs { get; set; } = 120;
        public int LiveIntervalMs { get; set; } = 1000;
        public int MaxHops { get; set; } = 2;

        public string LastGameId { get; set; }
        public string LastServerName { get; set; }
        public List<string> PreferredRelayIds { get; set; } = new List<string>();
        public Dictionary<string, GameProfile> Profiles { get; set; } = new Dictionary<string, GameProfile>();

        public GameProfile ProfileFor(string gameId)
        {
            if (string.IsNullOrEmpty(gameId)) return new GameProfile();
            GameProfile profile;
            if (!Profiles.TryGetValue(gameId, out profile))
            {
                profile = new GameProfile();
                Profiles[gameId] = profile;
            }
            return profile;
        }
    }

    public class GameProfile
    {
        public string ServerName { get; set; }
        public string PinnedRouteKey { get; set; }
        public RoutingMode? Mode { get; set; }
        public bool? PacketDuplication { get; set; }
        public int? DuplicateRoutes { get; set; }
    }

    public static class SettingsStore
    {
        public static string DataDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ExitClone");

        public static string SettingsPath => Path.Combine(DataDirectory, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var loaded = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(SettingsPath));
                    if (loaded != null) return loaded;
                }
            }
            catch (Exception)
            {
                // Reset to defaults when the settings file cannot be parsed.
            }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(DataDirectory);
                File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
            catch (Exception)
            {
                // Saving settings is best effort; a read-only profile must not crash the app.
            }
        }
    }
}

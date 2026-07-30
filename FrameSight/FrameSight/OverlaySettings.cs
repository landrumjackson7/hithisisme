using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;

namespace FrameSight
{
    public enum OverlayCorner
    {
        TopLeft = 0,
        TopRight,
        BottomLeft,
        BottomRight,
        Custom
    }

    /// <summary>Everything the user can configure, persisted as JSON.</summary>
    [DataContract]
    public class OverlaySettings
    {
        [DataMember(Order = 0)]
        public List<SensorKind> Sensors { get; set; }

        [DataMember(Order = 1)]
        public OverlayCorner Corner { get; set; }

        [DataMember(Order = 2)]
        public int CustomX { get; set; }

        [DataMember(Order = 3)]
        public int CustomY { get; set; }

        /// <summary>Background opacity, 0..255. 0 makes the panel invisible but keeps the text.</summary>
        [DataMember(Order = 4)]
        public int BackgroundAlpha { get; set; }

        [DataMember(Order = 5)]
        public float FontSize { get; set; }

        [DataMember(Order = 6)]
        public int UpdateIntervalMs { get; set; }

        /// <summary>When true the overlay ignores the mouse entirely.</summary>
        [DataMember(Order = 7)]
        public bool ClickThrough { get; set; }

        [DataMember(Order = 8)]
        public bool ShowBars { get; set; }

        [DataMember(Order = 9)]
        public bool ShowLabels { get; set; }

        [DataMember(Order = 10)]
        public string AccentHtmlColor { get; set; }

        [DataMember(Order = 11)]
        public bool LoggingEnabled { get; set; }

        [DataMember(Order = 12)]
        public int LogIntervalMs { get; set; }

        [DataMember(Order = 13)]
        public string LogDirectory { get; set; }

        /// <summary>Virtual-key code for the show/hide hotkey.</summary>
        [DataMember(Order = 14)]
        public int HotkeyVirtualKey { get; set; }

        [DataMember(Order = 15)]
        public bool OverlayVisible { get; set; }

        public OverlaySettings()
        {
            Sensors = new List<SensorKind>
            {
                SensorKind.CpuLoad, SensorKind.CpuClock, SensorKind.RamUsedGb,
                SensorKind.GpuLoad, SensorKind.DiskActivity, SensorKind.Clock
            };
            Corner = OverlayCorner.TopLeft;
            BackgroundAlpha = 150;
            FontSize = 11f;
            UpdateIntervalMs = 1000;
            ClickThrough = true;
            ShowBars = true;
            ShowLabels = true;
            AccentHtmlColor = "#5ED69E";
            LogIntervalMs = 1000;
            LogDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FrameSight");
            HotkeyVirtualKey = (int)Keys.F10;
            OverlayVisible = true;
        }

        public Color AccentColor
        {
            get
            {
                try
                {
                    return ColorTranslator.FromHtml(AccentHtmlColor);
                }
                catch (Exception)
                {
                    return Color.FromArgb(94, 214, 158);
                }
            }
        }

        private static string FilePath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FrameSight", "settings.json");
            }
        }

        public static OverlaySettings Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new OverlaySettings();

                using (var stream = File.OpenRead(FilePath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(OverlaySettings));
                    var settings = (OverlaySettings)serializer.ReadObject(stream);
                    if (settings == null)
                        return new OverlaySettings();
                    settings.Normalize();
                    return settings;
                }
            }
            catch (Exception)
            {
                return new OverlaySettings();
            }
        }

        public void Save()
        {
            Normalize();
            var directory = Path.GetDirectoryName(FilePath);
            if (directory != null)
                Directory.CreateDirectory(directory);

            using (var stream = File.Create(FilePath))
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true))
            {
                var serializer = new DataContractJsonSerializer(typeof(OverlaySettings));
                serializer.WriteObject(writer, this);
            }
        }

        private void Normalize()
        {
            if (Sensors == null || Sensors.Count == 0)
                Sensors = new List<SensorKind> { SensorKind.CpuLoad, SensorKind.RamUsedGb };
            BackgroundAlpha = Math.Max(0, Math.Min(255, BackgroundAlpha));
            FontSize = Math.Max(7f, Math.Min(28f, FontSize));
            UpdateIntervalMs = Math.Max(100, Math.Min(5000, UpdateIntervalMs));
            LogIntervalMs = Math.Max(250, Math.Min(60000, LogIntervalMs));
            if (string.IsNullOrWhiteSpace(LogDirectory))
                LogDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FrameSight");
        }
    }
}

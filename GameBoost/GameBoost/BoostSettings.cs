using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace GameBoost
{
    /// <summary>User-configurable choices for what a boost does.</summary>
    [DataContract]
    public class BoostSettings
    {
        [DataMember(Order = 0)]
        public bool SuspendProcesses { get; set; }

        [DataMember(Order = 1)]
        public bool StopServices { get; set; }

        [DataMember(Order = 2)]
        public bool HighPerformancePowerPlan { get; set; }

        [DataMember(Order = 3)]
        public bool TrimWorkingSets { get; set; }

        [DataMember(Order = 4)]
        public bool PurgeStandbyMemory { get; set; }

        [DataMember(Order = 5)]
        public bool ClearTempFiles { get; set; }

        /// <summary>Process names (without .exe) the user chose to suspend.</summary>
        [DataMember(Order = 6)]
        public List<string> SuspendList { get; set; }

        /// <summary>Windows service names the user chose to stop for the session.</summary>
        [DataMember(Order = 7)]
        public List<string> ServiceList { get; set; }

        /// <summary>Optional game executable that gets High priority while boosted.</summary>
        [DataMember(Order = 8)]
        public string GameProcessName { get; set; }

        public BoostSettings()
        {
            SuspendProcesses = true;
            StopServices = true;
            HighPerformancePowerPlan = true;
            TrimWorkingSets = true;
            PurgeStandbyMemory = true;
            ClearTempFiles = false;
            SuspendList = new List<string>();
            ServiceList = new List<string>(ServiceCatalog.DefaultNames);
            GameProcessName = string.Empty;
        }

        private static string Path
        {
            get
            {
                return System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "GameBoost", "settings.json");
            }
        }

        public static BoostSettings Load()
        {
            try
            {
                if (!File.Exists(Path))
                    return new BoostSettings();

                using (var stream = File.OpenRead(Path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(BoostSettings));
                    var settings = (BoostSettings)serializer.ReadObject(stream);
                    if (settings == null)
                        return new BoostSettings();
                    if (settings.SuspendList == null)
                        settings.SuspendList = new List<string>();
                    if (settings.ServiceList == null)
                        settings.ServiceList = new List<string>();
                    return settings;
                }
            }
            catch (Exception)
            {
                return new BoostSettings();
            }
        }

        public void Save()
        {
            var directory = System.IO.Path.GetDirectoryName(Path);
            if (directory != null)
                Directory.CreateDirectory(directory);

            using (var stream = File.Create(Path))
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true))
            {
                var serializer = new DataContractJsonSerializer(typeof(BoostSettings));
                serializer.WriteObject(writer, this);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace OpenWASD
{
    /// <summary>Reads and writes profiles as JSON files under %APPDATA%\OpenWASD\Profiles.</summary>
    public static class ProfileStore
    {
        private const string Extension = ".json";

        public static string Directory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "OpenWASD", "Profiles");
            }
        }

        /// <summary>Loads every stored profile, seeding the built-in defaults on first run.</summary>
        public static List<MappingProfile> LoadAll()
        {
            var profiles = new List<MappingProfile>();
            if (!System.IO.Directory.Exists(Directory))
            {
                profiles.Add(MappingProfile.CreateFpsDefault());
                profiles.Add(MappingProfile.CreateDesktopDefault());
                foreach (var profile in profiles)
                    Save(profile);
                return profiles;
            }

            foreach (var file in System.IO.Directory.GetFiles(Directory, "*" + Extension))
            {
                var profile = TryLoad(file);
                if (profile != null)
                    profiles.Add(profile);
            }

            if (profiles.Count == 0)
            {
                profiles.Add(MappingProfile.CreateFpsDefault());
                Save(profiles[0]);
            }

            return profiles.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        public static MappingProfile TryLoad(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(MappingProfile));
                    var profile = (MappingProfile)serializer.ReadObject(stream);
                    if (profile == null)
                        return null;
                    profile.Normalize();
                    return profile;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string Save(MappingProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException("profile");

            profile.Normalize();
            System.IO.Directory.CreateDirectory(Directory);
            var path = Path.Combine(Directory, SanitizeFileName(profile.Name) + Extension);
            Export(profile, path);
            return path;
        }

        public static void Export(MappingProfile profile, string path)
        {
            using (var stream = File.Create(path))
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true))
            {
                var serializer = new DataContractJsonSerializer(typeof(MappingProfile));
                serializer.WriteObject(writer, profile);
            }
        }

        public static void Delete(MappingProfile profile)
        {
            var path = Path.Combine(Directory, SanitizeFileName(profile.Name) + Extension);
            if (File.Exists(path))
                File.Delete(path);
        }

        private static string SanitizeFileName(string name)
        {
            var builder = new StringBuilder();
            var invalid = Path.GetInvalidFileNameChars();
            foreach (var character in name.Trim())
                builder.Append(Array.IndexOf(invalid, character) >= 0 ? '_' : character);
            var result = builder.ToString();
            return result.Length == 0 ? "profile" : result;
        }
    }
}

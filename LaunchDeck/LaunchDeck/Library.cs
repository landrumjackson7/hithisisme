using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace LaunchDeck
{
    /// <summary>The persisted collection of games plus the user's view preferences.</summary>
    [DataContract]
    public class Library
    {
        [DataMember(Order = 0)]
        public List<Game> Games { get; set; }

        [DataMember(Order = 1)]
        public bool ShowHidden { get; set; }

        public Library()
        {
            Games = new List<Game>();
        }

        private static string Path
        {
            get
            {
                return System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LaunchDeck", "library.json");
            }
        }

        public static Library Load()
        {
            try
            {
                if (!File.Exists(Path))
                    return new Library();

                using (var stream = File.OpenRead(Path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(Library));
                    var library = (Library)serializer.ReadObject(stream);
                    if (library == null)
                        return new Library();
                    if (library.Games == null)
                        library.Games = new List<Game>();
                    return library;
                }
            }
            catch (Exception)
            {
                return new Library();
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
                var serializer = new DataContractJsonSerializer(typeof(Library));
                serializer.WriteObject(writer, this);
            }
        }

        /// <summary>
        /// Folds scan results into the library. Existing entries keep their user data
        /// (art, playtime, favourite) and only refresh what the scanner owns.
        /// </summary>
        public int Merge(IEnumerable<Game> discovered)
        {
            var added = 0;
            foreach (var game in discovered)
            {
                var existing = Games.FirstOrDefault(g =>
                    g.Platform == game.Platform &&
                    string.Equals(g.LaunchTarget, game.LaunchTarget, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    Games.Add(game);
                    added++;
                    continue;
                }

                existing.Title = game.Title;
                existing.InstallDirectory = game.InstallDirectory;
                if (string.IsNullOrEmpty(existing.TrackProcessName))
                    existing.TrackProcessName = game.TrackProcessName;
            }

            return added;
        }

        public void Remove(Game game)
        {
            Games.Remove(game);
        }

        /// <summary>Applies the search box, platform filter and sort in one pass.</summary>
        public IEnumerable<Game> Filter(string search, GamePlatform? platform, GameSort sort)
        {
            var query = Games.Where(g => ShowHidden || !g.Hidden);

            if (platform.HasValue)
                query = query.Where(g => g.Platform == platform.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var needle = search.Trim();
                query = query.Where(g => g.Title.IndexOf(needle, StringComparison.CurrentCultureIgnoreCase) >= 0);
            }

            switch (sort)
            {
                case GameSort.Playtime:
                    query = query.OrderByDescending(g => g.PlaytimeSeconds);
                    break;
                case GameSort.LastPlayed:
                    query = query.OrderByDescending(g => g.LastPlayedUtc ?? DateTime.MinValue);
                    break;
                case GameSort.Platform:
                    query = query.OrderBy(g => g.Platform.ToString(), StringComparer.CurrentCultureIgnoreCase)
                        .ThenBy(g => g.Title, StringComparer.CurrentCultureIgnoreCase);
                    break;
                default:
                    query = query.OrderBy(g => g.Title, StringComparer.CurrentCultureIgnoreCase);
                    break;
            }

            return query.OrderByDescending(g => g.Favorite).ToList();
        }
    }

    public enum GameSort
    {
        Title = 0,
        Playtime,
        LastPlayed,
        Platform
    }
}

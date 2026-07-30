using System;
using System.Runtime.Serialization;

namespace LaunchDeck
{
    /// <summary>Where a game came from. Manual entries are never overwritten by a rescan.</summary>
    public enum GamePlatform
    {
        Manual = 0,
        Steam,
        Epic,
        Gog
    }

    /// <summary>One entry in the library.</summary>
    [DataContract]
    public class Game
    {
        [DataMember(Order = 0)]
        public string Id { get; set; }

        [DataMember(Order = 1)]
        public string Title { get; set; }

        [DataMember(Order = 2)]
        public GamePlatform Platform { get; set; }

        /// <summary>An exe path, or a launcher URI such as steam://rungameid/440.</summary>
        [DataMember(Order = 3)]
        public string LaunchTarget { get; set; }

        [DataMember(Order = 4)]
        public string InstallDirectory { get; set; }

        /// <summary>Optional box art on disk, assigned by the user.</summary>
        [DataMember(Order = 5)]
        public string ArtPath { get; set; }

        [DataMember(Order = 6)]
        public long PlaytimeSeconds { get; set; }

        [DataMember(Order = 7)]
        public DateTime? LastPlayedUtc { get; set; }

        [DataMember(Order = 8)]
        public bool Favorite { get; set; }

        [DataMember(Order = 9)]
        public bool Hidden { get; set; }

        /// <summary>Process name to watch for playtime when a launcher hands off to another exe.</summary>
        [DataMember(Order = 10)]
        public string TrackProcessName { get; set; }

        public Game()
        {
            Id = Guid.NewGuid().ToString("N");
            Title = string.Empty;
            LaunchTarget = string.Empty;
        }

        public string DescribePlaytime()
        {
            if (PlaytimeSeconds <= 0)
                return "Never played";

            var span = TimeSpan.FromSeconds(PlaytimeSeconds);
            if (span.TotalHours >= 1)
                return string.Format("{0:0.#} h played", span.TotalHours);
            return string.Format("{0:0} min played", Math.Max(1, span.TotalMinutes));
        }

        public string DescribeLastPlayed()
        {
            if (!LastPlayedUtc.HasValue)
                return string.Empty;

            var days = (DateTime.UtcNow - LastPlayedUtc.Value).TotalDays;
            if (days < 1)
                return "Played today";
            if (days < 2)
                return "Played yesterday";
            if (days < 30)
                return string.Format("Played {0:0} days ago", days);
            return "Played " + LastPlayedUtc.Value.ToLocalTime().ToString("d MMM yyyy");
        }
    }
}

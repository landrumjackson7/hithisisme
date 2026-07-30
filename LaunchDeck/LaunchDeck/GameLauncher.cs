using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LaunchDeck
{
    /// <summary>
    /// Starts a game and measures how long it actually ran. Store launchers exit
    /// immediately after handing off, so the session is timed by watching for the
    /// game's own process rather than by waiting on what we started.
    /// </summary>
    public class GameLauncher
    {
        private const int HandoffGraceSeconds = 45;

        private readonly Timer _watchdog = new Timer { Interval = 2000 };
        private Game _game;
        private DateTime _startedUtc;
        private DateTime? _processSeenUtc;
        private bool _processWasSeen;

        /// <summary>Raised when the tracked session ends, with the seconds to credit.</summary>
        public event Action<Game, long> SessionEnded;

        /// <summary>Raised for status text the UI can surface.</summary>
        public event Action<string> Status;

        public GameLauncher()
        {
            _watchdog.Tick += (s, e) => Poll();
        }

        public bool IsTracking
        {
            get { return _game != null; }
        }

        public void Launch(Game game)
        {
            if (game == null || string.IsNullOrWhiteSpace(game.LaunchTarget))
                return;

            if (IsTracking)
            {
                Report("Already tracking " + _game.Title + ".");
                return;
            }

            try
            {
                var info = new ProcessStartInfo(game.LaunchTarget) { UseShellExecute = true };
                if (!string.IsNullOrEmpty(game.InstallDirectory) && Directory.Exists(game.InstallDirectory))
                    info.WorkingDirectory = game.InstallDirectory;

                Process.Start(info);
            }
            catch (Exception ex)
            {
                Report("Could not launch " + game.Title + ": " + ex.Message);
                return;
            }

            _game = game;
            _startedUtc = DateTime.UtcNow;
            _processSeenUtc = null;
            _processWasSeen = false;

            if (string.IsNullOrEmpty(game.TrackProcessName))
            {
                Report("Launched " + game.Title + ". No process name set, so playtime is not tracked.");
                _game = null;
                return;
            }

            _watchdog.Start();
            Report("Launched " + game.Title + ", waiting for " + game.TrackProcessName + ".exe\u2026");
        }

        private void Poll()
        {
            if (_game == null)
            {
                _watchdog.Stop();
                return;
            }

            var running = IsProcessRunning(_game.TrackProcessName);

            if (running)
            {
                if (!_processWasSeen)
                {
                    _processWasSeen = true;
                    _processSeenUtc = DateTime.UtcNow;
                    Report(_game.Title + " is running.");
                }
                return;
            }

            if (!_processWasSeen)
            {
                // Give the store launcher time to start the game before giving up.
                if ((DateTime.UtcNow - _startedUtc).TotalSeconds < HandoffGraceSeconds)
                    return;

                Report("Never saw " + _game.TrackProcessName + ".exe; stopped tracking " + _game.Title + ".");
                Finish(0);
                return;
            }

            var seconds = (long)(DateTime.UtcNow - (_processSeenUtc ?? _startedUtc)).TotalSeconds;
            Finish(seconds);
        }

        private void Finish(long seconds)
        {
            var game = _game;
            _game = null;
            _watchdog.Stop();

            if (game == null)
                return;

            if (seconds > 0)
            {
                var handler = SessionEnded;
                if (handler != null)
                    handler(game, seconds);
                Report(string.Format("{0} closed after {1:0} minutes.", game.Title, seconds / 60.0));
            }
        }

        private static bool IsProcessRunning(string processName)
        {
            try
            {
                return Process.GetProcessesByName(processName).Any();
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void Report(string message)
        {
            var handler = Status;
            if (handler != null)
                handler(message);
        }
    }
}

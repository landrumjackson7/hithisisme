using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ExitClone.Core
{
    public class GameEventArgs : EventArgs
    {
        public Game Game { get; set; }
        public int ProcessId { get; set; }
    }

    /// <summary>
    /// Polls the process list and reports when a known title starts or exits so the app can
    /// auto-connect the way ExitLag does.
    /// </summary>
    public class GameDetector : IDisposable
    {
        private readonly Func<IEnumerable<Game>> _games;
        private readonly Timer _timer;
        private readonly HashSet<string> _running = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;

        public event EventHandler<GameEventArgs> GameStarted;
        public event EventHandler<GameEventArgs> GameStopped;

        public bool Enabled { get; set; } = true;

        public GameDetector(Func<IEnumerable<Game>> games, int intervalMs = 4000)
        {
            _games = games;
            _timer = new Timer(Scan, null, 1500, intervalMs);
        }

        private void Scan(object state)
        {
            if (_disposed || !Enabled) return;
            Dictionary<string, int> processes;
            try
            {
                processes = Process.GetProcesses()
                    .GroupBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return;
            }

            var games = _games() ?? Enumerable.Empty<Game>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var game in games)
            {
                var match = game.ProcessNames.FirstOrDefault(processes.ContainsKey);
                if (match == null) continue;
                seen.Add(game.Id);
                if (_running.Add(game.Id))
                    GameStarted?.Invoke(this, new GameEventArgs { Game = game, ProcessId = processes[match] });
            }

            foreach (var goneId in _running.Where(id => !seen.Contains(id)).ToList())
            {
                _running.Remove(goneId);
                var game = games.FirstOrDefault(g => g.Id == goneId);
                if (game != null) GameStopped?.Invoke(this, new GameEventArgs { Game = game });
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _timer.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using ExitClone.Core;

namespace ExitClone.UI
{
    /// <summary>Shared state handed to every page.</summary>
    public class AppState
    {
        public AppState()
        {
            Settings = SettingsStore.Load();
            Games = GameCatalog.Load();
            Relays = RelayCatalog.Load();
            Controller = new TunnelController(Settings);

            SelectedGame = Games.FirstOrDefault(g => g.Id == Settings.LastGameId) ?? Games.FirstOrDefault();
            SelectedServer = SelectedGame?.Servers.FirstOrDefault(s => s.Name == Settings.LastServerName)
                             ?? SelectedGame?.Servers.FirstOrDefault();
        }

        public AppSettings Settings { get; }
        public List<Game> Games { get; private set; }
        public List<Relay> Relays { get; }
        public TunnelController Controller { get; }

        public Game SelectedGame { get; private set; }
        public GameServer SelectedServer { get; private set; }

        public event EventHandler SelectionChanged;
        public event EventHandler GamesChanged;

        public void Select(Game game, GameServer server = null)
        {
            SelectedGame = game;
            SelectedServer = server ?? game?.Servers.FirstOrDefault();
            Settings.LastGameId = game?.Id;
            Settings.LastServerName = SelectedServer?.Name;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SelectServer(GameServer server)
        {
            SelectedServer = server;
            Settings.LastServerName = server?.Name;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void AddGame(Game game)
        {
            game.Custom = true;
            Games.Add(game);
            Games = Games.OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase).ToList();
            GameCatalog.Save(Games);
            GamesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveGame(Game game)
        {
            if (game == null || !game.Custom) return;
            Games.Remove(game);
            GameCatalog.Save(Games);
            GamesChanged?.Invoke(this, EventArgs.Empty);
        }

        public void PersistGames() => GameCatalog.Save(Games);

        public void Save()
        {
            SettingsStore.Save(Settings);
            GameCatalog.Save(Games);
        }
    }
}

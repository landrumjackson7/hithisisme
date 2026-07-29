using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ExitClone.Core
{
    /// <summary>
    /// Supported titles plus any games the user added by hand. Custom games are persisted, the
    /// built-in list is refreshed from the binary on every start.
    /// </summary>
    public static class GameCatalog
    {
        public static string CustomGamesPath => Path.Combine(SettingsStore.DataDirectory, "games.json");

        public static List<Game> Load()
        {
            var games = BuildIn().ToList();
            try
            {
                if (File.Exists(CustomGamesPath))
                {
                    var stored = JsonConvert.DeserializeObject<List<Game>>(File.ReadAllText(CustomGamesPath));
                    if (stored != null)
                    {
                        foreach (var g in stored)
                        {
                            var existing = games.FirstOrDefault(x => x.Id == g.Id);
                            if (existing == null) games.Add(g);
                            else existing.Favorite = g.Favorite;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Corrupt user catalog must never stop the app from starting.
            }
            return games.OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public static void Save(IEnumerable<Game> games)
        {
            Directory.CreateDirectory(SettingsStore.DataDirectory);
            var persisted = games.Where(g => g.Custom || g.Favorite).ToList();
            File.WriteAllText(CustomGamesPath, JsonConvert.SerializeObject(persisted, Formatting.Indented));
        }

        private static IEnumerable<Game> BuildIn()
        {
            yield return G("valorant", "VALORANT", "Riot Games", new[] { "VALORANT-Win64-Shipping", "RiotClientServices" },
                S("NA Central (Chicago)", NaCentral, "na.valorant.riotgames.com", 443),
                S("EU", "Europe", "eu.valorant.riotgames.com", 443),
                S("AP", "Asia Pacific", "ap.valorant.riotgames.com", 443),
                S("KR", "Korea", "kr.valorant.riotgames.com", 443),
                S("BR", "Brazil", "br.valorant.riotgames.com", 443));

            yield return G("lol", "League of Legends", "Riot Games", new[] { "League of Legends", "LeagueClient" },
                S("NA1 Central (Chicago)", NaCentral, "na1.api.riotgames.com", 443),
                S("EUW1", "Europe West", "euw1.api.riotgames.com", 443),
                S("EUN1", "Europe Nordic & East", "eun1.api.riotgames.com", 443),
                S("KR", "Korea", "kr.api.riotgames.com", 443),
                S("BR1", "Brazil", "br1.api.riotgames.com", 443));

            yield return G("cs2", "Counter-Strike 2", "Valve", new[] { "cs2" },
                S("Steam NA Central (Chicago)", NaCentral, "ext1-ord1.steamserver.net", 443),
                S("Steam NA East", "North America", "iad.steamcontent.com", 443),
                S("Steam EU", "Europe", "fra.steamcontent.com", 443),
                S("Steam SA", "South America", "gru.steamcontent.com", 443),
                S("Steam AS", "Asia", "sgp.steamcontent.com", 443));

            yield return G("dota2", "Dota 2", "Valve", new[] { "dota2" },
                S("Steam NA Central (Chicago)", NaCentral, "ext1-ord1.steamserver.net", 443),
                S("Steam NA East", "North America", "iad.steamcontent.com", 443),
                S("Steam EU", "Europe", "fra.steamcontent.com", 443),
                S("Steam AS", "Asia", "sgp.steamcontent.com", 443));

            yield return G("apex", "Apex Legends", "Electronic Arts", new[] { "r5apex" },
                S("EA NA Central", NaCentral, "ea.com", 443),
                S("EA EU", "Europe", "eu.ea.com", 443));

            yield return G("fortnite", "Fortnite", "Epic Games", new[] { "FortniteClient-Win64-Shipping" },
                S("Epic NA Central", NaCentral, "epicgames.com", 443),
                S("Epic NA East", "North America", "epicgames.com", 443),
                S("Epic EU", "Europe", "eu.epicgames.com", 443));

            yield return G("rocketleague", "Rocket League", "Psyonix", new[] { "RocketLeague" },
                S("Epic US-Central", NaCentral, "epicgames.com", 443),
                S("Epic EU", "Europe", "eu.epicgames.com", 443));

            yield return G("overwatch2", "Overwatch 2", "Blizzard", new[] { "Overwatch" },
                S("Bnet NA Central (Chicago)", NaCentral, "us.actual.battle.net", 443),
                S("Bnet EU", "Europe", "eu.actual.battle.net", 443),
                S("Bnet KR", "Korea", "kr.actual.battle.net", 443));

            yield return G("wow", "World of Warcraft", "Blizzard", new[] { "Wow", "WowClassic" },
                S("Bnet NA Central (Chicago)", NaCentral, "us.actual.battle.net", 443),
                S("Bnet EU", "Europe", "eu.actual.battle.net", 443));

            yield return G("cod", "Call of Duty: Warzone", "Activision", new[] { "cod", "ModernWarfare" },
                S("Demonware NA Central", NaCentral, "prod.demonware.net", 443),
                S("Demonware EU", "Europe", "eu.prod.demonware.net", 443));

            yield return G("gta", "GTA Online", "Rockstar", new[] { "GTA5", "PlayGTAV" },
                S("Rockstar NA Central", NaCentral, "prod.ros.rockstargames.com", 443),
                S("Rockstar EU", "Europe", "socialclub.rockstargames.com", 443));

            yield return G("rust", "Rust", "Facepunch", new[] { "RustClient" },
                S("Steam NA Central (Chicago)", NaCentral, "ext1-ord1.steamserver.net", 443),
                S("Steam NA East", "North America", "iad.steamcontent.com", 443),
                S("Steam EU", "Europe", "fra.steamcontent.com", 443));

            yield return G("minecraft", "Minecraft", "Mojang", new[] { "javaw", "Minecraft" },
                S("Mojang", "Global", "sessionserver.mojang.com", 443),
                S("Realms", "Global", "pc.realms.minecraft.net", 443));

            yield return G("roblox", "Roblox", "Roblox Corp", new[] { "RobloxPlayerBeta" },
                S("Roblox", "Global", "www.roblox.com", 443));

            yield return G("pubg", "PUBG: Battlegrounds", "Krafton", new[] { "TslGame" },
                S("PUBG NA Central", NaCentral, "pubg.com", 443),
                S("PUBG AS", "Asia", "as.pubg.com", 443));

            yield return G("lostark", "Lost Ark", "Smilegate", new[] { "LOSTARK" },
                S("Steam NA Central (Chicago)", NaCentral, "ext1-ord1.steamserver.net", 443),
                S("Steam NA East", "North America", "iad.steamcontent.com", 443),
                S("Steam EU", "Europe", "fra.steamcontent.com", 443));

            yield return G("ffxiv", "Final Fantasy XIV", "Square Enix", new[] { "ffxiv_dx11" },
                S("NA Central (Aether/Primal)", NaCentral, "square-enix.com", 443),
                S("JP", "Japan", "jp.square-enix.com", 443));

            yield return G("tarkov", "Escape from Tarkov", "Battlestate", new[] { "EscapeFromTarkov" },
                S("EFT EU", "Europe", "escapefromtarkov.com", 443));

            yield return G("r6", "Rainbow Six Siege", "Ubisoft", new[] { "RainbowSix" },
                S("Ubi NA Central", NaCentral, "public-ubiservices.ubi.com", 443),
                S("Ubi EU", "Europe", "ubisoft.com", 443));

            yield return G("destiny2", "Destiny 2", "Bungie", new[] { "destiny2" },
                S("Bungie", "Global", "www.bungie.net", 443));
        }

        private static Game G(string id, string name, string publisher, string[] processes, params GameServer[] servers)
        {
            return new Game
            {
                Id = id,
                Name = name,
                Publisher = publisher,
                ProcessNames = processes.ToList(),
                Servers = servers.ToList()
            };
        }

        /// <summary>Region label for the NA Central matchmaking regions this build defaults to.</summary>
        private const string NaCentral = "North America (Central)";

        private static GameServer S(string name, string region, string host, int port)
        {
            return new GameServer { Name = name, Region = region, Host = host, Port = port };
        }
    }
}

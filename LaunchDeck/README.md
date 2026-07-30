# LaunchDeck

A free, open source unified game library for Windows — the paid pitch of tools like LaunchBox Premium, without the licence.

C# (.NET Framework 4.8, WinForms), no third-party dependencies, no account, no telemetry.

## Features

- **Scans installed games** from Steam, Epic Games and GOG, and takes manual entries for anything else (emulators, itch.io builds, old discs).
  - Steam: reads `libraryfolders.vdf` so games on secondary drives are found, then every `appmanifest_*.acf`.
  - Epic: reads `%ProgramData%\Epic\EpicGamesLauncher\Data\Manifests\*.item`.
  - GOG: reads `HKLM\SOFTWARE\GOG.com\Games` (32- and 64-bit views).
- **Launches through the store** (`steam://rungameid/...`, `com.epicgames.launcher://...`) so overlays, cloud saves and DRM keep working.
- **Tracks playtime for every platform**, including manual entries, by watching for the game's own process — not by waiting on the launcher, which exits immediately after handing off.
- **Box art** you assign per game; games without art get a stable colour tile with their initials, so the grid still scans visually.
- **Search, store filter, sort** by title / playtime / last played / store, plus favourites and hidden games.
- Arrow keys move the selection, Enter plays, F5 rescans.
- Library lives in readable JSON at `%APPDATA%\LaunchDeck\library.json`.

Rescans never clobber your edits: a merge keeps existing art, playtime, favourite and hidden flags, and only refreshes the title and install path that the scanner owns.

## Requirements

Windows 7 or newer, .NET Framework 4.8. No elevation needed.

## Build

```powershell
msbuild LaunchDeck.sln /p:Configuration=Release
# or without Visual Studio:
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" LaunchDeck.sln /p:Configuration=Release
```

Output: `LaunchDeck\bin\Release\LaunchDeck.exe`.

## Usage

1. Press **Scan stores** (or F5). Games appear as tiles.
2. Select a tile and press **Play**, or double-click it.
3. Use **Set box art…** to point at a local image, and **Edit…** to fix a title or set the process name used for playtime.

If a game's playtime never records, set its tracked process name — the scanner guesses the largest non-helper exe in the install folder, which is usually right but not always.

## Relationship to commercial launchers

Independent, clean-room implementation. It reads only the public, documented on-disk manifests and registry keys that each store writes, contains no code or assets from any commercial launcher, and bypasses no licensing.

## License

MIT — see [LICENSE](LICENSE).

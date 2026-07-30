# GameBoost

A free, open source "gaming mode" for Windows — the paid headline feature of tools like Game Fire Pro, without the paywall.

Written from scratch in C# (.NET Framework 4.8, WinForms) with no third-party dependencies, no license server, and no telemetry.

## What it does

One button pauses the noise while you play, and one button puts everything back:

- **Pauses background apps** with `NtSuspendProcess`, so Chrome, Discord or Slack stop burning CPU without losing their state — they resume exactly where they were.
- **Stops chatty services** (Superfetch, Windows Search, Windows Update, BITS, Delivery Optimization, telemetry) for the session only. Start types are never touched, so a reboot restores them even if the app crashes.
- **Switches to the High performance power plan**, remembering your previous plan and recreating the scheme if an OEM removed it.
- **Trims working sets** of the paused apps and **purges the standby memory list** to hand real free RAM back to the game.
- **Raises the game's priority** to High while boosted, and restores its old priority afterwards.
- **Optionally clears temp files**.
- Shows a live before/after readout: free RAM, memory pressure, process count, plus a timestamped log of every action.

## Safety

- A hardcoded critical list (`SafeList`) means session-breaking processes — `csrss`, `winlogon`, `lsass`, `dwm`, `explorer`, anything in session 0, PIDs ≤ 4 — are never listed or touched.
- Every change is journaled in memory and reversed by **Restore**; closing the app while boosted prompts you to restore first.
- Services are only ever *stopped*, never disabled.

## Requirements

- Windows 8 or newer, .NET Framework 4.8.
- Administrator rights (the manifest requests elevation). Without them the app still runs and pauses apps, but skips services, power plan and standby memory.

## Build

```powershell
msbuild GameBoost.sln /p:Configuration=Release
# or without Visual Studio:
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" GameBoost.sln /p:Configuration=Release
```

Output: `GameBoost\bin\Release\GameBoost.exe`.

## Usage

1. Launch it (accept the UAC prompt) and press **Recommended** to tick the known resource hogs plus the default service list.
2. Optionally type your game's exe name (e.g. `cs2`) so it gets High priority.
3. Press **Boost now**, play, then press **Restore**.

Selections and options persist to `%APPDATA%\GameBoost\settings.json`.

## Relationship to commercial boosters

Independent, clean-room implementation using documented Windows APIs (`NtSuspendProcess`, `EmptyWorkingSet`, `NtSetSystemInformation`, `ServiceController`, `powercfg`). No code, assets or data files from any commercial product, and no licensing bypass.

## License

MIT — see [LICENSE](LICENSE).

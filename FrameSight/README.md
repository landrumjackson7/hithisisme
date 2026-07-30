# FrameSight

A free, open source always-on-top hardware overlay for Windows — the paid pitch of FPS Monitor, without the licence.

C# (.NET Framework 4.8, WinForms), no third-party dependencies, no drivers, no kernel access.

## Features

- **Always-on-top overlay** that never steals focus: the window is created with `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`, and optionally `WS_EX_TRANSPARENT` so clicks pass straight through to the game.
- **Sensors**: CPU load and clock, RAM (GB or %), GPU load and dedicated memory, disk activity, network down/up, process count, uptime, foreground app, clock. Pick any subset and order.
- **Only offers what your machine reports.** Sensors are probed at startup and unavailable ones are listed as *(not available)* and cannot be ticked, rather than silently reading 0 — older GPU drivers expose no `GPU Engine` counter category at all.
- **Appearance**: corner or free-dragged position, background opacity down to fully transparent, font size, accent colour, load bars and labels on/off.
- **CSV logging** at its own interval, for graphing a session afterwards. Values are written unit-free so spreadsheets treat them as numbers.
- **Global hotkey** (default F10) to show/hide, and a tray icon; closing the settings window leaves the overlay running.
- Settings live at `%APPDATA%\FrameSight\settings.json`, logs default to `Documents\FrameSight`.

## Requirements

Windows 7 or newer, .NET Framework 4.8. Runs unelevated. Some GPU counters only appear on Windows 10 1709+.

## Build

```powershell
msbuild FrameSight.sln /p:Configuration=Release
# or without Visual Studio:
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" FrameSight.sln /p:Configuration=Release
```

Output: `FrameSight\bin\Release\FrameSight.exe`.

## Usage

1. Tick the sensors you want; the overlay updates live.
2. Choose a corner, or pick **Custom** and drag the panel (untick click-through first, then re-tick it).
3. Press **Start logging to CSV** before a benchmark run and **Stop logging** after.
4. F10 hides and shows the overlay in-game.

## Notes and limits

- Frame rate itself is not measured. Reading a game's FPS requires injecting into its Direct3D/Vulkan present path, which trips anti-cheat and needs per-API hooks; FrameSight deliberately stays outside the game process and reports system telemetry only.
- GPU figures come from Windows' own `GPU Engine` / `GPU Process Memory` performance counters, summed across engines, so they match Task Manager rather than a vendor SDK. Temperatures are not available this way and are not shown.

## Relationship to commercial overlays

Independent, clean-room implementation using documented Windows performance counters and window styles. No code or assets from any commercial monitoring tool, no licence bypass, no kernel driver.

## License

MIT — see [LICENSE](LICENSE).

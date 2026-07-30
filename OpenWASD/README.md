# OpenWASD

A free, open source Windows app that turns an Xbox-compatible gamepad into a keyboard and mouse — the core job people normally pay for in commercial remappers like reWASD.

It is written from scratch in C# (.NET Framework 4.8, WinForms) with no third-party dependencies, no license server, no paid tiers, and no telemetry. Every feature is in the box.

## Features

- **Per-control remapping** for all 14 buttons, both triggers, and all eight stick directions.
- **Sticks as mouse or scroll wheel**, with radial deadzone, adjustable speed, a response curve for fine aim, and optional Y inversion.
- **Turbo (auto-repeat)** on any key or mouse binding.
- **Live controller view** that lights up buttons and tracks stick deflection in real time, so you can confirm the pad is seen before you map anything.
- **Profiles** — named, saved as readable JSON under `%APPDATA%\OpenWASD\Profiles`, with duplicate / rename / import / export.
- **Two profiles built in**: an FPS layout (WASD + right stick aim + triggers on the mouse buttons) and a desktop layout (cursor, scroll, media keys).
- **Scan-code output** via `SendInput`, so DirectInput-era games that ignore virtual-key injection still see the presses.
- **Launch with Windows**, optionally starting minimized and remapping immediately.
- **Adjustable poll interval** (1–50 ms) to trade CPU for latency.

## Requirements

- Windows 7 or newer with an XInput runtime (`xinput1_4`, `xinput1_3`, or `xinput9_1_0` — all shipped with Windows).
- .NET Framework 4.8 (preinstalled on Windows 10 1903+).

## Build

```powershell
# Visual Studio / MSBuild
msbuild OpenWASD.sln /p:Configuration=Release

# or, without Visual Studio installed
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" OpenWASD.sln /p:Configuration=Release
```

The result is a single self-contained `OpenWASD\bin\Release\OpenWASD.exe`.

## Usage

1. Plug in a controller and launch `OpenWASD.exe`. The slot dropdown marks which slots are connected.
2. Pick a profile, or start from the FPS defaults and edit rows in the binding list.
3. Select a row, choose the output type, and either pick a key from the dropdown or click **Press a key…** and hit the key you want.
4. Press **Start remapping**. The button turns red while output is live; stopping releases every emulated key so nothing can stick down.

Set both sticks to *Digital directions* when a game expects pure keyboard movement, or put the right stick on *Mouse cursor* for aim.

## Notes and limitations

- Windows blocks synthetic input into higher-privilege windows (UIPI). Run OpenWASD as administrator if a game or launcher is elevated.
- Output is keyboard/mouse only; it does not create a virtual gamepad, so it will not remap one controller into another controller (that needs a kernel driver such as ViGEm).
- Anti-cheat systems vary in how they treat synthetic input. Check a game's rules before using it online.

## Relationship to commercial remappers

OpenWASD is an independent, clean-room implementation written against the public XInput and `SendInput` APIs. It contains no code, assets, or profile formats from any commercial product, and it does not bypass or modify anyone's licensing.

## License

MIT — see [LICENSE](LICENSE).

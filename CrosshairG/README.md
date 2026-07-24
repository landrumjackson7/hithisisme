# Crosshair G

A lightweight, customizable **crosshair overlay** for Windows — an open clone of
"Crosshair X". It draws a always-on-top, click-through crosshair in the center of
your screen so you get a consistent aim point in any game or app that doesn't
provide one.

Crosshair G lives in the **system tray**. Closing its windows does **not** quit
it — the overlay keeps running in the background. It only stops when you choose
**Exit** from the tray menu (or end the task from Task Manager).

## Features

- Always-on-top, fully **click-through** overlay (never steals focus or clicks).
- Multiple crosshair **styles**: Cross, Dot, Cross + Dot, Circle, T-shape.
- Customize **color** (RGBA + quick presets), **arm length**, **thickness**,
  **center gap**, **dot size**, **circle radius**, and **opacity**.
- Optional **outline** (color + thickness) for visibility on any background.
- Fine **position offset** (X/Y) to nudge the crosshair off dead-center.
- **Live preview** in the settings window.
- **System tray** icon: open Settings, toggle the crosshair, reset, or exit.
- Settings persist to `%APPDATA%\CrosshairG\settings.json`.
- **Single instance** — launching again just focuses the running app.

## Behavior: "still works when you close it"

- **Closing the Settings window** (the X button) hides it but the app keeps
  running and the crosshair stays on screen.
- **Toggle** the crosshair on/off from the tray without quitting.
- **Exit** from the tray menu — or ending the process in Task Manager — fully
  closes Crosshair G.

## Download / Run

Grab `CrosshairG.exe` from the build artifacts (see below). It's a **self-contained
single file** — no .NET install required. Double-click to run; look for the
crosshair icon in the system tray.

> Note: because it's an overlay drawn on top of other windows, exclusive
> **full-screen** games may hide it. Use **borderless / windowed full-screen**
> mode for the overlay to show, exactly as with other crosshair overlays.

## Build from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```powershell
# Run/build (on Windows)
dotnet build -c Release .\CrosshairG.csproj

# Produce a self-contained single-file exe
dotnet publish -c Release -r win-x64 --self-contained true `
  /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish .\CrosshairG.csproj
# -> publish\CrosshairG.exe
```

The project targets `net8.0-windows` (WPF for the overlay/UI + WinForms
`NotifyIcon` for the tray). It can be **cross-built from Linux/macOS CI** thanks
to `EnableWindowsTargeting`; the included GitHub Actions workflow builds the exe
on a Windows runner and uploads it as an artifact.

## Project layout

| File | Purpose |
| --- | --- |
| `App.xaml(.cs)` | Entry point, tray icon, single-instance, app lifecycle |
| `OverlayWindow.xaml(.cs)` | Transparent, topmost, click-through overlay window |
| `SettingsWindow.xaml(.cs)` | Settings UI with live preview |
| `CrosshairSettings.cs` | Settings model + JSON persistence |
| `CrosshairRenderer.cs` | Shared crosshair drawing (overlay + preview) |

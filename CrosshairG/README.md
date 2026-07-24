# Crosshair G

A lightweight, customizable **crosshair overlay** for Windows — an open clone of
"Crosshair X". It draws a always-on-top, click-through crosshair in the center of
your screen so you get a consistent aim point in any game or app that doesn't
provide one.

Crosshair G lives in the **system tray**. Closing its windows does **not** quit
it — the overlay keeps running in the background. It only stops when you choose
**Exit** from the tray menu (or end the task from Task Manager).

## Menu

A CrosshairZero-style dark app window with a custom title bar, top navigation
tabs and an **ON/OFF** overlay toggle:

- **Browse** — a style sidebar (Classic, Dot, Cross + Dot, Circle, T-Shape,
  Arrow) with counts, a searchable gallery of preset crosshairs, a preview, and
  **Use This Crosshair**.
- **Design** — build your own crosshair (see below) with a live preview.
- **Saved** — save the current design, then load or delete saved crosshairs.
- **Settings** — enable/disable overlay, position offset, and info.

## Designer

- Shape: Cross, Dot, Cross + Dot, Circle, T-shape, **Arrow**.
- **Line style**: Solid or **Wavy** (with amplitude + frequency).
- Quick actions: **Thin**, **Thick**, **Smaller**, **Bigger**.
- Sliders: arm length (size), thickness, center gap, dot size, circle radius,
  **opacity**.
- **Color**: RGBA sliders + quick presets (green/red/cyan/magenta/yellow/white).
- Optional **outline** (color + thickness) for contrast on any background.
- Fine **position offset** (X/Y) to nudge the crosshair off dead-center.

## Features

- Always-on-top, fully **click-through** overlay (never steals focus or clicks).
- **System tray** icon: open the menu, toggle the crosshair, reset, or exit.
- Settings persist to `%APPDATA%\CrosshairG\settings.json`; saved crosshairs to
  `saved.json`.
- **Single instance** — launching again just focuses the running app.

## Behavior: "still works when you close it"

- **Closing the app window** (the X button) hides it to the tray but the app
  keeps running and the crosshair stays on screen.
- **Toggle** the crosshair on/off with the ON/OFF switch or from the tray
  without quitting.
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
| `App.xaml(.cs)` | Entry point, theme resources, tray icon, single-instance, lifecycle |
| `OverlayWindow.xaml(.cs)` | Transparent, topmost, click-through overlay window |
| `MainWindow.xaml(.cs)` | Menu UI: Browse / Design / Saved / Settings tabs |
| `CrosshairSettings.cs` | Settings model + JSON persistence |
| `CrosshairPresets.cs` | Built-in preset crosshairs by category |
| `SavedStore.cs` | Load/save the user's saved crosshairs |
| `CrosshairRenderer.cs` | Shared crosshair drawing (overlay + preview + thumbnails) |

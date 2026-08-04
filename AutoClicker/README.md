# Astryx AutoClicker

A small standalone Windows auto clicker. Configurable click rate from **1 up to 10000 CPS**,
selectable mouse button, and a global start/stop hotkey.

## Features

- Target rate 1-10000 clicks per second.
- Left / right / middle mouse button.
- Global hotkey toggle (default `F6`; also `F7`-`F10`) that works even when the app is not focused.
- High-resolution `Stopwatch`-paced click thread so high rates stay accurate (a WinForms
  timer tops out around 64 Hz, which is why a dedicated thread is used instead).
- Clicks at the current cursor position, so aim first, then start.

## Build

Option A - .NET SDK / Visual Studio 2022 (matches the main repo):

```powershell
dotnet build .\AutoClicker.csproj -c Release
```

Option B - no SDK required, uses the compiler bundled with .NET Framework 4.x:

```bat
build.bat
```

Both produce `AstryxAutoClicker.exe`.

## Usage

1. Run `AstryxAutoClicker.exe`.
2. Set the target CPS and mouse button.
3. Move the cursor over the target and press **Start** (or the hotkey).
4. Press the hotkey again (or **Stop**) to stop.

## Notes

- Very high rates (thousands of CPS) are CPU intensive and many apps/games throttle or
  ignore synthetic clicks faster than they can process; the achievable effective rate
  depends on the target window.
- Use responsibly and only where automated clicking is permitted.

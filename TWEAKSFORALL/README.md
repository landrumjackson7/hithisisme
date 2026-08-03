# TWEAKSFORALL

A single-file, self-contained Windows system optimizer focused on **maximum FPS**
and **lowest latency**, tuned for **Windows 10 IoT Enterprise LTSC**. Black + blue UI
inspired by modern tuner apps, built in C# / WinForms (.NET Framework 4.8).

## What it does

One-click **MAXIMUM OPTIMIZE** applies every recommended low-latency / max-FPS tweak
(after creating a System Restore point), or you can apply individual tweaks per page:

- **Windows** – High Performance plan, min processor state 100%, disable core parking,
  aggressive boost, EPP 0, kill power throttling, program-priority scheduling,
  disable dynamic tick, telemetry/background apps off, optional Spectre/Meltdown off.
- **Cleanup** – temp / prefetch / shader cache, DNS flush, recycle bin, disk cleanup.
- **Network** – disable Nagle, kill network throttling index, SystemResponsiveness 0,
  RSS, disable auto-tuning/NetBIOS, larger DNS cache, optional Cloudflare DNS / IPv6 off.
- **GPU** – hardware-accelerated GPU scheduling, Games GPU priority 8, disable fullscreen
  optimizations, higher TDR delay, prefer dedicated VRAM, raw mouse input.
- **Games** – Game Mode, high-resolution timer, no audio ducking, disable Xbox services,
  optional live High-priority booster for Fortnite / Valorant / CS2.
- **Memory** – disable memory compression, keep kernel/drivers in RAM, large system cache.
- **Responsiveness** – instant menus, fast hung-app/service kill, no startup delay.

Every tweak that can be reversed exposes a **Revert**; Settings has a global
"Revert everything to Windows defaults".

## Build

Targets .NET Framework 4.8. Build with the .NET SDK on Windows:

```powershell
dotnet build .\TWEAKSFORALL.csproj -c Release
```

The single executable is written to `bin\Release\TWEAKSFORALL.exe` — no other files
required (it uses only the .NET Framework 4.8 that ships with Windows 10 IoT LTSC).

## Notes

- Must be run **as Administrator** (the manifest auto-elevates).
- Some tweaks (timers, mitigations, driver/GPU scheduling) take full effect after a reboot.

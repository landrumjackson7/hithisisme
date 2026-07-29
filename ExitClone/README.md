# ExitClone

An ExitLag-style route optimizer for online games, written in C# (WinForms, .NET Framework 4.8).
It measures every relay path to a game's servers, ranks them by latency/jitter/loss, and pushes
game traffic through the best one — or through several at once with packet duplication.

## Build

```powershell
dotnet build .\ExitClone\ExitClone.csproj -c Release
```

The executable is written to `ExitClone\bin\Release\net48\ExitClone.exe`. It is a single .exe and
needs .NET Framework 4.8, which ships with Windows 10/11.

## Defaults: NA Central

This build is tuned for a North America Central player: every built-in title selects its NA Central
region first (Chicago/Dallas endpoints where the publisher exposes one), and the relay pool starts
with only the North American relays enabled — US Central (Dallas), US Central 2 (Chicago), US East,
US East 2, US West, US West 2, Canada Central. Other regions are still listed and can be re-enabled
from the Routes page.

## Features

- **Game library** – 20 built-in titles with their regional server endpoints, favourites, search,
  and custom titles (name, process, server host/port).
- **Game detection & auto-connect** – polls the process list and connects when a known title
  launches, disconnects when it exits.
- **Route optimization** – builds direct, single-hop and two-hop relay candidates, probes each one
  (ICMP with automatic TCP-handshake fallback) and ranks them by `avg + 2.5*jitter + 8*loss`.
- **Routing modes** – `Optimized` (single best path), `MultiPath` (several disjoint exits with
  packet duplication), `Monitor` (measurement only, no data plane).
- **Data plane** – a local SOCKS5 endpoint on `127.0.0.1:1080`:
  - TCP `CONNECT` races every selected relay chain and keeps the winner;
  - UDP `ASSOCIATE` fans each datagram out over all selected relays and de-duplicates the
    replies with a rolling FNV-1a window, so the game only ever sees one copy.
- **System routes (optional)** – pins the game server addresses to the relay gateway with
  `route add` when running elevated, and reverts on disconnect.
- **Live telemetry** – ping/jitter/loss cards, accelerated vs. direct latency graph, improvement
  percentage, traffic counters, duplicate/deduplicate counters, CSV export.
- **Tools** – ping test, traceroute, download speed test, adapter/gateway inspector.
- **Settings** – run at logon, start minimized, tray behaviour, packet duplication, number of
  simultaneous routes, max hops, MTU, TCP_NODELAY, probe counts and intervals, proxy port.
- **Relay pool editor** – enable/disable relays per profile; the whole fleet can be replaced by
  dropping a `relays.json` in `%AppData%\ExitClone`.

## Pointing a game at the tunnel

The data plane is a standard SOCKS5 proxy, so any of these works:

- launchers that support a SOCKS5 proxy (Steam, most MMO clients) – `127.0.0.1:1080`;
- a per-process redirector (Proxifier, Proxycap) with a rule for the game executable;
- `Monitor` mode if you only want the measurements.

Kernel-level interception without a helper is deliberately not implemented: it requires a signed
driver (WinDivert/TAP) that cannot be shipped from this repository.

## Configuration files

`%AppData%\ExitClone\`

| File | Purpose |
| --- | --- |
| `settings.json` | all app settings and per-game profiles |
| `games.json` | custom titles and favourites |
| `relays.json` | optional relay fleet override (`Id`, `Host`, `SocksPort`, `ProbePort`, ...) |

The built-in relay list points at public regional edges so measurement works out of the box; the
relaying itself expects SOCKS5 servers, so set `relays.json` to your own fleet before using
`Optimized`/`MultiPath` for real traffic.

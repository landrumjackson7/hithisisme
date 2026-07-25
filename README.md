# Astryx Tweaks

Windows Forms source for Astryx Tweaks Maximum Tweaks Edition.

## Build

The project targets .NET Framework 4.8 and can be built on Windows with Visual Studio 2022 or the .NET SDK:

```powershell
dotnet build .\AstryxTweaksV3.csproj -c Release
```

The executable is written to `bin\Release\net48\`.

The initial source was recovered from the supplied application binary and cleaned up into a buildable project.

## ExitClone

`ExitClone/` holds a separate WinForms app: an ExitLag-style game route optimizer with relay
probing, multi-path packet duplication and a local SOCKS5 data plane. See
[ExitClone/README.md](ExitClone/README.md).

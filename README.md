# Astryx Tweaks

Windows Forms source for Astryx Tweaks Maximum Tweaks Edition.

## Build

The project targets .NET Framework 4.8 and can be built on Windows with Visual Studio 2022 or the .NET SDK:

```powershell
dotnet build .\AstryxTweaksV3.csproj -c Release
```

The executable is written to `bin\Release\net48\`.

The initial source was recovered from the supplied application binary and cleaned up into a buildable project.

## Utility tabs

The bottom of the sidebar contains non-tweak pages:

- **Account** changes the locally stored display name.
- **Themes** offers five saved color themes.
- **Network** configures reversible real-time priority for the Steam Client Service at `C:\Program Files (x86)\Common Files\Steam\steamservice.exe`.

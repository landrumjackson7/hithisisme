---
name: testing-astryx-windows-forms
description: Build and visually test Astryx Tweaks Windows Forms changes, especially navigation, custom controls, and page isolation.
---

# Testing Astryx Tweaks Windows Forms

## Devin Secrets Needed

None.

## Build

Run from the repository root:

```bash
dotnet build "AstryxTweaksV3.csproj" -c Release
```

The executable is written to:

```text
bin/Release/net48/AstryxTweaks-MAXIMUM-TWEAKS-EDITION.exe
```

## Runtime testing

Prefer testing on Windows with .NET Framework 4.8 because that is the target
runtime. Test navigation through the visible UI and record switch/page
transitions.

Mono can provide a limited visual smoke test on Linux:

```bash
mono "bin/Release/net48/AstryxTweaks-MAXIMUM-TWEAKS-EDITION.exe"
```

Mono 6.8 with libgdiplus 6.0.4 might exit during WinForms page navigation with
a `GenericError` in `System.Windows.Forms.Theming.Default.ButtonPainter.DrawFlat`.
If that happens twice in fresh processes, treat page-transition assertions as
untested and require a Windows rerun; do not infer success from source code.

## Toggle and page-isolation checks

1. Open a tweak page with visible switches.
2. Toggle one switch off and on.
3. Verify gray/left and blue/right endpoints, visible thumb travel, and no
   text/glow/background bleed inside the switch bounds.
4. Navigate directly to another full-page view such as One-Click Optimizer.
5. Verify zero switch, status-label, or card fragments from the previous page
   remain visible.

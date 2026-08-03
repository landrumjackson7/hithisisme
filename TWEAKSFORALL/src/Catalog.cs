using System;
using System.Collections.Generic;
using System.IO;

namespace TweaksForAll
{
    // The full catalog of tweaks, grouped by the sidebar page they live on.
    // Tuned for Windows 10 IoT Enterprise LTSC: maximum FPS + lowest latency.
    internal static class Catalog
    {
        public static List<Tweak> All()
        {
            var t = new List<Tweak>();

            // ---------------- WINDOWS (power, CPU, latency core) ----------------
            Add(t, "Windows", "High Performance power plan", "Unlocks max CPU clocks, no power saving", true, true,
                () => { Engine.Run("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"); },
                () => { Engine.Run("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e"); });

            Add(t, "Windows", "Minimum processor state 100%", "No CPU downclocking under load", true, true,
                () => { Engine.Run("powercfg", "/setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 100"); Engine.Run("powercfg", "/setactive scheme_current"); },
                () => { Engine.Run("powercfg", "/setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 5"); Engine.Run("powercfg", "/setactive scheme_current"); });

            Add(t, "Windows", "Disable core parking", "Keep every CPU core online for lower latency", true, true,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583", "ValueMax", 0u, null); Engine.Run("powercfg", "/setactive scheme_current"); },
                null);

            Add(t, "Windows", "Processor performance boost = aggressive", "Turbo ramps instantly on load", true, true,
                () => { Engine.Run("powercfg", "/setacvalueindex scheme_current sub_processor PERFBOOSTMODE 2"); Engine.Run("powercfg", "/setactive scheme_current"); },
                null);

            Add(t, "Windows", "Energy preference = max performance", "PERFEPP 0, favours speed over efficiency", true, true,
                () => { Engine.Run("powercfg", "/setacvalueindex scheme_current sub_processor PERFEPP 0"); Engine.Run("powercfg", "/setactive scheme_current"); },
                null);

            Add(t, "Windows", "Disable CPU power throttling", "Stops Windows throttling background/foreground cores", true, true,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling", "PowerThrottlingOff", 1u, null); },
                () => { Engine.RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling", "PowerThrottlingOff"); });

            Add(t, "Windows", "Disable USB selective suspend", "USB mice/keyboards never sleep", true, true,
                () => { Engine.Run("powercfg", "/setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0"); Engine.Run("powercfg", "/setactive scheme_current"); },
                null);

            Add(t, "Windows", "Disable hibernation", "Frees disk equal to RAM, faster shutdowns", true, true,
                () => { Engine.Run("powercfg", "/h off"); },
                () => { Engine.Run("powercfg", "/h on"); });

            Add(t, "Windows", "Processor scheduling = programs", "Foreground apps/games get more CPU quanta", true, true,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "Win32PrioritySeparation", 38u, null); },
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "Win32PrioritySeparation", 2u, null); });

            Add(t, "Windows", "Disable dynamic tick", "Removes timer coalescing latency spikes", true, true,
                () => { Engine.Run("bcdedit", "/set disabledynamictick yes"); },
                () => { Engine.Run("bcdedit", "/set disabledynamictick no"); });

            Add(t, "Windows", "Reset platform clock (use TSC/HPET off)", "Lets CPU invariant timer drive the clock", true, true,
                () => { Engine.Run("bcdedit", "/deletevalue useplatformclock"); },
                null);

            Add(t, "Windows", "Disable telemetry & data collection", "Stops DiagTrack sending diagnostics", true, true,
                () => { Engine.Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "AllowTelemetry", 0u, null); Engine.SvcDisable("DiagTrack"); Engine.SvcDisable("dmwappushservice"); },
                () => { Engine.RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "AllowTelemetry"); Engine.SvcAuto("DiagTrack"); });

            Add(t, "Windows", "Disable background apps", "No UWP apps running in the background", true, true,
                () => { Engine.Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled", 1u, null); },
                () => { Engine.Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled", 0u, null); });

            Add(t, "Windows", "Disable SysMain (Superfetch)", "No app pre-loading churn", true, true,
                () => { Engine.SvcDisable("SysMain"); },
                () => { Engine.SvcAuto("SysMain"); });

            Add(t, "Windows", "Windows Search delayed start", "Indexer no longer competes at boot", true, true,
                () => { Engine.Run("sc", "config WSearch start= delayed-auto"); },
                () => { Engine.SvcAuto("WSearch"); });

            Add(t, "Windows", "Disable Xbox Game Bar / DVR", "Removes overlay + capture overhead", true, true,
                () => { Engine.Reg("HKCU\\System\\GameConfigStore", "GameDVR_Enabled", 0u, null); Engine.Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR", "AllowGameDVR", 0u, null); },
                null);

            Add(t, "Windows", "Disable animations & transparency", "Snappier desktop, less GPU compositor work", true, true,
                () => { Engine.Reg("HKCU\\Control Panel\\Desktop\\WindowMetrics", "MinAnimate", 0, "0"); Engine.Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "EnableTransparency", 0u, null); },
                () => { Engine.Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "EnableTransparency", 1u, null); });

            Add(t, "Windows", "Disable Spectre / Meltdown mitigations", "Removes CPU mitigation overhead", false, true,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverride", 3u, null); Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverrideMask", 3u, null); },
                () => { Engine.RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverride"); },
                "Reduces CPU security. Enable only on trusted machines.");

            Add(t, "Windows", "Fast boot (no boot GUI)", "Skips the boot logo animation", true, false,
                () => { Engine.Run("bcdedit", "/set bootuxdisabled on"); },
                () => { Engine.Run("bcdedit", "/set bootuxdisabled off"); });

            // ---------------- CLEANUP ----------------
            Add(t, "Cleanup", "Clear temp files", "Deletes user + Windows Temp", true, true,
                () => { Engine.CleanDir(Path.GetTempPath()); Engine.CleanDir(Path.Combine(Environment.GetEnvironmentVariable("windir") ?? "C:\\Windows", "Temp")); },
                null);

            Add(t, "Cleanup", "Clear Prefetch", "Removes stale prefetch data", true, true,
                () => { Engine.CleanDir(Path.Combine(Environment.GetEnvironmentVariable("windir") ?? "C:\\Windows", "Prefetch")); },
                null);

            Add(t, "Cleanup", "Flush DNS cache", "Clears cached DNS entries", true, true,
                () => { Engine.Run("ipconfig", "/flushdns"); },
                null);

            Add(t, "Cleanup", "Clear DirectX shader cache", "Rebuilds compiled shaders (first launch slower)", false, true,
                () => { Engine.CleanDir(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache")); },
                null, "Your next game launch may take a little longer while shaders rebuild.");

            Add(t, "Cleanup", "Empty Recycle Bin", "Frees deleted-file space", false, false,
                () => { Engine.RunPS("Clear-RecycleBin -Force -EA 0"); },
                null);

            Add(t, "Cleanup", "Run Windows Disk Cleanup", "Opens cleanmgr for a deep clean", false, false,
                () => { Engine.Run("cleanmgr", "/sagerun:1"); },
                null);

            Add(t, "Cleanup", "Reset Windows Update cache", "Clears SoftwareDistribution downloads", false, false,
                () => { Engine.Run("net", "stop wuauserv"); Engine.CleanDir("C:\\Windows\\SoftwareDistribution\\Download"); Engine.Run("net", "start wuauserv"); },
                null);

            // ---------------- NETWORK (low latency) ----------------
            Add(t, "Network", "Disable Nagle's algorithm", "Sends packets immediately, lower game latency", true, true,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "TcpAckFrequency", 1u, null); Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "TCPNoDelay", 1u, null); },
                () => { Engine.RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "TcpAckFrequency"); Engine.RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "TCPNoDelay"); });

            Add(t, "Network", "Kill network throttling index", "Removes the multimedia network throttle", true, true,
                () => { Engine.Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex", uint.MaxValue, null); },
                () => { Engine.Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex", 10u, null); });

            Add(t, "Network", "System responsiveness = 0", "100% CPU available to foreground/games", true, true,
                () => { Engine.Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "SystemResponsiveness", 0u, null); },
                () => { Engine.Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "SystemResponsiveness", 20u, null); });

            Add(t, "Network", "Enable Receive Side Scaling", "Spreads NIC interrupts across cores", true, true,
                () => { Engine.Run("netsh", "interface tcp set global rss=enabled"); },
                null);

            Add(t, "Network", "Disable TCP auto-tuning", "Fixed receive window, steadier latency", false, true,
                () => { Engine.Run("netsh", "interface tcp set global autotuninglevel=disabled"); },
                () => { Engine.Run("netsh", "interface tcp set global autotuninglevel=normal"); });

            Add(t, "Network", "Disable NetBIOS over TCP/IP", "Cuts legacy broadcast overhead", true, true,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\NetBT\\Parameters\\Interfaces", "NetbiosOptions", 2u, null); },
                null);

            Add(t, "Network", "Larger DNS cache", "Fewer repeated DNS lookups", true, true,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Dnscache\\Parameters", "CacheHashTableBucketSize", 1u, null); Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Dnscache\\Parameters", "CacheHashTableSize", 384u, null); Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Dnscache\\Parameters", "MaxCacheTtl", 86400u, null); },
                null);

            Add(t, "Network", "Use Cloudflare DNS (1.1.1.1)", "Fast resolver on active adapter", false, false,
                () => { Engine.RunPS("Get-NetAdapter -Physical | Where Status -eq 'Up' | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ServerAddresses ('1.1.1.1','1.0.0.1') -EA 0 }"); },
                () => { Engine.RunPS("Get-NetAdapter -Physical | ForEach-Object { Set-DnsClientServerAddress -InterfaceIndex $_.ifIndex -ResetServerAddresses -EA 0 }"); });

            Add(t, "Network", "Disable IPv6", "Removes IPv6 stack overhead", false, false,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters", "DisabledComponents", 255u, null); },
                () => { Engine.RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters", "DisabledComponents"); },
                "Disable only if your network/games don't rely on IPv6.");

            // ---------------- GPU (frame pacing / latency) ----------------
            Add(t, "GPU", "Hardware-accelerated GPU scheduling", "Offloads frame scheduling to the GPU", true, true,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "HwSchMode", 2u, null); },
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "HwSchMode", 1u, null); });

            Add(t, "GPU", "Games GPU priority = 8", "Max GPU scheduling priority for games task", true, true,
                () => { Engine.Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "GPU Priority", 8u, null); Engine.Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "Priority", 6u, null); Engine.Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "Scheduling Category", 0, "High"); Engine.Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "SFIO Priority", 0, "High"); },
                null);

            Add(t, "GPU", "Disable fullscreen optimizations", "True exclusive fullscreen, lower input lag", true, true,
                () => { Engine.Reg("HKCU\\System\\GameConfigStore", "GameDVR_FSEBehaviorMode", 2u, null); },
                null);

            Add(t, "GPU", "Increase TDR delay", "Fewer 'display driver stopped' timeouts", true, true,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "TdrDelay", 10u, null); },
                null);

            Add(t, "GPU", "Prefer dedicated VRAM", "Avoids shared-memory fallback", false, true,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "PreferDedicatedVRAM", 1u, null); },
                null);

            Add(t, "GPU", "Disable mouse acceleration", "Raw 1:1 pointer for aiming", true, true,
                () => { Engine.Reg("HKCU\\Control Panel\\Mouse", "MouseSpeed", 0, "0"); Engine.Reg("HKCU\\Control Panel\\Mouse", "MouseThreshold1", 0, "0"); Engine.Reg("HKCU\\Control Panel\\Mouse", "MouseThreshold2", 0, "0"); },
                () => { Engine.Reg("HKCU\\Control Panel\\Mouse", "MouseSpeed", 0, "1"); Engine.Reg("HKCU\\Control Panel\\Mouse", "MouseThreshold1", 0, "6"); Engine.Reg("HKCU\\Control Panel\\Mouse", "MouseThreshold2", 0, "10"); });

            Add(t, "GPU", "Disable GPU preemption", "Lower-latency GPU dispatch", false, false,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\\Scheduler", "PreemptionMode", 0u, null); },
                null);

            // ---------------- GAMES (max FPS + booster) ----------------
            Add(t, "Games", "Enable Windows Game Mode", "Auto-prioritises resources for the active game", true, true,
                () => { Engine.Reg("HKCU\\SOFTWARE\\Microsoft\\GameBar", "AllowAutoGameMode", 1u, null); Engine.Reg("HKCU\\SOFTWARE\\Microsoft\\GameBar", "AutoGameModeEnabled", 1u, null); },
                null);

            Add(t, "Games", "High-resolution timer for games", "Requests 0.5ms timer for smoother frame pacing", true, true,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel", "GlobalTimerResolutionRequests", 1u, null); },
                null);

            Add(t, "Games", "Disable audio ducking", "Volume never dips during play/voice", true, true,
                () => { Engine.Reg("HKCU\\SOFTWARE\\Microsoft\\Multimedia\\Audio", "UserDuckingPreference", 3u, null); },
                null);

            Add(t, "Games", "Disable Xbox background services", "Frees CPU/RAM from unused Xbox stack", true, true,
                () => { Engine.SvcDisable("XblAuthManager"); Engine.SvcDisable("XblGameSave"); Engine.SvcDisable("XboxGipSvc"); Engine.SvcDisable("XboxNetApiSvc"); },
                () => { Engine.SvcManual("XblAuthManager"); Engine.SvcManual("XblGameSave"); Engine.SvcManual("XboxGipSvc"); Engine.SvcManual("XboxNetApiSvc"); });

            Add(t, "Games", "Live booster: Fortnite High priority", "Keeps Fortnite at High priority while running", false, false,
                () => { Engine.RunPS("Start-Process powershell -ArgumentList '-WindowStyle Hidden -Command \"while(1){$x=Get-Process FortniteClient-Win64-Shipping -EA 0;if($x){$x.PriorityClass=[Diagnostics.ProcessPriorityClass]::High};Sleep 5}\"'"); },
                null);

            Add(t, "Games", "Live booster: Valorant High priority", "Keeps Valorant at High priority while running", false, false,
                () => { Engine.RunPS("Start-Process powershell -ArgumentList '-WindowStyle Hidden -Command \"while(1){$x=Get-Process VALORANT-Win64-Shipping -EA 0;if($x){$x.PriorityClass=[Diagnostics.ProcessPriorityClass]::High};Sleep 5}\"'"); },
                null);

            Add(t, "Games", "Live booster: CS2 High priority", "Keeps CS2 at High priority while running", false, false,
                () => { Engine.RunPS("Start-Process powershell -ArgumentList '-WindowStyle Hidden -Command \"while(1){$x=Get-Process cs2 -EA 0;if($x){$x.PriorityClass=[Diagnostics.ProcessPriorityClass]::High};Sleep 5}\"'"); },
                null);

            // ---------------- MEMORY ----------------
            Add(t, "Memory", "Disable memory compression", "More usable RAM, less CPU overhead", true, true,
                () => { Engine.RunPS("Disable-MMAgent -MemoryCompression -EA 0"); },
                () => { Engine.RunPS("Enable-MMAgent -MemoryCompression -EA 0"); });

            Add(t, "Memory", "Keep kernel & drivers in RAM", "DisablePagingExecutive (needs 8GB+)", true, true,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingExecutive", 1u, null); },
                () => { Engine.RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingExecutive"); },
                "Best on systems with 8GB RAM or more.");

            Add(t, "Memory", "Large system cache", "More RAM for the file cache", false, false,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "LargeSystemCache", 1u, null); },
                () => { Engine.RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "LargeSystemCache"); });

            Add(t, "Memory", "Don't clear pagefile at shutdown", "Faster shutdowns", true, true,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "ClearPageFileAtShutdown", 0u, null); },
                null);

            // ---------------- REGISTRY / RESPONSIVENESS ----------------
            Add(t, "Registry", "Kill hung apps in 1s", "HungAppTimeout 1000ms", true, true,
                () => { Engine.Reg("HKCU\\Control Panel\\Desktop", "HungAppTimeout", 0, "1000"); Engine.Reg("HKCU\\Control Panel\\Desktop", "WaitToKillAppTimeout", 0, "2000"); },
                null);

            Add(t, "Registry", "Instant menu show delay", "MenuShowDelay 0ms", true, true,
                () => { Engine.Reg("HKCU\\Control Panel\\Desktop", "MenuShowDelay", 0, "0"); },
                () => { Engine.Reg("HKCU\\Control Panel\\Desktop", "MenuShowDelay", 0, "400"); });

            Add(t, "Registry", "Remove startup app delay", "Sign-in apps launch immediately", true, true,
                () => { Engine.Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize", "StartupDelayInMSec", 0u, null); },
                null);

            Add(t, "Registry", "Fast service shutdown", "WaitToKillServiceTimeout 2000ms", true, true,
                () => { Engine.Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control", "WaitToKillServiceTimeout", 0, "2000"); },
                null);

            Add(t, "Registry", "Disable lock screen", "Boot straight to sign-in", false, false,
                () => { Engine.Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Personalization", "NoLockScreen", 1u, null); },
                () => { Engine.RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Personalization", "NoLockScreen"); });

            return t;
        }

        private static void Add(List<Tweak> list, string cat, string name, string desc, bool defaultOn, bool maximum,
            Action apply, Action revert, string warning = null)
        {
            list.Add(new Tweak
            {
                Category = cat,
                Name = name,
                Desc = desc,
                Apply = apply,
                Revert = revert,
                Warning = warning,
                DefaultOn = defaultOn,
                Maximum = maximum
            });
        }
    }
}

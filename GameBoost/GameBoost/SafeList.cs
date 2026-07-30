using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GameBoost
{
    /// <summary>
    /// Guards against suspending anything that would destabilise the session.
    /// Suspending these leaves the desktop unusable or triggers a watchdog reboot,
    /// so they are never offered in the UI.
    /// </summary>
    public static class SafeList
    {
        private static readonly HashSet<string> Critical = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "system", "registry", "idle", "smss", "csrss", "wininit", "winlogon", "services",
            "lsass", "lsaiso", "svchost", "fontdrvhost", "dwm", "explorer", "sihost", "ctfmon",
            "taskhostw", "runtimebroker", "shellexperiencehost", "startmenuexperiencehost",
            "searchhost", "dllhost", "conhost", "audiodg", "spoolsv", "wudfhost", "memcompression",
            "securityhealthservice", "msmpeng", "nissrv", "wmiprvse", "logonui", "userinit",
            "gameboost"
        };

        /// <summary>Processes that are usually safe to pause and commonly hog resources while gaming.</summary>
        private static readonly HashSet<string> Recommended = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "chrome", "msedge", "firefox", "opera", "brave", "vivaldi",
            "slack", "teams", "ms-teams", "discord", "skype", "zoom", "telegram", "whatsapp",
            "spotify", "itunes", "vlc", "onedrive", "dropbox", "googledrivefs", "backuploop",
            "adobeupdateservice", "creativecloud", "acrotray", "code", "devenv", "rider64",
            "photoshop", "illustrator", "obs64", "notion", "figma", "postman"
        };

        public static bool IsCritical(Process process)
        {
            if (Critical.Contains(process.ProcessName))
                return true;
            try
            {
                return process.Id <= 4 || process.SessionId == 0;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public static bool IsRecommended(Process process)
        {
            return Recommended.Contains(process.ProcessName);
        }
    }
}

using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace OpenWASD
{
    /// <summary>Registers the app in the per-user Run key so remapping resumes after a reboot.</summary>
    public static class Startup
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "OpenWASD";

        public static bool IsEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
            {
                return key != null && key.GetValue(ValueName) != null;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (key == null)
                    return;
                if (enabled)
                    key.SetValue(ValueName, "\"" + Application.ExecutablePath + "\" --minimized");
                else if (key.GetValue(ValueName) != null)
                    key.DeleteValue(ValueName);
            }
        }
    }
}

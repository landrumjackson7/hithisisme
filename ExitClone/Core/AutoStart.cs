using System;
using System.Reflection;
using Microsoft.Win32;

namespace ExitClone.Core
{
    /// <summary>Run-at-logon registration under HKCU.</summary>
    public static class AutoStart
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "ExitClone";

        public static bool IsEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RunKey))
                    return key?.GetValue(ValueName) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void Set(bool enabled)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(RunKey))
                {
                    if (key == null) return;
                    if (enabled) key.SetValue(ValueName, "\"" + Assembly.GetExecutingAssembly().Location + "\" --minimized");
                    else if (key.GetValue(ValueName) != null) key.DeleteValue(ValueName);
                }
            }
            catch (Exception)
            {
                // Registry access can be blocked by policy; the setting simply will not stick.
            }
        }
    }
}

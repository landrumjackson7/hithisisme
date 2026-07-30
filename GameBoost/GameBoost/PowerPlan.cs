using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GameBoost
{
    /// <summary>Reads and switches the active Windows power scheme via powercfg.</summary>
    public static class PowerPlan
    {
        /// <summary>Built-in High performance scheme GUID.</summary>
        public const string HighPerformance = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

        /// <summary>Returns the active scheme GUID, or null when powercfg cannot be read.</summary>
        public static string GetActive()
        {
            var output = Run("/getactivescheme");
            if (output == null)
                return null;

            var match = Regex.Match(output, @"[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}");
            return match.Success ? match.Value : null;
        }

        public static bool SetActive(string schemeGuid)
        {
            if (string.IsNullOrEmpty(schemeGuid))
                return false;
            return Run("/setactive " + schemeGuid) != null;
        }

        /// <summary>Ensures the High performance scheme exists, duplicating it when an OEM removed it.</summary>
        public static bool EnsureHighPerformanceExists()
        {
            var list = Run("/list");
            if (list == null)
                return false;
            if (list.IndexOf(HighPerformance, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return Run("-duplicatescheme " + HighPerformance) != null;
        }

        private static string Run(string arguments)
        {
            try
            {
                var info = new ProcessStartInfo("powercfg.exe", arguments)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(info))
                {
                    if (process == null)
                        return null;
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);
                    return process.ExitCode == 0 ? output : null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

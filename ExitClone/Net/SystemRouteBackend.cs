using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Principal;

namespace ExitClone.Net
{
    /// <summary>
    /// Optional Windows routing-table integration: pins the game server addresses to the
    /// interface/gateway that reaches the chosen relay. Requires elevation and is reverted on
    /// disconnect.
    /// </summary>
    public class SystemRouteBackend
    {
        private readonly List<string> _added = new List<string>();

        public bool IsElevated
        {
            get
            {
                try
                {
                    using (var identity = WindowsIdentity.GetCurrent())
                        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public bool Apply(IEnumerable<string> destinationHosts, string gateway, out string error)
        {
            error = null;
            if (!IsElevated)
            {
                error = "Administrator rights are required to change the routing table.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(gateway))
            {
                error = "No relay gateway is available for system routes.";
                return false;
            }

            foreach (var host in destinationHosts.Distinct())
            {
                var address = Core.PingEngine.ResolveFirst(host);
                if (address == null || address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;
                var target = address.ToString();
                if (Run("route", string.Format("add {0} mask 255.255.255.255 {1} metric 1", target, gateway)))
                    _added.Add(target);
            }

            if (_added.Count == 0)
            {
                error = "No routes could be added.";
                return false;
            }
            return true;
        }

        public void Revert()
        {
            foreach (var target in _added.ToList())
                Run("route", "delete " + target);
            _added.Clear();
        }

        public IReadOnlyList<string> ActiveRoutes => _added;

        private static bool Run(string file, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(file, args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var process = Process.Start(psi))
                {
                    if (process == null) return false;
                    process.WaitForExit(8000);
                    return process.HasExited && process.ExitCode == 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

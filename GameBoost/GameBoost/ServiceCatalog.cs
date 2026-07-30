using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;

namespace GameBoost
{
    /// <summary>A stoppable service with a plain-English explanation of the trade-off.</summary>
    public class ServiceCandidate
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Impact { get; set; }
        public bool Running { get; set; }
    }

    /// <summary>
    /// Services that are commonly stopped while gaming. Everything here restarts
    /// on demand or at next boot, so stopping it cannot leave Windows broken.
    /// </summary>
    public static class ServiceCatalog
    {
        private static readonly Dictionary<string, string> Known = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "SysMain", "Superfetch prefetching; can cause disk spikes mid-game." },
            { "WSearch", "Windows Search indexing; heavy disk and CPU use." },
            { "wuauserv", "Windows Update; may download in the background." },
            { "BITS", "Background transfers used by updaters." },
            { "DoSvc", "Delivery Optimization peer-to-peer update traffic." },
            { "Spooler", "Print spooler; only needed when printing." },
            { "DiagTrack", "Connected User Experiences telemetry." },
            { "dmwappushservice", "WAP push message routing for telemetry." },
            { "MapsBroker", "Downloaded Maps Manager." },
            { "PcaSvc", "Program Compatibility Assistant." },
            { "TabletInputService", "Touch keyboard and handwriting panel." },
            { "RetailDemo", "Retail demo mode." },
            { "WpnService", "Windows push notifications (toast popups)." },
            { "CDPUserSvc", "Connected devices platform sync." },
            { "OneSyncSvc", "Mail, calendar and contacts sync." }
        };

        public static IEnumerable<string> DefaultNames
        {
            get { return new[] { "SysMain", "WSearch", "wuauserv", "BITS", "DoSvc", "DiagTrack", "dmwappushservice" }; }
        }

        /// <summary>Lists the catalog entries that actually exist on this machine, with live state.</summary>
        public static List<ServiceCandidate> Enumerate()
        {
            var installed = ServiceController.GetServices()
                .ToDictionary(s => s.ServiceName, StringComparer.OrdinalIgnoreCase);

            var candidates = new List<ServiceCandidate>();
            foreach (var entry in Known)
            {
                ServiceController service;
                if (!installed.TryGetValue(entry.Key, out service))
                    continue;

                candidates.Add(new ServiceCandidate
                {
                    Name = service.ServiceName,
                    DisplayName = service.DisplayName,
                    Impact = entry.Value,
                    Running = SafeStatus(service) == ServiceControllerStatus.Running
                });
            }

            return candidates.OrderBy(c => c.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        private static ServiceControllerStatus? SafeStatus(ServiceController service)
        {
            try
            {
                return service.Status;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;

namespace GameBoost
{
    /// <summary>
    /// Applies a boost and remembers exactly what it changed so the machine can be
    /// put back. Nothing is made permanent: suspended processes stay alive, stopped
    /// services keep their start type, and the previous power plan is restored.
    /// </summary>
    public class BoostEngine
    {
        private static readonly TimeSpan ServiceTimeout = TimeSpan.FromSeconds(10);

        private readonly List<int> _suspendedPids = new List<int>();
        private readonly List<string> _stoppedServices = new List<string>();
        private readonly Dictionary<int, ProcessPriorityClass> _priorityUndo =
            new Dictionary<int, ProcessPriorityClass>();

        private string _previousPowerPlan;

        public bool IsBoosted { get; private set; }

        public SystemLoad LoadBeforeBoost { get; private set; }

        /// <summary>Raised for every step so the UI can show a live log.</summary>
        public event Action<string> Logged;

        public void Boost(BoostSettings settings)
        {
            if (IsBoosted)
                return;

            LoadBeforeBoost = SystemInfo.Read();
            Log(string.Format("Baseline: {0:0.0} GB free, {1} processes.",
                LoadBeforeBoost.AvailableGigabytes, LoadBeforeBoost.ProcessCount));

            if (settings.HighPerformancePowerPlan)
                ApplyPowerPlan();

            if (settings.StopServices)
                StopServices(settings.ServiceList);

            if (settings.SuspendProcesses)
                SuspendProcesses(settings.SuspendList);

            if (settings.TrimWorkingSets)
                TrimWorkingSets(settings.SuspendList);

            if (settings.ClearTempFiles)
                ClearTempFiles();

            if (settings.PurgeStandbyMemory)
                Log(SystemInfo.PurgeStandbyMemory()
                    ? "Purged the standby memory list."
                    : "Could not purge standby memory (needs administrator).");

            if (!string.IsNullOrWhiteSpace(settings.GameProcessName))
                RaiseGamePriority(settings.GameProcessName);

            IsBoosted = true;

            var after = SystemInfo.Read();
            Log(string.Format("Boosted: {0:0.0} GB free (freed {1:0.0} GB), {2} processes.",
                after.AvailableGigabytes,
                after.AvailableGigabytes - LoadBeforeBoost.AvailableGigabytes,
                after.ProcessCount));
        }

        public void Restore()
        {
            if (!IsBoosted)
                return;

            foreach (var pid in _suspendedPids.ToList())
            {
                if (TryChangeSuspension(pid, false))
                    Log("Resumed PID " + pid + ".");
                else
                    Log("PID " + pid + " already exited.");
            }
            _suspendedPids.Clear();

            foreach (var name in _stoppedServices.ToList())
                StartService(name);
            _stoppedServices.Clear();

            foreach (var entry in _priorityUndo.ToList())
            {
                try
                {
                    using (var process = Process.GetProcessById(entry.Key))
                        process.PriorityClass = entry.Value;
                }
                catch (Exception)
                {
                    // The game closed; nothing to undo.
                }
            }
            _priorityUndo.Clear();

            if (_previousPowerPlan != null)
            {
                Log(PowerPlan.SetActive(_previousPowerPlan)
                    ? "Restored the previous power plan."
                    : "Could not restore the previous power plan.");
                _previousPowerPlan = null;
            }

            IsBoosted = false;
            Log("Everything is back to how it was.");
        }

        /// <summary>Background processes the user could choose to suspend, newest heavy hitters first.</summary>
        public static List<Process> CandidateProcesses()
        {
            var current = Process.GetCurrentProcess().Id;
            return Process.GetProcesses()
                .Where(p =>
                {
                    try
                    {
                        return p.Id != current && !SafeList.IsCritical(p);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                })
                .GroupBy(p => p.ProcessName, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(WorkingSet).First())
                .OrderByDescending(WorkingSet)
                .ToList();
        }

        public static long WorkingSet(Process process)
        {
            try
            {
                return process.WorkingSet64;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private void ApplyPowerPlan()
        {
            _previousPowerPlan = PowerPlan.GetActive();
            if (_previousPowerPlan == null)
            {
                Log("Could not read the active power plan; leaving it alone.");
                return;
            }

            if (string.Equals(_previousPowerPlan, PowerPlan.HighPerformance, StringComparison.OrdinalIgnoreCase))
            {
                Log("Already on High performance.");
                _previousPowerPlan = null;
                return;
            }

            PowerPlan.EnsureHighPerformanceExists();
            if (PowerPlan.SetActive(PowerPlan.HighPerformance))
                Log("Switched to the High performance power plan.");
            else
            {
                Log("Could not switch power plan (needs administrator).");
                _previousPowerPlan = null;
            }
        }

        private void SuspendProcesses(IEnumerable<string> names)
        {
            var wanted = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
            if (wanted.Count == 0)
            {
                Log("No processes selected to suspend.");
                return;
            }

            foreach (var process in Process.GetProcesses())
            {
                if (!wanted.Contains(process.ProcessName) || SafeList.IsCritical(process))
                    continue;

                if (TryChangeSuspension(process.Id, true))
                {
                    _suspendedPids.Add(process.Id);
                    Log(string.Format("Suspended {0} (PID {1}, {2:0} MB).",
                        process.ProcessName, process.Id, WorkingSet(process) / 1024.0 / 1024.0));
                }
                else
                {
                    Log("Could not suspend " + process.ProcessName + " (access denied).");
                }
            }
        }

        private void TrimWorkingSets(IEnumerable<string> names)
        {
            var wanted = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
            var trimmed = 0;
            foreach (var process in Process.GetProcesses())
            {
                if (SafeList.IsCritical(process) || !wanted.Contains(process.ProcessName))
                    continue;

                var handle = Native.OpenProcess(Native.ProcessSetQuota | Native.ProcessQueryLimitedInformation,
                    false, process.Id);
                if (handle == IntPtr.Zero)
                    continue;

                try
                {
                    if (Native.EmptyWorkingSet(handle))
                        trimmed++;
                }
                finally
                {
                    Native.CloseHandle(handle);
                }
            }

            Log("Trimmed the working set of " + trimmed + " process(es).");
        }

        private void StopServices(IEnumerable<string> names)
        {
            foreach (var name in names)
            {
                try
                {
                    using (var service = new ServiceController(name))
                    {
                        if (service.Status != ServiceControllerStatus.Running)
                            continue;
                        if (!service.CanStop)
                        {
                            Log(name + " cannot be stopped on demand.");
                            continue;
                        }

                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, ServiceTimeout);
                        _stoppedServices.Add(name);
                        Log("Stopped service " + name + ".");
                    }
                }
                catch (Exception ex)
                {
                    Log("Could not stop " + name + ": " + ex.Message);
                }
            }
        }

        private void StartService(string name)
        {
            try
            {
                using (var service = new ServiceController(name))
                {
                    if (service.Status == ServiceControllerStatus.Running)
                        return;
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running, ServiceTimeout);
                    Log("Restarted service " + name + ".");
                }
            }
            catch (Exception ex)
            {
                Log("Could not restart " + name + ": " + ex.Message);
            }
        }

        private void RaiseGamePriority(string processName)
        {
            var trimmed = processName.Trim();
            if (trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(0, trimmed.Length - 4);

            var matches = Process.GetProcessesByName(trimmed);
            if (matches.Length == 0)
            {
                Log("Game process \"" + trimmed + "\" is not running yet.");
                return;
            }

            foreach (var process in matches)
            {
                try
                {
                    _priorityUndo[process.Id] = process.PriorityClass;
                    process.PriorityClass = ProcessPriorityClass.High;
                    Log("Raised " + process.ProcessName + " to High priority.");
                }
                catch (Exception ex)
                {
                    Log("Could not change priority of " + process.ProcessName + ": " + ex.Message);
                }
            }
        }

        private void ClearTempFiles()
        {
            var freed = 0L;
            var deleted = 0;
            foreach (var directory in new[] { Path.GetTempPath(), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp") })
            {
                if (!Directory.Exists(directory))
                    continue;

                foreach (var file in SafeEnumerate(directory))
                {
                    try
                    {
                        var length = file.Length;
                        file.Delete();
                        freed += length;
                        deleted++;
                    }
                    catch (Exception)
                    {
                        // In use by another process; skip it.
                    }
                }
            }

            Log(string.Format("Deleted {0} temp file(s), {1:0.0} MB.", deleted, freed / 1024.0 / 1024.0));
        }

        private static IEnumerable<FileInfo> SafeEnumerate(string directory)
        {
            try
            {
                return new DirectoryInfo(directory).GetFiles("*", SearchOption.TopDirectoryOnly);
            }
            catch (Exception)
            {
                return Enumerable.Empty<FileInfo>();
            }
        }

        private static bool TryChangeSuspension(int pid, bool suspend)
        {
            var handle = Native.OpenProcess(Native.ProcessSuspendResume, false, pid);
            if (handle == IntPtr.Zero)
                return false;

            try
            {
                var status = suspend ? Native.NtSuspendProcess(handle) : Native.NtResumeProcess(handle);
                return status == 0;
            }
            finally
            {
                Native.CloseHandle(handle);
            }
        }

        private void Log(string message)
        {
            var handler = Logged;
            if (handler != null)
                handler(message);
        }
    }
}

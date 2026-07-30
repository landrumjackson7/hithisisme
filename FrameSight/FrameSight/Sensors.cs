using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace FrameSight
{
    /// <summary>Everything the overlay can display, in the order it is offered in the UI.</summary>
    public enum SensorKind
    {
        CpuLoad,
        CpuClock,
        RamUsedGb,
        RamPercent,
        GpuLoad,
        GpuMemoryMb,
        DiskActivity,
        NetworkDown,
        NetworkUp,
        ProcessCount,
        Uptime,
        ForegroundApp,
        Clock
    }

    /// <summary>One reading: a label, a formatted value, and 0..1 for the bar graph.</summary>
    public class Reading
    {
        public SensorKind Kind { get; set; }
        public string Label { get; set; }
        public string Value { get; set; }
        public double Fraction { get; set; }
        public bool HasBar { get; set; }
    }

    /// <summary>
    /// Samples Windows performance counters and WMI-free APIs on a background-safe
    /// path. Counters that a machine does not expose (older GPU drivers have no
    /// "GPU Engine" category) are dropped instead of throwing.
    /// </summary>
    public class SensorHub : IDisposable
    {
        private readonly Dictionary<SensorKind, PerformanceCounter> _counters =
            new Dictionary<SensorKind, PerformanceCounter>();

        private readonly Dictionary<string, PerformanceCounter> _instanceCounters =
            new Dictionary<string, PerformanceCounter>();

        private readonly ulong _totalRamBytes;
        private PerformanceCounterCategory _gpuEngine;
        private PerformanceCounterCategory _gpuMemory;
        private long _baseCpuMhz;

        public SensorHub()
        {
            _totalRamBytes = ReadTotalRam();
            TryAddCounter(SensorKind.CpuLoad, "Processor Information", "% Processor Utility", "_Total");
            if (!_counters.ContainsKey(SensorKind.CpuLoad))
                TryAddCounter(SensorKind.CpuLoad, "Processor", "% Processor Time", "_Total");
            TryAddCounter(SensorKind.CpuClock, "Processor Information", "% Processor Performance", "_Total");
            TryAddCounter(SensorKind.DiskActivity, "PhysicalDisk", "% Disk Time", "_Total");
            TryAddCounter(SensorKind.ProcessCount, "System", "Processes", null);

            _gpuEngine = TryGetCategory("GPU Engine");
            _gpuMemory = TryGetCategory("GPU Process Memory");
            _baseCpuMhz = ReadBaseCpuMhz();
        }

        /// <summary>Sensors that produced a usable value on this machine.</summary>
        public bool IsSupported(SensorKind kind)
        {
            switch (kind)
            {
                case SensorKind.GpuLoad:
                    return _gpuEngine != null;
                case SensorKind.GpuMemoryMb:
                    return _gpuMemory != null;
                case SensorKind.CpuClock:
                    return _counters.ContainsKey(SensorKind.CpuClock) && _baseCpuMhz > 0;
                case SensorKind.RamUsedGb:
                case SensorKind.RamPercent:
                    return _totalRamBytes > 0;
                case SensorKind.NetworkDown:
                case SensorKind.NetworkUp:
                case SensorKind.Uptime:
                case SensorKind.ForegroundApp:
                case SensorKind.Clock:
                    return true;
                default:
                    return _counters.ContainsKey(kind);
            }
        }

        public Reading Read(SensorKind kind)
        {
            switch (kind)
            {
                case SensorKind.CpuLoad:
                {
                    var value = Math.Min(100, NextValue(SensorKind.CpuLoad));
                    return Percent(kind, "CPU", value);
                }
                case SensorKind.CpuClock:
                {
                    var performance = NextValue(SensorKind.CpuClock);
                    var mhz = _baseCpuMhz * performance / 100.0;
                    return new Reading
                    {
                        Kind = kind,
                        Label = "CPU clock",
                        Value = (mhz / 1000.0).ToString("0.00") + " GHz"
                    };
                }
                case SensorKind.RamUsedGb:
                {
                    var status = ReadMemoryStatus();
                    var usedGb = (status.TotalPhys - status.AvailPhys) / 1073741824.0;
                    var totalGb = status.TotalPhys / 1073741824.0;
                    return new Reading
                    {
                        Kind = kind,
                        Label = "RAM",
                        Value = usedGb.ToString("0.0") + " / " + totalGb.ToString("0.0") + " GB",
                        Fraction = totalGb <= 0 ? 0 : usedGb / totalGb,
                        HasBar = true
                    };
                }
                case SensorKind.RamPercent:
                {
                    var status = ReadMemoryStatus();
                    return Percent(kind, "RAM", status.MemoryLoad);
                }
                case SensorKind.GpuLoad:
                    return Percent(kind, "GPU", Math.Min(100, SumInstances(_gpuEngine, "Utilization Percentage", false)));
                case SensorKind.GpuMemoryMb:
                {
                    var bytes = SumInstances(_gpuMemory, "Dedicated Usage", true);
                    return new Reading
                    {
                        Kind = kind,
                        Label = "GPU mem",
                        Value = (bytes / 1048576.0).ToString("0") + " MB"
                    };
                }
                case SensorKind.DiskActivity:
                    return Percent(kind, "Disk", Math.Min(100, NextValue(SensorKind.DiskActivity)));
                case SensorKind.NetworkDown:
                    return Rate(kind, "Net down", NetworkRates.Sample().DownBytesPerSecond);
                case SensorKind.NetworkUp:
                    return Rate(kind, "Net up", NetworkRates.Sample().UpBytesPerSecond);
                case SensorKind.ProcessCount:
                    return new Reading
                    {
                        Kind = kind,
                        Label = "Processes",
                        Value = ((int)NextValue(SensorKind.ProcessCount)).ToString()
                    };
                case SensorKind.Uptime:
                {
                    var uptime = TimeSpan.FromMilliseconds(Environment.TickCount & int.MaxValue);
                    return new Reading
                    {
                        Kind = kind,
                        Label = "Uptime",
                        Value = string.Format("{0}h {1:00}m", (int)uptime.TotalHours, uptime.Minutes)
                    };
                }
                case SensorKind.ForegroundApp:
                    return new Reading { Kind = kind, Label = "Focus", Value = ForegroundProcessName() };
                default:
                    return new Reading
                    {
                        Kind = kind,
                        Label = "Clock",
                        Value = DateTime.Now.ToString("HH:mm:ss")
                    };
            }
        }

        private static Reading Percent(SensorKind kind, string label, double value)
        {
            return new Reading
            {
                Kind = kind,
                Label = label,
                Value = value.ToString("0") + " %",
                Fraction = Math.Max(0, Math.Min(1, value / 100.0)),
                HasBar = true
            };
        }

        private static Reading Rate(SensorKind kind, string label, double bytesPerSecond)
        {
            var megabits = bytesPerSecond * 8 / 1000000.0;
            return new Reading
            {
                Kind = kind,
                Label = label,
                Value = megabits < 10 ? megabits.ToString("0.00") + " Mb/s" : megabits.ToString("0.0") + " Mb/s"
            };
        }

        private double NextValue(SensorKind kind)
        {
            PerformanceCounter counter;
            if (!_counters.TryGetValue(kind, out counter))
                return 0;

            try
            {
                return counter.NextValue();
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// GPU counters are per-engine and per-process, so a machine reports dozens of
        /// instances and the meaningful figure is their sum.
        ///
        /// Rate counters (utilisation) only yield a value from the delta between two
        /// samples, so their instances are cached across ticks — a freshly constructed
        /// counter always reports 0. Gauges (memory) are read raw.
        /// </summary>
        private double SumInstances(PerformanceCounterCategory category, string counterName, bool isGauge)
        {
            if (category == null)
                return 0;

            var total = 0.0;
            try
            {
                foreach (var instance in category.GetInstanceNames())
                {
                    try
                    {
                        if (isGauge)
                        {
                            foreach (var counter in category.GetCounters(instance)
                                .Where(c => c.CounterName == counterName))
                            {
                                using (counter)
                                    total += counter.RawValue;
                            }
                            continue;
                        }

                        total += CachedRate(category.CategoryName, counterName, instance);
                    }
                    catch (Exception)
                    {
                        // Instance vanished between enumeration and read.
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }

            return total;
        }

        private double CachedRate(string category, string counterName, string instance)
        {
            var key = category + "|" + counterName + "|" + instance;

            PerformanceCounter counter;
            if (!_instanceCounters.TryGetValue(key, out counter))
            {
                counter = new PerformanceCounter(category, counterName, instance, true);
                counter.NextValue();
                _instanceCounters[key] = counter;
                // First read primes the delta; a value arrives on the next tick.
                return 0;
            }

            return counter.NextValue();
        }

        private void TryAddCounter(SensorKind kind, string category, string counter, string instance)
        {
            try
            {
                var performanceCounter = instance == null
                    ? new PerformanceCounter(category, counter, true)
                    : new PerformanceCounter(category, counter, instance, true);
                performanceCounter.NextValue();
                _counters[kind] = performanceCounter;
            }
            catch (Exception)
            {
                // Counter category missing or disabled on this machine.
            }
        }

        private static PerformanceCounterCategory TryGetCategory(string name)
        {
            try
            {
                return PerformanceCounterCategory.Exists(name)
                    ? new PerformanceCounterCategory(name)
                    : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static long ReadBaseCpuMhz()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                {
                    if (key == null)
                        return 0;
                    var value = key.GetValue("~MHz");
                    return value == null ? 0 : Convert.ToInt64(value);
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static ulong ReadTotalRam()
        {
            return ReadMemoryStatus().TotalPhys;
        }

        private static MemoryStatusEx ReadMemoryStatus()
        {
            var status = new MemoryStatusEx();
            status.Length = (uint)Marshal.SizeOf(typeof(MemoryStatusEx));
            GlobalMemoryStatusEx(ref status);
            return status;
        }

        private static string ForegroundProcessName()
        {
            try
            {
                var window = GetForegroundWindow();
                if (window == IntPtr.Zero)
                    return "—";

                uint pid;
                GetWindowThreadProcessId(window, out pid);
                using (var process = Process.GetProcessById((int)pid))
                    return process.ProcessName;
            }
            catch (Exception)
            {
                return "—";
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;
        }

        [DllImport("kernel32.dll")]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        public void Dispose()
        {
            foreach (var counter in _counters.Values)
                counter.Dispose();
            _counters.Clear();

            foreach (var counter in _instanceCounters.Values)
                counter.Dispose();
            _instanceCounters.Clear();
        }
    }

    /// <summary>Turns cumulative NIC byte totals into per-second rates.</summary>
    internal static class NetworkRates
    {
        public struct Rates
        {
            public double DownBytesPerSecond;
            public double UpBytesPerSecond;
        }

        private static long _lastReceived;
        private static long _lastSent;
        private static DateTime _lastSampleUtc;
        private static Rates _cached;

        public static Rates Sample()
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastSampleUtc).TotalSeconds;

            // The overlay asks for down and up separately; reuse one sample per tick.
            if (elapsed < 0.25)
                return _cached;

            long received = 0;
            long sent = 0;
            try
            {
                foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                        continue;
                    var statistics = nic.GetIPStatistics();
                    received += statistics.BytesReceived;
                    sent += statistics.BytesSent;
                }
            }
            catch (Exception)
            {
                return _cached;
            }

            if (_lastSampleUtc != default(DateTime) && elapsed > 0)
            {
                _cached = new Rates
                {
                    DownBytesPerSecond = Math.Max(0, (received - _lastReceived) / elapsed),
                    UpBytesPerSecond = Math.Max(0, (sent - _lastSent) / elapsed)
                };
            }

            _lastReceived = received;
            _lastSent = sent;
            _lastSampleUtc = now;
            return _cached;
        }
    }
}

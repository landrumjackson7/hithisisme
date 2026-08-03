using System;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace TweaksForAll
{
    // Lightweight live metrics + one-shot hardware spec lookups.
    internal sealed class Telemetry : IDisposable
    {
        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();

        private PerformanceCounter _cpu;
        private PerformanceCounter _disk;

        public float CpuPercent { get; private set; }
        public float DiskPercent { get; private set; }
        public float MemoryPercent { get; private set; }
        public float GpuPercent { get; private set; }
        public ulong TotalMemMB { get; private set; }
        public ulong UsedMemMB { get; private set; }

        public string Cpu = "Unknown CPU";
        public string Gpu = "Unknown GPU";
        public string Ram = "Unknown RAM";
        public string Board = "Unknown board";
        public string Os = "Windows";
        public int Cores, Threads;

        public Telemetry()
        {
            try { _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total"); _cpu.NextValue(); } catch { }
            try { _disk = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total"); _disk.NextValue(); } catch { }
            LoadSpecs();
        }

        public string Uptime
        {
            get
            {
                try { var ts = TimeSpan.FromMilliseconds(GetTickCount64()); return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}"; }
                catch { return "--:--:--"; }
            }
        }

        public int ProcessCount { get { try { return Process.GetProcesses().Length; } catch { return 0; } } }

        public void Sample()
        {
            try { if (_cpu != null) CpuPercent = _cpu.NextValue(); } catch { }
            try { if (_disk != null) DiskPercent = Math.Min(100f, _disk.NextValue()); } catch { }
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT FreePhysicalMemory,TotalVisibleMemorySize FROM Win32_OperatingSystem"))
                    foreach (ManagementObject o in s.Get())
                    {
                        double total = Convert.ToDouble(o["TotalVisibleMemorySize"]); // KB
                        double free = Convert.ToDouble(o["FreePhysicalMemory"]);
                        TotalMemMB = (ulong)(total / 1024);
                        UsedMemMB = (ulong)((total - free) / 1024);
                        if (total > 0) MemoryPercent = (float)((total - free) / total * 100.0);
                    }
            }
            catch { }
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT UtilizationPercentage FROM Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine"))
                {
                    float sum = 0;
                    foreach (ManagementObject o in s.Get())
                        sum += Convert.ToSingle(o["UtilizationPercentage"]);
                    if (sum > 0) GpuPercent = Math.Min(100f, sum);
                }
            }
            catch { }
        }

        private void LoadSpecs()
        {
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Name,NumberOfCores,NumberOfLogicalProcessors FROM Win32_Processor"))
                    foreach (ManagementObject o in s.Get())
                    {
                        Cpu = (o["Name"]?.ToString() ?? Cpu).Trim();
                        Cores = Convert.ToInt32(o["NumberOfCores"]);
                        Threads = Convert.ToInt32(o["NumberOfLogicalProcessors"]);
                    }
            }
            catch { }
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                    foreach (ManagementObject o in s.Get()) { Gpu = o["Name"]?.ToString() ?? Gpu; break; }
            }
            catch { }
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Capacity,Speed FROM Win32_PhysicalMemory"))
                {
                    ulong bytes = 0; uint speed = 0;
                    foreach (ManagementObject o in s.Get()) { bytes += Convert.ToUInt64(o["Capacity"]); try { speed = Convert.ToUInt32(o["Speed"]); } catch { } }
                    if (bytes > 0) Ram = $"{bytes / 1024 / 1024 / 1024} GB" + (speed > 0 ? $"  {speed} MHz" : "");
                }
            }
            catch { }
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Manufacturer,Product FROM Win32_BaseBoard"))
                    foreach (ManagementObject o in s.Get()) { Board = $"{o["Manufacturer"]} {o["Product"]}".Trim(); break; }
            }
            catch { }
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                    foreach (ManagementObject o in s.Get()) { Os = o["Caption"]?.ToString() ?? Os; break; }
            }
            catch { }
        }

        public void Dispose()
        {
            _cpu?.Dispose();
            _disk?.Dispose();
        }
    }
}

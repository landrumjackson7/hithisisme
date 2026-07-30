using System;
using System.Diagnostics;

namespace GameBoost
{
    /// <summary>Snapshot of the machine's load, shown before and after a boost.</summary>
    public struct SystemLoad
    {
        public ulong TotalPhysicalBytes;
        public ulong AvailablePhysicalBytes;
        public int ProcessCount;

        public double MemoryUsedPercent
        {
            get
            {
                if (TotalPhysicalBytes == 0)
                    return 0;
                return 100.0 * (TotalPhysicalBytes - AvailablePhysicalBytes) / TotalPhysicalBytes;
            }
        }

        public double AvailableGigabytes
        {
            get { return AvailablePhysicalBytes / 1024.0 / 1024.0 / 1024.0; }
        }
    }

    public static class SystemInfo
    {
        public static SystemLoad Read()
        {
            var status = new Native.MemoryStatusEx();
            status.Length = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.MemoryStatusEx));

            var load = new SystemLoad();
            if (Native.GlobalMemoryStatusEx(ref status))
            {
                load.TotalPhysicalBytes = status.TotalPhys;
                load.AvailablePhysicalBytes = status.AvailPhys;
            }

            try
            {
                load.ProcessCount = Process.GetProcesses().Length;
            }
            catch (Exception)
            {
                load.ProcessCount = 0;
            }

            return load;
        }

        /// <summary>Purges the standby memory list. Requires elevation.</summary>
        public static bool PurgeStandbyMemory()
        {
            if (!Native.EnablePrivilege("SeProfileSingleProcessPrivilege"))
                return false;

            var command = Native.MemoryPurgeStandbyList;
            return Native.NtSetSystemInformation(
                Native.SystemMemoryListInformationClass, ref command, sizeof(int)) == 0;
        }
    }
}

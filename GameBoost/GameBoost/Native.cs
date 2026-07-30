using System;
using System.Runtime.InteropServices;

namespace GameBoost
{
    /// <summary>P/Invoke surface used to suspend processes and trim memory.</summary>
    internal static class Native
    {
        public const int ProcessSuspendResume = 0x0800;
        public const int ProcessSetQuota = 0x0100;
        public const int ProcessQueryLimitedInformation = 0x1000;

        /// <summary>SystemMemoryListInformation.</summary>
        public const int SystemMemoryListInformationClass = 0x0050;

        /// <summary>MemoryPurgeStandbyList command for SystemMemoryListInformation.</summary>
        public const int MemoryPurgeStandbyList = 4;

        [StructLayout(LayoutKind.Sequential)]
        public struct MemoryStatusEx
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

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(int access, bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool EmptyWorkingSet(IntPtr processHandle);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtSuspendProcess(IntPtr processHandle);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtResumeProcess(IntPtr processHandle);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtSetSystemInformation(int informationClass, ref int information, int length);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr processHandle, int access, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LookupPrivilegeValue(string systemName, string privilegeName, out long luid);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAll,
            ref TokenPrivileges newState, int bufferLength, IntPtr previousState, IntPtr returnLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct TokenPrivileges
        {
            public int PrivilegeCount;
            public long Luid;
            public int Attributes;
        }

        public const int TokenAdjustPrivileges = 0x0020;
        public const int TokenQuery = 0x0008;
        public const int SePrivilegeEnabled = 0x0002;

        /// <summary>Enables a privilege on the current process token, e.g. SeProfileSingleProcessPrivilege.</summary>
        public static bool EnablePrivilege(string name)
        {
            IntPtr token;
            var process = System.Diagnostics.Process.GetCurrentProcess().Handle;
            if (!OpenProcessToken(process, TokenAdjustPrivileges | TokenQuery, out token))
                return false;

            try
            {
                long luid;
                if (!LookupPrivilegeValue(null, name, out luid))
                    return false;

                var privileges = new TokenPrivileges
                {
                    PrivilegeCount = 1,
                    Luid = luid,
                    Attributes = SePrivilegeEnabled
                };
                return AdjustTokenPrivileges(token, false, ref privileges,
                    Marshal.SizeOf(typeof(TokenPrivileges)), IntPtr.Zero, IntPtr.Zero);
            }
            finally
            {
                CloseHandle(token);
            }
        }
    }
}

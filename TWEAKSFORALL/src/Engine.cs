using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using Microsoft.Win32;

namespace TweaksForAll
{
    // A single reversible optimization.
    internal sealed class Tweak
    {
        public string Category;
        public string Name;
        public string Desc;
        public Action Apply;
        public Action Revert;       // may be null
        public string Warning;      // may be null
        public bool DefaultOn;      // pre-checked in the page
        public bool Maximum;        // included in the one-click MAXIMUM run
    }

    // Low-level primitives that actually change the system.
    internal static class Engine
    {
        public static void Reg(string path, string name, object dword, string sval)
        {
            try
            {
                string[] parts = path.Split('\\');
                RegistryKey root = parts[0].StartsWith("HKLM") ? Registry.LocalMachine : Registry.CurrentUser;
                string sub = string.Join("\\", parts, 1, parts.Length - 1);
                using (RegistryKey k = root.CreateSubKey(sub))
                {
                    if (k == null) return;
                    if (sval != null) k.SetValue(name, sval, RegistryValueKind.String);
                    else if (dword is byte[] bytes) k.SetValue(name, bytes, RegistryValueKind.Binary);
                    else if (dword is ulong ql) k.SetValue(name, ql, RegistryValueKind.QWord);
                    else if (dword is uint u) k.SetValue(name, unchecked((int)u), RegistryValueKind.DWord);
                    else k.SetValue(name, Convert.ToInt32(dword), RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        public static void RegDel(string path, string name)
        {
            try
            {
                string[] parts = path.Split('\\');
                RegistryKey root = parts[0].StartsWith("HKLM") ? Registry.LocalMachine : Registry.CurrentUser;
                string sub = string.Join("\\", parts, 1, parts.Length - 1);
                using (RegistryKey k = root.OpenSubKey(sub, true))
                    k?.DeleteValue(name, false);
            }
            catch { }
        }

        public static void Run(string file, string args)
        {
            try
            {
                Process.Start(new ProcessStartInfo(file, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                })?.WaitForExit(30000);
            }
            catch { }
        }

        public static void RunPS(string cmd)
        {
            Run("powershell", "-NoProfile -ExecutionPolicy Bypass -Command \"" + cmd.Replace("\"", "'") + "\"");
        }

        public static void SvcDisable(string name)
        {
            Run("sc", "config " + name + " start= disabled");
            Run("sc", "stop " + name);
        }

        public static void SvcAuto(string name) => Run("sc", "config " + name + " start= auto");
        public static void SvcManual(string name) => Run("sc", "config " + name + " start= demand");
        public static void DisableTask(string name) => Run("schtasks", "/Change /TN \"" + name + "\" /Disable");

        public static void CleanDir(string dir)
        {
            try
            {
                foreach (string f in Directory.GetFiles(dir))
                {
                    try { File.Delete(f); } catch { }
                }
                foreach (string d in Directory.GetDirectories(dir))
                {
                    try { Directory.Delete(d, true); } catch { }
                }
            }
            catch { }
        }

        // Best-effort System Restore checkpoint. Returns true if it reports success.
        public static bool CreateRestorePoint(string label)
        {
            try
            {
                Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\SystemRestore",
                    "SystemRestorePointCreationFrequency", 0u, null);
            }
            catch { }
            try { RunPS("Enable-ComputerRestore -Drive \"$env:SystemDrive\\\" -EA 0"); }
            catch { }
            try
            {
                var cls = new ManagementClass("root\\default", "SystemRestore", null);
                uint rc = Convert.ToUInt32(cls.InvokeMethod("CreateRestorePoint", new object[] { label, 12, 100 }));
                return rc == 0;
            }
            catch { return false; }
        }
    }
}

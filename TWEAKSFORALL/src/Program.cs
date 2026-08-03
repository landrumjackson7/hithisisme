using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace TweaksForAll
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // Relaunch elevated if not running as administrator.
            if (Environment.OSVersion.Platform == PlatformID.Win32NT &&
                !new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Application.ExecutablePath,
                        UseShellExecute = true,
                        Verb = "runas",
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                    });
                    return;
                }
                catch
                {
                    MessageBox.Show("TWEAKSFORALL needs administrator rights to tune your PC.",
                        "Administrator required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Fatal error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

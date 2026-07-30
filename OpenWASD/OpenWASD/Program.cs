using System;
using System.Linq;
using System.Windows.Forms;

namespace OpenWASD
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var startMinimized = args.Any(a =>
                string.Equals(a, "--minimized", StringComparison.OrdinalIgnoreCase));

            Application.Run(new MainForm(startMinimized));
        }
    }
}

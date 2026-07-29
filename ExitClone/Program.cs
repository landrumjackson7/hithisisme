using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using ExitClone.Core;
using ExitClone.UI;

namespace ExitClone
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbedded;
            using (var single = new Mutex(true, "ExitClone.SingleInstance", out bool isFirst))
            {
                if (!isFirst)
                {
                    MessageBox.Show("ExitClone is already running.", "ExitClone");
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += (s, e) =>
                    MessageBox.Show(e.Exception.Message, "ExitClone", MessageBoxButtons.OK, MessageBoxIcon.Error);

                var settings = SettingsStore.Load();
                if (string.IsNullOrEmpty(settings.AccountName))
                {
                    using (var login = new LoginForm())
                    {
                        if (login.ShowDialog() != DialogResult.OK) return;
                        settings.AccountName = login.AccountName;
                        SettingsStore.Save(settings);
                    }
                }

                bool minimized = args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase));
                Application.Run(new MainForm(minimized));

                GC.KeepAlive(single);
            }
        }

        private static Assembly ResolveEmbedded(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name + ".dll";
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (stream == null) return null;
                var raw = new byte[stream.Length];
                stream.Read(raw, 0, raw.Length);
                return Assembly.Load(raw);
            }
        }
    }
}

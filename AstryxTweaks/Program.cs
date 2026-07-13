using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace AstryxTweaks;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		if (Environment.OSVersion.Platform == PlatformID.Win32NT && !new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
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
				MessageBox.Show("This app requires administrator privileges.", "Admin Required", (MessageBoxButtons)0, (MessageBoxIcon)48);
				return;
			}
		}
		try
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run((Form)(object)new MainForm());
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.ToString(), "Fatal Error", (MessageBoxButtons)0, (MessageBoxIcon)16);
		}
	}
}

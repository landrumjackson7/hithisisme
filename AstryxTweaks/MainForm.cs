using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.Layout;
using Microsoft.Win32;

namespace AstryxTweaks;

public partial class MainForm : Form
{
	private sealed class MaximumStep
	{
		public string Name;

		public Action Apply;
	}

	private sealed class MaximumProgress
	{
		public int Current;

		public string Name;

		public string Result;
	}

	private sealed class OptStep
	{
		public string Name;

		public Action Apply;

		public Action Revert;

		public CheckBox Toggle;
	}

	private sealed class OptimizerOptions
	{
		public bool RunTalon;

		public string RestoreName;

		public int Level = 1;

		public List<OptStep> DefenderSteps = new List<OptStep>();
	}

	private static Color BG = Color.FromArgb(8, 12, 22);

	private static Color SIDEBAR = Color.FromArgb(10, 17, 31);

	private static Color ACC = Color.FromArgb(56, 132, 255);

	private static Color TXT = Color.FromArgb(244, 246, 255);

	private static Color MUTED = Color.FromArgb(132, 143, 175);

	private static Color GRN = Color.FromArgb(38, 201, 132);

	private static Color WARN = Color.FromArgb(255, 172, 54);

	private static Color ACTIVE_BG = Color.FromArgb(24, 62, 126);

	private static Color ACC2 = Color.FromArgb(146, 102, 255);

	private static Color CARD_BOT = Color.FromArgb(15, 32, 62);

	private static Font FN = new Font("Segoe UI", 9f);

	private static Font FS = new Font("Segoe UI", 8f);

	private static Font FB = new Font("Segoe UI Semibold", 9.5f);

	private static Font FH = new Font("Segoe UI Semibold", 13f);

	private static Font FICON = new Font("Segoe MDL2 Assets", 11f);

	private Dictionary<CheckBox, Action> acts = new Dictionary<CheckBox, Action>();

	private Dictionary<CheckBox, Action> revs = new Dictionary<CheckBox, Action>();

	private List<CheckBox> allCB = new List<CheckBox>();

	private Dictionary<CheckBox, Panel> tweakPages = new Dictionary<CheckBox, Panel>();

	private Dictionary<CheckBox, Control> tweakCards = new Dictionary<CheckBox, Control>();

	private Dictionary<CheckBox, string> tweakNames = new Dictionary<CheckBox, string>();

	private Dictionary<Panel, int> gridCol = new Dictionary<Panel, int>();

	private Dictionary<Panel, List<Panel>> pageRows = new Dictionary<Panel, List<Panel>>();

	private Dictionary<Panel, int> gridY = new Dictionary<Panel, int>();

	private Panel sidebar;

	private Panel indicator;

	private Label statusLbl;

	private TextBox searchBox;

	private Panel activeNav;

	private Dictionary<Panel, Panel> navMap = new Dictionary<Panel, Panel>();

	private Dictionary<Panel, string> navGroups = new Dictionary<Panel, string>();

	private List<Panel> orderedPages = new List<Panel>();

	private List<Panel> orderedNav = new List<Panel>();

	private Panel homePage;

	private Label homeVersion;

	private ActivityChart homeChart;

	private Panel homeQuick;

	private Panel homeSpacer;

	private Panel homeHero;

	private Button homeHeroBtn;

	private Label homeHeroTag;

	private Label homeUpdatesLabel;

	private Panel[] homeTop = (Panel[])(object)new Panel[3];

	private Button[] homeTopBtn = (Button[])(object)new Button[3];

	private Label[] homeTopBig = (Label[])(object)new Label[3];

	private Label[] homeTopSub = (Label[])(object)new Label[3];

	private Panel[] homeUpd = (Panel[])(object)new Panel[3];

	private Label[] homeUpdTitle = (Label[])(object)new Label[3];

	private Label[] homeUpdSub = (Label[])(object)new Label[3];

	private Panel backupPage;

	private Panel backupRows;

	private Panel defenderPage;

	private Panel powerPage;

	private Panel powerRows;

	private Panel secondaryNav;

	private Panel optimizerNav;

	private Button optimizerButton;

	private System.Windows.Forms.Timer pageAnimation;

	private System.Windows.Forms.Timer formFadeAnimation;

	private Dictionary<Control, Point> animatedPositions = new Dictionary<Control, Point>();

	private Dictionary<Control, System.Windows.Forms.Timer> colorAnimations = new Dictionary<Control, System.Windows.Forms.Timer>();

	private Dictionary<Control, System.Windows.Forms.Timer> positionAnimations = new Dictionary<Control, System.Windows.Forms.Timer>();

	private List<MaximumStep> maximumSteps = new List<MaximumStep>();

	private Label maximumStatus;

	private TextBox maximumLog;

	private Panel maximumProgressTrack;

	private Panel maximumProgressFill;

	private Button maximumButton;

	private bool maximumRunning;

	private bool fullOptRunning;

	private bool fullOptCancel;

	private string profileName = "";

	private string profileEmail = "";

	public MainForm()
	{
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Expected O, but got Unknown
		try
		{
			SetStyle((ControlStyles)139266, true);
			DoubleBuffered = true;
			((Form)this).Opacity = 0.0;
			BuildAll();
			((Form)this).FormClosing += new FormClosingEventHandler(AnimatedFormClosing);
		}
		catch (Exception ex)
		{
			MessageBox.Show("Init: " + ex.Message + "\n" + ex.StackTrace, "Error");
		}
	}

	[DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
	private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

	protected override void OnShown(EventArgs e)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		base.OnShown(e);
		try
		{
			ApplyDarkScrollbars((Control)this);
			if (formFadeAnimation != null)
			{
				formFadeAnimation.Stop();
				formFadeAnimation.Dispose();
			}
			formFadeAnimation = new System.Windows.Forms.Timer();
			formFadeAnimation.Interval = 15;
			formFadeAnimation.Tick += delegate
			{
				((Form)this).Opacity = Math.Min(1.0, ((Form)this).Opacity + 0.09);
				if (((Form)this).Opacity >= 1.0)
				{
					formFadeAnimation.Stop();
					formFadeAnimation.Dispose();
					formFadeAnimation = null;
				}
			};
			formFadeAnimation.Start();
		}
		catch
		{
		}
	}

	private void ApplyDarkScrollbars(Control root)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		bool flag = false;
		if (root is Panel)
		{
			flag = ((ScrollableControl)root).AutoScroll;
		}
		else if (root is TextBoxBase)
		{
			flag = true;
		}
		if (flag)
		{
			try
			{
				SetWindowTheme(root.Handle, "DarkMode_Explorer", null);
			}
			catch
			{
			}
		}
		foreach (Control item in (ArrangedElementCollection)root.Controls)
		{
			Control root2 = item;
			ApplyDarkScrollbars(root2);
		}
	}

	private void BuildAll()
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Expected O, but got Unknown
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Expected O, but got Unknown
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Expected O, but got Unknown
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Expected O, but got Unknown
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Expected O, but got Unknown
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Expected O, but got Unknown
		//IL_03c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cc: Expected O, but got Unknown
		//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0402: Unknown result type (might be due to invalid IL or missing references)
		//IL_0409: Expected O, but got Unknown
		//IL_043a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0441: Expected O, but got Unknown
		//IL_04c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ca: Expected O, but got Unknown
		//IL_0558: Unknown result type (might be due to invalid IL or missing references)
		//IL_055f: Expected O, but got Unknown
		//IL_05ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f4: Expected O, but got Unknown
		//IL_0850: Unknown result type (might be due to invalid IL or missing references)
		//IL_085a: Expected O, but got Unknown
		//IL_0887: Unknown result type (might be due to invalid IL or missing references)
		//IL_088e: Expected O, but got Unknown
		//IL_08bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c4: Expected O, but got Unknown
		//IL_08f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_08fe: Expected O, but got Unknown
		//IL_095a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0961: Expected O, but got Unknown
		//IL_09df: Unknown result type (might be due to invalid IL or missing references)
		//IL_09e9: Expected O, but got Unknown
		//IL_0a9e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aa5: Expected O, but got Unknown
		//IL_0ad6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ae0: Expected O, but got Unknown
		//IL_0b2c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b36: Expected O, but got Unknown
		//IL_0c98: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ca2: Expected O, but got Unknown
		LoadUserPreferences();
		((Control)this).Text = "Astryx Tweaks Maximum Tweaks Edition";
		try
		{
			Icon val = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
			if (val != null)
			{
				((Form)this).Icon = val;
			}
		}
		catch
		{
		}
		((Form)this).Size = new Size(1280, 820);
		((Form)this).StartPosition = (FormStartPosition)1;
		((Form)this).WindowState = (FormWindowState)2;
		((Form)this).MaximizeBox = true;
		((Control)this).BackColor = BG;
		((Control)this).ForeColor = TXT;
		((Control)this).Font = FN;
		((Control)this).MinimumSize = new Size(950, 650);
		sidebar = new Panel();
		((Control)sidebar).Dock = (DockStyle)3;
		((Control)sidebar).Width = 224;
		((Control)sidebar).BackColor = SIDEBAR;
		((ScrollableControl)sidebar).AutoScroll = false;
		((Control)this).Controls.Add((Control)(object)sidebar);
		try
		{
			if (((Form)this).Icon != null)
			{
				PictureBox val2 = new PictureBox();
				((Control)val2).Location = new Point(16, 14);
				((Control)val2).Size = new Size(32, 32);
				val2.SizeMode = (PictureBoxSizeMode)4;
				val2.Image = (Image)new Icon(((Form)this).Icon, 32, 32).ToBitmap();
				((Control)val2).BackColor = Color.Transparent;
				((Control)sidebar).Controls.Add((Control)(object)val2);
			}
		}
		catch
		{
		}
		Panel val3 = new Panel();
		((Control)val3).Location = new Point(58, 12);
		((Control)val3).Size = new Size(158, 24);
		((Control)val3).BackColor = SIDEBAR;
		((Control)val3).Paint += (PaintEventHandler)delegate(object _ls, PaintEventArgs _le)
		{
			DrawLogoText(_le.Graphics, ((Control)val3).ClientRectangle);
		};
		((Control)sidebar).Controls.Add((Control)(object)val3);
		Label val4 = new Label();
		((Control)val4).Text = "SYSTEM OPTIMIZER";
		((Control)val4).Font = FS;
		((Control)val4).ForeColor = ACC;
		((Control)val4).Location = new Point(59, 37);
		((Control)val4).AutoSize = true;
		((Control)sidebar).Controls.Add((Control)(object)val4);
		indicator = new Panel();
		((Control)indicator).Size = new Size(3, 36);
		((Control)indicator).BackColor = ACC;
		((Control)indicator).Location = new Point(0, 72);
		((Control)sidebar).Controls.Add((Control)(object)indicator);
		string[] array = new string[18]
		{
			"General", "CPU & Power", "GPU & Graphics", "Memory", "Debloat", "Network", "Gaming", "Windows Settings", "App Controls", "Storage",
			"Repair", "Drivers", "Security", "One-Click", "Advanced", "Appearance", "Install Apps", "Tools & Recovery"
		};
		string[] array2 = new string[18]
		{
			"System", "Hardware", "Hardware", "Hardware", "System", "Privacy", "Hardware", "System", "System", "Storage",
			"Storage", "Storage", "Privacy", "Advanced", "Advanced", "System", "System", "Storage"
		};
		for (int num = 0; num < array.Length; num++)
		{
			Panel val5 = new Panel();
			((Control)val5).Dock = (DockStyle)5;
			((ScrollableControl)val5).AutoScroll = true;
			((Control)val5).BackColor = BG;
			((Control)val5).Visible = false;
			((Control)val5).Padding = new Padding(14, 6, 14, 6);
			Panel val6 = new Panel();
			((Control)val6).Dock = (DockStyle)2;
			((Control)val6).Height = 46;
			((Control)val6).BackColor = Color.FromArgb(18, 18, 28);
			((Control)val5).Controls.Add((Control)(object)val6);
			Button val7 = new Button();
			((Control)val7).Text = "Apply Selected";
			((Control)val7).Location = new Point(10, 8);
			((Control)val7).Size = new Size(120, 30);
			((ButtonBase)val7).FlatStyle = (FlatStyle)0;
			((Control)val7).BackColor = ACC;
			((Control)val7).ForeColor = Color.White;
			((ButtonBase)val7).FlatAppearance.BorderSize = 0;
			((Control)val7).Tag = val5;
			((Control)val7).Click += ApplyClick;
			((Control)val6).Controls.Add((Control)(object)val7);
			Button val8 = new Button();
			((Control)val8).Text = "Select All";
			((Control)val8).Location = new Point(140, 8);
			((Control)val8).Size = new Size(80, 30);
			((ButtonBase)val8).FlatStyle = (FlatStyle)0;
			((Control)val8).BackColor = Color.FromArgb(29, 82, 184);
			((Control)val8).ForeColor = Color.White;
			((ButtonBase)val8).FlatAppearance.BorderSize = 0;
			((Control)val8).Tag = val5;
			((Control)val8).Click += SelAllClick;
			((Control)val6).Controls.Add((Control)(object)val8);
			Button val9 = new Button();
			((Control)val9).Text = "Deselect";
			((Control)val9).Location = new Point(230, 8);
			((Control)val9).Size = new Size(80, 30);
			((ButtonBase)val9).FlatStyle = (FlatStyle)0;
			((Control)val9).BackColor = Color.FromArgb(29, 82, 184);
			((Control)val9).ForeColor = Color.White;
			((ButtonBase)val9).FlatAppearance.BorderSize = 0;
			((Control)val9).Tag = val5;
			((Control)val9).Click += DeselClick;
			((Control)val6).Controls.Add((Control)(object)val9);
			Button val10 = new Button();
			((Control)val10).Text = "Revert";
			((Control)val10).Location = new Point(320, 8);
			((Control)val10).Size = new Size(80, 30);
			((ButtonBase)val10).FlatStyle = (FlatStyle)0;
			((Control)val10).BackColor = Color.FromArgb(23, 73, 166);
			((Control)val10).ForeColor = Color.White;
			((ButtonBase)val10).FlatAppearance.BorderSize = 0;
			((Control)val10).Tag = val5;
			((Control)val10).Click += RevertClick;
			((Control)val6).Controls.Add((Control)(object)val10);
			((Control)this).Controls.Add((Control)(object)val5);
			Panel val11 = MakeNavItem(array[num], 300 + num * 40, val5);
			navMap[val11] = val5;
			navGroups[val11] = array2[num];
			orderedPages.Add(val5);
			orderedNav.Add(val11);
			if (num == 13)
			{
				optimizerNav = val11;
			}
			((Control)val11).Visible = false;
		}
		AddSidebarLabel("GENERAL", 88);
		AddSidebarAction("Home", 110, delegate
		{
			ShowHome();
		});
		AddSidebarAction("Backups", 150, delegate
		{
			ShowBackups();
		});
		AddSidebarAction("Fixes", 190, delegate
		{
			ShowGroup("Storage");
		});
		optimizerButton = AddSidebarAction("✦  Optimizer", 230, delegate
		{
			ShowOptimizer();
		});
		AddSidebarAction("⚡  Power Plan", 270, delegate
		{
			ShowPower();
		});
		AddSidebarAction("\ud83d\udee1  Windows Defender", 310, delegate
		{
			ShowDefender();
		});
		AddSidebarLabel("MAIN", 366);
		AddSidebarAction("System", 388, delegate
		{
			ShowGroup("System");
		});
		AddSidebarAction("Hardware", 428, delegate
		{
			ShowGroup("Hardware");
		});
		AddSidebarAction("Storage & Repair", 468, delegate
		{
			ShowGroup("Storage");
		});
		AddSidebarAction("Privacy", 508, delegate
		{
			ShowGroup("Privacy");
		});
		BuildSidebarUtilities();
		Panel val12 = new Panel();
		((Control)val12).Dock = (DockStyle)2;
		((Control)val12).Height = 28;
		((Control)val12).BackColor = Color.FromArgb(10, 16, 28);
		((Control)this).Controls.Add((Control)(object)val12);
		statusLbl = new Label();
		((Control)statusLbl).Dock = (DockStyle)5;
		((Control)statusLbl).ForeColor = GRN;
		statusLbl.TextAlign = (ContentAlignment)16;
		((Control)statusLbl).Text = "  Ready. Create a restore point, then apply tweaks.";
		((Control)val12).Controls.Add((Control)(object)statusLbl);
		secondaryNav = new Panel();
		((Control)secondaryNav).Location = new Point(240, 8);
		((Control)secondaryNav).Size = new Size(980, 44);
		((Control)secondaryNav).Anchor = (AnchorStyles)13;
		((Control)secondaryNav).BackColor = BG;
		((Control)secondaryNav).Visible = false;
		((Control)this).Controls.Add((Control)(object)secondaryNav);
		BuildHome();
		BuildBackups();
		BuildDefenderPage();
		BuildPowerPage();
		BuildUtilityPages();
		foreach (Panel orderedPage in orderedPages)
		{
			((Control)orderedPage).SuspendLayout();
		}
		BuildGeneral();
		BuildCPU();
		BuildGPU();
		BuildRAM();
		BuildDebloat();
		BuildNetwork();
		BuildGameMode();
		BuildRegistry();
		BuildAppTweaks();
		BuildDefrag();
		BuildRepair();
		BuildDrivers();
		BuildSecurity();
		BuildOneClick();
		BuildAdvanced();
		BuildUITweaks();
		BuildInstall();
		BuildTools();
		BuildBulkExtras();
		BuildMegaExtras();
		BuildBoosterXExtras();
		BuildDeepExtras();
		foreach (Panel orderedPage2 in orderedPages)
		{
			((Control)orderedPage2).ResumeLayout(false);
		}
		ApplyRoundedStyle((Control)this);
		StartOptimizerGlow();
		ShowHome();
	}

	private void BuildMaximumEdition()
	{
		//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Expected O, but got Unknown
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Expected O, but got Unknown
		//IL_032a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0334: Expected O, but got Unknown
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		//IL_033b: Expected O, but got Unknown
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0378: Expected O, but got Unknown
		//IL_038c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0396: Expected O, but got Unknown
		//IL_0396: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Expected O, but got Unknown
		//IL_0403: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Expected O, but got Unknown
		//IL_040d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0414: Expected O, but got Unknown
		//IL_046a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0474: Expected O, but got Unknown
		//IL_0488: Unknown result type (might be due to invalid IL or missing references)
		//IL_0492: Expected O, but got Unknown
		//IL_0492: Unknown result type (might be due to invalid IL or missing references)
		//IL_0499: Expected O, but got Unknown
		//IL_04f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_04fe: Expected O, but got Unknown
		//IL_04fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0505: Expected O, but got Unknown
		//IL_0551: Unknown result type (might be due to invalid IL or missing references)
		//IL_055b: Expected O, but got Unknown
		//IL_055b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0562: Expected O, but got Unknown
		//IL_0592: Unknown result type (might be due to invalid IL or missing references)
		//IL_059c: Expected O, but got Unknown
		//IL_05b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bb: Expected O, but got Unknown
		//IL_05bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c2: Expected O, but got Unknown
		//IL_05fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0607: Expected O, but got Unknown
		//IL_061c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0626: Expected O, but got Unknown
		//IL_0627: Unknown result type (might be due to invalid IL or missing references)
		//IL_0631: Expected O, but got Unknown
		//IL_06da: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e4: Expected O, but got Unknown
		//IL_0718: Unknown result type (might be due to invalid IL or missing references)
		//IL_0722: Expected O, but got Unknown
		//IL_0723: Unknown result type (might be due to invalid IL or missing references)
		//IL_072d: Expected O, but got Unknown
		//IL_077d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0787: Expected O, but got Unknown
		//IL_0788: Unknown result type (might be due to invalid IL or missing references)
		//IL_0792: Expected O, but got Unknown
		//IL_07e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_07ed: Expected O, but got Unknown
		//IL_07ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_07f8: Expected O, but got Unknown
		//IL_086f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0879: Expected O, but got Unknown
		//IL_0886: Unknown result type (might be due to invalid IL or missing references)
		//IL_0890: Expected O, but got Unknown
		//IL_0933: Unknown result type (might be due to invalid IL or missing references)
		//IL_093d: Expected O, but got Unknown
		//IL_0978: Unknown result type (might be due to invalid IL or missing references)
		//IL_0982: Expected O, but got Unknown
		//IL_0982: Unknown result type (might be due to invalid IL or missing references)
		//IL_0989: Expected O, but got Unknown
		//IL_09ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_09f7: Expected O, but got Unknown
		//IL_09f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a03: Expected O, but got Unknown
		//IL_0a06: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a12: Expected O, but got Unknown
		//IL_0a19: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a25: Expected O, but got Unknown
		//IL_0a2c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a37: Expected O, but got Unknown
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Expected O, but got Unknown
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Expected O, but got Unknown
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Expected O, but got Unknown
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0216: Expected O, but got Unknown
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0258: Expected O, but got Unknown
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Expected O, but got Unknown
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Expected O, but got Unknown
		//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cd: Expected O, but got Unknown
		maximumSteps.Clear();
		foreach (CheckBox item3 in allCB)
		{
			Panel item = tweakPages[item3];
			int num = orderedPages.IndexOf(item);
			string text = tweakNames[item3];
			if (num >= 0 && num <= 15 && num != 13 && !text.StartsWith("Open ", StringComparison.OrdinalIgnoreCase))
			{
				MaximumStep maximumStep = new MaximumStep();
				maximumStep.Name = text;
				maximumStep.Apply = acts[item3];
				maximumSteps.Add(maximumStep);
			}
		}
		List<Control> list = new List<Control>();
		foreach (Control item4 in (ArrangedElementCollection)((Control)this).Controls)
		{
			Control item2 = item4;
			list.Add(item2);
		}
		((Control)this).Controls.Clear();
		foreach (Component item5 in list)
		{
			item5.Dispose();
		}
		homePage = null;
		backupPage = null;
		backupRows = null;
		sidebar = null;
		secondaryNav = null;
		optimizerButton = null;
		bool flag = IsRunningAsAdministrator();
		((Control)this).Text = "Astryx Tweaks Maximum Tweaks Edition — " + maximumSteps.Count + " Tweaks — " + (flag ? "Administrator" : "Administrator Required");
		((Form)this).Size = new Size(980, 760);
		((Control)this).MinimumSize = new Size(900, 680);
		((Control)this).BackColor = BG;
		Panel val = new Panel();
		((Control)val).Dock = (DockStyle)5;
		((Control)val).BackColor = BG;
		((Control)this).Controls.Add((Control)val);
		Panel val2 = new Panel();
		((Control)val2).Location = new Point(0, 0);
		((Control)val2).Size = new Size(980, 5);
		((Control)val2).Anchor = (AnchorStyles)13;
		((Control)val2).BackColor = ACC;
		((Control)val).Controls.Add((Control)val2);
		try
		{
			if (((Form)this).Icon != null)
			{
				PictureBox val3 = new PictureBox();
				((Control)val3).Location = new Point(54, 38);
				((Control)val3).Size = new Size(70, 70);
				val3.SizeMode = (PictureBoxSizeMode)4;
				val3.Image = (Image)new Icon(((Form)this).Icon, 70, 70).ToBitmap();
				((Control)val3).BackColor = Color.Transparent;
				((Control)val).Controls.Add((Control)val3);
			}
		}
		catch
		{
		}
		Label val4 = new Label();
		((Control)val4).Text = "ASTRYX TWEAKS";
		((Control)val4).Location = new Point(142, 39);
		((Control)val4).AutoSize = true;
		((Control)val4).Font = new Font("Segoe UI Semibold", 25f);
		((Control)val4).ForeColor = Color.White;
		((Control)val).Controls.Add((Control)val4);
		Label val5 = new Label();
		((Control)val5).Text = "MAXIMUM TWEAKS EDITION";
		((Control)val5).Location = new Point(146, 82);
		((Control)val5).AutoSize = true;
		((Control)val5).Font = new Font("Segoe UI Semibold", 11f);
		((Control)val5).ForeColor = ACC;
		((Control)val).Controls.Add((Control)val5);
		Label val6 = new Label();
		((Control)val6).Text = (flag ? "●  ADMINISTRATOR VERIFIED" : "●  ADMINISTRATOR REQUIRED");
		((Control)val6).Location = new Point(146, 107);
		((Control)val6).Size = new Size(260, 20);
		((Control)val6).Font = FS;
		((Control)val6).ForeColor = (flag ? GRN : WARN);
		((Control)val).Controls.Add((Control)val6);
		Label val7 = new Label();
		((Control)val7).Text = maximumSteps.Count.ToString();
		((Control)val7).Location = new Point(690, 28);
		((Control)val7).Size = new Size(220, 70);
		val7.TextAlign = (ContentAlignment)64;
		((Control)val7).Font = new Font("Segoe UI Semibold", 43f);
		((Control)val7).ForeColor = Color.White;
		((Control)val).Controls.Add((Control)val7);
		Label val8 = new Label();
		((Control)val8).Text = "EXACT ONE-CLICK TWEAK COUNT";
		((Control)val8).Location = new Point(682, 98);
		((Control)val8).Size = new Size(228, 20);
		val8.TextAlign = (ContentAlignment)64;
		((Control)val8).Font = FS;
		((Control)val8).ForeColor = MUTED;
		((Control)val).Controls.Add((Control)val8);
		Panel val9 = new Panel();
		((Control)val9).Location = new Point(54, 142);
		((Control)val9).Size = new Size(856, 518);
		((Control)val9).Anchor = (AnchorStyles)15;
		((Control)val9).BackColor = Color.FromArgb(12, 27, 54);
		((Control)val).Controls.Add((Control)val9);
		Label val10 = new Label();
		((Control)val10).Text = "One click. Maximum Windows 11 optimization.";
		((Control)val10).Location = new Point(34, 28);
		((Control)val10).AutoSize = true;
		((Control)val10).Font = new Font("Segoe UI Semibold", 18f);
		((Control)val10).ForeColor = Color.White;
		((Control)val9).Controls.Add((Control)val10);
		Label val11 = new Label();
		((Control)val11).Text = "Aggressive edition: includes performance, privacy, debloat, service, network, gaming, storage, UI, and security-impacting changes. A verified restore point is required.";
		((Control)val11).Location = new Point(36, 70);
		((Control)val11).Size = new Size(780, 42);
		((Control)val11).Font = new Font("Segoe UI", 9f);
		((Control)val11).ForeColor = WARN;
		((Control)val9).Controls.Add((Control)val11);
		maximumButton = new Button();
		((Control)maximumButton).Text = "✦  RUN ALL " + maximumSteps.Count + " TWEAKS";
		((Control)maximumButton).Location = new Point(36, 128);
		((Control)maximumButton).Size = new Size(784, 62);
		((ButtonBase)maximumButton).FlatStyle = (FlatStyle)0;
		((ButtonBase)maximumButton).FlatAppearance.BorderSize = 0;
		((Control)maximumButton).BackColor = ACC;
		((Control)maximumButton).ForeColor = Color.White;
		((Control)maximumButton).Font = new Font("Segoe UI Semibold", 15f);
		((Control)maximumButton).Cursor = Cursors.Hand;
		((Control)maximumButton).Click += delegate
		{
			RunMaximumOptimizer();
		};
		((Control)val9).Controls.Add((Control)maximumButton);
		maximumProgressTrack = new Panel();
		((Control)maximumProgressTrack).Location = new Point(36, 207);
		((Control)maximumProgressTrack).Size = new Size(784, 8);
		((Control)maximumProgressTrack).BackColor = Color.FromArgb(25, 48, 86);
		((Control)val9).Controls.Add((Control)maximumProgressTrack);
		maximumProgressFill = new Panel();
		((Control)maximumProgressFill).Location = new Point(0, 0);
		((Control)maximumProgressFill).Size = new Size(0, 8);
		((Control)maximumProgressFill).BackColor = Color.FromArgb(77, 156, 255);
		((Control)maximumProgressTrack).Controls.Add((Control)maximumProgressFill);
		maximumStatus = new Label();
		((Control)maximumStatus).Text = "Ready — no changes are applied until you click the optimizer.";
		((Control)maximumStatus).Location = new Point(36, 229);
		((Control)maximumStatus).Size = new Size(784, 22);
		((Control)maximumStatus).Font = FB;
		((Control)maximumStatus).ForeColor = Color.FromArgb(116, 186, 255);
		((Control)val9).Controls.Add((Control)maximumStatus);
		statusLbl = maximumStatus;
		maximumLog = new TextBox();
		((Control)maximumLog).Location = new Point(36, 260);
		((Control)maximumLog).Size = new Size(784, 215);
		((Control)maximumLog).Anchor = (AnchorStyles)15;
		((TextBoxBase)maximumLog).Multiline = true;
		((TextBoxBase)maximumLog).ReadOnly = true;
		maximumLog.ScrollBars = (ScrollBars)2;
		((Control)maximumLog).BackColor = Color.FromArgb(6, 16, 34);
		((Control)maximumLog).ForeColor = Color.FromArgb(116, 186, 255);
		((Control)maximumLog).Font = new Font("Consolas", 8.5f);
		((Control)maximumLog).Text = "Maximum Tweaks Edition loaded.\r\nExact optimizer count: " + maximumSteps.Count + " tweaks.\r\n";
		((Control)val9).Controls.Add((Control)maximumLog);
		Label val12 = new Label();
		((Control)val12).Text = "RESTORE POINT REQUIRED  •  RUN AS ADMINISTRATOR  •  RESTART WINDOWS AFTER COMPLETION";
		((Control)val12).Location = new Point(54, 681);
		((Control)val12).Size = new Size(856, 20);
		((Control)val12).Anchor = (AnchorStyles)14;
		val12.TextAlign = (ContentAlignment)32;
		((Control)val12).Font = FS;
		((Control)val12).ForeColor = MUTED;
		((Control)val).Controls.Add((Control)val12);
		ApplyRoundedStyle((Control)val);
		RoundControl((Control)val9, 20);
		RoundControl((Control)maximumButton, 14);
		RoundControl((Control)maximumProgressTrack, 4);
		if (maximumButton != null)
		{
			((Control)maximumButton).BackColor = Color.FromArgb(38, 116, 235);
		}
		AnimatePage(val);
	}

	private void RunMaximumOptimizer()
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Invalid comparison between Unknown and I4
		if (maximumRunning || !EnsureAdministrator() || (int)MessageBox.Show("This will apply exactly " + maximumSteps.Count + " aggressive Windows 11 tweak modules.\n\nThe optimizer can remove built-in apps, disable optional services, and reduce some Windows security features. A restore point will be created first.\n\nContinue?", "Astryx Tweaks Maximum Tweaks Edition", (MessageBoxButtons)4, (MessageBoxIcon)48) != 6)
		{
			return;
		}
		maximumRunning = true;
		((Control)maximumButton).Enabled = false;
		((Control)maximumButton).BackColor = Color.FromArgb(22, 66, 140);
		((TextBoxBase)maximumLog).Clear();
		((Control)maximumStatus).Text = "Creating and verifying Windows restore point...";
		((Control)maximumStatus).ForeColor = WARN;
		((Control)maximumProgressFill).Width = 0;
		if (!CreateBackup())
		{
			((Control)maximumStatus).Text = "Cancelled — Windows did not create a restore point.";
			((TextBoxBase)maximumLog).AppendText("[CANCELLED] No tweaks were applied.\r\n");
			((Control)maximumButton).Enabled = true;
			maximumRunning = false;
			return;
		}
		((TextBoxBase)maximumLog).AppendText("[OK] Restore point created.\r\n");
		((Control)maximumStatus).Text = "Launching the Talon debloat assistant...";
		if (!RunTalonGuidedStep())
		{
			((Control)maximumStatus).Text = "Cancelled — Talon was not confirmed complete. The " + maximumSteps.Count + " tweaks were not started.";
			((Control)maximumStatus).ForeColor = WARN;
			((TextBoxBase)maximumLog).AppendText("[CANCELLED] Talon was not confirmed complete. The built-in tweaks were not started.\r\n");
			((Control)maximumButton).Enabled = true;
			maximumRunning = false;
			return;
		}
		((TextBoxBase)maximumLog).AppendText("[OK] Talon workflow confirmed complete. Starting the built-in tweaks.\r\n");
		BackgroundWorker worker = new BackgroundWorker();
		worker.WorkerReportsProgress = true;
		worker.DoWork += delegate
		{
			for (int i = 0; i < maximumSteps.Count; i++)
			{
				MaximumStep maximumStep = maximumSteps[i];
				MaximumProgress maximumProgress = new MaximumProgress
				{
					Current = i + 1,
					Name = maximumStep.Name
				};
				try
				{
					maximumStep.Apply();
					maximumProgress.Result = "[OK]";
				}
				catch (Exception ex)
				{
					maximumProgress.Result = "[FAIL] " + ex.Message;
				}
				worker.ReportProgress((i + 1) * 100 / maximumSteps.Count, maximumProgress);
			}
		};
		worker.ProgressChanged += delegate(object sender, ProgressChangedEventArgs e)
		{
			MaximumProgress maximumProgress = (MaximumProgress)e.UserState;
			((Control)maximumProgressFill).Width = ((Control)maximumProgressTrack).ClientSize.Width * maximumProgress.Current / maximumSteps.Count;
			((Control)maximumStatus).Text = "Applying " + maximumProgress.Current + " / " + maximumSteps.Count + " — " + maximumProgress.Name;
			((TextBoxBase)maximumLog).AppendText(maximumProgress.Result + "  " + maximumProgress.Current + "/" + maximumSteps.Count + "  " + maximumProgress.Name + "\r\n");
			((TextBoxBase)maximumLog).SelectionStart = ((TextBoxBase)maximumLog).TextLength;
			((TextBoxBase)maximumLog).ScrollToCaret();
		};
		worker.RunWorkerCompleted += delegate
		{
			((Control)maximumProgressFill).Width = ((Control)maximumProgressTrack).ClientSize.Width;
			((Control)maximumStatus).Text = "COMPLETE — exactly " + maximumSteps.Count + " tweak modules processed. Restart Windows.";
			((Control)maximumStatus).ForeColor = GRN;
			((TextBoxBase)maximumLog).AppendText("\r\n=== MAXIMUM OPTIMIZATION COMPLETE: " + maximumSteps.Count + " / " + maximumSteps.Count + " ===\r\nRestart Windows now.");
			((Control)maximumButton).Enabled = true;
			maximumRunning = false;
			worker.Dispose();
		};
		worker.RunWorkerAsync();
	}

	private static bool IsRunningAsAdministrator()
	{
		if (Environment.OSVersion.Platform != PlatformID.Win32NT)
		{
			return true;
		}
		try
		{
			return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch
		{
			return false;
		}
	}

	private bool EnsureAdministrator()
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		if (IsRunningAsAdministrator())
		{
			return true;
		}
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = Application.ExecutablePath,
				UseShellExecute = true,
				Verb = "runas",
				WorkingDirectory = Path.GetDirectoryName(Application.ExecutablePath)
			});
			((Form)this).Close();
		}
		catch (Exception ex)
		{
			MessageBox.Show("Administrator access is required and Windows did not elevate the app.\n\n" + ex.Message, "Administrator required", (MessageBoxButtons)0, (MessageBoxIcon)48);
		}
		return false;
	}

	private bool RunTalonGuidedStep()
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Invalid comparison between Unknown and I4
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		Process process;
		try
		{
			process = Process.Start(new ProcessStartInfo
			{
				FileName = "powershell.exe",
				Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"irm https://raventechnologiesgroup.com/talon/run | iex\"",
				UseShellExecute = true,
				Verb = "runas",
				WindowStyle = ProcessWindowStyle.Normal
			});
			if (process == null)
			{
				throw new InvalidOperationException("PowerShell did not start.");
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show("Talon could not be launched as administrator.\n\n" + ex.Message, "Talon launch failed", (MessageBoxButtons)0, (MessageBoxIcon)16);
			return false;
		}
		MessageBox.Show("Follow through with Talon's instructions.\n\nIt is an open source tool that is not mine and further helps debloat the PC. Do not restart yet.\n\nTalon temporarily adds a Windows Defender exclusion while it downloads and verifies its files, then removes the exclusion during cleanup.", "Complete Talon first", (MessageBoxButtons)0, (MessageBoxIcon)64);
		while (true)
		{
			if ((int)MessageBox.Show("Did you finish Talon's instructions and close both Talon and its PowerShell window?\n\nLeave this question open while Talon runs, then return and click Yes. Click No to cancel without starting the " + maximumSteps.Count + " built-in tweaks.", "Talon completion check", (MessageBoxButtons)4, (MessageBoxIcon)32) != 6)
			{
				return false;
			}
			try
			{
				if (!process.HasExited)
				{
					MessageBox.Show("The Talon PowerShell process is still open. Finish and close it before confirming.", "Talon is still running", (MessageBoxButtons)0, (MessageBoxIcon)64);
					continue;
				}
			}
			catch
			{
			}
			break;
		}
		return true;
	}

	private void AddSidebarLabel(string text, int y)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		Label val = new Label();
		((Control)val).Text = text;
		((Control)val).Location = new Point(14, y);
		((Control)val).AutoSize = true;
		((Control)val).Font = FS;
		((Control)val).ForeColor = Color.FromArgb(112, 126, 160);
		((Control)sidebar).Controls.Add((Control)(object)val);
	}

	private Button AddSidebarAction(string text, int y, EventHandler click)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		Button val = new Button();
		((Control)val).Text = text;
		((ButtonBase)val).TextAlign = (ContentAlignment)16;
		((Control)val).Location = new Point(8, y);
		((Control)val).Size = new Size(208, 34);
		((ButtonBase)val).FlatStyle = (FlatStyle)0;
		((ButtonBase)val).FlatAppearance.BorderSize = 0;
		((Control)val).BackColor = Color.FromArgb(12, 25, 45);
		((Control)val).ForeColor = Color.FromArgb(196, 212, 238);
		((Control)val).Font = FB;
		((Control)val).Padding = new Padding(13, 0, 0, 0);
		((Control)val).Cursor = Cursors.Hand;
		((Control)val).MouseEnter += delegate
		{
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Expected O, but got Unknown
			AnimateBackColor((Control)val, Color.FromArgb(24, 58, 106), 150);
		};
		((Control)val).MouseLeave += delegate
		{
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Expected O, but got Unknown
			AnimateBackColor((Control)val, (val == optimizerButton) ? Color.FromArgb(34, 103, 239) : Color.FromArgb(12, 25, 45), 200);
		};
		((Control)val).Click += click;
		((Control)sidebar).Controls.Add((Control)(object)val);
		RoundControl((Control)val, 9);
		return val;
	}

	private void BuildSidebarUserCard()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected O, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Expected O, but got Unknown
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Expected O, but got Unknown
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Expected O, but got Unknown
		string text = "user";
		try
		{
			text = Environment.UserName;
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "user";
			}
		}
		catch
		{
		}
		string text2 = text.Substring(0, 1).ToUpper();
		Panel val = new Panel();
		((Control)val).Dock = (DockStyle)2;
		((Control)val).Height = 60;
		((Control)val).BackColor = Color.FromArgb(14, 22, 40);
		((Control)val).Padding = new Padding(10, 8, 10, 8);
		((Control)sidebar).Controls.Add((Control)(object)val);
		Panel val2 = new Panel();
		((Control)val2).Location = new Point(12, 14);
		((Control)val2).Size = new Size(32, 32);
		((Control)val2).BackColor = ACC;
		RoundControl((Control)(object)val2, 16);
		((Control)val).Controls.Add((Control)(object)val2);
		Label val3 = new Label();
		((Control)val3).Text = text2;
		((Control)val3).Font = FB;
		((Control)val3).ForeColor = Color.White;
		val3.TextAlign = (ContentAlignment)32;
		((Control)val3).Dock = (DockStyle)5;
		((Control)val3).BackColor = Color.Transparent;
		((Control)val2).Controls.Add((Control)(object)val3);
		Label val4 = new Label();
		((Control)val4).Text = text;
		((Control)val4).Font = FB;
		((Control)val4).ForeColor = TXT;
		((Control)val4).Location = new Point(52, 12);
		((Control)val4).Size = new Size(140, 18);
		val4.AutoEllipsis = true;
		((Control)val).Controls.Add((Control)(object)val4);
		Label val5 = new Label();
		((Control)val5).Text = "Free";
		((Control)val5).Font = FS;
		((Control)val5).ForeColor = ACC;
		((Control)val5).Location = new Point(52, 31);
		((Control)val5).Size = new Size(140, 16);
		((Control)val).Controls.Add((Control)(object)val5);
	}

	private void ShowGroup(string group)
	{
		Panel val = null;
		foreach (Panel key in navMap.Keys)
		{
			((Control)key).Visible = false;
			if (navGroups[key] == group && val == null)
			{
				val = key;
			}
		}
		if (val != null)
		{
			BuildSecondaryTabs(group);
			SwitchNav(val);
			((Control)secondaryNav).Visible = true;
			((Control)secondaryNav).BringToFront();
		}
	}

	private void ShowOptimizer()
	{
		if (optimizerNav != null)
		{
			BuildSecondaryTabs("Advanced");
			SwitchNav(optimizerNav);
			((Control)secondaryNav).Visible = true;
			((Control)secondaryNav).BringToFront();
		}
	}

	private void BuildSecondaryTabs(string group)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected O, but got Unknown
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Expected O, but got Unknown
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Expected O, but got Unknown
		((Control)secondaryNav).Controls.Clear();
		((Control)secondaryNav).Width = Math.Max(620, ((Control)this).ClientSize.Width - ((Control)sidebar).Width - 32);
		int num = 0;
		foreach (Panel key in navMap.Keys)
		{
			if (navGroups[key] != group)
			{
				continue;
			}
			Label val = null;
			foreach (Control item in (ArrangedElementCollection)((Control)key).Controls)
			{
				Control val2 = item;
				if (val2 is Label)
				{
					val = (Label)val2;
					break;
				}
			}
			Button val3 = new Button();
			((Control)val3).Text = ((val == null) ? group : ((Control)val).Text);
			((Control)val3).Location = new Point(num, 4);
			((Control)val3).Size = new Size(Math.Max(88, Math.Min(142, ((Control)val3).Text.Length * 9 + 28)), 32);
			((ButtonBase)val3).FlatStyle = (FlatStyle)0;
			((ButtonBase)val3).FlatAppearance.BorderSize = 0;
			((Control)val3).BackColor = Color.FromArgb(18, 51, 104);
			((Control)val3).ForeColor = Color.FromArgb(188, 215, 255);
			((Control)val3).Font = FB;
			Color tabRest = ((Control)val3).BackColor;
			((Control)val3).MouseEnter += delegate
			{
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0029: Expected O, but got Unknown
				AnimateBackColor((Control)val3, Color.FromArgb(36, 92, 188), 150);
			};
			((Control)val3).MouseLeave += delegate
			{
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0021: Expected O, but got Unknown
				AnimateBackColor((Control)val3, tabRest, 210);
			};
			Panel target = key;
			((Control)val3).Click += delegate
			{
				SwitchNav(target);
				((Control)secondaryNav).BringToFront();
			};
			((Control)secondaryNav).Controls.Add((Control)(object)val3);
			RoundControl((Control)val3, 9);
			num += ((Control)val3).Width + 4;
		}
		int searchWidth = 210;
		if (num + searchWidth + 18 <= ((Control)secondaryNav).Width)
		{
			searchBox = new TextBox();
			((Control)searchBox).Location = new Point(((Control)secondaryNav).Width - searchWidth - 8, 8);
			((Control)searchBox).Size = new Size(searchWidth, 26);
			((Control)searchBox).Anchor = (AnchorStyles)9;
			((Control)searchBox).Text = "Search tweaks...";
			((Control)searchBox).ForeColor = Color.FromArgb(118, 132, 160);
			((Control)searchBox).BackColor = Color.FromArgb(14, 24, 42);
			((TextBoxBase)searchBox).BorderStyle = (BorderStyle)1;
			((Control)searchBox).GotFocus += SearchFocus;
			((Control)searchBox).LostFocus += SearchBlur;
			((Control)searchBox).TextChanged += SearchChange;
			((Control)secondaryNav).Controls.Add((Control)(object)searchBox);
			RoundControl((Control)searchBox, 7);
		}
	}

	private void ShowHome()
	{
		HideUtilityPages();
		foreach (Panel key in navMap.Keys)
		{
			((Control)key).Visible = false;
		}
		foreach (Panel value in navMap.Values)
		{
			((Control)value).Visible = false;
		}
		activeNav = null;
		((Control)secondaryNav).Visible = false;
		if (backupPage != null)
		{
			((Control)backupPage).Visible = false;
		}
		if (defenderPage != null)
		{
			((Control)defenderPage).Visible = false;
		}
		if (powerPage != null)
		{
			((Control)powerPage).Visible = false;
		}
		((Control)homePage).Location = new Point(((Control)sidebar).Width, 0);
		((Control)homePage).Size = new Size(Math.Max(760, ((Control)this).ClientSize.Width - ((Control)sidebar).Width), Math.Max(560, ((Control)this).ClientSize.Height - 28));
		((Control)homePage).Visible = true;
		((Control)homePage).BringToFront();
		RelayoutHome();
		AnimatePage(homePage);
	}

	private void BuildHome()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Expected O, but got Unknown
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Expected O, but got Unknown
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Expected O, but got Unknown
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Expected O, but got Unknown
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Expected O, but got Unknown
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Expected O, but got Unknown
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Expected O, but got Unknown
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Expected O, but got Unknown
		//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Expected O, but got Unknown
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Expected O, but got Unknown
		//IL_03c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Expected O, but got Unknown
		//IL_045c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0467: Expected O, but got Unknown
		//IL_04fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0502: Expected O, but got Unknown
		//IL_05be: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c5: Expected O, but got Unknown
		//IL_05dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e7: Expected O, but got Unknown
		//IL_0628: Unknown result type (might be due to invalid IL or missing references)
		//IL_062f: Expected O, but got Unknown
		//IL_070c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0713: Expected O, but got Unknown
		homePage = new Panel();
		((Control)homePage).Location = new Point(224, 0);
		((Control)homePage).Size = new Size(840, 700);
		((Control)homePage).Anchor = (AnchorStyles)15;
		((Control)homePage).BackColor = BG;
		((ScrollableControl)homePage).AutoScroll = true;
		((Control)homePage).Padding = new Padding(28, 20, 28, 28);
		((Control)this).Controls.Add((Control)(object)homePage);
		string text = string.IsNullOrWhiteSpace(profileName) ? "there" : profileName;
		Label val = new Label();
		((Control)val).Text = "Welcome Back, " + text + "!";
		((Control)val).Font = new Font("Segoe UI Semibold", 21f);
		((Control)val).ForeColor = Color.White;
		((Control)val).Location = new Point(28, 18);
		((Control)val).AutoSize = true;
		((Control)homePage).Controls.Add((Control)(object)val);
		homeWelcomeLabel = val;
		Label val2 = new Label();
		((Control)val2).Text = "Ready to enhance your system performance?";
		((Control)val2).Font = new Font("Segoe UI", 10.5f);
		((Control)val2).ForeColor = MUTED;
		((Control)val2).Location = new Point(30, 58);
		((Control)val2).AutoSize = true;
		((Control)homePage).Controls.Add((Control)(object)val2);
		int y = 100;
		AddHomeTopCard(0, 28, y, "\ue896", "Create backup", GetBackupCount().ToString() ?? "", "Backups found", bigIsNumber: true, delegate
		{
			ShowBackups();
		}, comingSoon: false, redTint: false);
		AddHomeTopCard(1, 295, y, "\ue897", "Coming soon", "Need help?", "Community server coming soon", bigIsNumber: false, delegate
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			MessageBox.Show("A dedicated support server is coming soon.", "Coming soon", (MessageBoxButtons)0, (MessageBoxIcon)64);
		}, comingSoon: true, redTint: false);
		AddHomeTopCard(2, 562, y, "\ue786", "Coming soon", "How to use Guide", "Full tutorial video coming soon", bigIsNumber: false, delegate
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			MessageBox.Show("A full tutorial video is coming soon.", "Coming soon", (MessageBoxButtons)0, (MessageBoxIcon)64);
		}, comingSoon: true, redTint: true);
		Panel val3 = new Panel();
		((Control)val3).Location = new Point(28, 232);
		((Control)val3).Size = new Size(784, 150);
		((Control)val3).BackColor = Color.FromArgb(18, 30, 66);
		((Control)homePage).Controls.Add((Control)(object)val3);
		homeHero = val3;
		RoundControl((Control)val3, 18);
		Panel heroRef = val3;
		((Control)val3).Paint += (PaintEventHandler)delegate(object hs, PaintEventArgs he)
		{
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Expected O, but got Unknown
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Expected O, but got Unknown
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Expected O, but got Unknown
			Graphics graphics = he.Graphics;
			graphics.SmoothingMode = (SmoothingMode)4;
			Rectangle rectangle = new Rectangle(0, 0, ((Control)heroRef).Width, ((Control)heroRef).Height);
			LinearGradientBrush val12 = new LinearGradientBrush(rectangle, Color.FromArgb(30, 58, 150), Color.FromArgb(74, 42, 150), (LinearGradientMode)0);
			try
			{
				graphics.FillRectangle((Brush)(object)val12, rectangle);
			}
			finally
			{
				((IDisposable)val12)?.Dispose();
			}
			SolidBrush val13 = new SolidBrush(Color.FromArgb(46, 120, 170, 255));
			try
			{
				graphics.FillEllipse((Brush)(object)val13, ((Control)heroRef).Width - 190, -50, 240, 240);
			}
			finally
			{
				((IDisposable)val13)?.Dispose();
			}
			SolidBrush val14 = new SolidBrush(Color.FromArgb(30, 150, 120, 255));
			try
			{
				graphics.FillEllipse((Brush)(object)val14, ((Control)heroRef).Width - 110, 40, 170, 170);
			}
			finally
			{
				((IDisposable)val14)?.Dispose();
			}
		};
		((Control)val3).Resize += delegate
		{
			((Control)heroRef).Invalidate();
		};
		Label val4 = new Label();
		((Control)val4).Text = "Boost your FPS in one click";
		((Control)val4).Font = new Font("Segoe UI Semibold", 18f);
		((Control)val4).ForeColor = Color.White;
		((Control)val4).BackColor = Color.Transparent;
		((Control)val4).Location = new Point(28, 24);
		((Control)val4).AutoSize = true;
		((Control)val3).Controls.Add((Control)(object)val4);
		Label val5 = new Label();
		((Control)val5).Text = "Astryx Tweaks applies 570+ reversible tweaks and reduces system latency to the minimum possible.";
		((Control)val5).Font = FB;
		((Control)val5).ForeColor = Color.FromArgb(212, 222, 255);
		((Control)val5).BackColor = Color.Transparent;
		((Control)val5).Location = new Point(30, 62);
		((Control)val5).Size = new Size(430, 40);
		((Control)val3).Controls.Add((Control)(object)val5);
		homeHeroTag = val5;
		Button val6 = new Button();
		((Control)val6).Text = "Start Optimization";
		((Control)val6).Location = new Point(30, 104);
		((Control)val6).Size = new Size(210, 34);
		((ButtonBase)val6).FlatStyle = (FlatStyle)0;
		((ButtonBase)val6).FlatAppearance.BorderSize = 0;
		((Control)val6).BackColor = ACC;
		((Control)val6).ForeColor = Color.White;
		((Control)val6).Font = FB;
		((Control)val6).Click += delegate
		{
			ShowOptimizer();
		};
		((Control)val3).Controls.Add((Control)(object)val6);
		homeHeroBtn = val6;
		RoundControl((Control)val6, 8);
		int y2 = 398;
		homeChart = new ActivityChart();
		((Control)homeChart).Location = new Point(28, y2);
		((Control)homeChart).Size = new Size(508, 250);
		((Control)homePage).Controls.Add((Control)(object)homeChart);
		Panel val7 = (homeQuick = CardPanel(552, y2, 260, 250, Color.FromArgb(12, 19, 36)));
		((Control)homePage).Controls.Add((Control)(object)val7);
		Label val8 = new Label();
		((Control)val8).Text = "⚡  Quickstart";
		((Control)val8).Font = FH;
		((Control)val8).ForeColor = TXT;
		((Control)val8).Location = new Point(16, 14);
		((Control)val8).AutoSize = true;
		((Control)val7).Controls.Add((Control)(object)val8);
		AddQuickItem(val7, 50, done: true, "Create a backup", "Back up your system settings for safety");
		AddQuickItem(val7, 90, done: false, "Apply a tweak", "Enable tweaks to boost performance");
		AddQuickItem(val7, 130, done: false, "Clean your system files", "Free up the clutter on your drive");
		AddQuickItem(val7, 170, done: false, "Debloat your system", "Remove useless services");
		AddQuickItem(val7, 210, done: false, "Optimize your PC", "Run the one-click optimizer");
		Label val9 = new Label();
		((Control)val9).Text = "\ud83d\udce2  News & Tips";
		((Control)val9).Font = new Font("Segoe UI Semibold", 13f);
		((Control)val9).ForeColor = TXT;
		((Control)val9).Location = new Point(28, 664);
		((Control)val9).AutoSize = true;
		((Control)homePage).Controls.Add((Control)(object)val9);
		homeUpdatesLabel = val9;
		Label val10 = new Label();
		((Control)val10).Text = "Astryx Tweaks Maximum Tweaks Edition";
		((Control)val10).Font = FB;
		((Control)val10).ForeColor = ACC;
		((Control)val10).Location = new Point(560, 666);
		((Control)val10).Size = new Size(252, 20);
		val10.TextAlign = (ContentAlignment)64;
		((Control)homePage).Controls.Add((Control)(object)val10);
		homeVersion = val10;
		int num = 696;
		AddUpdateCard(0, 28, num, "News", "No patch notes yet - check back soon.", Color.FromArgb(20, 30, 52));
		AddUpdateCard(1, 295, num, "Tips & Tricks", "Fastest optimize: open the Optimizer, pick Extreme, then Apply.", Color.FromArgb(20, 34, 40));
		AddUpdateCard(2, 562, num, "Community", "No Discord yet - coming soon.", Color.FromArgb(16, 34, 54));
		Panel val11 = new Panel();
		((Control)val11).Location = new Point(28, num + 150);
		((Control)val11).Size = new Size(10, 10);
		((Control)val11).BackColor = BG;
		((Control)homePage).Controls.Add((Control)(object)val11);
		homeSpacer = val11;
		((Control)homePage).Resize += delegate
		{
			RelayoutHome();
		};
		RelayoutHome();
	}

	private void RelayoutHome()
	{
		if (homePage == null || homeChart == null)
		{
			return;
		}
		int num = 28;
		int num2 = ((Control)homePage).ClientSize.Width - num * 2;
		if (num2 < 760)
		{
			num2 = 760;
		}
		int num3 = 16;
		int num4 = (num2 - 2 * num3) / 3;
		for (int i = 0; i < 3; i++)
		{
			if (homeTop[i] != null)
			{
				int x = num + i * (num4 + num3);
				((Control)homeTop[i]).Location = new Point(x, 100);
				((Control)homeTop[i]).Size = new Size(num4, 118);
				if (homeTopBtn[i] != null)
				{
					((Control)homeTopBtn[i]).Location = new Point(num4 - 128, 14);
				}
				if (homeTopBig[i] != null)
				{
					((Control)homeTopBig[i]).Size = new Size(num4 - 30, ((Control)homeTopBig[i]).Height);
				}
				if (homeTopSub[i] != null)
				{
					((Control)homeTopSub[i]).Size = new Size(num4 - 30, ((Control)homeTopSub[i]).Height);
				}
				((Control)homeTop[i]).Invalidate();
			}
		}
		if (homeHero != null)
		{
			((Control)homeHero).Location = new Point(num, 232);
			((Control)homeHero).Size = new Size(num2, 150);
			((Control)homeHero).Invalidate();
		}
		int num5 = (int)((double)num2 * 0.66);
		int num6 = num2 - num5 - num3;
		if (num6 < 240)
		{
			num6 = 240;
			num5 = num2 - num6 - num3;
		}
		((Control)homeChart).Location = new Point(num, 398);
		((Control)homeChart).Size = new Size(num5, 250);
		((Control)homeChart).Invalidate();
		if (homeQuick != null)
		{
			((Control)homeQuick).Location = new Point(num + num5 + num3, 398);
			((Control)homeQuick).Size = new Size(num6, 250);
			((Control)homeQuick).Invalidate();
		}
		if (homeUpdatesLabel != null)
		{
			((Control)homeUpdatesLabel).Location = new Point(num, 664);
		}
		if (homeVersion != null)
		{
			((Control)homeVersion).Location = new Point(num + num2 - 252, 666);
		}
		int num7 = (num2 - 2 * num3) / 3;
		for (int j = 0; j < 3; j++)
		{
			if (homeUpd[j] != null)
			{
				int x2 = num + j * (num7 + num3);
				((Control)homeUpd[j]).Location = new Point(x2, 696);
				((Control)homeUpd[j]).Size = new Size(num7, 140);
				if (homeUpdTitle[j] != null)
				{
					((Control)homeUpdTitle[j]).Size = new Size(num7 - 28, ((Control)homeUpdTitle[j]).Height);
				}
				if (homeUpdSub[j] != null)
				{
					((Control)homeUpdSub[j]).Size = new Size(num7 - 28, ((Control)homeUpdSub[j]).Height);
				}
				((Control)homeUpd[j]).Invalidate();
			}
		}
		if (homeSpacer != null)
		{
			((Control)homeSpacer).Location = new Point(num, 850);
		}
	}

	private int GetBackupCount()
	{
		try
		{
			string text = RunPSCapture("(Get-ComputerRestorePoint -EA SilentlyContinue | Measure-Object).Count");
			if (text != null && int.TryParse(text.Trim(), out var result))
			{
				return result;
			}
		}
		catch
		{
		}
		return 0;
	}

	private string GetCpuName()
	{
		try
		{
			RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");
			if (registryKey != null)
			{
				object value = registryKey.GetValue("ProcessorNameString");
				registryKey.Close();
				if (value != null)
				{
					string text = value.ToString().Trim();
					if (text.Length > 0)
					{
						return text;
					}
				}
			}
		}
		catch
		{
		}
		return "Your Processor";
	}

	private Panel CardPanel(int x, int y, int w, int h, Color bg)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		Panel pnl = new Panel();
		((Control)pnl).Location = new Point(x, y);
		((Control)pnl).Size = new Size(w, h);
		((Control)pnl).BackColor = bg;
		RoundControl((Control)(object)pnl, 12);
		((Control)pnl).Paint += (PaintEventHandler)delegate(object cs, PaintEventArgs ce)
		{
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Expected O, but got Unknown
			Graphics graphics = ce.Graphics;
			graphics.SmoothingMode = (SmoothingMode)4;
			GraphicsPath val = RoundedPath(new Rectangle(0, 0, ((Control)pnl).Width - 1, ((Control)pnl).Height - 1), 12);
			try
			{
				Pen val2 = new Pen(Color.FromArgb(70, 90, 140, 220));
				try
				{
					graphics.DrawPath(val2, val);
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		};
		return pnl;
	}

	private Button PillButton(string text, Color bg, int w)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		Button val = new Button();
		((Control)val).Text = text;
		((Control)val).Size = new Size(w, 26);
		((ButtonBase)val).FlatStyle = (FlatStyle)0;
		((ButtonBase)val).FlatAppearance.BorderSize = 0;
		((Control)val).BackColor = bg;
		((Control)val).ForeColor = Color.White;
		((Control)val).Font = FS;
		((Control)val).Cursor = Cursors.Hand;
		RoundControl((Control)(object)val, 8);
		return val;
	}

	private void AddHomeTopCard(int idx, int x, int y, string glyph, string btnText, string bigText, string subText, bool bigIsNumber, EventHandler click, bool comingSoon, bool redTint)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Expected O, but got Unknown
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Expected O, but got Unknown
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Expected O, but got Unknown
		int num = 250;
		int num2 = 118;
		Panel val = CardPanel(x, y, num, num2, redTint ? Color.FromArgb(26, 16, 24) : Color.FromArgb(12, 19, 36));
		((Control)homePage).Controls.Add((Control)(object)val);
		Label val2 = new Label();
		((Control)val2).Text = glyph;
		((Control)val2).Font = FICON;
		((Control)val2).ForeColor = (redTint ? Color.FromArgb(240, 100, 130) : Color.FromArgb(96, 156, 255));
		((Control)val2).Location = new Point(16, 14);
		((Control)val2).Size = new Size(24, 24);
		((Control)val2).BackColor = Color.Transparent;
		((Control)val).Controls.Add((Control)(object)val2);
		Button val3 = PillButton(btnText, comingSoon ? Color.FromArgb(40, 46, 66) : (redTint ? Color.FromArgb(150, 46, 66) : ACC), 116);
		((Control)val3).Location = new Point(num - 128, 14);
		((Control)val3).Click += click;
		((Control)val).Controls.Add((Control)(object)val3);
		Label val4 = new Label();
		((Control)val4).Text = bigText;
		((Control)val4).Font = (bigIsNumber ? new Font("Segoe UI Semibold", 22f) : new Font("Segoe UI Semibold", 14f));
		((Control)val4).ForeColor = Color.White;
		((Control)val4).Location = new Point(16, bigIsNumber ? 46 : 52);
		((Control)val4).Size = new Size(num - 30, bigIsNumber ? 34 : 26);
		val4.AutoEllipsis = true;
		((Control)val).Controls.Add((Control)(object)val4);
		Label val5 = new Label();
		((Control)val5).Text = subText;
		((Control)val5).Font = FS;
		((Control)val5).ForeColor = MUTED;
		((Control)val5).Location = new Point(16, num2 - 30);
		((Control)val5).Size = new Size(num - 30, 18);
		val5.AutoEllipsis = true;
		((Control)val).Controls.Add((Control)(object)val5);
		if (idx >= 0 && idx < 3)
		{
			homeTop[idx] = val;
			homeTopBtn[idx] = val3;
			homeTopBig[idx] = val4;
			homeTopSub[idx] = val5;
		}
	}

	private void AddQuickItem(Panel parent, int y, bool done, string title, string sub)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Expected O, but got Unknown
		Label val = new Label();
		((Control)val).Text = (done ? "◉" : "○");
		((Control)val).Font = new Font("Segoe UI", 11f);
		((Control)val).ForeColor = (done ? GRN : MUTED);
		((Control)val).Location = new Point(16, y);
		((Control)val).Size = new Size(22, 22);
		((Control)val).BackColor = Color.Transparent;
		((Control)parent).Controls.Add((Control)(object)val);
		Label val2 = new Label();
		((Control)val2).Text = title;
		((Control)val2).Font = FB;
		((Control)val2).ForeColor = TXT;
		((Control)val2).Location = new Point(42, y - 2);
		((Control)val2).Size = new Size(200, 18);
		val2.AutoEllipsis = true;
		((Control)parent).Controls.Add((Control)(object)val2);
		Label val3 = new Label();
		((Control)val3).Text = sub;
		((Control)val3).Font = FS;
		((Control)val3).ForeColor = MUTED;
		((Control)val3).Location = new Point(42, y + 15);
		((Control)val3).Size = new Size(206, 16);
		val3.AutoEllipsis = true;
		((Control)parent).Controls.Add((Control)(object)val3);
	}

	private void AddUpdateCard(int idx, int x, int y, string title, string sub, Color tint)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Expected O, but got Unknown
		int num = 250;
		int num2 = 140;
		Panel val = CardPanel(x, y, num, num2, tint);
		((Control)homePage).Controls.Add((Control)(object)val);
		Label val2 = new Label();
		((Control)val2).Text = title;
		((Control)val2).Font = new Font("Segoe UI Semibold", 12f);
		((Control)val2).ForeColor = Color.White;
		((Control)val2).Location = new Point(16, num2 - 58);
		((Control)val2).Size = new Size(num - 28, 24);
		((Control)val).Controls.Add((Control)(object)val2);
		Label val3 = new Label();
		((Control)val3).Text = sub;
		((Control)val3).Font = FS;
		((Control)val3).ForeColor = MUTED;
		((Control)val3).Location = new Point(16, num2 - 32);
		((Control)val3).Size = new Size(num - 28, 20);
		((Control)val).Controls.Add((Control)(object)val3);
		if (idx >= 0 && idx < 3)
		{
			homeUpd[idx] = val;
			homeUpdTitle[idx] = val2;
			homeUpdSub[idx] = val3;
		}
	}

	private Panel MakeNavItem(string text, int yPos, Panel page)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		Panel item = new Panel();
		((Control)item).Location = new Point(0, yPos);
		((Control)item).Size = new Size(190, 38);
		((Control)item).BackColor = Color.FromArgb(12, 28, 56);
		((Control)item).Cursor = Cursors.Hand;
		((Control)item).Tag = page;
		Label val = new Label();
		((Control)val).Text = text;
		((Control)val).Font = FB;
		((Control)val).ForeColor = Color.FromArgb(140, 140, 170);
		((Control)val).Location = new Point(20, 9);
		((Control)val).AutoSize = true;
		((Control)val).Tag = item;
		((Control)item).Controls.Add((Control)(object)val);
		((Control)item).MouseEnter += delegate
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Expected O, but got Unknown
			if (item != activeNav)
			{
				AnimateBackColor((Control)item, Color.FromArgb(23, 61, 125), 150);
			}
		};
		((Control)item).MouseLeave += delegate
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Expected O, but got Unknown
			if (item != activeNav)
			{
				AnimateBackColor((Control)item, Color.FromArgb(12, 28, 56), 210);
			}
		};
		((Control)item).Click += delegate
		{
			SwitchNav(item);
		};
		((Control)val).Click += delegate
		{
			SwitchNav(item);
		};
		((Control)sidebar).Controls.Add((Control)(object)item);
		return item;
	}

	private void SwitchNav(Panel nav)
	{
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected O, but got Unknown
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Expected O, but got Unknown
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Expected O, but got Unknown
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected O, but got Unknown
		HideUtilityPages();
		if (homePage != null)
		{
			((Control)homePage).Visible = false;
		}
		if (backupPage != null)
		{
			((Control)backupPage).Visible = false;
		}
		if (defenderPage != null)
		{
			((Control)defenderPage).Visible = false;
		}
		if (powerPage != null)
		{
			((Control)powerPage).Visible = false;
		}
		if (secondaryNav != null)
		{
			((Control)secondaryNav).Visible = true;
		}
		if (activeNav != null)
		{
			AnimateBackColor((Control)activeNav, Color.FromArgb(12, 28, 56), 180);
			foreach (Control item in (ArrangedElementCollection)((Control)activeNav).Controls)
			{
				Control val = item;
				if (val is Label)
				{
					val.ForeColor = Color.FromArgb(140, 140, 170);
				}
			}
		}
		activeNav = nav;
		AnimateBackColor((Control)nav, ACTIVE_BG, 190);
		foreach (Control item2 in (ArrangedElementCollection)((Control)nav).Controls)
		{
			Control val2 = item2;
			if (val2 is Label)
			{
				val2.ForeColor = Color.White;
			}
		}
		AnimateIndicator(((Control)nav).Top + 1);
		foreach (Panel value in navMap.Values)
		{
			((Control)value).Visible = false;
		}
		Panel val3 = navMap[nav];
		((Control)val3).Visible = true;
		((Control)val3).BringToFront();
		RelayoutRows(val3);
		AnimatePage(val3);
		if (secondaryNav != null)
		{
			((Control)secondaryNav).BringToFront();
		}
		DoSearch(null, null);
	}

	private void AnimateIndicator(int targetY)
	{
		if (indicator != null)
		{
			AnimatePosition((Control)indicator, new Point(((Control)indicator).Left, targetY), 190);
		}
	}

	private void AnimatePage(Panel page)
	{
		if (page == null || page.IsDisposed)
		{
			return;
		}
		if (pageAnimation != null)
		{
			pageAnimation.Stop();
			((Component)(object)pageAnimation).Dispose();
			pageAnimation = null;
		}
		foreach (KeyValuePair<Control, Point> animatedPosition in animatedPositions)
		{
			if (!animatedPosition.Key.IsDisposed)
			{
				animatedPosition.Key.Location = animatedPosition.Value;
			}
		}
		animatedPositions.Clear();

		if (page.Dock == DockStyle.None)
		{
			Point target = page.Location;
			animatedPositions[page] = target;
			page.Location = new Point(target.X + 18, target.Y);
			AnimatePosition(page, target, 230);
		}
		else
		{
			int viewportTop = -page.AutoScrollPosition.Y;
			Rectangle viewport = new Rectangle(0, viewportTop, page.ClientSize.Width, page.ClientSize.Height);
			int count = 0;
			foreach (Control control in page.Controls)
			{
				if (!control.Visible || control.Dock != DockStyle.None || !viewport.IntersectsWith(control.Bounds))
				{
					continue;
				}
				Point target = control.Location;
				animatedPositions[control] = target;
				control.Location = new Point(target.X, target.Y + 12);
				AnimatePosition(control, target, 180 + Math.Min(90, count * 8));
				count++;
				if (count >= 18)
				{
					break;
				}
			}
		}
		pageAnimation = new System.Windows.Forms.Timer();
		pageAnimation.Interval = 320;
		pageAnimation.Tick += delegate
		{
			pageAnimation.Stop();
			((Component)(object)pageAnimation).Dispose();
			pageAnimation = null;
			animatedPositions.Clear();
		};
		pageAnimation.Start();
	}

	private void StartOptimizerGlow()
	{
		if (optimizerButton != null)
		{
			((Control)optimizerButton).BackColor = Color.FromArgb(34, 103, 239);
			((Control)optimizerButton).ForeColor = Color.White;
		}
	}

	private void ApplyRoundedStyle(Control root)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Expected O, but got Unknown
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Expected O, but got Unknown
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Expected O, but got Unknown
		foreach (Control item in (ArrangedElementCollection)root.Controls)
		{
			Control val2 = item;
			Button button = (Button)((val2 is Button) ? val2 : null);
			if (button != null)
			{
				((ButtonBase)button).FlatStyle = (FlatStyle)0;
				((ButtonBase)button).UseMnemonic = false;
				((ButtonBase)button).FlatAppearance.BorderSize = ((((Control)button).Height >= 60) ? 1 : 0);
				bool preserveColor = string.Equals(((Control)button).Tag as string, "preserve-color", StringComparison.Ordinal) || string.Equals(((Control)button).Tag as string, "theme-preview", StringComparison.Ordinal);
				if ((object)((Control)button).Parent == sidebar)
				{
					((Control)button).BackColor = ((button == optimizerButton) ? Color.FromArgb(34, 103, 239) : Color.FromArgb(12, 25, 45));
				}
				else if (!preserveColor && ((Control)button).Height >= 60)
				{
					((Control)button).BackColor = Color.FromArgb(16, 45, 93);
				}
				else if (!preserveColor)
				{
					((Control)button).BackColor = ACC;
				}
				((Control)button).ForeColor = Color.White;
				RoundControl((Control)button, (((Control)button).Height >= 60) ? 15 : 9);
				if (!preserveColor && (object)((Control)button).Parent != sidebar && ((Control)button).Height < 60 && ((Control)button).Text.IndexOf("OPTIMIZE", StringComparison.OrdinalIgnoreCase) < 0)
				{
					Color restColor = ((Control)button).BackColor;
					((Control)button).MouseEnter += delegate
					{
						//IL_000c: Unknown result type (might be due to invalid IL or missing references)
						//IL_0030: Expected O, but got Unknown
						AnimateBackColor((Control)button, Blend(restColor, Color.White, 0.18f), 150);
					};
					((Control)button).MouseLeave += delegate
					{
						//IL_000c: Unknown result type (might be due to invalid IL or missing references)
						//IL_0021: Expected O, but got Unknown
						AnimateBackColor((Control)button, restColor, 210);
					};
				}
			}
			else if (val2 is TextBox)
			{
				RoundControl(val2, 7);
			}
			else
			{
				Panel val3 = (Panel)((val2 is Panel) ? val2 : null);
				if (val3 != null && (int)((Control)val3).Dock == 0 && ((Control)val3).Width >= 80 && ((Control)val3).Height >= 38 && val3 != homePage && val3 != backupPage && val3 != secondaryNav)
				{
					RoundControl((Control)val3, (((Control)val3).Height >= 90) ? 15 : 10);
				}
			}
			if (val2.HasChildren)
			{
				ApplyRoundedStyle(val2);
			}
		}
	}

	private void RoundControl(Control control, int radius)
	{
		Action update = delegate
		{
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Expected O, but got Unknown
			if (control.Width <= 1 || control.Height <= 1 || control.IsDisposed)
			{
				return;
			}
			if (control.Region != null)
			{
				control.Region.Dispose();
			}
			GraphicsPath val = RoundedPath(new Rectangle(0, 0, control.Width, control.Height), radius);
			try
			{
				control.Region = new Region(val);
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		};
		update();
		control.Resize += delegate
		{
			update();
		};
	}

	private static GraphicsPath RoundedPath(Rectangle rectangle, int radius)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Expected O, but got Unknown
		GraphicsPath val = new GraphicsPath();
		int num = Math.Max(2, radius * 2);
		val.AddArc(rectangle.Left, rectangle.Top, num, num, 180f, 90f);
		val.AddArc(rectangle.Right - num, rectangle.Top, num, num, 270f, 90f);
		val.AddArc(rectangle.Right - num, rectangle.Bottom - num, num, num, 0f, 90f);
		val.AddArc(rectangle.Left, rectangle.Bottom - num, num, num, 90f, 90f);
		val.CloseFigure();
		return val;
	}

	private static void DrawLogoText(Graphics g, Rectangle area)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Expected O, but got Unknown
		g.SmoothingMode = (SmoothingMode)4;
		g.TextRenderingHint = (TextRenderingHint)4;
		Font val = new Font("Segoe UI Semibold", 11f, (FontStyle)1);
		try
		{
			string text = "ASTRYX TWEAKS";
			PointF pointF = new PointF(0f, 1f);
			LinearGradientBrush val2 = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, area.Width), Math.Max(1, area.Height)), Color.FromArgb(224, 238, 255), Color.FromArgb(147, 174, 255), (LinearGradientMode)0);
			try
			{
				g.DrawString(text, val, (Brush)(object)val2, pointF);
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private void StyleCard(Panel card)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		Control c = (Control)(object)card;
		bool[] hover = new bool[1];
		RoundControl(c, 13);
		c.Paint += (PaintEventHandler)delegate(object _cs, PaintEventArgs _ce)
		{
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Expected O, but got Unknown
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Expected O, but got Unknown
			Graphics graphics = _ce.Graphics;
			graphics.SmoothingMode = (SmoothingMode)4;
			GraphicsPath val2 = RoundedPath(new Rectangle(0, 0, c.Width - 1, c.Height - 1), 13);
			try
			{
				Pen val3 = new Pen(hover[0] ? Color.FromArgb(170, 108, 165, 255) : Color.FromArgb(58, 74, 120, 205), hover[0] ? 1.5f : 1f);
				try
				{
					graphics.DrawPath(val3, val2);
				}
				finally
				{
					((IDisposable)val3)?.Dispose();
				}
				Pen val4 = new Pen(Color.FromArgb(hover[0] ? 72 : 34, 255, 255, 255));
				try
				{
					graphics.DrawLine(val4, 10f, 1f, (float)(c.Width - 11), 1f);
				}
				finally
				{
					((IDisposable)val4)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val2)?.Dispose();
			}
		};
		EventHandler eventHandler = delegate
		{
			if (!hover[0])
			{
				hover[0] = true;
				AnimateBackColor(c, Blend(CARD_BOT, ACC, 0.12f), 150);
				c.Invalidate();
			}
		};
		EventHandler eventHandler2 = delegate
		{
			if (!c.ClientRectangle.Contains(c.PointToClient(Cursor.Position)))
			{
				hover[0] = false;
				AnimateBackColor(c, CARD_BOT, 200);
				c.Invalidate();
			}
		};
		c.MouseEnter += eventHandler;
		c.MouseLeave += eventHandler2;
		foreach (Control item in (ArrangedElementCollection)c.Controls)
		{
			item.MouseEnter += eventHandler;
			item.MouseLeave += eventHandler2;
		}
	}

	private static Color Blend(Color from, Color to, float amount)
	{
		amount = Math.Max(0f, Math.Min(1f, amount));
		return Color.FromArgb((int)((float)(int)from.R + (float)(to.R - from.R) * amount), (int)((float)(int)from.G + (float)(to.G - from.G) * amount), (int)((float)(int)from.B + (float)(to.B - from.B) * amount));
	}

	private static float EaseOutCubic(float progress)
	{
		progress = Math.Max(0f, Math.Min(1f, progress));
		float num = 1f - progress;
		return 1f - num * num * num;
	}

	private static float EaseInCubic(float progress)
	{
		progress = Math.Max(0f, Math.Min(1f, progress));
		return progress * progress * progress;
	}

	private void AnimateBackColor(Control control, Color target, int duration)
	{
		if (control == null || control.IsDisposed)
		{
			return;
		}
		if (colorAnimations.TryGetValue(control, out System.Windows.Forms.Timer existing))
		{
			existing.Stop();
			existing.Dispose();
			colorAnimations.Remove(control);
		}
		Color start = control.BackColor;
		if (start == target || duration <= 0)
		{
			control.BackColor = target;
			return;
		}
		DateTime started = DateTime.UtcNow;
		System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
		timer.Interval = 15;
		colorAnimations[control] = timer;
		timer.Tick += delegate
		{
			if (control.IsDisposed)
			{
				timer.Stop();
				timer.Dispose();
				colorAnimations.Remove(control);
				return;
			}
			float progress = (float)(DateTime.UtcNow - started).TotalMilliseconds / Math.Max(1, duration);
			if (progress >= 1f)
			{
				control.BackColor = target;
				timer.Stop();
				timer.Dispose();
				colorAnimations.Remove(control);
				return;
			}
			control.BackColor = Blend(start, target, EaseOutCubic(progress));
		};
		timer.Start();
	}

	private void AnimatePosition(Control control, Point target, int duration)
	{
		if (control == null || control.IsDisposed)
		{
			return;
		}
		if (positionAnimations.TryGetValue(control, out System.Windows.Forms.Timer existing))
		{
			existing.Stop();
			existing.Dispose();
			positionAnimations.Remove(control);
		}
		Point start = control.Location;
		if (start == target || duration <= 0)
		{
			control.Location = target;
			return;
		}
		DateTime started = DateTime.UtcNow;
		System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
		timer.Interval = 15;
		positionAnimations[control] = timer;
		timer.Tick += delegate
		{
			if (control.IsDisposed)
			{
				timer.Stop();
				timer.Dispose();
				positionAnimations.Remove(control);
				return;
			}
			float progress = (float)(DateTime.UtcNow - started).TotalMilliseconds / Math.Max(1, duration);
			if (progress >= 1f)
			{
				control.Location = target;
				timer.Stop();
				timer.Dispose();
				positionAnimations.Remove(control);
				return;
			}
			float eased = EaseOutCubic(progress);
			control.Location = new Point(
				start.X + (int)((target.X - start.X) * eased),
				start.Y + (int)((target.Y - start.Y) * eased));
		};
		timer.Start();
	}

	private void AnimatedFormClosing(object sender, FormClosingEventArgs e)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (maximumRunning)
		{
			((CancelEventArgs)(object)e).Cancel = true;
			MessageBox.Show("The Maximum optimizer is still running. Wait for all tweaks to finish before closing.", "Optimization in progress", (MessageBoxButtons)0, (MessageBoxIcon)64);
		}
	}

	private void RPClick(object s, EventArgs e)
	{
		string text = PromptRestoreName();
		if (text != null)
		{
			CreateNamedRestorePoint(text);
		}
	}

	private void EnsureProfile()
	{
		try
		{
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\AstryxTweaks");
			if (registryKey != null)
			{
				profileName = (registryKey.GetValue("Name") as string) ?? "";
				profileEmail = (registryKey.GetValue("Email") as string) ?? "";
				registryKey.Close();
			}
		}
		catch
		{
		}
		if (!string.IsNullOrEmpty(profileName))
		{
			return;
		}
		ShowLoginDialog();
		try
		{
			RegistryKey registryKey2 = Registry.CurrentUser.CreateSubKey("Software\\AstryxTweaks");
			if (registryKey2 != null)
			{
				registryKey2.SetValue("Name", profileName ?? "");
				registryKey2.SetValue("Email", profileEmail ?? "");
				registryKey2.Close();
			}
		}
		catch
		{
		}
	}

	private void ShowLoginDialog()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Expected O, but got Unknown
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Expected O, but got Unknown
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Expected O, but got Unknown
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Expected O, but got Unknown
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Expected O, but got Unknown
		//IL_0207: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Expected O, but got Unknown
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Expected O, but got Unknown
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Expected O, but got Unknown
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_033d: Expected O, but got Unknown
		//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b8: Expected O, but got Unknown
		//IL_0426: Unknown result type (might be due to invalid IL or missing references)
		//IL_0430: Expected O, but got Unknown
		//IL_0455: Unknown result type (might be due to invalid IL or missing references)
		//IL_045c: Expected O, but got Unknown
		//IL_04ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d4: Expected O, but got Unknown
		//IL_04ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0505: Invalid comparison between Unknown and I4
		Form dlg = new Form();
		((Control)dlg).Text = "Welcome to Astryx Tweaks";
		dlg.FormBorderStyle = (FormBorderStyle)0;
		dlg.StartPosition = (FormStartPosition)1;
		dlg.Size = new Size(460, 360);
		((Control)dlg).BackColor = BG;
		((Control)dlg).ForeColor = TXT;
		((Control)dlg).Font = FN;
		((Control)dlg).Paint += (PaintEventHandler)delegate(object _bs, PaintEventArgs _be)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Expected O, but got Unknown
			Pen val8 = new Pen(ACC, 1f);
			try
			{
				_be.Graphics.DrawRectangle(val8, 0, 0, ((Control)dlg).Width - 1, ((Control)dlg).Height - 1);
			}
			finally
			{
				((IDisposable)val8)?.Dispose();
			}
		};
		Panel val = new Panel();
		((Control)val).Location = new Point(0, 0);
		((Control)val).Size = new Size(460, 5);
		((Control)val).BackColor = ACC;
		((Control)dlg).Controls.Add((Control)(object)val);
		Label val2 = new Label();
		((Control)val2).Text = "Welcome to Astryx Tweaks";
		((Control)val2).Font = new Font("Segoe UI Semibold", 20f);
		((Control)val2).ForeColor = TXT;
		((Control)val2).Location = new Point(30, 30);
		((Control)val2).AutoSize = true;
		((Control)dlg).Controls.Add((Control)(object)val2);
		Label val3 = new Label();
		((Control)val3).Text = "Sign in with Google to create your free profile.\r\nClick the button, log in, then paste your Gmail address below.";
		((Control)val3).Font = FB;
		((Control)val3).ForeColor = MUTED;
		((Control)val3).Location = new Point(32, 74);
		((Control)val3).Size = new Size(400, 40);
		((Control)dlg).Controls.Add((Control)(object)val3);
		Button val4 = new Button();
		((Control)val4).Text = "Sign in with Google";
		((Control)val4).Location = new Point(32, 122);
		((Control)val4).Size = new Size(396, 40);
		((ButtonBase)val4).FlatStyle = (FlatStyle)0;
		((ButtonBase)val4).FlatAppearance.BorderSize = 0;
		((Control)val4).BackColor = Color.White;
		((Control)val4).ForeColor = Color.FromArgb(32, 33, 36);
		((Control)val4).Font = new Font("Segoe UI Semibold", 11f);
		((Control)val4).Click += delegate
		{
			try
			{
				Process.Start(new ProcessStartInfo("https://accounts.google.com/")
				{
					UseShellExecute = true
				});
			}
			catch
			{
			}
		};
		((Control)dlg).Controls.Add((Control)(object)val4);
		Label val5 = new Label();
		((Control)val5).Text = "Your Gmail address";
		((Control)val5).Font = FS;
		((Control)val5).ForeColor = ACC;
		((Control)val5).Location = new Point(32, 180);
		((Control)val5).AutoSize = true;
		((Control)dlg).Controls.Add((Control)(object)val5);
		TextBox emBox = new TextBox();
		((Control)emBox).Location = new Point(32, 202);
		((Control)emBox).Size = new Size(396, 26);
		((TextBoxBase)emBox).BorderStyle = (BorderStyle)1;
		((Control)emBox).BackColor = Color.FromArgb(18, 26, 46);
		((Control)emBox).ForeColor = TXT;
		((Control)emBox).Font = FB;
		((Control)dlg).Controls.Add((Control)(object)emBox);
		Label err = new Label();
		((Control)err).Text = "";
		((Control)err).Font = FS;
		((Control)err).ForeColor = WARN;
		((Control)err).Location = new Point(32, 232);
		((Control)err).Size = new Size(396, 18);
		((Control)dlg).Controls.Add((Control)(object)err);
		Button val6 = new Button();
		((Control)val6).Text = "Continue";
		((Control)val6).Location = new Point(228, 262);
		((Control)val6).Size = new Size(200, 42);
		((ButtonBase)val6).FlatStyle = (FlatStyle)0;
		((ButtonBase)val6).FlatAppearance.BorderSize = 0;
		((Control)val6).BackColor = ACC;
		((Control)val6).ForeColor = Color.White;
		((Control)val6).Font = new Font("Segoe UI Semibold", 11f);
		((Control)val6).Click += delegate
		{
			string text = (((Control)emBox).Text ?? "").Trim();
			int num = text.IndexOf('@');
			if (num < 1 || !text.Contains("."))
			{
				((Control)err).Text = "Enter a valid email address (e.g. yourname@gmail.com).";
			}
			else
			{
				profileEmail = text;
				profileName = text.Substring(0, num);
				dlg.DialogResult = (DialogResult)1;
			}
		};
		((Control)dlg).Controls.Add((Control)(object)val6);
		Button val7 = new Button();
		((Control)val7).Text = "Skip";
		((Control)val7).Location = new Point(32, 262);
		((Control)val7).Size = new Size(120, 42);
		((ButtonBase)val7).FlatStyle = (FlatStyle)0;
		((ButtonBase)val7).FlatAppearance.BorderSize = 0;
		((Control)val7).BackColor = Color.FromArgb(34, 40, 58);
		((Control)val7).ForeColor = TXT;
		((Control)val7).Font = new Font("Segoe UI Semibold", 11f);
		((Control)val7).Click += delegate
		{
			profileEmail = "";
			profileName = "Guest";
			dlg.DialogResult = (DialogResult)1;
		};
		((Control)dlg).Controls.Add((Control)(object)val7);
		if ((int)dlg.ShowDialog() != 1 && string.IsNullOrEmpty(profileName))
		{
			profileName = "Guest";
		}
	}

	private string PromptRestoreName()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected O, but got Unknown
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Expected O, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Expected O, but got Unknown
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Expected O, but got Unknown
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Expected O, but got Unknown
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Expected O, but got Unknown
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Expected O, but got Unknown
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Expected O, but got Unknown
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Invalid comparison between Unknown and I4
		Form dlg = new Form();
		((Control)dlg).Text = "Name your restore point";
		dlg.FormBorderStyle = (FormBorderStyle)0;
		dlg.StartPosition = (FormStartPosition)4;
		dlg.Size = new Size(440, 190);
		((Control)dlg).BackColor = BG;
		((Control)dlg).ForeColor = TXT;
		((Control)dlg).Font = FN;
		((Control)dlg).Paint += (PaintEventHandler)delegate(object _bs, PaintEventArgs _be)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Expected O, but got Unknown
			Pen val6 = new Pen(ACC, 1f);
			try
			{
				_be.Graphics.DrawRectangle(val6, 0, 0, ((Control)dlg).Width - 1, ((Control)dlg).Height - 1);
			}
			finally
			{
				((IDisposable)val6)?.Dispose();
			}
		};
		Panel val = new Panel();
		((Control)val).Location = new Point(0, 0);
		((Control)val).Size = new Size(440, 5);
		((Control)val).BackColor = ACC;
		((Control)dlg).Controls.Add((Control)(object)val);
		Label val2 = new Label();
		((Control)val2).Text = "Name your restore point";
		((Control)val2).Font = new Font("Segoe UI Semibold", 14f);
		((Control)val2).ForeColor = TXT;
		((Control)val2).Location = new Point(24, 22);
		((Control)val2).AutoSize = true;
		((Control)dlg).Controls.Add((Control)(object)val2);
		TextBox val3 = new TextBox();
		((Control)val3).Text = "Astryx Tweaks Restore " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
		((Control)val3).Location = new Point(24, 60);
		((Control)val3).Size = new Size(392, 26);
		((TextBoxBase)val3).BorderStyle = (BorderStyle)1;
		((Control)val3).BackColor = Color.FromArgb(18, 26, 46);
		((Control)val3).ForeColor = TXT;
		((Control)val3).Font = FB;
		((Control)dlg).Controls.Add((Control)(object)val3);
		Button val4 = new Button();
		((Control)val4).Text = "Create";
		((Control)val4).Location = new Point(236, 110);
		((Control)val4).Size = new Size(180, 38);
		((ButtonBase)val4).FlatStyle = (FlatStyle)0;
		((ButtonBase)val4).FlatAppearance.BorderSize = 0;
		((Control)val4).BackColor = ACC;
		((Control)val4).ForeColor = Color.White;
		((Control)val4).Font = new Font("Segoe UI Semibold", 11f);
		((Control)val4).Click += delegate
		{
			dlg.DialogResult = (DialogResult)1;
		};
		((Control)dlg).Controls.Add((Control)(object)val4);
		Button val5 = new Button();
		((Control)val5).Text = "Cancel";
		((Control)val5).Location = new Point(24, 110);
		((Control)val5).Size = new Size(120, 38);
		((ButtonBase)val5).FlatStyle = (FlatStyle)0;
		((ButtonBase)val5).FlatAppearance.BorderSize = 0;
		((Control)val5).BackColor = Color.FromArgb(34, 40, 58);
		((Control)val5).ForeColor = TXT;
		((Control)val5).Font = new Font("Segoe UI Semibold", 11f);
		((Control)val5).Click += delegate
		{
			dlg.DialogResult = (DialogResult)2;
		};
		((Control)dlg).Controls.Add((Control)(object)val5);
		if ((int)dlg.ShowDialog((IWin32Window)(object)this) == 1)
		{
			if (!string.IsNullOrWhiteSpace(((Control)val3).Text))
			{
				return ((Control)val3).Text.Trim();
			}
			return "Astryx Tweaks Restore Point";
		}
		return null;
	}

	private void BuildProfileCard()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Expected O, but got Unknown
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Expected O, but got Unknown
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Expected O, but got Unknown
		Panel val = new Panel();
		((Control)val).Dock = (DockStyle)2;
		((Control)val).Height = 64;
		((Control)val).BackColor = Color.FromArgb(13, 21, 38);
		string initial = (string.IsNullOrEmpty(profileName) ? "?" : profileName.Substring(0, 1).ToUpper());
		Panel val2 = new Panel();
		((Control)val2).Location = new Point(14, 14);
		((Control)val2).Size = new Size(36, 36);
		((Control)val2).BackColor = Color.FromArgb(13, 21, 38);
		((Control)val2).Paint += (PaintEventHandler)delegate(object _as, PaintEventArgs _ae)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Expected O, but got Unknown
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Expected O, but got Unknown
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Expected O, but got Unknown
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Expected O, but got Unknown
			_ae.Graphics.SmoothingMode = (SmoothingMode)4;
			LinearGradientBrush val5 = new LinearGradientBrush(new Rectangle(0, 0, 36, 36), ACC, ACC2, (LinearGradientMode)2);
			try
			{
				_ae.Graphics.FillEllipse((Brush)(object)val5, 0, 0, 35, 35);
			}
			finally
			{
				((IDisposable)val5)?.Dispose();
			}
			StringFormat val6 = new StringFormat
			{
				Alignment = (StringAlignment)1,
				LineAlignment = (StringAlignment)1
			};
			try
			{
				SolidBrush val7 = new SolidBrush(Color.White);
				try
				{
					_ae.Graphics.DrawString(initial, new Font("Segoe UI Semibold", 13f), (Brush)(object)val7, new RectangleF(0f, 0f, 36f, 36f), val6);
				}
				finally
				{
					((IDisposable)val7)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val6)?.Dispose();
			}
		};
		((Control)val).Controls.Add((Control)(object)val2);
		Label val3 = new Label();
		((Control)val3).Text = (string.IsNullOrEmpty(profileName) ? "Guest" : profileName);
		((Control)val3).Font = FB;
		((Control)val3).ForeColor = TXT;
		((Control)val3).Location = new Point(58, 13);
		((Control)val3).Size = new Size(138, 20);
		val3.AutoEllipsis = true;
		((Control)val).Controls.Add((Control)(object)val3);
		Label val4 = new Label();
		((Control)val4).Text = "Free";
		((Control)val4).Font = FS;
		((Control)val4).ForeColor = ACC;
		((Control)val4).Location = new Point(58, 34);
		((Control)val4).AutoSize = true;
		((Control)val).Controls.Add((Control)(object)val4);
		((Control)sidebar).Controls.Add((Control)(object)val);
		((Control)val).BringToFront();
	}

	private void BuildBackups()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected O, but got Unknown
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Expected O, but got Unknown
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Expected O, but got Unknown
		backupPage = new Panel();
		((Control)backupPage).Location = new Point(224, 0);
		((Control)backupPage).Size = new Size(840, 700);
		((Control)backupPage).Anchor = (AnchorStyles)15;
		((Control)backupPage).BackColor = BG;
		((Control)backupPage).Visible = false;
		((Control)this).Controls.Add((Control)(object)backupPage);
		Label val = new Label();
		((Control)val).Text = "Backup & Restore";
		val.UseMnemonic = false;
		((Control)val).Font = new Font("Segoe UI Semibold", 22f);
		((Control)val).ForeColor = TXT;
		((Control)val).Location = new Point(28, 20);
		((Control)val).AutoSize = true;
		((Control)backupPage).Controls.Add((Control)(object)val);
		Label val2 = new Label();
		((Control)val2).Text = "Create a Windows restore point and verify the backups available on this PC.";
		((Control)val2).Font = FB;
		((Control)val2).ForeColor = MUTED;
		((Control)val2).Location = new Point(30, 54);
		((Control)val2).AutoSize = true;
		((Control)backupPage).Controls.Add((Control)(object)val2);
		Button val3 = BackupButton("+  Create new", 28, 92, ACC);
		((Control)val3).Click += delegate
		{
			string text = PromptRestoreName();
			if (text != null)
			{
				CreateNamedRestorePoint(text);
			}
		};
		((Control)backupPage).Controls.Add((Control)(object)val3);
		Button val4 = BackupButton("↻  Refresh", 176, 92, Color.FromArgb(30, 32, 48));
		((Control)val4).Click += delegate
		{
			RenderBackups();
		};
		((Control)backupPage).Controls.Add((Control)(object)val4);
		Button val5 = BackupButton("Open System Restore", 304, 92, Color.FromArgb(30, 32, 48));
		((Control)val5).Click += delegate
		{
			Process.Start("rstrui.exe");
		};
		((Control)backupPage).Controls.Add((Control)(object)val5);
		backupRows = new Panel();
		((Control)backupRows).Location = new Point(28, 144);
		((Control)backupRows).Size = new Size(780, 470);
		((Control)backupRows).BackColor = Color.FromArgb(16, 18, 30);
		((Control)backupPage).Controls.Add((Control)(object)backupRows);
		RenderBackups();
	}

	private Button BackupButton(string text, int x, int y, Color color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		Button val = new Button
		{
			Text = text,
			Location = new Point(x, y),
			Size = new Size(136, 34),
			FlatStyle = (FlatStyle)0
		};
		((ButtonBase)val).FlatAppearance.BorderSize = 0;
		((Control)val).BackColor = color;
		((Control)val).ForeColor = Color.White;
		((Control)val).Font = FB;
		return val;
	}

	private void ShowBackups()
	{
		HideUtilityPages();
		foreach (Panel value in navMap.Values)
		{
			((Control)value).Visible = false;
		}
		((Control)homePage).Visible = false;
		((Control)secondaryNav).Visible = false;
		if (defenderPage != null)
		{
			((Control)defenderPage).Visible = false;
		}
		if (powerPage != null)
		{
			((Control)powerPage).Visible = false;
		}
		((Control)backupPage).Location = new Point(((Control)sidebar).Width, 0);
		((Control)backupPage).Size = new Size(Math.Max(760, ((Form)this).ClientSize.Width - ((Control)sidebar).Width), Math.Max(580, ((Form)this).ClientSize.Height - 28));
		((Control)backupPage).Visible = true;
		((Control)backupPage).BringToFront();
		AnimatePage(backupPage);
		RenderBackups();
	}

	private bool CreateBackup()
	{
		return CreateNamedRestorePoint("Astryx Tweaks Maximum Tweaks Edition backup");
	}

	private bool CreateNamedRestorePoint(string name)
	{
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrWhiteSpace(name))
		{
			name = "Astryx Tweaks Restore Point";
		}
		Status("Creating restore point...");
		try
		{
			try
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\SystemRestore", "SystemRestorePointCreationFrequency", 0, null);
			}
			catch
			{
			}
			try
			{
				RunPS("Enable-ComputerRestore -Drive \"$env:SystemDrive\\\" -EA 0");
			}
			catch
			{
			}
			uint num = Convert.ToUInt32(((ManagementObject)new ManagementClass("root\\default", "SystemRestore", (ObjectGetOptions)null)).InvokeMethod("CreateRestorePoint", new object[3] { name, 12, 100 }));
			if (num == 0)
			{
				Status("Restore point created and verified.");
				if (backupPage != null && ((Control)backupPage).Visible)
				{
					RenderBackups();
				}
				return true;
			}
			throw new InvalidOperationException("Windows System Restore returned code " + num + ".");
		}
		catch (Exception ex)
		{
			Status("Restore point was not created.");
			MessageBox.Show("Windows did not create a restore point. System Protection may be disabled or Windows may be limiting new restore points.\n\n" + ex.Message, "Backup not created", (MessageBoxButtons)0, (MessageBoxIcon)48);
			return false;
		}
	}

	private void RenderBackups()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Expected O, but got Unknown
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Expected O, but got Unknown
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Expected O, but got Unknown
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Expected O, but got Unknown
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Expected O, but got Unknown
		if (backupRows == null)
		{
			return;
		}
		((Control)backupRows).Controls.Clear();
		Label val = new Label();
		((Control)val).Text = "BACKUP NAME                                      CREATION DATE                         TYPE";
		((Control)val).Location = new Point(22, 18);
		((Control)val).Size = new Size(720, 20);
		((Control)val).Font = FS;
		((Control)val).ForeColor = MUTED;
		((Control)backupRows).Controls.Add((Control)(object)val);
		string[] array = RunPSCapture("Get-ComputerRestorePoint -EA SilentlyContinue | Sort-Object SequenceNumber -Descending | Select-Object -First 8 | ForEach-Object { $_.Description + '|' + $_.CreationTime }").Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 0)
		{
			Label val2 = new Label();
			((Control)val2).Text = "No restore points found. Create one before applying major changes.";
			((Control)val2).Location = new Point(22, 64);
			((Control)val2).Size = new Size(600, 24);
			((Control)val2).ForeColor = MUTED;
			((Control)val2).Font = FB;
			((Control)backupRows).Controls.Add((Control)(object)val2);
			return;
		}
		int num = 48;
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string[] array3 = array2[i].Split(new char[1] { '|' });
			Panel val3 = new Panel();
			((Control)val3).Location = new Point(10, num);
			((Control)val3).Size = new Size(760, 46);
			((Control)val3).BackColor = ((num % 2 == 0) ? Color.FromArgb(20, 22, 36) : Color.FromArgb(17, 19, 31));
			Label val4 = new Label();
			((Control)val4).Text = array3[0];
			((Control)val4).Location = new Point(14, 14);
			((Control)val4).Size = new Size(390, 20);
			((Control)val4).ForeColor = TXT;
			((Control)val4).Font = FB;
			((Control)val3).Controls.Add((Control)(object)val4);
			Label val5 = new Label();
			((Control)val5).Text = ((array3.Length > 1) ? array3[1] : "");
			((Control)val5).Location = new Point(410, 14);
			((Control)val5).Size = new Size(210, 20);
			((Control)val5).ForeColor = MUTED;
			((Control)val5).Font = FS;
			((Control)val3).Controls.Add((Control)(object)val5);
			Label val6 = new Label();
			((Control)val6).Text = "WINDOWS";
			((Control)val6).Location = new Point(650, 14);
			((Control)val6).Size = new Size(90, 20);
			((Control)val6).ForeColor = GRN;
			((Control)val6).Font = FS;
			((Control)val3).Controls.Add((Control)(object)val6);
			((Control)backupRows).Controls.Add((Control)(object)val3);
			num += 48;
		}
	}

	private void ShowDefender()
	{
		HideUtilityPages();
		foreach (Panel value in navMap.Values)
		{
			((Control)value).Visible = false;
		}
		((Control)homePage).Visible = false;
		((Control)secondaryNav).Visible = false;
		if (backupPage != null)
		{
			((Control)backupPage).Visible = false;
		}
		if (powerPage != null)
		{
			((Control)powerPage).Visible = false;
		}
		((Control)defenderPage).Location = new Point(((Control)sidebar).Width, 0);
		((Control)defenderPage).Size = new Size(Math.Max(760, ((Form)this).ClientSize.Width - ((Control)sidebar).Width), Math.Max(580, ((Form)this).ClientSize.Height - 28));
		((Control)defenderPage).Visible = true;
		((Control)defenderPage).BringToFront();
		AnimatePage(defenderPage);
	}

	private void ShowPower()
	{
		HideUtilityPages();
		foreach (Panel value in navMap.Values)
		{
			((Control)value).Visible = false;
		}
		((Control)homePage).Visible = false;
		((Control)secondaryNav).Visible = false;
		if (backupPage != null)
		{
			((Control)backupPage).Visible = false;
		}
		if (defenderPage != null)
		{
			((Control)defenderPage).Visible = false;
		}
		((Control)powerPage).Location = new Point(((Control)sidebar).Width, 0);
		((Control)powerPage).Size = new Size(Math.Max(760, ((Form)this).ClientSize.Width - ((Control)sidebar).Width), Math.Max(580, ((Form)this).ClientSize.Height - 28));
		((Control)powerPage).Visible = true;
		((Control)powerPage).BringToFront();
		AnimatePage(powerPage);
		RenderPowerPlans();
	}

	private List<object[]> DefenderDefs()
	{
		List<object[]> list = new List<object[]>();
		list.Add(new object[4]
		{
			"Real-Time Protection",
			"Live file scanning — biggest FPS gain when off",
			(Action)delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection", "DisableRealtimeMonitoring", 1, null);
			},
			(Action)delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection", "DisableRealtimeMonitoring");
			}
		});
		list.Add(new object[4]
		{
			"Behavior Monitoring",
			"Constant real-time process behavior analysis",
			(Action)delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection", "DisableBehaviorMonitoring", 1, null);
			},
			(Action)delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection", "DisableBehaviorMonitoring");
			}
		});
		list.Add(new object[4]
		{
			"On-Access Protection",
			"Scans files the moment they are opened",
			(Action)delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection", "DisableOnAccessProtection", 1, null);
			},
			(Action)delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection", "DisableOnAccessProtection");
			}
		});
		list.Add(new object[4]
		{
			"Scan On Realtime Enable",
			"Scan triggered when protection re-enables",
			(Action)delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection", "DisableScanOnRealtimeEnable", 1, null);
			},
			(Action)delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection", "DisableScanOnRealtimeEnable");
			}
		});
		list.Add(new object[4]
		{
			"Cloud-Delivered Protection (MAPS)",
			"Live cloud lookups that add latency",
			(Action)delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet", "SpynetReporting", 0, null);
			},
			(Action)delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet", "SpynetReporting");
			}
		});
		list.Add(new object[4]
		{
			"Automatic Sample Submission",
			"Uploads file samples to Microsoft",
			(Action)delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet", "SubmitSamplesConsent", 2, null);
			},
			(Action)delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet", "SubmitSamplesConsent");
			}
		});
		list.Add(new object[4]
		{
			"Windows Defender (policy)",
			"Turns Defender off via policy for max headroom",
			(Action)delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender", "DisableAntiSpyware", 1, null);
			},
			(Action)delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender", "DisableAntiSpyware");
			}
		});
		list.Add(new object[4]
		{
			"Windows SmartScreen",
			"App/URL reputation checks on launch",
			(Action)delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableSmartScreen", 0, null);
			},
			(Action)delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableSmartScreen");
			}
		});
		list.Add(new object[4]
		{
			"Defender Scheduled Scans",
			"Background scans that spike CPU/disk",
			(Action)delegate
			{
				DisTask("Microsoft\\Windows\\Windows Defender\\Windows Defender Scheduled Scan");
				DisTask("Microsoft\\Windows\\Windows Defender\\Windows Defender Cache Maintenance");
				DisTask("Microsoft\\Windows\\Windows Defender\\Windows Defender Cleanup");
				DisTask("Microsoft\\Windows\\Windows Defender\\Windows Defender Verification");
			},
			(Action)delegate
			{
				Run("schtasks", "/Change /TN \"Microsoft\\Windows\\Windows Defender\\Windows Defender Scheduled Scan\" /ENABLE");
			}
		});
		return list;
	}

	private void BuildDefenderPage()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Expected O, but got Unknown
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Expected O, but got Unknown
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Expected O, but got Unknown
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Expected O, but got Unknown
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_0278: Expected O, but got Unknown
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Expected O, but got Unknown
		//IL_0315: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Expected O, but got Unknown
		//IL_038a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0394: Expected O, but got Unknown
		defenderPage = new Panel();
		((Control)defenderPage).Location = new Point(224, 0);
		((Control)defenderPage).Size = new Size(840, 700);
		((Control)defenderPage).Anchor = (AnchorStyles)15;
		((Control)defenderPage).BackColor = BG;
		((Control)defenderPage).Visible = false;
		((Control)this).Controls.Add((Control)(object)defenderPage);
		Label val = new Label();
		((Control)val).Text = "Windows Defender";
		val.UseMnemonic = false;
		((Control)val).Font = new Font("Segoe UI Semibold", 22f);
		((Control)val).ForeColor = TXT;
		((Control)val).Location = new Point(28, 20);
		((Control)val).AutoSize = true;
		((Control)defenderPage).Controls.Add((Control)(object)val);
		Label val2 = new Label();
		((Control)val2).Text = "Turn a switch OFF to disable that protection for more FPS, then click Apply. Flip it back ON and Apply to restore it — every option here is fully reversible.";
		((Control)val2).Font = FB;
		((Control)val2).ForeColor = MUTED;
		((Control)val2).Location = new Point(30, 56);
		((Control)val2).Size = new Size(790, 22);
		((Control)defenderPage).Controls.Add((Control)(object)val2);
		Label val3 = new Label();
		((Control)val3).Text = "⚠  Disabling Defender reduces your security. Windows needs a Defender exclusion for these changes to apply — Astryx Tweaks adds and removes that exclusion for you automatically.";
		((Control)val3).Font = FB;
		((Control)val3).ForeColor = WARN;
		((Control)val3).Location = new Point(30, 80);
		((Control)val3).Size = new Size(800, 40);
		((Control)defenderPage).Controls.Add((Control)(object)val3);
		Panel val4 = new Panel();
		((Control)val4).Location = new Point(20, 128);
		((Control)val4).Size = new Size(620, 470);
		((Control)val4).BackColor = BG;
		((ScrollableControl)val4).AutoScroll = true;
		((Control)defenderPage).Controls.Add((Control)(object)val4);
		List<object[]> rows = new List<object[]>();
		int ry = 4;
		foreach (object[] item in DefenderDefs())
		{
			ToggleSwitch toggleSwitch = AddOptRow(val4, ref ry, (string)item[0], (string)item[1], GRN, on: true);
			rows.Add(new object[3]
			{
				toggleSwitch,
				item[2],
				item[3]
			});
		}
		Button val5 = new Button();
		((Control)val5).Text = "APPLY CHANGES";
		((Control)val5).Location = new Point(652, 130);
		((Control)val5).Size = new Size(168, 42);
		((ButtonBase)val5).FlatStyle = (FlatStyle)0;
		((ButtonBase)val5).FlatAppearance.BorderSize = 0;
		((Control)val5).BackColor = ACC;
		((Control)val5).ForeColor = Color.White;
		((Control)val5).Font = new Font("Segoe UI Semibold", 10f);
		((Control)val5).Click += delegate
		{
			ApplyDefenderChanges(rows);
		};
		((Control)defenderPage).Controls.Add((Control)(object)val5);
		Button val6 = new Button();
		((Control)val6).Text = "RESTORE ALL PROTECTION";
		((Control)val6).Location = new Point(652, 184);
		((Control)val6).Size = new Size(168, 42);
		((ButtonBase)val6).FlatStyle = (FlatStyle)0;
		((ButtonBase)val6).FlatAppearance.BorderSize = 0;
		((Control)val6).BackColor = GRN;
		((Control)val6).ForeColor = Color.White;
		((Control)val6).Font = new Font("Segoe UI Semibold", 9f);
		((Control)val6).Click += delegate
		{
			foreach (object[] item2 in rows)
			{
				((CheckBox)(ToggleSwitch)item2[0]).Checked = true;
			}
			ApplyDefenderChanges(rows);
		};
		((Control)defenderPage).Controls.Add((Control)(object)val6);
	}

	private void ApplyDefenderChanges(List<object[]> rows)
	{
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Invalid comparison between Unknown and I4
		int num = 0;
		foreach (object[] row in rows)
		{
			if (!((CheckBox)(ToggleSwitch)row[0]).Checked)
			{
				num++;
			}
		}
		if (num > 0)
		{
			if ((int)MessageBox.Show("You are about to DISABLE " + num + " Windows Defender protection(s).\n\nThis reduces your security. Astryx Tweaks will also add a Windows Defender exclusion automatically so the changes apply.\n\nDo you REALLY want to do this?", "Confirm Defender changes", (MessageBoxButtons)4, (MessageBoxIcon)48) != 6)
			{
				return;
			}
			try
			{
				RunPS("Add-MpPreference -ExclusionPath \"$env:SystemDrive\\\" -EA 0");
			}
			catch
			{
			}
		}
		else
		{
			try
			{
				RunPS("Remove-MpPreference -ExclusionPath \"$env:SystemDrive\\\" -EA 0");
			}
			catch
			{
			}
		}
		int num2 = 0;
		int num3 = 0;
		foreach (object[] row2 in rows)
		{
			try
			{
				if (((CheckBox)(ToggleSwitch)row2[0]).Checked)
				{
					((Action)row2[2])();
				}
				else
				{
					((Action)row2[1])();
				}
				num2++;
			}
			catch
			{
				num3++;
			}
		}
		Status("Windows Defender settings applied (" + num2 + " ok, " + num3 + " failed).");
		MessageBox.Show("Windows Defender settings applied.\n\n" + num + " protection(s) disabled, " + (rows.Count - num) + " left at / returned to Windows defaults.\n\nTo fully restore protection later, click \"RESTORE ALL PROTECTION\" (or flip every switch ON and Apply). A restart is recommended.", "Defender updated", (MessageBoxButtons)0, (MessageBoxIcon)64);
	}

	private void BuildPowerPage()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected O, but got Unknown
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Expected O, but got Unknown
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Expected O, but got Unknown
		powerPage = new Panel();
		((Control)powerPage).Location = new Point(224, 0);
		((Control)powerPage).Size = new Size(840, 700);
		((Control)powerPage).Anchor = (AnchorStyles)15;
		((Control)powerPage).BackColor = BG;
		((Control)powerPage).Visible = false;
		((Control)this).Controls.Add((Control)(object)powerPage);
		Label val = new Label();
		((Control)val).Text = "Power Plan";
		val.UseMnemonic = false;
		((Control)val).Font = new Font("Segoe UI Semibold", 22f);
		((Control)val).ForeColor = TXT;
		((Control)val).Location = new Point(28, 20);
		((Control)val).AutoSize = true;
		((Control)powerPage).Controls.Add((Control)(object)val);
		Label val2 = new Label();
		((Control)val2).Text = "Every Windows power plan on this PC is listed below. Click Activate to switch. High/Ultimate performance keep the CPU at full speed for gaming.";
		((Control)val2).Font = FB;
		((Control)val2).ForeColor = MUTED;
		((Control)val2).Location = new Point(30, 56);
		((Control)val2).Size = new Size(790, 22);
		((Control)powerPage).Controls.Add((Control)(object)val2);
		Button val3 = BackupButton("↻  Refresh", 28, 92, Color.FromArgb(30, 32, 48));
		((Control)val3).Click += delegate
		{
			RenderPowerPlans();
		};
		((Control)powerPage).Controls.Add((Control)(object)val3);
		Button val4 = BackupButton("+  Ultimate Performance", 176, 92, ACC);
		((Control)val4).Size = new Size(196, 34);
		((Control)val4).Click += delegate
		{
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				Run("powercfg", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
				Status("Ultimate Performance plan unlocked.");
			}
			catch (Exception ex)
			{
				MessageBox.Show("Could not unlock the Ultimate Performance plan.\n\n" + ex.Message, "Power plan", (MessageBoxButtons)0, (MessageBoxIcon)48);
			}
			RenderPowerPlans();
		};
		((Control)powerPage).Controls.Add((Control)(object)val4);
		Button val5 = BackupButton("Open Power Options", 384, 92, Color.FromArgb(30, 32, 48));
		((Control)val5).Size = new Size(170, 34);
		((Control)val5).Click += delegate
		{
			try
			{
				Run("control", "powercfg.cpl");
			}
			catch
			{
			}
		};
		((Control)powerPage).Controls.Add((Control)(object)val5);
		powerRows = new Panel();
		((Control)powerRows).Location = new Point(28, 144);
		((Control)powerRows).Size = new Size(790, 470);
		((Control)powerRows).BackColor = Color.FromArgb(16, 18, 30);
		((ScrollableControl)powerRows).AutoScroll = true;
		((Control)powerPage).Controls.Add((Control)(object)powerRows);
		RenderPowerPlans();
	}

	private void RenderPowerPlans()
	{
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Expected O, but got Unknown
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Expected O, but got Unknown
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Expected O, but got Unknown
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Expected O, but got Unknown
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Expected O, but got Unknown
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Expected O, but got Unknown
		if (powerRows == null)
		{
			return;
		}
		((Control)powerRows).Controls.Clear();
		string text = RunPSCapture("powercfg /list");
		string[] array = text.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
		int num = 12;
		int num2 = 0;
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			int num3 = text2.IndexOf("GUID:", StringComparison.OrdinalIgnoreCase);
			if (num3 < 0)
			{
				continue;
			}
			string text3 = text2.Substring(num3 + 5).Trim();
			int num4 = text3.IndexOf(' ');
			string text4 = ((num4 > 0) ? text3.Substring(0, num4) : text3);
			bool flag = text2.TrimEnd(new char[0]).EndsWith("*");
			string text5 = text4;
			int num5 = text2.IndexOf('(');
			int num6 = text2.IndexOf(')');
			if (num5 >= 0 && num6 > num5)
			{
				text5 = text2.Substring(num5 + 1, num6 - num5 - 1);
			}
			Panel val = new Panel();
			((Control)val).Location = new Point(10, num);
			((Control)val).Size = new Size(760, 52);
			((Control)val).BackColor = (flag ? Color.FromArgb(20, 46, 34) : ((num2 % 2 == 0) ? Color.FromArgb(20, 22, 36) : Color.FromArgb(17, 19, 31)));
			Label val2 = new Label();
			((Control)val2).Text = text5;
			((Control)val2).Location = new Point(16, 8);
			((Control)val2).Size = new Size(430, 22);
			((Control)val2).ForeColor = TXT;
			((Control)val2).Font = FB;
			((Control)val).Controls.Add((Control)(object)val2);
			Label val3 = new Label();
			((Control)val3).Text = text4;
			((Control)val3).Location = new Point(16, 30);
			((Control)val3).Size = new Size(430, 16);
			((Control)val3).ForeColor = MUTED;
			((Control)val3).Font = FS;
			((Control)val).Controls.Add((Control)(object)val3);
			if (flag)
			{
				Label val4 = new Label();
				((Control)val4).Text = "● ACTIVE";
				((Control)val4).Location = new Point(560, 16);
				((Control)val4).Size = new Size(120, 22);
				((Control)val4).ForeColor = GRN;
				((Control)val4).Font = FB;
				((Control)val).Controls.Add((Control)(object)val4);
			}
			else
			{
				Button val5 = new Button();
				((Control)val5).Text = "Activate";
				((Control)val5).Location = new Point(620, 10);
				((Control)val5).Size = new Size(120, 32);
				((ButtonBase)val5).FlatStyle = (FlatStyle)0;
				((ButtonBase)val5).FlatAppearance.BorderSize = 0;
				((Control)val5).BackColor = ACC;
				((Control)val5).ForeColor = Color.White;
				((Control)val5).Font = FB;
				string g = text4;
				((Control)val5).Click += delegate
				{
					//IL_0045: Unknown result type (might be due to invalid IL or missing references)
					try
					{
						Run("powercfg", "/setactive " + g);
						Status("Active power plan changed.");
					}
					catch (Exception ex)
					{
						MessageBox.Show("Could not switch power plan.\n\n" + ex.Message, "Power plan", (MessageBoxButtons)0, (MessageBoxIcon)48);
					}
					RenderPowerPlans();
				};
				((Control)val).Controls.Add((Control)(object)val5);
			}
			((Control)powerRows).Controls.Add((Control)(object)val);
			num += 58;
			num2++;
		}
		if (num2 == 0)
		{
			Label val6 = new Label();
			((Control)val6).Text = (text.StartsWith("ERROR") ? ("Could not read power plans: " + text) : "No power plans found.");
			((Control)val6).Location = new Point(16, 16);
			((Control)val6).Size = new Size(720, 40);
			((Control)val6).ForeColor = MUTED;
			((Control)val6).Font = FB;
			((Control)powerRows).Controls.Add((Control)(object)val6);
		}
	}

	private string RunPSCapture(string script)
	{
		try
		{
			Process process = Process.Start(new ProcessStartInfo("powershell", "-NoProfile -ExecutionPolicy Bypass -Command \"" + script.Replace("\"", "'") + "\"")
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			});
			string text = process.StandardOutput.ReadToEnd();
			string text2 = process.StandardError.ReadToEnd();
			process.WaitForExit(60000);
			return string.IsNullOrEmpty(text) ? text2 : text;
		}
		catch (Exception ex)
		{
			return "ERROR:" + ex.Message;
		}
	}

	private void SearchFocus(object s, EventArgs e)
	{
		if (searchBox != null && (((Control)searchBox).ForeColor == Color.Gray || ((Control)searchBox).Text == "Search tweaks..."))
		{
			((Control)searchBox).Text = "";
			((Control)searchBox).ForeColor = TXT;
		}
	}

	private void SearchBlur(object s, EventArgs e)
	{
		if (searchBox != null && string.IsNullOrEmpty(((Control)searchBox).Text))
		{
			((Control)searchBox).Text = "Search tweaks...";
			((Control)searchBox).ForeColor = Color.FromArgb(118, 132, 160);
		}
	}

	private void SearchChange(object s, EventArgs e)
	{
		DoSearch(s, e);
	}

	private void DoSearch(object s, EventArgs e)
	{
		if (searchBox == null || searchBox.IsDisposed)
		{
			return;
		}
		string value = ((((Control)searchBox).Text == "Search tweaks...") ? "" : ((Control)searchBox).Text.ToLower());
		foreach (CheckBox item in allCB)
		{
			tweakCards[item].Visible = string.IsNullOrEmpty(value) || tweakCards[item].Controls[1].Text.ToLower().Contains(value);
		}
	}

	private Panel GetActivePage()
	{
		if (activeNav != null && navMap.ContainsKey(activeNav))
		{
			return navMap[activeNav];
		}
		return null;
	}

	private Panel GetPageByIndex(int idx)
	{
		if (idx >= 0 && idx < orderedPages.Count)
		{
			return orderedPages[idx];
		}
		return null;
	}

	private int Head(Panel p, int y, string t)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		int num = (gridY.ContainsKey(p) ? (gridY[p] + 14) : ((y == 5) ? 62 : y));
		Label val = new Label();
		((Control)val).Text = t;
		((Control)val).Font = FH;
		((Control)val).ForeColor = TXT;
		((Control)val).Location = new Point(16, num);
		((Control)val).AutoSize = true;
		((Control)val).BackColor = Color.Transparent;
		((Control)p).Controls.Add((Control)(object)val);
		int num2 = num + 40;
		gridY[p] = num2;
		return num2;
	}

	private void GetTweakGlyph(string name, out string glyph, out Color color)
	{
		string text = (name ?? "").ToLower();
		glyph = "\ue90f";
		color = Color.FromArgb(96, 156, 255);
		if (text.Contains("mouse") || text.Contains("cursor") || text.Contains("pointer"))
		{
			glyph = "\ue962";
			color = Color.FromArgb(96, 190, 255);
		}
		else if (text.Contains("gpu") || text.Contains("nvidia") || text.Contains("graphic") || text.Contains("game") || text.Contains("fps") || text.Contains("fullscreen") || text.Contains("fso") || text.Contains("dx") || text.Contains("directx"))
		{
			glyph = "\ue7fc";
			color = Color.FromArgb(120, 220, 120);
		}
		else if (text.Contains("power") || text.Contains("battery") || text.Contains("sleep") || text.Contains("hibernate"))
		{
			glyph = "\ue945";
			color = Color.FromArgb(255, 200, 70);
		}
		else if (text.Contains("network") || text.Contains("wifi") || text.Contains("wi-fi") || text.Contains("tcp") || text.Contains("dns") || text.Contains("adapter") || text.Contains("latency") || text.Contains("nagle"))
		{
			glyph = "\ue839";
			color = Color.FromArgb(96, 200, 255);
		}
		else if (text.Contains("privacy") || text.Contains("telemetry") || text.Contains("track") || text.Contains("data collect") || text.Contains("feedback") || text.Contains("advertis") || text.Contains("cortana"))
		{
			glyph = "\ue72e";
			color = Color.FromArgb(170, 140, 255);
		}
		else if (text.Contains("defender") || text.Contains("smartscreen") || text.Contains("security") || text.Contains("uac"))
		{
			glyph = "\uea18";
			color = Color.FromArgb(255, 120, 120);
		}
		else if (text.Contains("debloat") || text.Contains("remove") || text.Contains("uninstall") || text.Contains("app") || text.Contains("store") || text.Contains("bloat"))
		{
			glyph = "\ue74d";
			color = Color.FromArgb(255, 150, 120);
		}
		else if (text.Contains("clean") || text.Contains("temp") || text.Contains("cache") || text.Contains("disk") || text.Contains("storage"))
		{
			glyph = "\uea99";
			color = Color.FromArgb(120, 210, 200);
		}
		else if (text.Contains("service") || text.Contains("svchost") || text.Contains("sysmain") || text.Contains("prefetch") || text.Contains("superfetch") || text.Contains("indexing") || text.Contains("search"))
		{
			glyph = "\ue9d9";
			color = Color.FromArgb(140, 180, 255);
		}
		else if (text.Contains("notification") || text.Contains("toast") || text.Contains("tips") || text.Contains("suggest"))
		{
			glyph = "\ue7e7";
			color = Color.FromArgb(255, 190, 110);
		}
		else if (text.Contains("update") || text.Contains("driver"))
		{
			glyph = "\ue895";
			color = Color.FromArgb(120, 200, 255);
		}
		else if (text.Contains("visual") || text.Contains("animation") || text.Contains("transparen") || text.Contains("theme") || text.Contains("wallpaper") || text.Contains("appearance") || text.Contains("menu") || text.Contains("taskbar") || text.Contains("start"))
		{
			glyph = "\ue790";
			color = Color.FromArgb(180, 150, 255);
		}
		else if (text.Contains("cpu") || text.Contains("processor") || text.Contains("core") || text.Contains("priority") || text.Contains("scheduler"))
		{
			glyph = "\ue950";
			color = Color.FromArgb(110, 220, 160);
		}
		else if (text.Contains("memory") || text.Contains("ram") || text.Contains("pagefile") || text.Contains("paging"))
		{
			glyph = "\ueea0";
			color = Color.FromArgb(120, 210, 255);
		}
		else if (text.Contains("task") || text.Contains("schedul") || text.Contains("autorun") || text.Contains("startup"))
		{
			glyph = "\ue823";
			color = Color.FromArgb(160, 180, 255);
		}
	}

	private void RelayoutRows(Panel p)
	{
		if (p == null || !pageRows.ContainsKey(p))
		{
			return;
		}
		int width = Math.Max(420, ((Control)p).ClientSize.Width - 28);
		foreach (Panel item in pageRows[p])
		{
			if (!((Control)item).IsDisposed)
			{
				((Control)item).Width = width;
				((Control)item).Invalidate();
			}
		}
	}

	private int Twk(Panel p, int y, string name, string desc, Action apply, Action revert, string warn, bool on)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Expected O, but got Unknown
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Expected O, but got Unknown
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Expected O, but got Unknown
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Expected O, but got Unknown
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Expected O, but got Unknown
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Expected O, but got Unknown
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Expected O, but got Unknown
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Expected O, but got Unknown
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Expected O, but got Unknown
		//IL_0479: Unknown result type (might be due to invalid IL or missing references)
		//IL_047e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0488: Unknown result type (might be due to invalid IL or missing references)
		int x = 14;
		int num = 980;
		int num2 = 8;
		bool flag = warn != null && warn.Length > 0;
		int num3 = (flag ? 66 : 50);
		if (!gridY.ContainsKey(p))
		{
			gridY[p] = ((y == 5) ? 14 : y);
		}
		if (!pageRows.ContainsKey(p))
		{
			pageRows[p] = new List<Panel>();
			Panel pageRef = p;
			((Control)p).Resize += delegate
			{
				RelayoutRows(pageRef);
			};
		}
		int num4 = gridY[p];
		Panel val = new Panel();
		((Control)val).Location = new Point(x, num4);
		((Control)val).Size = new Size(num, num3);
		((Control)val).BackColor = Color.FromArgb(13, 20, 38);
		((Control)p).Controls.Add((Control)(object)val);
		RoundControl((Control)val, 12);
		Panel rowRef = val;
		((Control)val).Paint += (PaintEventHandler)delegate(object cs, PaintEventArgs ce)
		{
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Expected O, but got Unknown
			Graphics graphics = ce.Graphics;
			graphics.SmoothingMode = (SmoothingMode)4;
			GraphicsPath val8 = RoundedPath(new Rectangle(0, 0, ((Control)rowRef).Width - 1, ((Control)rowRef).Height - 1), 12);
			try
			{
				Pen val9 = new Pen(Color.FromArgb(64, 92, 140, 220));
				try
				{
					graphics.DrawPath(val9, val8);
				}
				finally
				{
					((IDisposable)val9)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val8)?.Dispose();
			}
		};
		((Control)val).Resize += delegate
		{
			((Control)rowRef).Invalidate();
		};
		GetTweakGlyph(name, out var glyph, out var color);
		Label val2 = new Label();
		((Control)val2).Text = glyph;
		((Control)val2).Font = FICON;
		((Control)val2).ForeColor = color;
		((Control)val2).Location = new Point(16, (num3 - 24) / 2);
		((Control)val2).Size = new Size(26, 26);
		((Control)val2).BackColor = Color.Transparent;
		((Control)val).Controls.Add((Control)(object)val2);
		Label val3 = new Label();
		((Control)val3).Text = name;
		((Control)val3).Font = FB;
		((Control)val3).ForeColor = TXT;
		((Control)val3).Location = new Point(52, flag ? 11 : 15);
		((Control)val3).AutoSize = true;
		((Control)val3).BackColor = Color.Transparent;
		((Control)val).Controls.Add((Control)(object)val3);
		Label val4 = new Label();
		((Control)val4).Text = "\ue70d";
		((Control)val4).Font = new Font("Segoe MDL2 Assets", 7f);
		((Control)val4).ForeColor = MUTED;
		((Control)val4).AutoSize = true;
		((Control)val4).BackColor = Color.Transparent;
		((Control)val4).Location = new Point(52 + val3.PreferredWidth + 6, (flag ? 11 : 15) + 5);
		((Control)val).Controls.Add((Control)(object)val4);
		if (flag)
		{
			Label val5 = new Label();
			((Control)val5).Text = "⚠  " + warn;
			((Control)val5).Font = FS;
			((Control)val5).ForeColor = Color.FromArgb(255, 182, 72);
			((Control)val5).Location = new Point(52, 36);
			((Control)val5).AutoSize = true;
			((Control)val5).BackColor = Color.Transparent;
			((Control)val).Controls.Add((Control)(object)val5);
		}
		ToggleSwitch toggleSwitch = new ToggleSwitch();
		((Control)toggleSwitch).Location = new Point(num - 16 - 48, (num3 - 26) / 2);
		((CheckBox)toggleSwitch).Checked = on;
		((Control)toggleSwitch).Anchor = (AnchorStyles)9;
		((Control)val).Controls.Add((Control)(object)toggleSwitch);
		Label val6 = new Label();
		((Control)val6).Text = (on ? "On" : "Off");
		((Control)val6).Font = FB;
		((Control)val6).ForeColor = (on ? ACC : MUTED);
		((Control)val6).AutoSize = true;
		((Control)val6).BackColor = Color.Transparent;
		((Control)val).Controls.Add((Control)(object)val6);
		((Control)val6).Location = new Point(((Control)toggleSwitch).Left - val6.PreferredWidth - 12, (num3 - val6.PreferredHeight) / 2);
		((Control)val6).Anchor = (AnchorStyles)9;
		ToggleSwitch tgRef = toggleSwitch;
		Label stRef = val6;
		int rowHRef = num3;
		((CheckBox)toggleSwitch).CheckedChanged += delegate
		{
			bool flag2 = ((CheckBox)tgRef).Checked;
			((Control)stRef).Text = (flag2 ? "On" : "Off");
			((Control)stRef).ForeColor = (flag2 ? ACC : MUTED);
			((Control)stRef).Location = new Point(((Control)tgRef).Left - stRef.PreferredWidth - 12, (rowHRef - stRef.PreferredHeight) / 2);
		};
		allCB.Add((CheckBox)(object)toggleSwitch);
		acts[(CheckBox)(object)toggleSwitch] = apply;
		tweakNames[(CheckBox)(object)toggleSwitch] = name;
		if (revert != null)
		{
			revs[(CheckBox)(object)toggleSwitch] = revert;
		}
		tweakPages[(CheckBox)(object)toggleSwitch] = p;
		tweakCards[(CheckBox)(object)toggleSwitch] = (Control)(object)val;
		string text = (string.IsNullOrEmpty(desc) ? name : desc) + (flag ? ("\n\n⚠ " + warn) : "");
		ToolTip val7 = new ToolTip();
		val7.SetToolTip((Control)(object)val3, text);
		val7.SetToolTip((Control)(object)val2, text);
		val7.SetToolTip((Control)(object)val, text);
		pageRows[p].Add(val);
		gridY[p] = num4 + num3 + num2;
		return gridY[p];
	}

	private void ApplyClick(object s, EventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		Panel val = (Panel)((Control)(Button)s).Tag;
		List<CheckBox> list = new List<CheckBox>();
		foreach (CheckBox item in allCB)
		{
			if (tweakPages[item] == val && item.Checked && acts.ContainsKey(item))
			{
				list.Add(item);
			}
		}
		if (list.Count == 0)
		{
			Status("No tweaks selected.");
			return;
		}
		Status("Applying " + list.Count + " tweaks...");
		int num = 0;
		foreach (CheckBox item2 in list)
		{
			try
			{
				acts[item2]();
				num++;
				Status("Applied " + num + "/" + list.Count + ": " + ((Control)item2).Text);
			}
			catch
			{
				num++;
			}
		}
		Status("Done! " + list.Count + " tweaks applied.");
	}

	private void SelAllClick(object s, EventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		Panel val = (Panel)((Control)(Button)s).Tag;
		foreach (CheckBox item in allCB)
		{
			if (tweakPages[item] == val)
			{
				item.Checked = true;
			}
		}
	}

	private void DeselClick(object s, EventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		Panel val = (Panel)((Control)(Button)s).Tag;
		foreach (CheckBox item in allCB)
		{
			if (tweakPages[item] == val)
			{
				item.Checked = false;
			}
		}
	}

	private void RevertClick(object s, EventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Invalid comparison between Unknown and I4
		Panel val = (Panel)((Control)(Button)s).Tag;
		List<CheckBox> list = new List<CheckBox>();
		foreach (CheckBox item in allCB)
		{
			if (tweakPages[item] == val && item.Checked && revs.ContainsKey(item))
			{
				list.Add(item);
			}
		}
		if (list.Count == 0)
		{
			Status("No reversible tweaks.");
		}
		else
		{
			if ((int)MessageBox.Show("Revert " + list.Count + " tweaks?", "Confirm", (MessageBoxButtons)4) != 6)
			{
				return;
			}
			int num = 0;
			foreach (CheckBox item2 in list)
			{
				try
				{
					revs[item2]();
					num++;
				}
				catch
				{
				}
			}
			Status("Reverted " + num + " tweaks.");
		}
	}

	private void Status(string m)
	{
		((Control)statusLbl).Text = "  " + m;
		Application.DoEvents();
	}

	private static void Reg(string path, string name, object val, object sv)
	{
		try
		{
			string[] array = path.Split(new char[1] { '\\' });
			RegistryKey obj = (array[0].StartsWith("HKLM") ? Registry.LocalMachine : Registry.CurrentUser);
			string subkey = string.Join("\\", array, 1, array.Length - 1);
			RegistryKey registryKey = obj.CreateSubKey(subkey);
			if (registryKey != null)
			{
				if (sv != null)
				{
					registryKey.SetValue(name, sv, RegistryValueKind.String);
				}
				else if (val is uint)
				{
					registryKey.SetValue(name, val, RegistryValueKind.DWord);
				}
				else if (val is byte[])
				{
					registryKey.SetValue(name, val, RegistryValueKind.Binary);
				}
				else
				{
					registryKey.SetValue(name, val);
				}
				registryKey.Close();
			}
		}
		catch
		{
		}
	}

	private static void RegDel(string path, string name)
	{
		try
		{
			string[] array = path.Split(new char[1] { '\\' });
			RegistryKey obj = (array[0].StartsWith("HKLM") ? Registry.LocalMachine : Registry.CurrentUser);
			string name2 = string.Join("\\", array, 1, array.Length - 1);
			RegistryKey registryKey = obj.OpenSubKey(name2, writable: true);
			if (registryKey != null)
			{
				registryKey.DeleteValue(name, throwOnMissingValue: false);
				registryKey.Close();
			}
		}
		catch
		{
		}
	}

	private static void SvcDis(string n)
	{
		Run("sc", "config " + n + " start= disabled");
		Run("sc", "stop " + n);
	}

	private static void SvcAuto(string n)
	{
		Run("sc", "config " + n + " start= auto");
	}

	private static void DisTask(string n)
	{
		Run("schtasks", "/Change /TN \"" + n + "\" /Disable");
	}

	private static void Winget(string id)
	{
		Run("winget", "install --id " + id + " --accept-package-agreements --accept-source-agreements --silent");
	}

	private static void RemoveApp(string n)
	{
		RunPS("Get-AppxPackage *" + n + "* | Remove-AppxPackage -EA 0");
		RunPS("Get-AppxProvisionedPackage -Online | Where {$_.DisplayName -like '*" + n + "*'} | Remove-AppxProvisionedPackage -Online -EA 0");
	}

	private static void CleanDir(string d)
	{
		try
		{
			string[] files = Directory.GetFiles(d);
			for (int i = 0; i < files.Length; i++)
			{
				File.Delete(files[i]);
			}
		}
		catch
		{
		}
	}

	private static void Run(string f, string a)
	{
		try
		{
			Process.Start(new ProcessStartInfo(f, a)
			{
				UseShellExecute = false,
				CreateNoWindow = true
			})?.WaitForExit(30000);
		}
		catch
		{
		}
	}

	private static void RunPS(string c)
	{
		Run("powershell", "-NoProfile -ExecutionPolicy Bypass -Command \"" + c.Replace("\"", "'") + "\"");
	}

	private void BuildGeneral()
	{
		Panel pageByIndex = GetPageByIndex(0);
		int y = 5;
		y = Head(pageByIndex, y, "Power & Performance");
		y = Twk(pageByIndex, y, "Set power plan to High Performance", "Disables power saving, max CPU clock", delegate
		{
			Run("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
		}, delegate
		{
			Run("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable hibernation", "Frees disk space equal to RAM", delegate
		{
			Run("powercfg", "/h off");
		}, delegate
		{
			Run("powercfg", "/h on");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable core parking (all cores active)", "All CPU cores always online", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings\\54533251-82be-4824-96c1-47b60b740d00\\0cc5b647-c1df-4637-891a-dec35c318583", "ValueMax", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Set minimum processor state to 100%", "No CPU downclocking", delegate
		{
			Run("powercfg", "/setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN 100");
			Run("powercfg", "/setactive scheme_current");
		}, null, "Higher temps on laptops", on: true);
		y = Twk(pageByIndex, y, "Disable USB selective suspend", "USB devices never sleep", delegate
		{
			Run("powercfg", "/setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0");
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Privacy & Telemetry");
		y = Twk(pageByIndex, y, "Disable telemetry and data collection", "Stops diagnostic data to Microsoft", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "AllowTelemetry", 0, null);
			SvcDis("DiagTrack");
			SvcDis("dmwappushservice");
		}, delegate
		{
			RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "AllowTelemetry");
			SvcAuto("DiagTrack");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable location tracking", "Blocks location access", delegate
		{
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\location", "Value", 0, "Deny");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable activity history", "Stops activity recording", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableActivityFeed", 0, null);
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "PublishUserActivities", 0, null);
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "UploadUserActivities", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable advertising ID", "Blocks targeted ad ID", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo", "Enabled", 0, null);
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AdvertisingInfo", "DisabledByGroupPolicy", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable feedback prompts", "No feedback nagging", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Siuf\\Rules", "NumberOfSIUFInPeriod", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Windows tips/suggestions", "No popup tips", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SoftLandingEnabled", 0, null);
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338389Enabled", 0, null);
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0, null);
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Windows Features");
		y = Twk(pageByIndex, y, "Disable Cortana", "Removes voice assistant", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search", "AllowCortana", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Widgets on taskbar", "Removes weather/news widget", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarDa", 0, null);
		}, delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarDa", 1, null);
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable Copilot button", "Removes AI Copilot", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowCopilotButton", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Chat/Meet Now", "Removes Teams chat", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarMn", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Consumer Features", "No auto-installed apps", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent", "DisableWindowsConsumerFeatures", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable delivery optimization", "No P2P update sharing", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DeliveryOptimization", "DODownloadMode", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Xbox Game Bar/DVR", "No overlay/recording overhead", delegate
		{
			Reg("HKCU\\System\\GameConfigStore", "GameDVR_Enabled", 0, null);
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR", "AllowGameDVR", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable background apps", "No UWP background running", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled", 1, null);
		}, delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled", 0, null);
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable Windows Error Reporting", "No crash reports", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Error Reporting", "Disabled", 1, null);
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Cleanup & Maintenance");
		y = Twk(pageByIndex, y, "Clear temporary files", "Deletes temp/prefetch/Windows Temp", delegate
		{
			CleanDir(Path.GetTempPath());
			CleanDir("C:\\Windows\\Temp");
			CleanDir("C:\\Windows\\Prefetch");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Run disk cleanup", "Windows disk cleanup utility", delegate
		{
			Run("cleanmgr", "/autoclean");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Flush DNS cache", "Clears cached DNS", delegate
		{
			Run("ipconfig", "/flushdns");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable SysMain (Superfetch)", "No app pre-loading into RAM", delegate
		{
			SvcDis("SysMain");
		}, delegate
		{
			SvcAuto("SysMain");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Windows Search delayed start", "Delayed indexer startup", delegate
		{
			Run("sc", "config WSearch start= delayed-auto");
		}, delegate
		{
			Run("sc", "config WSearch start= auto");
		}, null, on: true);
		y = Head(pageByIndex, y, "Scheduled Tasks");
		y = Twk(pageByIndex, y, "Disable telemetry tasks", "Compatibility appraiser + CEIP", delegate
		{
			DisTask("Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser");
			DisTask("Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator");
			DisTask("Microsoft\\Windows\\Customer Experience Improvement Program\\UsbCeip");
			DisTask("Microsoft\\Windows\\Feedback\\Siuf\\DmClient");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable disk diagnostic task", "Disk health data collection", delegate
		{
			DisTask("Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Maps update task", "Background map downloads", delegate
		{
			DisTask("Microsoft\\Windows\\Maps\\MapsUpdateTask");
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Filesystem (NTFS)");
		y = Twk(pageByIndex, y, "NTFS: Disable last access time", "Perf gain on file reads", delegate
		{
			Run("fsutil", "behavior set disablelastaccess 1");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "NTFS: Disable 8.3 filenames", "Reduced filesystem overhead", delegate
		{
			Run("fsutil", "behavior set disable8dot3 1");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "NTFS: Set mftzone to 2", "Reduced MFT fragmentation", delegate
		{
			Run("fsutil", "behavior set mftzone 2");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Remove startup app delay", "Launches sign-in apps without the Windows delay", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize", "StartupDelayInMSec", 0, null);
		}, delegate
		{
			RegDel("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize", "StartupDelayInMSec");
		}, null, on: true);
	}

	private void BuildCPU()
	{
		Panel pageByIndex = GetPageByIndex(1);
		int y = 5;
		y = Head(pageByIndex, y, "CPU Power & Throttling");
		y = Twk(pageByIndex, y, "Disable CPU power throttling", "Prevents CPU throttling for power saving", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling", "PowerThrottlingOff", 1, null);
		}, delegate
		{
			RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling", "PowerThrottlingOff");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Processor boost to aggressive", "Max turbo on workload", delegate
		{
			Run("powercfg", "/setacvalueindex scheme_current sub_processor PERFBOOSTMODE 2");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Connected Standby", "Traditional S3 sleep only", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power", "CsEnabled", 0, null);
		}, null, "Changes sleep behavior", on: true);
		y = Twk(pageByIndex, y, "Processor scheduling to programs", "Foreground apps get more CPU", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "Win32PrioritySeparation", 38, null);
		}, delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "Win32PrioritySeparation", 2, null);
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable HPET", "Lower interrupt latency", delegate
		{
			Run("bcdedit", "/deletevalue useplatformclock");
		}, delegate
		{
			Run("bcdedit", "/set useplatformclock true");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable dynamic tick", "Prevents latency spikes", delegate
		{
			Run("bcdedit", "/set disabledynamictick yes");
		}, delegate
		{
			Run("bcdedit", "/set disabledynamictick no");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable Spectre/Meltdown mitigations", "Max perf but less secure", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverride", 3, null);
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverrideMask", 3, null);
		}, null, "Reduces CPU security!", on: true);
		y = Twk(pageByIndex, y, "Disable kernel mitigations", "Removes validation overhead", delegate
		{
			RunPS("Set-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel' -Name 'DisableExceptionChainValidation' -Value 1 -EA 0");
		}, null, null, on: true);
		y = Head(pageByIndex, y, "CPU Boot & Startup");
		y = Twk(pageByIndex, y, "Fast boot (no GUI)", "Skips logo animation", delegate
		{
			Run("bcdedit", "/set quietboot yes");
		}, delegate
		{
			Run("bcdedit", "/set quietboot no");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Boot timeout 3 seconds", "Less wait at OS selection", delegate
		{
			Run("bcdedit", "/timeout 3");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable boot splash", "Removes spinning dots", delegate
		{
			Run("bcdedit", "/set bootuxdisabled on");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Remove startup delay", "Apps launch immediately", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize", "StartupDelayInMSec", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable lock screen", "Go straight to login", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Personalization", "NoLockScreen", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable auto-restart on BSOD", "Keep error screen visible", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\CrashControl", "AutoReboot", 0, null);
		}, null, null, on: true);
		y = Head(pageByIndex, y, "CPU Services");
		y = Twk(pageByIndex, y, "Disable Fax service", "Legacy fax", delegate
		{
			SvcDis("Fax");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Remote Registry", "Security + perf", delegate
		{
			SvcDis("RemoteRegistry");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Retail Demo", "Store demo mode", delegate
		{
			SvcDis("RetailDemo");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Maps Manager", "Background maps", delegate
		{
			SvcDis("MapsBroker");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Phone Service", "Mobile management", delegate
		{
			SvcDis("PhoneSvc");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Windows Insider", "Preview checks", delegate
		{
			SvcDis("wisvc");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Print Spooler to manual", "Starts only when printing", delegate
		{
			Run("sc", "config Spooler start= demand");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Prefer maximum CPU performance", "Sets processor energy preference to performance on AC power", delegate
		{
			Run("powercfg", "/setacvalueindex scheme_current sub_processor PERFEPP 0");
			Run("powercfg", "/setactive scheme_current");
		}, null, "Uses more power and may increase temperatures.", on: false);
	}

	private void BuildGPU()
	{
		Panel pageByIndex = GetPageByIndex(2);
		int y = 5;
		y = Head(pageByIndex, y, "GPU Rendering & Scheduling");
		y = Twk(pageByIndex, y, "Hardware-accelerated GPU scheduling", "Offloads frame scheduling to GPU", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "HwSchMode", 2, null);
		}, delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "HwSchMode", 1, null);
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable fullscreen optimizations", "True exclusive fullscreen", delegate
		{
			Reg("HKCU\\SYSTEM\\GameConfigStore", "GameDVR_FSEBehaviorMode", 2, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "GPU priority highest for games", "Max GPU scheduling priority", delegate
		{
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "GPU Priority", 8, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Increase GPU watchdog timeout (TDR)", "Prevents display driver timeout crashes", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "TdrDelay", 10, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Increase desktop heap for GPU", "More VA space for graphics", delegate
		{
			Run("bcdedit", "/set IncreaseUserVa 3072");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable mouse acceleration", "Raw 1:1 mouse input", delegate
		{
			Reg("HKCU\\Control Panel\\Mouse", "MouseSpeed", 0, "0");
			Reg("HKCU\\Control Panel\\Mouse", "MouseThreshold1", 0, "0");
			Reg("HKCU\\Control Panel\\Mouse", "MouseThreshold2", 0, "0");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Game DVR completely", "No recording overhead on GPU", delegate
		{
			Reg("HKCU\\System\\GameConfigStore", "GameDVR_Enabled", 0, null);
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR", "AllowGameDVR", 0, null);
		}, null, null, on: true);
		y = Head(pageByIndex, y, "GPU Power & Display");
		y = Twk(pageByIndex, y, "Disable variable refresh for desktop", "Fixed refresh rate for input consistency", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "DisableDynamicRefreshRate", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable GPU preemption overhead", "Lower latency GPU dispatch", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\\Scheduler", "PreemptionMode", 0, null);
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Force GPU to use dedicated memory", "Prevents shared memory fallback", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "PreferDedicatedVRAM", 1, null);
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Disable DWM composition", "Lower compositor latency", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\DWM", "Composition", 0, null);
		}, null, "May affect screen recording", on: false);
		y = Twk(pageByIndex, y, "Set display scaling to 100%", "Native pixel scaling", delegate
		{
			Reg("HKCU\\Control Panel\\Desktop", "LogPixels", 96, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable transparency effects", "Faster desktop rendering", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "EnableTransparency", 0, null);
		}, delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "EnableTransparency", 1, null);
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable animations", "Faster window transitions", delegate
		{
			Reg("HKCU\\Control Panel\\Desktop\\WindowMetrics", "MinAnimate", 0, "0");
			Reg("HKCU\\Control Panel\\Desktop", "UserPreferencesMask", new byte[8] { 144, 18, 3, 128, 16, 0, 0, 0 }, null);
		}, delegate
		{
			RegDel("HKCU\\Control Panel\\Desktop", "UserPreferencesMask");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Clear DirectX shader cache", "Removes stale compiled shaders so Windows can rebuild them", delegate
		{
			CleanDir(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"));
		}, null, "The first game launch afterward may take longer.", on: false);
	}

	private void BuildRAM()
	{
		Panel pageByIndex = GetPageByIndex(3);
		int y = 5;
		y = Head(pageByIndex, y, "Memory Management");
		y = Twk(pageByIndex, y, "Disable memory compression", "More RAM usable, less CPU overhead", delegate
		{
			RunPS("Disable-MMAgent -MemoryCompression -EA 0");
		}, delegate
		{
			RunPS("Enable-MMAgent -MemoryCompression");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable paging executive", "Keeps drivers in RAM (needs 8GB+ RAM)", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingExecutive", 1, null);
		}, delegate
		{
			RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingExecutive");
		}, "Needs 8GB+ RAM", on: true);
		y = Twk(pageByIndex, y, "Increase pagefile size to 2x RAM", "Prevents out-of-memory on heavy workloads", delegate
		{
			RunPS("$ram=[math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory/1MB); $pg=$ram*2; Set-ItemProperty 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management' PagingFiles \"C:\\pagefile.sys $pg $pg\" -EA 0");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Disable Superfetch prefetch", "Stops RAM pre-loading apps", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnableSuperfetch", 0, null);
		}, delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnableSuperfetch", 1, null);
		}, null, on: true);
		y = Twk(pageByIndex, y, "Set large system cache", "More RAM for file cache", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "LargeSystemCache", 1, null);
		}, delegate
		{
			RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "LargeSystemCache");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable memory integrity (HVCI)", "Lower overhead, less security", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity", "Enabled", 0, null);
		}, null, "Reduces security!", on: false);
		y = Twk(pageByIndex, y, "Disable Credential Guard", "Lower overhead for auth", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\CredentialGuard", "Enabled", 0, null);
		}, null, "Reduces security!", on: false);
		y = Twk(pageByIndex, y, "Disable automatic memory defrag", "Less CPU overhead from idle defrag", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "ClearPageFileAtShutdown", 0, null);
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Advanced Memory Tweaks");
		y = Twk(pageByIndex, y, "Set IO page lock limit to 512MB", "Larger locked page buffer", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "IoPageLockLimit", 536870912, null);
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Disable second-level address translation logging", "Less TLB overhead", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "TrackLockedPages", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Enable write combining for GPU memory", "Batched GPU writes", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "EnableWriteCombining", 1, null);
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Set NonPagedPoolSize to 0 (auto)", "Let Windows manage non-paged pool", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "NonPagedPoolSize", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable standby list trim", "Keep standby pages in RAM", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "TrimStandbyList", 0, null);
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Set system cache working set max to 4GB", "Larger cache for frequently used files", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "SystemCacheWorkingSetMax", 4294967296uL, null);
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Disable NUMA node interleaving", "Affinity-based memory access", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "NumaNodeInterleaving", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Let Windows manage the page file", "Restores automatic virtual-memory sizing for stability", delegate
		{
			RunPS("$cs=Get-CimInstance Win32_ComputerSystem; Set-CimInstance $cs -Property @{AutomaticManagedPagefile=$true} -EA 0");
		}, null, null, on: false);
	}

	private void BuildDebloat()
	{
		Panel pageByIndex = GetPageByIndex(4);
		int y = 5;
		y = Head(pageByIndex, y, "Microsoft Bloatware (45+ apps)");
		string[] array = new string[47]
		{
			"Microsoft.549981C3F5F10", "Microsoft.BingNews", "Microsoft.BingWeather", "Microsoft.GetHelp", "Microsoft.Getstarted", "Microsoft.Microsoft3DViewer", "Microsoft.MicrosoftOfficeHub", "Microsoft.MicrosoftSolitaireCollection", "Microsoft.MixedReality.Portal", "Microsoft.News",
			"Microsoft.Office.OneNote", "Microsoft.OneConnect", "Microsoft.People", "Microsoft.PowerAutomateDesktop", "Microsoft.Print3D", "Microsoft.ScreenSketch", "Microsoft.SkypeApp", "Microsoft.StorePurchaseApp", "Microsoft.Todos", "Microsoft.Wallet",
			"Microsoft.Whiteboard", "Microsoft.WindowsAlarms", "Microsoft.WindowsCommunicationsApps", "Microsoft.WindowsFeedbackHub", "Microsoft.WindowsMaps", "Microsoft.WindowsSoundRecorder", "Microsoft.Xbox.TCUI", "Microsoft.XboxApp", "Microsoft.XboxGameOverlay", "Microsoft.XboxGamingOverlay",
			"Microsoft.XboxIdentityProvider", "Microsoft.XboxSpeechToTextOverlay", "Microsoft.YourPhone", "Microsoft.ZuneMusic", "Microsoft.ZuneVideo", "MicrosoftCorporationII.MicrosoftFamily", "Microsoft.GamingApp", "Microsoft.OutlookForWindows", "Microsoft.MicrosoftStickyNotes", "Microsoft.Windows.DevHome",
			"Microsoft.Copilot", "Microsoft.Tips", "Microsoft.3DBuilder", "Microsoft.HEIFImageExtension", "Microsoft.VP9VideoExtensions", "Microsoft.WebMediaExtensions", "Clipchamp.Clipchamp"
		};
		foreach (string text in array)
		{
			string a = text;
			y = Twk(pageByIndex, y, "Remove " + text, "Uninstalls UWP app", delegate
			{
				RemoveApp(a);
			}, null, null, on: false);
		}
		y = Head(pageByIndex, y, "Third-Party Bloatware (40+ apps)");
		array = new string[43]
		{
			"SpotifyAB.SpotifyMusic", "Disney.37853FC22B2CE", "king.com.CandyCrushFriends", "king.com.CandyCrushSaga", "king.com.CandyCrushSodaSaga", "Facebook.Facebook", "Facebook.InstagramBeta", "BytedancePte.Ltd.TikTok", "AmazonVideo.PrimeVideo", "Amazon.com.Amazon",
			"Plex.Plex", "ShazamEntertainmentLtd.Shazam", "Flipboard.Flipboard", "Twitter.Twitter", "Evernote.Evernote", "Dolby.DolbyAccess", "Duolingo-LearnLanguagesforFree", "EclipseManager", "ActiproSoftwareLLC", "PandoraMediaInc",
			"C27EB4BA.DropboxOEM", "Nordcurrent.CookingFever", "A278AB0D.MarchofEmpires", "ThumbmunkeysLtd.PhototasticCollage", "Drawboard.DrawboardPDF", "WinZipComputing.WinZipUniversal", "XINGAG.XING", "flaregames.RoyalRevolt2", "Microsoft.BingFinance", "Microsoft.BingSports",
			"Microsoft.BingTranslator", "Microsoft.Office.Sway", "Microsoft.Advertising.Xaml", "Microsoft.Services.Store.Engagement", "Microsoft.AppConnector", "Microsoft.ConnectivityStore", "Microsoft.CommsPhone", "Microsoft.Messaging", "Microsoft.Office.Excel", "Microsoft.Office.PowerPoint",
			"Microsoft.Office.Word", "Microsoft.OneDriveSync", "Microsoft.RemoteDesktop"
		};
		foreach (string text2 in array)
		{
			string a2 = text2;
			y = Twk(pageByIndex, y, "Remove " + text2, "Uninstalls pre-installed app", delegate
			{
				RemoveApp(a2);
			}, null, null, on: false);
		}
		y = Head(pageByIndex, y, "Microsoft Edge (WARNING)");
		y = Twk(pageByIndex, y, "Remove Microsoft Edge (ALL)", "Fully uninstalls Edge browser", delegate
		{
			RunPS("Get-AppxPackage *edge* | Remove-AppxPackage -EA 0; $e=(Get-ChildItem 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application' -EA 0 | Sort Name -Desc)[0]; if($e){Start-Process ($e.FullName + '\\Installer\\setup.exe') -Arg '--uninstall','--system-level','--force-uninstall' -Wait}");
		}, null, "May break WebView2-dependent apps!", on: false);
		y = Twk(pageByIndex, y, "Remove Edge WebView2", "Removes shared WebView2 component", delegate
		{
			RunPS("Get-AppxPackage *WebView2* | Remove-AppxPackage -EA 0");
		}, null, "Some apps need WebView2!", on: false);
		y = Twk(pageByIndex, y, "Prevent Edge reinstalling", "Blocks Windows Update from re-adding Edge", delegate
		{
			Reg("HKLM\\SOFTWARE\\Microsoft\\EdgeUpdate", "DoNotUpdateToEdgeWithChromium", 1, null);
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\EdgeUpdate", "InstallDefault", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Remove Edge shortcuts", "Deletes Edge desktop and taskbar icons", delegate
		{
			RunPS("Remove-Item (Join-Path $env:Public 'Desktop\\Microsoft Edge.lnk') -EA 0");
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Optional Windows Features");
		y = Twk(pageByIndex, y, "Remove Internet Explorer", "Disables legacy IE", delegate
		{
			Run("dism", "/online /norestart /disable-feature /featurename:Internet-Explorer-Optional-amd64");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Remove Windows Media Player", "Legacy WMP", delegate
		{
			Run("dism", "/online /norestart /disable-feature /featurename:WindowsMediaPlayer");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Remove XPS Writer", "Virtual XPS printer", delegate
		{
			Run("dism", "/online /norestart /disable-feature /featurename:Printing-XPSServices-Features");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Remove Work Folders", "Enterprise sync", delegate
		{
			Run("dism", "/online /norestart /disable-feature /featurename:WorkFolders-Client");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Remove PowerShell v2", "Legacy PS engine", delegate
		{
			Run("dism", "/online /norestart /disable-feature /featurename:MicrosoftWindowsPowerShellV2Root");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Disable Microsoft consumer experiences", "Stops suggested apps and promotional installs", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent", "DisableWindowsConsumerFeatures", 1, null);
		}, delegate
		{
			RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\CloudContent", "DisableWindowsConsumerFeatures");
		}, null, on: true);
	}

	private void BuildNetwork()
	{
		Panel pageByIndex = GetPageByIndex(5);
		int y = 5;
		y = Head(pageByIndex, y, "Network Latency & Throughput");
		y = Twk(pageByIndex, y, "Disable Nagle's algorithm", "Lower network latency for games", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "TcpAckFrequency", 1, null);
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "TCPNoDelay", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Increase network throttling index", "Higher throughput for streaming", delegate
		{
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex", uint.MaxValue, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "System responsiveness high", "Prioritizes foreground apps", delegate
		{
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "SystemResponsiveness", 10, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable IPv6", "Removes IPv6 overhead", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters", "DisabledComponents", 255, null);
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Increase TCP receive window", "Larger buffer for downloads", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\AFD\\Parameters", "DefaultReceiveWindow", 65536, null);
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Increase TCP send buffer", "Larger upload buffer", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\AFD\\Parameters", "DefaultSendWindow", 65536, null);
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Enable RSS (Receive Side Scaling)", "Multi-core network processing", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Ndis\\Parameters", "RssBaseCpu", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Large Send Offload", "Lower CPU overhead for network", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "TcpMaxDataRetransmissions", 5, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Windows Auto-Tuning", "Manual TCP window sizing", delegate
		{
			Run("netsh", "interface tcp set global autotuninglevel=disabled");
		}, delegate
		{
			Run("netsh", "interface tcp set global autotuninglevel=normal");
		}, null, on: false);
		y = Twk(pageByIndex, y, "Set MTU to 1500", "Standard Ethernet MTU", delegate
		{
			Run("netsh", "interface ipv4 set subinterface \"Ethernet\" mtu=1500 store=persistent");
		}, null, null, on: true);
		y = Head(pageByIndex, y, "DNS Optimization");
		y = Twk(pageByIndex, y, "Flush DNS cache", "Clears cached DNS entries", delegate
		{
			Run("ipconfig", "/flushdns");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Set DNS to Cloudflare (1.1.1.1)", "Fastest public DNS", delegate
		{
			Run("netsh", "interface ip set dns \"Ethernet\" static 1.1.1.1");
			Run("netsh", "interface ip add dns \"Ethernet\" 1.0.0.1 index=2");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Set DNS to Google (8.8.8.8)", "Google public DNS", delegate
		{
			Run("netsh", "interface ip set dns \"Ethernet\" static 8.8.8.8");
			Run("netsh", "interface ip add dns \"Ethernet\" 8.8.4.4 index=2");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Increase DNS cache size", "Fewer DNS lookups", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Dnscache\\Parameters", "CacheHashTableBucketSize", 1, null);
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Dnscache\\Parameters", "CacheHashTableSize", 384, null);
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Dnscache\\Parameters", "MaxCacheTtl", 86400, null);
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Dnscache\\Parameters", "MaxNegativeCacheTtl", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable NetBIOS over TCP/IP", "Less legacy network overhead", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\NetBT\\Parameters\\Interfaces", "NetbiosOptions", 2, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Restore TCP auto-tuning", "Uses Windows automatic receive-window scaling", delegate
		{
			Run("netsh", "interface tcp set global autotuninglevel=normal");
		}, null, null, on: false);
	}

	private void BuildGameMode()
	{
		Panel pageByIndex = GetPageByIndex(6);
		int y = 5;
		y = Head(pageByIndex, y, "Fortnite & General Gaming");
		y = Twk(pageByIndex, y, "Enable Windows Game Mode", "Auto-prioritizes game resources", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\GameBar", "AllowAutoGameMode", 1, null);
			Reg("HKCU\\SOFTWARE\\Microsoft\\GameBar", "AutoGameModeEnabled", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable fullscreen optimizations", "Lower input lag", delegate
		{
			Reg("HKCU\\SYSTEM\\GameConfigStore", "GameDVR_FSEBehaviorMode", 2, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Game DVR completely", "More FPS, no recording overhead", delegate
		{
			Reg("HKCU\\System\\GameConfigStore", "GameDVR_Enabled", 0, null);
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR", "AllowGameDVR", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Game scheduling highest priority", "Max CPU/GPU/IO for games", delegate
		{
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "GPU Priority", 8, null);
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "Priority", 6, null);
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "Scheduling Category", 0, "High");
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "SFIO Priority", 0, "High");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "System responsiveness 0 for games", "100% CPU to foreground game", delegate
		{
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "SystemResponsiveness", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable HPET for gaming", "Lower timer latency", delegate
		{
			Run("bcdedit", "/deletevalue useplatformclock");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "High-res timer for games", "Smoother frame pacing", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\kernel", "GlobalTimerResolutionRequests", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable network throttling for games", "Max throughput for online games", delegate
		{
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex", uint.MaxValue, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable audio ducking", "Prevents volume drops", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Multimedia\\Audio", "UserDuckingPreference", 3, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Xbox services", "Saves resources", delegate
		{
			SvcDis("XblAuthManager");
			SvcDis("XblGameSave");
			SvcDis("XboxGipSvc");
			SvcDis("XboxNetApiSvc");
		}, delegate
		{
			SvcAuto("XblAuthManager");
			SvcAuto("XblGameSave");
			SvcAuto("XboxGipSvc");
			SvcAuto("XboxNetApiSvc");
		}, null, on: true);
		y = Head(pageByIndex, y, "Live Game Priority Booster (Optional)");
		y = Twk(pageByIndex, y, "Auto-boost Fortnite", "Sets High priority when running", delegate
		{
			RunPS("Start-Process powershell -ArgumentList '-WindowStyle Hidden -Command \"while(1){$x=Get-Process FortniteClient-Win64-Shipping -EA 0;if($x){$x.PriorityClass=[Diagnostics.ProcessPriorityClass]::High};Sleep 5}\"'");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Auto-boost Valorant", "Sets High priority when running", delegate
		{
			RunPS("Start-Process powershell -ArgumentList '-WindowStyle Hidden -Command \"while(1){$x=Get-Process VALORANT-Win64-Shipping -EA 0;if($x){$x.PriorityClass=[Diagnostics.ProcessPriorityClass]::High};Sleep 5}\"'");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Auto-boost Apex Legends", "Sets High priority when running", delegate
		{
			RunPS("Start-Process powershell -ArgumentList '-WindowStyle Hidden -Command \"while(1){$x=Get-Process r5apex -EA 0;if($x){$x.PriorityClass=[Diagnostics.ProcessPriorityClass]::High};Sleep 5}\"'");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Auto-boost CS2", "Sets High priority when running", delegate
		{
			RunPS("Start-Process powershell -ArgumentList '-WindowStyle Hidden -Command \"while(1){$x=Get-Process cs2 -EA 0;if($x){$x.PriorityClass=[Diagnostics.ProcessPriorityClass]::High};Sleep 5}\"'");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Disable enhanced pointer precision", "Uses consistent 1:1 mouse movement for games", delegate
		{
			Reg("HKCU\\Control Panel\\Mouse", "MouseSpeed", 0, "0");
			Reg("HKCU\\Control Panel\\Mouse", "MouseThreshold1", 0, "0");
			Reg("HKCU\\Control Panel\\Mouse", "MouseThreshold2", 0, "0");
		}, null, null, on: true);
	}

	private void BuildRegistry()
	{
		Panel pageByIndex = GetPageByIndex(7);
		int y = 5;
		y = Head(pageByIndex, y, "System Responsiveness");
		y = Twk(pageByIndex, y, "Reduce shutdown wait to 2s", "Kills hung apps faster", delegate
		{
			Reg("HKCU\\Control Panel\\Desktop", "WaitToKillAppTimeout", 2000, "2000");
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control", "WaitToKillServiceTimeout", 2000, "2000");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Reduce hung app timeout to 1s", "Force-kills unresponsive apps after 1s", delegate
		{
			Reg("HKCU\\Control Panel\\Desktop", "HungAppTimeout", 1000, "1000");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable menu show delay", "Menus appear instantly", delegate
		{
			Reg("HKCU\\Control Panel\\Desktop", "MenuShowDelay", 0, "0");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Aero Shake", "Stops minimize-on-shake", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DisallowShaking", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable snap assist flyout", "Removes snap layout popup", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "EnableSnapAssistFlyout", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Verbose status messages", "Shows detailed boot progress", delegate
		{
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "VerboseStatus", 1, null);
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Explorer & UI");
		y = Twk(pageByIndex, y, "Explorer opens to This PC", "Shows drives not Quick Access", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "LaunchTo", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Show file extensions", "Always shows .exe, .txt etc.", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "HideFileExt", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Show hidden files", "Reveals hidden files", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Hidden", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable folder auto-discovery", "Speeds up Explorer", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "UseAutoDiscover", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Enable End Task right-click", "Adds End Task to taskbar menu", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\TaskbarDeveloperSettings", "TaskbarEndTask", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Taskbar alignment left", "Classic left-aligned icons", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarAl", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable compact mode forced", "Keep full-size taskbar", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "UseCompactMode", 0, null);
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Security (Performance Impact)");
		y = Twk(pageByIndex, y, "Disable UAC for admins", "Faster but less secure", delegate
		{
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "ConsentPromptBehaviorAdmin", 0, null);
		}, null, "Less secure!", on: true);
		y = Twk(pageByIndex, y, "Disable SmartScreen", "Stops URL checking overhead", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableSmartScreen", 0, null);
		}, delegate
		{
			RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableSmartScreen");
		}, "Reduces malware protection!", on: false);
		y = Twk(pageByIndex, y, "Disable BitLocker auto-encrypt", "Prevents auto drive encryption", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\BitLocker", "PreventDeviceEncryption", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable startup app delay", "Starts desktop apps immediately after sign-in", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize", "StartupDelayInMSec", 0, null);
		}, delegate
		{
			RegDel("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize", "StartupDelayInMSec");
		}, null, on: true);
	}

	private void BuildAppTweaks()
	{
		Panel pageByIndex = GetPageByIndex(8);
		int y = 5;
		y = Head(pageByIndex, y, "Google Chrome");
		y = Twk(pageByIndex, y, "Disable Chrome software reporter", "Stops PC scanning", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Google\\Chrome", "ChromeCleanupEnabled", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Chrome background", "Chrome stops when closed", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Google\\Chrome", "BackgroundModeEnabled", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Enable GPU rasterization", "Faster web rendering", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Google\\Chrome", "GpuRasterization", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Chrome metrics", "Stops data collection", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Google\\Chrome", "MetricsReportingEnabled", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Chrome prefetch", "Stops DNS pre-resolving", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Google\\Chrome", "DNSPrefetchingEnabled", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Chrome hardware acceleration", "Use GPU directly", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Google\\Chrome", "HardwareAccelerationModeEnabled", 0, null);
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Brave Browser");
		y = Twk(pageByIndex, y, "Disable Brave Rewards", "Removes crypto notifications", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\BraveSoftware\\Brave", "BraveRewardsDisabled", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Brave VPN", "Removes VPN promotion", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\BraveSoftware\\Brave", "BraveVPNDisabled", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Brave Wallet", "Removes crypto wallet", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\BraveSoftware\\Brave", "BraveWalletDisabled", 1, null);
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Firefox");
		y = Twk(pageByIndex, y, "Disable Firefox telemetry", "Stops data collection", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Mozilla\\Firefox", "DisableTelemetry", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable default browser check", "Stops nag", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Mozilla\\Firefox", "DefaultBrowserOptions", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Enable Firefox GPU acceleration", "Hardware video decode", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Mozilla\\Firefox", "HardwareAcceleration", 1, null);
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Discord");
		y = Twk(pageByIndex, y, "Disable Discord autostart", "Removes from startup", delegate
		{
			try
			{
				Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true)?.DeleteValue("Discord", throwOnMissingValue: false);
			}
			catch
			{
			}
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Discord high priority", "More CPU for voice chat", delegate
		{
			RunPS("Get-Process -Name Discord -EA 0 | ForEach {$_.PriorityClass='High'}");
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Adobe");
		y = Twk(pageByIndex, y, "Disable Adobe auto-start", "Stops Adobe ARM and updater", delegate
		{
			SvcDis("AdobeARMservice");
			SvcDis("AdobeFlashPlayerUpdateSvc");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Adobe Genuine check", "Stops anti-piracy scanner", delegate
		{
			SvcDis("AGSService");
			SvcDis("AGMService");
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Spotify");
		y = Twk(pageByIndex, y, "Disable Spotify autostart", "Removes from startup", delegate
		{
			try
			{
				Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true)?.DeleteValue("Spotify", throwOnMissingValue: false);
			}
			catch
			{
			}
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Block Spotify ads (hosts)", "Adds ad domains to hosts file", delegate
		{
			string path = "C:\\Windows\\System32\\drivers\\etc\\hosts";
			string[] obj = new string[9] { "adclick.g.doubleclick.net", "adeventtracker.spotify.com", "ads-fa.spotify.com", "analytics.spotify.com", "audio-ads.spotify.com", "crashdump.spotify.com", "log.spotify.com", "pixel.spotify.com", "pubads.g.doubleclick.net" };
			string text = File.ReadAllText(path);
			string[] array = obj;
			foreach (string text2 in array)
			{
				string text3 = "0.0.0.0 " + text2;
				if (!text.Contains(text3))
				{
					File.AppendAllText(path, "\n" + text3);
				}
			}
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Other Apps");
		y = Twk(pageByIndex, y, "Disable Steam overlay", "More FPS in games", delegate
		{
			Reg("HKCU\\SOFTWARE\\Valve\\Steam", "DisableOverlay", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Epic Games autostart", "Removes from startup", delegate
		{
			try
			{
				Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true)?.DeleteValue("EpicGamesLauncher", throwOnMissingValue: false);
			}
			catch
			{
			}
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable OneDrive autostart", "Removes from startup", delegate
		{
			try
			{
				RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
				if (registryKey != null)
				{
					registryKey.DeleteValue("OneDriveSetup", throwOnMissingValue: false);
					registryKey.DeleteValue("OneDrive", throwOnMissingValue: false);
				}
			}
			catch
			{
			}
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Teams autostart", "Removes from startup", delegate
		{
			try
			{
				Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true)?.DeleteValue("com.squirrel.Teams.Teams", throwOnMissingValue: false);
			}
			catch
			{
			}
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Edge Startup Boost", "Stops Edge preloading during Windows sign-in", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "StartupBoostEnabled", 0, null);
		}, delegate
		{
			RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "StartupBoostEnabled");
		}, null, on: true);
	}

	private void BuildDefrag()
	{
		//IL_034e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Expected O, but got Unknown
		//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ad: Expected O, but got Unknown
		//IL_0409: Unknown result type (might be due to invalid IL or missing references)
		//IL_0413: Expected O, but got Unknown
		Panel pageByIndex = GetPageByIndex(9);
		int y = 5;
		y = Head(pageByIndex, y, "Disk Optimization");
		y = Twk(pageByIndex, y, "Defragment C: drive (HDD)", "Runs Windows defrag on C: (SSD safe - will TRIM instead)", delegate
		{
			Run("defrag", "C: /O");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Defragment all fixed drives", "Optimizes all non-removable drives", delegate
		{
			Run("defrag", "/C /O");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Analyze fragmentation on C:", "Shows fragmentation percentage", delegate
		{
			Run("defrag", "C: /A /V");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "TRIM all SSDs", "Runs TRIM on all SSD drives", delegate
		{
			Run("defrag", "/C /O /L");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Boot optimization defrag", "Defrags boot files for faster startup", delegate
		{
			Run("defrag", "C: /B");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable scheduled defrag", "Stop automatic weekly defrag", delegate
		{
			Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Defrag\\ScheduledDefrag\" /Disable");
		}, delegate
		{
			Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Defrag\\ScheduledDefrag\" /Enable");
		}, null, on: false);
		y = Head(pageByIndex, y, "Storage Optimization");
		y = Twk(pageByIndex, y, "Compact OS (compress Windows files)", "Saves 2-4GB disk space by compressing OS files", delegate
		{
			Run("compact", "/compactos:always");
		}, delegate
		{
			Run("compact", "/compactos:never");
		}, "Slows down on weak CPUs", on: true);
		y = Twk(pageByIndex, y, "Clear Windows Update cache", "Deletes downloaded updates (frees space)", delegate
		{
			SvcDis("wuauserv");
			RunPS("Remove-Item C:\\Windows\\SoftwareDistribution\\Download\\* -Recurse -Force -EA 0");
			SvcAuto("wuauserv");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Clear Delivery Optimization cache", "Removes P2P update cache", delegate
		{
			SvcDis("DoSvc");
			RunPS("Remove-Item C:\\Windows\\ServiceProfiles\\NetworkService\\AppData\\Local\\Microsoft\\Windows\\DeliveryOptimization\\* -Recurse -Force -EA 0");
			SvcAuto("DoSvc");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Clear Event Logs", "Deletes all Windows event logs", delegate
		{
			RunPS("wevtutil el | ForEach-Object {wevtutil cl $_}");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Reserved Storage", "Frees 7GB reserved space", delegate
		{
			Run("dism", "/Online /Set-ReservedStorageState /Disabled");
		}, delegate
		{
			Run("dism", "/Online /Set-ReservedStorageState /Enabled");
		}, "May prevent updates", on: false);
		y = Twk(pageByIndex, y, "Run Storage Sense", "Auto-cleans temp files", delegate
		{
			RunPS("Optimize-Storage -EA 0");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Enable SSD TRIM", "Ensures Windows sends delete notifications to solid-state drives", delegate
		{
			Run("fsutil", "behavior set DisableDeleteNotify 0");
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Queue & Data Size Info");
		Label val = new Label();
		((Control)val).Text = "Click the button to scan disk and queue sizes:";
		((Control)val).Location = new Point(12, y);
		((Control)val).Size = new Size(600, 18);
		((Control)val).ForeColor = MUTED;
		((Control)val).Font = FS;
		((Control)pageByIndex).Controls.Add((Control)(object)val);
		y += 22;
		Button val2 = new Button();
		((Control)val2).Text = "Scan Disk & Queue Size";
		((Control)val2).Location = new Point(12, y);
		((Control)val2).Size = new Size(200, 32);
		((ButtonBase)val2).FlatStyle = (FlatStyle)0;
		((Control)val2).BackColor = ACC;
		((Control)val2).ForeColor = Color.White;
		((ButtonBase)val2).FlatAppearance.BorderSize = 0;
		Label scanResult = new Label();
		((Control)scanResult).Location = new Point(12, y + 38);
		((Control)scanResult).Size = new Size(750, 300);
		((Control)scanResult).ForeColor = GRN;
		((Control)scanResult).Font = FS;
		((Control)scanResult).Text = "";
		((Control)val2).Click += delegate
		{
			try
			{
				string text = "";
				DriveInfo[] drives = DriveInfo.GetDrives();
				foreach (DriveInfo driveInfo in drives)
				{
					if (driveInfo.IsReady && driveInfo.DriveType == DriveType.Fixed)
					{
						long availableFreeSpace = driveInfo.AvailableFreeSpace;
						long totalSize = driveInfo.TotalSize;
						long num = totalSize - availableFreeSpace;
						double value = ((totalSize > 0) ? ((double)num / (double)totalSize * 100.0) : 0.0);
						object obj = text;
						text = string.Concat(obj, driveInfo.Name, "  Used: ", num / 1073741824, " GB / ", totalSize / 1073741824, " GB (", Math.Round(value, 1), "%)\n");
					}
				}
				text += "\nPrint Queue: ";
				try
				{
					Process process = Process.Start(new ProcessStartInfo("powershell", "-NoProfile -Command \"(Get-PrintJob -PrinterName * -EA 0 | Measure-Object).Count\"")
					{
						UseShellExecute = false,
						CreateNoWindow = true,
						RedirectStandardOutput = true
					});
					string text2 = process.StandardOutput.ReadToEnd().Trim();
					process.WaitForExit();
					text = text + text2 + " pending jobs\n";
				}
				catch
				{
					text += "N/A\n";
				}
				long num2 = 0L;
				try
				{
					string[] files = Directory.GetFiles(Path.GetTempPath(), "*", SearchOption.AllDirectories);
					foreach (string fileName in files)
					{
						num2 += new FileInfo(fileName).Length;
					}
				}
				catch
				{
				}
				object obj4 = text;
				text = string.Concat(obj4, "Temp Files: ", num2 / 1048576, " MB\n");
				text += "Windows Update Cache: ";
				try
				{
					long num3 = 0L;
					string path = "C:\\Windows\\SoftwareDistribution\\Download";
					if (Directory.Exists(path))
					{
						string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
						foreach (string fileName2 in files)
						{
							num3 += new FileInfo(fileName2).Length;
						}
					}
					text = text + num3 / 1048576 + " MB\n";
				}
				catch
				{
					text += "N/A\n";
				}
				((Control)scanResult).Text = text;
			}
			catch (Exception ex)
			{
				((Control)scanResult).Text = "Error: " + ex.Message;
			}
		};
		((Control)pageByIndex).Controls.Add((Control)(object)val2);
		((Control)pageByIndex).Controls.Add((Control)(object)scanResult);
	}

	private void BuildRepair()
	{
		Panel pageByIndex = GetPageByIndex(10);
		int y = 5;
		y = Head(pageByIndex, y, "System Repair");
		y = Twk(pageByIndex, y, "Run SFC /scannow", "Scans and repairs corrupt system files", delegate
		{
			Run("sfc", "/scannow");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Run DISM RestoreHealth", "Repairs Windows component store", delegate
		{
			Run("dism", "/Online /Cleanup-Image /RestoreHealth");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Run DISM CheckHealth", "Quick health check of component store", delegate
		{
			Run("dism", "/Online /Cleanup-Image /CheckHealth");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Run DISM ScanHealth", "Full scan of component store", delegate
		{
			Run("dism", "/Online /Cleanup-Image /ScanHealth");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Repair Windows components", "Fixes component store corruption", delegate
		{
			Run("dism", "/Online /Cleanup-Image /RestoreHealth /Source:wim:install.wim:1 /LimitAccess");
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Fix Common Errors");
		y = Twk(pageByIndex, y, "Fix Windows Update errors (2502/2503)", "Repairs MSI installer permissions", delegate
		{
			Run("icacls", "C:\\Windows\\Temp /grant administrators:F");
			Run("msiexec", "/unregister");
			Run("msiexec", "/regserver");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Reset Windows Store cache", "Fixes Store download issues", delegate
		{
			Run("wsreset", "");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Repair network (Winsock reset)", "Fixes network connectivity", delegate
		{
			Run("netsh", "winsock reset");
			Run("netsh", "int ip reset");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Reset DNS client", "Flushes and resets DNS", delegate
		{
			Run("ipconfig", "/flushdns");
			Run("netsh", "int ipv4 set dnsservers name=\"Ethernet\" dhcp");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Restart Windows Explorer", "Refreshes shell (fixes UI glitches)", delegate
		{
			RunPS("Stop-Process -Name explorer -Force; Start-Sleep 2; Start-Process explorer");
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Services & Defaults");
		y = Twk(pageByIndex, y, "Set services to defaults", "Restores all service start types to default", delegate
		{
			RunPS("Get-Service | Where {$_.StartType -eq 'Disabled'} | Set-Service -StartupType Manual -EA 0");
		}, null, "May re-enable disabled services!", on: false);
		y = Twk(pageByIndex, y, "Disable telemetry components", "Removes telemetry scheduled tasks + services", delegate
		{
			SvcDis("DiagTrack");
			SvcDis("dmwappushservice");
			DisTask("Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser");
			DisTask("Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Rebuild icon cache", "Fixes broken/missing icons", delegate
		{
			RunPS("Stop-Process -Name explorer -Force; Remove-Item $env:LOCALAPPDATA\\IconCache.db -Force -EA 0; Remove-Item $env:LOCALAPPDATA\\Microsoft\\Windows\\Explorer\\iconcache* -Force -EA 0; Start-Process explorer");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Scan C: without restarting", "Runs the online CHKDSK scan for filesystem errors", delegate
		{
			Run("chkdsk", "C: /scan");
		}, null, null, on: false);
	}

	private void BuildDrivers()
	{
		Panel pageByIndex = GetPageByIndex(11);
		int y = 5;
		y = Head(pageByIndex, y, "Driver Updates");
		y = Twk(pageByIndex, y, "Update all drivers from Windows Update", "Installs pending driver updates", delegate
		{
			RunPS("Install-Module PSWindowsUpdate -Force -Scope CurrentUser -EA 0; Get-WindowsUpdate -MicrosoftUpdate -Category 'Drivers' -Install -AcceptAll -AutoReboot -EA 0");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Install optional drivers from WU", "Includes optional driver updates", delegate
		{
			RunPS("Get-WindowsUpdate -MicrosoftUpdate -NotCategory 'Security Updates' -Install -AcceptAll -EA 0");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "GPU Drivers - NVIDIA latest", "Downloads latest NVIDIA driver via GeForce Experience", delegate
		{
			RunPS("if(Test-Path 'C:\\Program Files\\NVIDIA Corporation\\NVIDIA GeForce Experience\\NVIDIA GeForce Experience.exe'){Start-Process 'C:\\Program Files\\NVIDIA Corporation\\NVIDIA GeForce Experience\\NVIDIA GeForce Experience.exe'}");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "GPU Drivers - AMD latest", "Opens AMD Adrenalin for driver update", delegate
		{
			RunPS("if(Test-Path 'C:\\Program Files\\AMD\\CNext\\CNext\\RadeonSoftware.exe'){Start-Process 'C:\\Program Files\\AMD\\CNext\\CNext\\RadeonSoftware.exe'}");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Clean GPU driver install (DDU)", "Opens Display Driver Uninstaller for clean install", delegate
		{
			RunPS("if(Test-Path 'C:\\Program Files\\DDU\\Display Driver Uninstaller.exe'){Start-Process 'C:\\Program Files\\DDU\\Display Driver Uninstaller.exe'}else{Write-Host 'DDU not found - install from guru3d.com'}");
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Driver Settings");
		y = Twk(pageByIndex, y, "Disable driver auto-search on WU", "Stops Windows from auto-updating drivers", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", 1, null);
		}, delegate
		{
			RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate", "ExcludeWUDriversInQualityUpdate");
		}, "You'll need to update manually", on: true);
		y = Twk(pageByIndex, y, "Disable driver searching in Device Manager", "No online driver search", delegate
		{
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DriverSearching", "SearchOrderConfig", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Windows Update auto-restart", "Prevents forced restart after driver updates", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsUpdate\\AU", "NoAutoRebootWithLoggedOnUsers", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Open Optional Driver Updates", "Opens the Windows 11 optional updates page", delegate
		{
			Process.Start("cmd.exe", "/c start ms-settings:windowsupdate-optionalupdates");
		}, null, null, on: false);
	}

	private void BuildSecurity()
	{
		Panel pageByIndex = GetPageByIndex(12);
		int y = 5;
		y = Head(pageByIndex, y, "Core Isolation (VBS)");
		y = Twk(pageByIndex, y, "Disable Core Isolation / Memory Integrity", "Lower overhead, weaker security", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity", "Enabled", 0, null);
		}, delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity", "Enabled", 1, null);
		}, "Reduces security!", on: false);
		y = Twk(pageByIndex, y, "Disable Virtualization-Based Security", "Disables VBS completely", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard", "EnableVirtualizationBasedSecurity", 0, null);
		}, delegate
		{
			RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard", "EnableVirtualizationBasedSecurity");
		}, "Major security reduction!", on: false);
		y = Twk(pageByIndex, y, "Disable Credential Guard", "Lower overhead for auth processes", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\CredentialGuard", "Enabled", 0, null);
		}, delegate
		{
			RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\CredentialGuard", "Enabled");
		}, "Reduces security!", on: false);
		y = Twk(pageByIndex, y, "Disable Kernel DMA Protection", "Lower DMA overhead", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\KernelDma", "Enabled", 0, null);
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Windows Defender");
		y = Twk(pageByIndex, y, "Disable real-time protection", "Stops Defender scanning overhead", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection", "DisableRealtimeMonitoring", 1, null);
		}, delegate
		{
			RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Real-Time Protection", "DisableRealtimeMonitoring");
		}, "No virus protection!", on: false);
		y = Twk(pageByIndex, y, "Disable cloud-based protection", "Stops cloud sample submission", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet", "SpynetReporting", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable automatic sample submission", "No files sent to Microsoft", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows Defender\\Spynet", "SubmitSamplesConsent", 2, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable Tamper Protection", "Allows Defender config changes", delegate
		{
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows Defender\\Features", "TamperProtection", 0, null);
		}, null, "Reduces security!", on: false);
		y = Twk(pageByIndex, y, "Add C:\\ to Defender exclusions", "Excludes C: drive from scanning", delegate
		{
			RunPS("Add-MpPreference -ExclusionPath 'C:\\'");
		}, delegate
		{
			RunPS("Remove-MpPreference -ExclusionPath 'C:\\'");
		}, "All C: files unscanned!", on: false);
		y = Head(pageByIndex, y, "Mitigations");
		y = Twk(pageByIndex, y, "Disable Spectre/Meltdown mitigations", "Max perf but vulnerable to side-channel attacks", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverride", 3, null);
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverrideMask", 3, null);
		}, delegate
		{
			RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverride");
			RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "FeatureSettingsOverrideMask");
		}, "CPU vulnerability exposed!", on: true);
		y = Twk(pageByIndex, y, "Disable kernel ASLR", "Lower overhead for kernel loads", delegate
		{
			Run("bcdedit", "/set nx AlwaysOff");
		}, delegate
		{
			Run("bcdedit", "/set nx OptIn");
		}, "Reduces exploit protection!", on: false);
		y = Twk(pageByIndex, y, "Disable Control Flow Guard", "Lower overhead for code execution", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "EnableCfg", 0, null);
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Protection Options");
		y = Twk(pageByIndex, y, "Enable Defender PUA protection", "Blocks potentially unwanted apps and bundled installers", delegate
		{
			RunPS("Set-MpPreference -PUAProtection Enabled -EA 0");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Enable Windows SmartScreen", "Restores reputation checks for downloaded apps", delegate
		{
			RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableSmartScreen");
		}, null, null, on: false);
	}

	private void BuildOneClick()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Expected O, but got Unknown
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Expected O, but got Unknown
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Expected O, but got Unknown
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Expected O, but got Unknown
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_029b: Expected O, but got Unknown
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Expected O, but got Unknown
		//IL_0337: Unknown result type (might be due to invalid IL or missing references)
		//IL_0341: Expected O, but got Unknown
		//IL_03d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e0: Expected O, but got Unknown
		Panel pageByIndex = GetPageByIndex(13);
		int y = 5;
		y = Head(pageByIndex, y, "One-Click Optimizer");
		Label val = new Label();
		((Control)val).Text = "SAFE QUICK OPTIMIZE avoids app removal and security changes. FULL OPTIMIZER applies every\nadvanced tweak, including debloat and security reductions. Both create a restore point first.";
		((Control)val).Location = new Point(12, y);
		((Control)val).Size = new Size(750, 36);
		((Control)val).ForeColor = WARN;
		((Control)val).Font = FS;
		((Control)pageByIndex).Controls.Add((Control)(object)val);
		y += 42;
		Button val2 = new Button();
		((Control)val2).Text = "RUN FULL OPTIMIZER";
		((Control)val2).Location = new Point(310, y);
		((Control)val2).Size = new Size(240, 45);
		((ButtonBase)val2).FlatStyle = (FlatStyle)0;
		((Control)val2).BackColor = Color.FromArgb(18, 58, 128);
		((Control)val2).ForeColor = Color.White;
		((Control)val2).Font = new Font("Segoe UI Semibold", 12f);
		((ButtonBase)val2).FlatAppearance.BorderSize = 0;
		((Control)pageByIndex).Controls.Add((Control)(object)val2);
		Button fullCancelButton = new Button();
		((Control)fullCancelButton).Text = "✕  CANCEL";
		((Control)fullCancelButton).Location = new Point(560, y);
		((Control)fullCancelButton).Size = new Size(120, 45);
		((ButtonBase)fullCancelButton).FlatStyle = (FlatStyle)0;
		((Control)fullCancelButton).BackColor = Color.FromArgb(158, 42, 55);
		((Control)fullCancelButton).ForeColor = Color.White;
		((Control)fullCancelButton).Font = new Font("Segoe UI Semibold", 11f);
		((ButtonBase)fullCancelButton).FlatAppearance.BorderSize = 0;
		((Control)fullCancelButton).Visible = false;
		((Control)fullCancelButton).Click += delegate
		{
			fullOptCancel = true;
			((Control)fullCancelButton).Enabled = false;
		};
		((Control)pageByIndex).Controls.Add((Control)(object)fullCancelButton);
		Button safeButton = new Button();
		((Control)safeButton).Text = "✦  SAFE QUICK OPTIMIZE";
		((Control)safeButton).Location = new Point(12, y);
		((Control)safeButton).Size = new Size(280, 45);
		((ButtonBase)safeButton).FlatStyle = (FlatStyle)0;
		((Control)safeButton).BackColor = ACC;
		((Control)safeButton).ForeColor = Color.White;
		((Control)safeButton).Font = new Font("Segoe UI Semibold", 12f);
		((ButtonBase)safeButton).FlatAppearance.BorderSize = 0;
		((Control)pageByIndex).Controls.Add((Control)(object)safeButton);
		Label progress = new Label();
		((Control)progress).Location = new Point(12, y + 52);
		((Control)progress).Size = new Size(750, 18);
		((Control)progress).ForeColor = GRN;
		((Control)progress).Font = FB;
		((Control)progress).Text = "";
		((Control)pageByIndex).Controls.Add((Control)(object)progress);
		TextBox log = new TextBox();
		((Control)log).Location = new Point(12, y + 75);
		((Control)log).Size = new Size(750, 350);
		((TextBoxBase)log).Multiline = true;
		((TextBoxBase)log).ReadOnly = true;
		log.ScrollBars = (ScrollBars)2;
		((Control)log).BackColor = Color.FromArgb(7, 20, 43);
		((Control)log).ForeColor = Color.FromArgb(111, 181, 255);
		((Control)log).Font = new Font("Consolas", 8.5f);
		((Control)pageByIndex).Controls.Add((Control)(object)log);
		((Control)safeButton).BackColor = Color.FromArgb(38, 116, 235);
		((Control)safeButton).Click += delegate
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Invalid comparison between Unknown and I4
			if ((int)MessageBox.Show("Safe Quick Optimize applies common Windows 11 performance changes without removing apps or disabling security.\n\nCreate a restore point and continue?", "Astryx Tweaks - Safe Optimize", (MessageBoxButtons)4, (MessageBoxIcon)64) == 6)
			{
				((TextBoxBase)log).Clear();
				((Control)progress).Text = "Creating restore point...";
				((Control)progress).ForeColor = WARN;
				if (!CreateBackup())
				{
					((Control)progress).Text = "Safe optimization cancelled: no restore point was created.";
				}
				else
				{
					int complete = 0;
					Action<string, Action> obj = delegate(string name, Action step)
					{
						try
						{
							step();
							complete++;
							((TextBoxBase)log).AppendText("[OK] " + name + "\r\n");
						}
						catch (Exception ex)
						{
							((TextBoxBase)log).AppendText("[FAIL] " + name + ": " + ex.Message + "\r\n");
						}
						((Control)progress).Text = "Safe optimize: " + complete + "/10";
						Application.DoEvents();
					};
					obj("High Performance power plan", delegate
					{
						Run("powercfg", "/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
					});
					obj("Hardware GPU scheduling", delegate
					{
						Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "HwSchMode", 2, null);
					});
					obj("Windows Game Mode", delegate
					{
						Reg("HKCU\\SOFTWARE\\Microsoft\\GameBar", "AutoGameModeEnabled", 1, null);
					});
					obj("Disable Game DVR", delegate
					{
						Reg("HKCU\\System\\GameConfigStore", "GameDVR_Enabled", 0, null);
					});
					obj("Remove startup delay", delegate
					{
						Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize", "StartupDelayInMSec", 0, null);
					});
					obj("Disable Edge Startup Boost", delegate
					{
						Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "StartupBoostEnabled", 0, null);
					});
					obj("Enable Storage Sense", delegate
					{
						Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy", "01", 1, null);
					});
					obj("Enable SSD TRIM", delegate
					{
						Run("fsutil", "behavior set DisableDeleteNotify 0");
					});
					obj("Flush DNS cache", delegate
					{
						Run("ipconfig", "/flushdns");
					});
					obj("Clear DirectX shader cache", delegate
					{
						CleanDir(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"));
					});
					((Control)progress).Text = "SAFE OPTIMIZATION COMPLETE — restart Windows for best results.";
					((Control)progress).ForeColor = GRN;
				}
			}
		};
		((Control)val2).Click += delegate
		{
			RunFullOptimizer(log, progress, val2, safeButton, fullCancelButton);
		};
		y += 440;
		y = Head(pageByIndex, y, "Undo One-Click");
		y = Twk(pageByIndex, y, "Restore default power plan", "Reverts to Balanced power plan", delegate
		{
			Run("powercfg", "/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Re-enable hibernation", "Restores hibernate", delegate
		{
			Run("powercfg", "/h on");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Re-enable telemetry", "Allows diagnostic data", delegate
		{
			RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "AllowTelemetry");
			SvcAuto("DiagTrack");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Re-enable HPET", "Restores platform clock", delegate
		{
			Run("bcdedit", "/set useplatformclock true");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Run safe cleanup only", "Clears temporary files, shader cache, and DNS without security changes", delegate
		{
			CleanDir(Path.GetTempPath());
			CleanDir(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"));
			Run("ipconfig", "/flushdns");
			Run("fsutil", "behavior set DisableDeleteNotify 0");
		}, null, null, on: false);
	}

	private void LaunchTalon()
	{
		Process.Start(new ProcessStartInfo
		{
			FileName = "powershell.exe",
			Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"irm https://raventechnologiesgroup.com/talon/run | iex\"",
			UseShellExecute = true,
			Verb = "runas",
			WindowStyle = ProcessWindowStyle.Normal
		});
	}

	private void RunTalonBlocking()
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Invalid comparison between Unknown and I4
		do
		{
			Process process;
			try
			{
				process = Process.Start(new ProcessStartInfo
				{
					FileName = "powershell.exe",
					Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"irm https://raventechnologiesgroup.com/talon/run | iex\"",
					UseShellExecute = true,
					Verb = "runas",
					WindowStyle = ProcessWindowStyle.Normal
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show("Talon could not be launched as administrator.\n\n" + ex.Message, "Talon launch failed", (MessageBoxButtons)0, (MessageBoxIcon)16);
				break;
			}
			if (process == null)
			{
				break;
			}
			while (true)
			{
				try
				{
					if (process.HasExited)
					{
						break;
					}
				}
				catch
				{
					break;
				}
				Application.DoEvents();
				Thread.Sleep(150);
			}
		}
		while ((int)MessageBox.Show("Did you follow through with ALL of Talon's instructions and close its PowerShell window?\n\nClick Yes to continue with Astryx Tweaks' built-in tweaks.\nClick No to open Talon again.", "Talon completion check", (MessageBoxButtons)4, (MessageBoxIcon)32) != 6);
	}

	private bool LevelIncludesPage(int level, int idx)
	{
		bool flag = idx == 1 || idx == 2 || idx == 3 || idx == 6 || idx == 15;
		if (level <= 0)
		{
			return flag;
		}
		if (level == 1)
		{
			if (!flag && idx != 0 && idx != 4 && idx != 5 && idx != 7)
			{
				return idx == 11;
			}
			return true;
		}
		return true;
	}

	private void RunFullOptimizer(TextBox log, Label progress, Button runBtn, Button safeBtn, Button cancelBtn)
	{
		//IL_0609: Unknown result type (might be due to invalid IL or missing references)
		if (fullOptRunning)
		{
			return;
		}
		OptimizerOptions optimizerOptions = ShowOptimizerPreDialog();
		if (optimizerOptions == null)
		{
			return;
		}
		((TextBoxBase)log).Clear();
		((Control)progress).ForeColor = WARN;
		((Control)progress).Text = "Creating restore point \"" + optimizerOptions.RestoreName + "\"...";
		Application.DoEvents();
		if (!CreateNamedRestorePoint(optimizerOptions.RestoreName))
		{
			((Control)progress).Text = "Optimization cancelled: no verified restore point was created.";
			((TextBoxBase)log).AppendText("[CANCELLED] No tweaks were applied.\r\n");
			return;
		}
		List<OptStep> list = new List<OptStep>();
		if (optimizerOptions.RunTalon)
		{
			list.Add(new OptStep
			{
				Name = "Talon debloat assistant (open-source)",
				Apply = RunTalonBlocking,
				Revert = null,
				Toggle = null
			});
		}
		foreach (OptStep defenderStep in optimizerOptions.DefenderSteps)
		{
			list.Add(defenderStep);
		}
		foreach (CheckBox item2 in allCB)
		{
			Panel item = tweakPages[item2];
			int num = orderedPages.IndexOf(item);
			string text = tweakNames[item2];
			if (num >= 0 && num <= 15 && num != 13 && !text.StartsWith("Open ", StringComparison.OrdinalIgnoreCase) && text.IndexOf("Defender", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("SmartScreen", StringComparison.OrdinalIgnoreCase) < 0 && LevelIncludesPage(optimizerOptions.Level, num))
			{
				list.Add(new OptStep
				{
					Name = text,
					Apply = acts[item2],
					Revert = (revs.ContainsKey(item2) ? revs[item2] : null),
					Toggle = item2
				});
			}
		}
		int count = list.Count;
		fullOptRunning = true;
		fullOptCancel = false;
		((Control)cancelBtn).Visible = true;
		((Control)cancelBtn).Enabled = true;
		((Control)cancelBtn).BringToFront();
		((Control)runBtn).Enabled = false;
		((Control)safeBtn).Enabled = false;
		string text2 = ((optimizerOptions.Level <= 0) ? "BASIC" : ((optimizerOptions.Level == 1) ? "OPTIMIZED" : "EXTREME"));
		((TextBoxBase)log).AppendText("Starting " + text2 + " OPTIMIZER — " + count + " actions queued.\r\n");
		if (optimizerOptions.RunTalon)
		{
			((TextBoxBase)log).AppendText("Talon (open-source debloat) will launch first in its own window.\r\n");
		}
		((TextBoxBase)log).AppendText("\r\n");
		List<OptStep> list2 = new List<OptStep>();
		int num2 = 0;
		for (int i = 0; i < list.Count; i++)
		{
			if (fullOptCancel)
			{
				break;
			}
			OptStep optStep = list[i];
			((Control)progress).ForeColor = WARN;
			((Control)progress).Text = "Progress: " + (i + 1) + "/" + count + " — " + optStep.Name;
			try
			{
				optStep.Apply();
				num2++;
				list2.Add(optStep);
				((TextBoxBase)log).AppendText("[OK] " + (i + 1) + "/" + count + "  " + optStep.Name + "\r\n");
				if (optStep.Toggle != null)
				{
					optStep.Toggle.Checked = true;
				}
			}
			catch (Exception ex)
			{
				((TextBoxBase)log).AppendText("[FAIL] " + optStep.Name + ": " + ex.Message + "\r\n");
			}
			((TextBoxBase)log).SelectionStart = ((TextBoxBase)log).TextLength;
			((TextBoxBase)log).ScrollToCaret();
			Application.DoEvents();
		}
		if (fullOptCancel)
		{
			((Control)progress).ForeColor = WARN;
			((Control)progress).Text = "Cancelling — reverting " + list2.Count + " applied tweak(s)...";
			Application.DoEvents();
			int num3 = 0;
			for (int num4 = list2.Count - 1; num4 >= 0; num4--)
			{
				OptStep optStep2 = list2[num4];
				if (optStep2.Revert != null)
				{
					try
					{
						optStep2.Revert();
						num3++;
						if (optStep2.Toggle != null)
						{
							optStep2.Toggle.Checked = false;
						}
						((TextBoxBase)log).AppendText("[REVERTED] " + optStep2.Name + "\r\n");
					}
					catch (Exception ex2)
					{
						((TextBoxBase)log).AppendText("[REVERT FAILED] " + optStep2.Name + ": " + ex2.Message + "\r\n");
					}
				}
				else
				{
					((TextBoxBase)log).AppendText("[NO UNDO] " + optStep2.Name + " has no automatic revert.\r\n");
				}
				((TextBoxBase)log).SelectionStart = ((TextBoxBase)log).TextLength;
				((TextBoxBase)log).ScrollToCaret();
				Application.DoEvents();
			}
			((Control)progress).ForeColor = WARN;
			((Control)progress).Text = "CANCELLED — " + num3 + " tweak(s) reverted. Your PC was NOT fully optimized.";
			((TextBoxBase)log).AppendText("\r\n=== CANCELLED ===\r\nReverted " + num3 + " of " + list2.Count + " applied tweaks.\r\n");
			MessageBox.Show("Optimization cancelled.\n\nEvery tweak that had already been applied has been reverted where an undo exists (" + num3 + " of " + list2.Count + "). Your system could NOT be optimized as expected because you cancelled the process partway through.\n\nAnything launched in a separate window (such as Talon) is not controlled by Astryx Tweaks and must be closed manually. You can run the optimizer again at any time.", "Optimization cancelled", (MessageBoxButtons)0, (MessageBoxIcon)48);
		}
		else
		{
			((Control)progress).ForeColor = GRN;
			((Control)progress).Text = "DONE! Applied " + num2 + "/" + count + " optimizations. RESTART NOW!";
			((TextBoxBase)log).AppendText("\r\n=== FULL OPTIMIZATION COMPLETE: " + num2 + "/" + count + " ===\r\nRESTART YOUR PC NOW for all changes to take effect.");
		}
		((Control)cancelBtn).Visible = false;
		((Control)cancelBtn).Enabled = true;
		((Control)runBtn).Enabled = true;
		((Control)safeBtn).Enabled = true;
		fullOptRunning = false;
	}

	private OptimizerOptions ShowOptimizerPreDialog()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Expected O, but got Unknown
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Expected O, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Expected O, but got Unknown
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Expected O, but got Unknown
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Expected O, but got Unknown
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Expected O, but got Unknown
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Expected O, but got Unknown
		//IL_0314: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Expected O, but got Unknown
		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c7: Expected O, but got Unknown
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_044e: Expected O, but got Unknown
		//IL_0523: Unknown result type (might be due to invalid IL or missing references)
		//IL_052a: Expected O, but got Unknown
		//IL_0598: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a2: Expected O, but got Unknown
		//IL_05c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ce: Expected O, but got Unknown
		//IL_0642: Unknown result type (might be due to invalid IL or missing references)
		//IL_064c: Expected O, but got Unknown
		//IL_0678: Unknown result type (might be due to invalid IL or missing references)
		//IL_067e: Invalid comparison between Unknown and I4
		Form dlg = new Form();
		((Control)dlg).Text = "Astryx Tweaks — Before We Optimize";
		dlg.FormBorderStyle = (FormBorderStyle)0;
		dlg.StartPosition = (FormStartPosition)4;
		dlg.Size = new Size(660, 500);
		((Control)dlg).BackColor = BG;
		((Control)dlg).ForeColor = TXT;
		((Control)dlg).Font = FN;
		((Control)dlg).Paint += (PaintEventHandler)delegate(object _bs, PaintEventArgs _be)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Expected O, but got Unknown
			Pen val10 = new Pen(ACC, 1f);
			try
			{
				_be.Graphics.DrawRectangle(val10, 0, 0, ((Control)dlg).Width - 1, ((Control)dlg).Height - 1);
			}
			finally
			{
				((IDisposable)val10)?.Dispose();
			}
		};
		Panel val = new Panel();
		((Control)val).Location = new Point(0, 0);
		((Control)val).Size = new Size(660, 5);
		((Control)val).BackColor = ACC;
		((Control)dlg).Controls.Add((Control)(object)val);
		Label val2 = new Label();
		((Control)val2).Text = "Before We Optimize";
		((Control)val2).Font = new Font("Segoe UI Semibold", 18f);
		((Control)val2).ForeColor = TXT;
		((Control)val2).Location = new Point(24, 20);
		((Control)val2).AutoSize = true;
		((Control)dlg).Controls.Add((Control)(object)val2);
		Label val3 = new Label();
		((Control)val3).Text = "Name your restore point, then choose whether to run Talon first. Windows Defender is left\nuntouched here — use the separate Windows Defender tab for those changes.";
		((Control)val3).Font = FB;
		((Control)val3).ForeColor = MUTED;
		((Control)val3).Location = new Point(26, 58);
		((Control)val3).Size = new Size(610, 40);
		((Control)dlg).Controls.Add((Control)(object)val3);
		Label val4 = new Label();
		((Control)val4).Text = "Restore point name";
		((Control)val4).Font = FS;
		((Control)val4).ForeColor = ACC;
		((Control)val4).Location = new Point(26, 104);
		((Control)val4).AutoSize = true;
		((Control)dlg).Controls.Add((Control)(object)val4);
		TextBox rpBox = new TextBox();
		((Control)rpBox).Text = "Astryx Tweaks Optimizer " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
		((Control)rpBox).Location = new Point(26, 124);
		((Control)rpBox).Size = new Size(608, 24);
		((TextBoxBase)rpBox).BorderStyle = (BorderStyle)1;
		((Control)rpBox).BackColor = Color.FromArgb(18, 26, 46);
		((Control)rpBox).ForeColor = TXT;
		((Control)rpBox).Font = FB;
		((Control)dlg).Controls.Add((Control)(object)rpBox);
		Panel val5 = new Panel();
		((Control)val5).Location = new Point(20, 162);
		((Control)val5).Size = new Size(620, 120);
		((Control)val5).BackColor = BG;
		((Control)dlg).Controls.Add((Control)(object)val5);
		int ry = 4;
		ToggleSwitch talonSwitch = AddOptRow(val5, ref ry, "Talon debloat (open-source) — OPTIONAL", "Launches Talon, a separate open-source tool that deep-debloats Windows. It opens first in its own window; Astryx Tweaks waits until you finish and close it before applying its own tweaks. Not made by Astryx Tweaks.", ACC2, on: true);
		int[] levelSel = new int[1] { 1 };
		Label val6 = new Label();
		((Control)val6).Text = "Optimization level";
		((Control)val6).Font = FS;
		((Control)val6).ForeColor = ACC;
		((Control)val6).Location = new Point(26, 290);
		((Control)val6).AutoSize = true;
		((Control)dlg).Controls.Add((Control)(object)val6);
		string[] array = new string[3] { "Basic", "Optimized", "Extreme" };
		string[] lvlDescs = new string[3] { "Basic — safe performance & visual tweaks only (CPU, GPU, RAM, gaming, appearance). Lowest risk.", "Optimized — recommended. Adds debloat, network, general and driver tweaks on top of Basic.", "Extreme — applies EVERY eligible reversible tweak, including advanced and privacy modules. Most aggressive." };
		Button[] lvlBtns = (Button[])(object)new Button[3];
		Label lvlDesc = new Label();
		((Control)lvlDesc).Font = FS;
		((Control)lvlDesc).ForeColor = MUTED;
		((Control)lvlDesc).Location = new Point(26, 352);
		((Control)lvlDesc).Size = new Size(610, 40);
		Action<int> setLevel = delegate(int L)
		{
			levelSel[0] = L;
			for (int i = 0; i < 3; i++)
			{
				((Control)lvlBtns[i]).BackColor = ((i == L) ? ACC : Color.FromArgb(20, 28, 48));
				((Control)lvlBtns[i]).ForeColor = ((i == L) ? Color.White : MUTED);
			}
			((Control)lvlDesc).Text = lvlDescs[L];
		};
		for (int num = 0; num < 3; num++)
		{
			int kk = num;
			Button val7 = new Button();
			((Control)val7).Text = array[num];
			((Control)val7).Location = new Point(26 + num * 150, 312);
			((Control)val7).Size = new Size(142, 34);
			((ButtonBase)val7).FlatStyle = (FlatStyle)0;
			((ButtonBase)val7).FlatAppearance.BorderSize = 0;
			((Control)val7).Font = FB;
			((Control)val7).Click += delegate
			{
				setLevel(kk);
			};
			lvlBtns[num] = val7;
			((Control)dlg).Controls.Add((Control)(object)val7);
		}
		((Control)dlg).Controls.Add((Control)(object)lvlDesc);
		setLevel(1);
		OptimizerOptions[] outResult = new OptimizerOptions[1];
		Button val8 = new Button();
		((Control)val8).Text = "APPLY & OPTIMIZE";
		((Control)val8).Location = new Point(300, 424);
		((Control)val8).Size = new Size(220, 40);
		((ButtonBase)val8).FlatStyle = (FlatStyle)0;
		((ButtonBase)val8).FlatAppearance.BorderSize = 0;
		((Control)val8).BackColor = ACC;
		((Control)val8).ForeColor = Color.White;
		((Control)val8).Font = new Font("Segoe UI Semibold", 11f);
		((Control)val8).Click += delegate
		{
			OptimizerOptions optimizerOptions = new OptimizerOptions
			{
				RunTalon = ((CheckBox)talonSwitch).Checked,
				Level = levelSel[0],
				RestoreName = (string.IsNullOrWhiteSpace(((Control)rpBox).Text) ? ("Astryx Tweaks Optimizer " + DateTime.Now.ToString("yyyy-MM-dd HH:mm")) : ((Control)rpBox).Text.Trim())
			};
			outResult[0] = optimizerOptions;
			dlg.DialogResult = (DialogResult)1;
		};
		((Control)dlg).Controls.Add((Control)(object)val8);
		Button val9 = new Button();
		((Control)val9).Text = "CANCEL";
		((Control)val9).Location = new Point(140, 424);
		((Control)val9).Size = new Size(150, 40);
		((ButtonBase)val9).FlatStyle = (FlatStyle)0;
		((ButtonBase)val9).FlatAppearance.BorderSize = 0;
		((Control)val9).BackColor = Color.FromArgb(34, 40, 58);
		((Control)val9).ForeColor = TXT;
		((Control)val9).Font = new Font("Segoe UI Semibold", 11f);
		((Control)val9).Click += delegate
		{
			dlg.DialogResult = (DialogResult)2;
		};
		((Control)dlg).Controls.Add((Control)(object)val9);
		if ((int)dlg.ShowDialog((IWin32Window)(object)this) == 1)
		{
			return outResult[0];
		}
		return null;
	}

	private ToggleSwitch AddOptRow(Panel list, ref int ry, string name, string desc, Color accent, bool on)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Expected O, but got Unknown
		Panel val = new Panel();
		((Control)val).Location = new Point(8, ry);
		((Control)val).Size = new Size(588, 62);
		((Control)val).BackColor = Color.FromArgb(16, 24, 44);
		Label val2 = new Label();
		((Control)val2).Text = name;
		((Control)val2).Font = FB;
		((Control)val2).ForeColor = TXT;
		((Control)val2).Location = new Point(14, 9);
		((Control)val2).Size = new Size(500, 20);
		((Control)val).Controls.Add((Control)(object)val2);
		Label val3 = new Label();
		((Control)val3).Text = desc;
		((Control)val3).Font = FS;
		((Control)val3).ForeColor = accent;
		((Control)val3).Location = new Point(14, 31);
		((Control)val3).Size = new Size(510, 26);
		((Control)val).Controls.Add((Control)(object)val3);
		ToggleSwitch toggleSwitch = new ToggleSwitch();
		((Control)toggleSwitch).Location = new Point(532, 18);
		((CheckBox)toggleSwitch).Checked = on;
		((Control)val).Controls.Add((Control)(object)toggleSwitch);
		((Control)list).Controls.Add((Control)(object)val);
		ry += 70;
		return toggleSwitch;
	}

	private void BuildAdvanced()
	{
		Panel pageByIndex = GetPageByIndex(14);
		int y = 5;
		y = Head(pageByIndex, y, "Advanced Tweaks");
		y = Twk(pageByIndex, y, "Adobe URL Block List - Enable", "Blocks Adobe telemetry URLs", delegate
		{
			string path = "C:\\Windows\\System32\\drivers\\etc\\hosts";
			string[] obj = new string[5] { "lmlicenses.wip4.adobe.com", "lm.licenses.adobe.com", "na1r.services.adobe.com", "hlrcv.stage.adobe.com", "practivate.adobe.com" };
			string text = File.ReadAllText(path);
			string[] array = obj;
			foreach (string text2 in array)
			{
				string text3 = "0.0.0.0 " + text2;
				if (!text.Contains(text3))
				{
					File.AppendAllText(path, "\n" + text3);
				}
			}
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Background Apps - Disable", "Stops all UWP background activity", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled", 1, null);
		}, delegate
		{
			RegDel("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications", "GlobalUserDisabled");
		}, null, on: true);
		y = Twk(pageByIndex, y, "IPv6 - Disable", "Removes IPv6 overhead", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters", "DisabledComponents", 255, null);
		}, delegate
		{
			RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip6\\Parameters", "DisabledComponents");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Teredo - Disable", "Removes Teredo tunneling", delegate
		{
			Run("netsh", "interface teredo set state disabled");
		}, delegate
		{
			Run("netsh", "interface teredo set state default");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Date & Time - Set to UTC", "Forces UTC time (fixes dual-boot)", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\TimeZoneInformation", "RealTimeIsUniversal", 1, null);
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Disable Reserved Storage", "Frees ~7GB reserved space", delegate
		{
			Run("dism", "/Online /Set-ReservedStorageState /Disabled");
		}, delegate
		{
			Run("dism", "/Online /Set-ReservedStorageState /Enabled");
		}, null, on: false);
		y = Twk(pageByIndex, y, "File Explorer Home and Gallery - Disable", "Removes Home/Gallery from Explorer", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "ShowGallery", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Fullscreen Optimizations - Disable", "Lower input lag in games", delegate
		{
			Reg("HKCU\\SYSTEM\\GameConfigStore", "GameDVR_FSEBehaviorMode", 2, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Microsoft OneDrive - Remove", "Uninstalls OneDrive", delegate
		{
			RunPS("Stop-Process -Name OneDrive -Force -EA 0; $p=if(Test-Path \"$env:SystemRoot\\SysWOW64\\OneDriveSetup.exe\"){\"$env:SystemRoot\\SysWOW64\\OneDriveSetup.exe\"}else{\"$env:SystemRoot\\System32\\OneDriveSetup.exe\"}; Start-Process $p -Arg '/uninstall' -Wait; Remove-Item $env:LOCALAPPDATA\\OneDrive -Recurse -Force -EA 0");
		}, null, "All OneDrive files stay local", on: false);
		y = Twk(pageByIndex, y, "RDP Unsigned File Warnings - Disable", "Stops RDP file warning popups", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Terminal Server Client", "AuthenticationLevel", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Right-Click Menu - Classic Layout", "Restores old right-click menu", delegate
		{
			Reg("HKCU\\SOFTWARE\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32", "", 0, "");
		}, delegate
		{
			RunPS("Remove-Item 'HKCU:\\SOFTWARE\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}' -Recurse -EA 0");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Storage Sense - Disable", "Prevents auto cleanup of files", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy", "01", 0, null);
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "System Tray Notifications & Calendar - Disable", "Removes tray overflow and calendar", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "EnableAutoTray", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Visual Effects - Best Performance", "Disables all visual effects for max speed", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\VisualEffects", "VisualFXSetting", 2, null);
			Reg("HKCU\\Control Panel\\Desktop", "UserPreferencesMask", new byte[8] { 144, 18, 3, 128, 16, 0, 0, 0 }, null);
		}, delegate
		{
			RegDel("HKCU\\Control Panel\\Desktop", "UserPreferencesMask");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Windows AI - Disable and Remove", "Removes Copilot, Recall, AI features", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsCopilot", "TurnOffWindowsCopilot", 1, null);
			Reg("HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsCopilot", "TurnOffWindowsCopilot", 1, null);
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsAI", "DisableAIDataAnalysis", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Xbox & Gaming Components - Remove", "Removes Xbox services and Game Bar", delegate
		{
			SvcDis("XblAuthManager");
			SvcDis("XblGameSave");
			SvcDis("XboxGipSvc");
			SvcDis("XboxNetApiSvc");
			Reg("HKCU\\System\\GameConfigStore", "GameDVR_Enabled", 0, null);
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR", "AllowGameDVR", 0, null);
		}, delegate
		{
			SvcAuto("XblAuthManager");
			SvcAuto("XblGameSave");
			SvcAuto("XboxGipSvc");
			SvcAuto("XboxNetApiSvc");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Razer Software Auto-Install - Disable", "Stops Razer auto-installing bloatware", delegate
		{
			Reg("HKLM\\SOFTWARE\\Razer", "AutoInstall", 0, null);
			SvcDis("Razer Synapse Service");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Microsoft Edge - Remove", "Full Edge removal (may break WebView2)", delegate
		{
			RunPS("Get-AppxPackage *edge* | Remove-AppxPackage -EA 0; $e=(Get-ChildItem 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application' -EA 0 | Sort Name -Desc)[0]; if($e){Start-Process ($e.FullName + '\\Installer\\setup.exe') -Arg '--uninstall','--system-level','--force-uninstall' -Wait}");
		}, null, "May break WebView2 apps!", on: false);
		y = Twk(pageByIndex, y, "Brave Browser - Deblock", "Removes Brave Brave-specific blocking", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\BraveSoftware\\Brave", "BraveRewardsDisabled", 1, null);
			Reg("HKLM\\SOFTWARE\\Policies\\BraveSoftware\\Brave", "BraveVPNDisabled", 1, null);
			Reg("HKLM\\SOFTWARE\\Policies\\BraveSoftware\\Brave", "BraveWalletDisabled", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Enable Win32 long paths", "Allows supported apps to use file paths longer than 260 characters", delegate
		{
			Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem", "LongPathsEnabled", 1, null);
		}, delegate
		{
			RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem", "LongPathsEnabled");
		}, null, on: true);
	}

	private void BuildUITweaks()
	{
		Panel pageByIndex = GetPageByIndex(15);
		int y = 5;
		y = Head(pageByIndex, y, "Lock Screen");
		y = Twk(pageByIndex, y, "Disable lock screen", "Go straight to login", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Personalization", "NoLockScreen", 1, null);
		}, delegate
		{
			RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Personalization", "NoLockScreen");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable lock screen tips", "No tips on lock screen", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "RotatingLockScreenOverlayEnabled", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable rotating lock screen images", "Static lock screen", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "RotatingLockScreenEnabled", 0, null);
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Start Menu");
		y = Twk(pageByIndex, y, "Remove recommended section from Start", "Clean start menu", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_TrackDocs", 0, null);
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer", "HideRecommendedSection", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Remove recently added apps from Start", "No app suggestions", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_NotifyRecentlyAdded", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Remove most used apps from Start", "No usage-based suggestions", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_TrackProgs", 0, null);
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Taskbar");
		y = Twk(pageByIndex, y, "Taskbar alignment left", "Classic left-aligned icons", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarAl", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable search icon on taskbar", "Removes search from taskbar", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search", "SearchboxTaskbarMode", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable task view button", "Removes task view from taskbar", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowTaskViewButton", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Hide taskbar labels", "Icons only, no text", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarGlomLevel", 2, null);
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Edge Swipe & Gestures");
		y = Twk(pageByIndex, y, "Disable Edge Swipe from left", "Prevents accidental gesture", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\EdgeUI", "AllowEdgeSwipe", 0, null);
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Snap Layouts");
		y = Twk(pageByIndex, y, "Disable snap assist flyout", "No snap layout popup on hover", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "EnableSnapAssistFlyout", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Disable snap assist", "No window arrangement helper", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "SnapAssist", 0, null);
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Context Menus");
		y = Twk(pageByIndex, y, "Classic right-click menu", "Old-style right-click (Win10)", delegate
		{
			Reg("HKCU\\SOFTWARE\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32", "", 0, "");
		}, delegate
		{
			RunPS("Remove-Item 'HKCU:\\SOFTWARE\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}' -Recurse -EA 0");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Remove 'Give access to' from context menu", "Cleaner right-click", delegate
		{
			RunPS("Remove-Item 'HKLM:\\SOFTWARE\\Classes\\*\\shellex\\ContextMenuHandlers\\Sharing' -Recurse -EA 0");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Remove 'Cast to Device' from context menu", "Cleaner right-click", delegate
		{
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Blocked", "{7AD84985-87B4-4a16-BE58-8B72A5B390F7}", 0, "");
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Remove 'Share' from context menu", "Cleaner right-click", delegate
		{
			RunPS("Remove-Item 'HKLM:\\SOFTWARE\\Classes\\*\\shellex\\ContextMenuHandlers\\ModernSharing' -Recurse -EA 0");
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Visual Effects & Animations");
		y = Twk(pageByIndex, y, "Disable all animations", "Instant window transitions", delegate
		{
			Reg("HKCU\\Control Panel\\Desktop\\WindowMetrics", "MinAnimate", 0, "0");
			Reg("HKCU\\Control Panel\\Desktop", "UserPreferencesMask", new byte[8] { 144, 18, 3, 128, 16, 0, 0, 0 }, null);
		}, delegate
		{
			RegDel("HKCU\\Control Panel\\Desktop", "UserPreferencesMask");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable transparency effects", "Faster desktop rendering", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "EnableTransparency", 0, null);
		}, delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "EnableTransparency", 1, null);
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable window peek (Aero Peek)", "No transparency on hover", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\DWM", "EnableAeroPeek", 0, null);
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Desktop Icons");
		y = Twk(pageByIndex, y, "Show This PC on desktop", "Adds computer icon", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel", "{20D04FE0-3AEA-1069-A2D8-08002B30309D}", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Show Recycle Bin on desktop", "Adds trash icon", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel", "{645FF040-5081-101B-9F08-00AA002F954E}", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Show Network on desktop", "Adds network icon", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\HideDesktopIcons\\NewStartPanel", "{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}", 0, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Remove shortcut arrow from icons", "Cleaner shortcuts", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "IsShortcut", 0, "");
		}, null, null, on: true);
		y = Head(pageByIndex, y, "Verbose Status Messages");
		y = Twk(pageByIndex, y, "Enable verbose startup/shutdown messages", "Shows detailed progress during boot", delegate
		{
			Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "VerboseStatus", 1, null);
		}, null, null, on: true);
		y = Twk(pageByIndex, y, "Use Windows dark mode", "Applies a dark theme to apps and the system shell", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", 0, null);
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "SystemUsesLightTheme", 0, null);
		}, delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", 1, null);
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "SystemUsesLightTheme", 1, null);
		}, null, on: false);
	}

	private void BuildInstall()
	{
		Panel pageByIndex = GetPageByIndex(16);
		int y = 5;
		y = Head(pageByIndex, y, "Browsers");
		y = Twk(pageByIndex, y, "Install Google Chrome", "Most popular browser", delegate
		{
			Winget("Google.Chrome");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Brave", "Privacy browser", delegate
		{
			Winget("Brave.Brave");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Firefox", "Open-source browser", delegate
		{
			Winget("Mozilla.Firefox");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Vivaldi", "Customizable browser", delegate
		{
			Winget("Vivaldi.Vivaldi");
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Communication");
		y = Twk(pageByIndex, y, "Install Discord", "Voice/text chat", delegate
		{
			Winget("Discord.Discord");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Telegram", "Secure messaging", delegate
		{
			Winget("Telegram.TelegramDesktop");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Slack", "Team collaboration", delegate
		{
			Winget("SlackTechnologies.Slack");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Zoom", "Video conferencing", delegate
		{
			Winget("Zoom.Zoom");
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Development");
		y = Twk(pageByIndex, y, "Install VS Code", "Code editor", delegate
		{
			Winget("Microsoft.VisualStudioCode");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Git", "Version control", delegate
		{
			Winget("Git.Git");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Node.js", "JS runtime", delegate
		{
			Winget("OpenJS.NodeJS.LTS");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Python 3.12", "Programming language", delegate
		{
			Winget("Python.Python.3.12");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Windows Terminal", "Modern terminal", delegate
		{
			Winget("Microsoft.WindowsTerminal");
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Utilities");
		y = Twk(pageByIndex, y, "Install 7-Zip", "File archiver", delegate
		{
			Winget("7zip.7zip");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Notepad++", "Text editor", delegate
		{
			Winget("Notepad++.Notepad++");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install VLC", "Media player", delegate
		{
			Winget("VideoLAN.VLC");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install PowerToys", "Productivity tools", delegate
		{
			Winget("Microsoft.PowerToys");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Everything", "Instant file search", delegate
		{
			Winget("voidtools.Everything");
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Gaming");
		y = Twk(pageByIndex, y, "Install Steam", "PC gaming platform", delegate
		{
			Winget("Valve.Steam");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Epic Games", "Epic Store launcher", delegate
		{
			Winget("EpicGames.EpicGamesLauncher");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install GeForce Experience", "NVIDIA driver updates", delegate
		{
			Winget("Nvidia.GeForceExperience");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install OBS Studio", "Streaming/recording", delegate
		{
			Winget("OBSProject.OBSStudio");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install MSI Afterburner", "GPU overclocking", delegate
		{
			Winget("Guru3D.Afterburner");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install HWiNFO64", "Hardware monitoring", delegate
		{
			Winget("REALiX.HWiNFO");
		}, null, null, on: false);
		y = Head(pageByIndex, y, "System Tools");
		y = Twk(pageByIndex, y, "Install CPU-Z", "CPU info tool", delegate
		{
			Winget("CPUID.CPU-Z");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install CrystalDiskInfo", "Drive health", delegate
		{
			Winget("CrystalDewWorld.CrystalDiskInfo");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install DDU", "Driver uninstaller", delegate
		{
			Winget("Wagnardsoft.DDU");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Malwarebytes", "Anti-malware", delegate
		{
			Winget("Malwarebytes.Malwarebytes");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Revo Uninstaller", "Thorough uninstaller", delegate
		{
			Winget("RevoUninstaller.RevoUninstaller");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install WizTree", "Disk analyzer", delegate
		{
			Winget("AntibodySoftware.WizTree");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install O&O ShutUp10", "Privacy tool", delegate
		{
			Winget("OO Software.ShutUp10");
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Media");
		y = Twk(pageByIndex, y, "Install GIMP", "Free Photoshop", delegate
		{
			Winget("GIMP.GIMP");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Blender", "3D modeling", delegate
		{
			Winget("BlenderFoundation.Blender");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Audacity", "Audio editor", delegate
		{
			Winget("Audacity.Audacity");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Handbrake", "Video converter", delegate
		{
			Winget("HandBrake.HandBrake");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Install Paint.NET", "Image editor", delegate
		{
			Winget("dotPDN.PaintDotNet");
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Windows Utilities");
		y = Twk(pageByIndex, y, "Install Microsoft PowerToys", "Adds FancyZones, remapping, launcher, and power-user tools", delegate
		{
			Winget("Microsoft.PowerToys");
		}, null, null, on: false);
	}

	private int NextY(Panel p)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		int num = 5;
		foreach (Control item in (ArrangedElementCollection)((Control)p).Controls)
		{
			Control val = item;
			if ((int)val.Dock == 0 && val.Bottom > num)
			{
				num = val.Bottom;
			}
		}
		return num + 12;
	}

	private void BuildBoosterXExtras()
	{
		Panel pageByIndex = GetPageByIndex(15);
		if (pageByIndex != null)
		{
			int y = NextY(pageByIndex);
			y = Head(pageByIndex, y, "Customization (BoosterX)");
			y = Twk(pageByIndex, y, "Disable recent documents tracking", "Stops tracking recently opened documents", delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoRecentDocsHistory", 1, null);
			}, delegate
			{
				RegDel("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoRecentDocsHistory");
			}, null, on: true);
			y = Twk(pageByIndex, y, "Disable Microsoft account settings sync", "Stops syncing settings to your account", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\SettingSync", "DisableSettingSync", 2, null);
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\SettingSync", "DisableSettingSyncUserOverride", 1, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\SettingSync", "DisableSettingSync");
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\SettingSync", "DisableSettingSyncUserOverride");
			}, null, on: true);
			y = Twk(pageByIndex, y, "Disable desktop wallpaper compression", "Full-quality desktop background (no JPEG compression)", delegate
			{
				Reg("HKCU\\Control Panel\\Desktop", "JPEGImportQuality", 100, null);
			}, delegate
			{
				RegDel("HKCU\\Control Panel\\Desktop", "JPEGImportQuality");
			}, null, on: true);
			y = Twk(pageByIndex, y, "Disable Windows Spotlight", "Turns off Spotlight images and suggestions", delegate
			{
				Reg("HKCU\\Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableWindowsSpotlightFeatures", 1, null);
				Reg("HKCU\\Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableSpotlightCollectionOnDesktop", 1, null);
			}, delegate
			{
				RegDel("HKCU\\Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableWindowsSpotlightFeatures");
				RegDel("HKCU\\Software\\Policies\\Microsoft\\Windows\\CloudContent", "DisableSpotlightCollectionOnDesktop");
			}, null, on: true);
			y = Twk(pageByIndex, y, "Disable Dynamic Lighting (RGB)", "Turns off Windows RGB device control", delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Lighting", "AmbientLightingEnabled", 0, null);
				Reg("HKCU\\Software\\Microsoft\\Lighting", "ControlledByForegroundApp", 0, null);
			}, delegate
			{
				RegDel("HKCU\\Software\\Microsoft\\Lighting", "AmbientLightingEnabled");
				RegDel("HKCU\\Software\\Microsoft\\Lighting", "ControlledByForegroundApp");
			}, null, on: true);
			y = Twk(pageByIndex, y, "Disable Windows Mobility Center", "Hides Mobility Center on desktop", delegate
			{
				Reg("HKCU\\Software\\Microsoft\\MobilePC\\MobilityCenter", "RunOnDesktop", 0, null);
			}, delegate
			{
				RegDel("HKCU\\Software\\Microsoft\\MobilePC\\MobilityCenter", "RunOnDesktop");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable Customer Experience Program (CEIP)", "Opts out of the CEIP", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\SQMClient\\Windows", "CEIPEnable", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\SQMClient\\Windows", "CEIPEnable");
			}, null, on: true);
			y = Twk(pageByIndex, y, "Add 'Take Ownership' to context menu", "Adds a right-click option to take file/folder ownership", delegate
			{
				RunPS("New-Item -Path 'HKCU:\\Software\\Classes\\*\\shell\\TakeOwnership\\command' -Force | Out-Null; Set-ItemProperty -Path 'HKCU:\\Software\\Classes\\*\\shell\\TakeOwnership' -Name '(default)' -Value 'Take Ownership'; Set-ItemProperty -Path 'HKCU:\\Software\\Classes\\*\\shell\\TakeOwnership' -Name 'HasLUAShield' -Value ''; Set-ItemProperty -Path 'HKCU:\\Software\\Classes\\*\\shell\\TakeOwnership\\command' -Name '(default)' -Value 'powershell -windowstyle hidden -command \\\"$file=$args[0]; takeown /f \\\"$file\\\"; icacls \\\"$file\\\" /grant *S-1-3-4:F /t /c /l\\\" -- \\\"%1\\\"'");
			}, delegate
			{
				RunPS("Remove-Item -Path 'HKCU:\\Software\\Classes\\*\\shell\\TakeOwnership' -Recurse -EA 0");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Enable classic Windows Photo Viewer", "Restores the legacy Photo Viewer for image files", delegate
			{
				RunPS("$k='HKCU:\\Software\\Classes'; foreach($e in '.jpg','.jpeg','.png','.bmp','.gif','.tif','.tiff'){ New-Item -Path \"$k\\$e\" -Force | Out-Null; Set-ItemProperty -Path \"$k\\$e\" -Name '(default)' -Value 'PhotoViewer.FileAssoc.Tiff' }; New-Item -Path 'HKCU:\\Software\\Classes\\Applications\\photoviewer.dll\\shell\\open\\command' -Force | Out-Null; Set-ItemProperty -Path 'HKCU:\\Software\\Classes\\Applications\\photoviewer.dll\\shell\\open\\command' -Name '(default)' -Value '%SystemRoot%\\System32\\rundll32.exe \\\"%ProgramFiles%\\Windows Photo Viewer\\PhotoViewer.dll\\\", ImageView_Fullscreen %1'");
			}, delegate
			{
				RunPS("foreach($e in '.jpg','.jpeg','.png','.bmp','.gif','.tif','.tiff'){ Remove-Item -Path \"HKCU:\\Software\\Classes\\$e\" -Recurse -EA 0 }");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Always show all icons in the taskbar", "Disables the hidden-icons overflow tray", delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", "EnableAutoTray", 0, null);
			}, delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", "EnableAutoTray", 1, null);
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable Sticky Keys shortcut", "Stops the 5x Shift Sticky Keys prompt", delegate
			{
				Reg("HKCU\\Control Panel\\Accessibility\\StickyKeys", "Flags", 0, "506");
			}, delegate
			{
				Reg("HKCU\\Control Panel\\Accessibility\\StickyKeys", "Flags", 0, "510");
			}, null, on: true);
			y = Twk(pageByIndex, y, "Speed up Windows startup (no delay)", "Removes the startup delay for logon apps", delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize", "StartupDelayInMSec", 0, null);
			}, delegate
			{
				RegDel("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize", "StartupDelayInMSec");
			}, null, on: true);
		}
		Panel pageByIndex2 = GetPageByIndex(4);
		if (pageByIndex2 != null)
		{
			int y2 = NextY(pageByIndex2);
			y2 = Head(pageByIndex2, y2, "Debloat (BoosterX)");
			y2 = Twk(pageByIndex2, y2, "Disable Windows 11 notifications", "Turns off toast notifications", delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\PushNotifications", "ToastEnabled", 0, null);
			}, delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\PushNotifications", "ToastEnabled", 1, null);
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Disable automatic Store app updates", "Stops the Microsoft Store from auto-updating apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsStore", "AutoDownload", 2, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsStore", "AutoDownload");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Disable network diagnostic driver (Ndu)", "Frees RAM used by the network data usage driver", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Ndu", "Start", 4, null);
			}, delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Ndu", "Start", 2, null);
			}, "Disables network usage graphs in Task Manager.", on: false);
			y2 = Twk(pageByIndex2, y2, "Pause Windows Updates", "Pauses quality and feature updates far into the future", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseUpdatesExpiryTime", 0, "2099-12-31T00:00:00Z");
				Reg("HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseFeatureUpdatesStartTime", 0, "2024-01-01T00:00:00Z");
				Reg("HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseFeatureUpdatesEndTime", 0, "2099-12-31T00:00:00Z");
				Reg("HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseQualityUpdatesStartTime", 0, "2024-01-01T00:00:00Z");
				Reg("HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseQualityUpdatesEndTime", 0, "2099-12-31T00:00:00Z");
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseUpdatesExpiryTime");
				RegDel("HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseFeatureUpdatesStartTime");
				RegDel("HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseFeatureUpdatesEndTime");
				RegDel("HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseQualityUpdatesStartTime");
				RegDel("HKLM\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings", "PauseQualityUpdatesEndTime");
			}, null, on: false);
		}
		Panel pageByIndex3 = GetPageByIndex(14);
		if (pageByIndex3 != null)
		{
			int y3 = NextY(pageByIndex3);
			y3 = Head(pageByIndex3, y3, "System Tweaks (BoosterX)");
			y3 = Twk(pageByIndex3, y3, "Disable Cross-Device Resume", "Stops cross-device continue/handoff with your phone", delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CDP", "CdpSessionUserAuthzPolicy", 0, null);
				Reg("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CDP", "RomeSdkChannelUserAuthzPolicy", 0, null);
			}, delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CDP", "CdpSessionUserAuthzPolicy", 1, null);
				Reg("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\CDP", "RomeSdkChannelUserAuthzPolicy", 1, null);
			}, "Disables cross-device sync with phone.", on: false);
			y3 = Twk(pageByIndex3, y3, "Enable svchost service grouping", "Groups services into fewer svchost.exe processes", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control", "SvcHostSplitThresholdInKB", 805306368, null);
			}, delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control", "SvcHostSplitThresholdInKB", 380000, null);
			}, null, on: false);
			y3 = Twk(pageByIndex3, y3, "Disable Diagnostic Events (DPS)", "Turns off the Diagnostic Policy Service and WDI hosts", delegate
			{
				SvcDis("DPS");
				SvcDis("WdiServiceHost");
				SvcDis("WdiSystemHost");
			}, delegate
			{
				SvcAuto("DPS");
			}, null, on: false);
			y3 = Twk(pageByIndex3, y3, "Disable Application Compatibility engine", "Turns off PCA and app compat telemetry", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat", "DisablePCA", 1, null);
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat", "DisableEngine", 1, null);
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat", "AITEnable", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat", "DisablePCA");
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat", "DisableEngine");
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat", "AITEnable");
			}, "May affect launching some games in EA/other launchers.", on: false);
			y3 = Twk(pageByIndex3, y3, "Disable Automatic Maintenance", "Stops scheduled automatic maintenance", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance", "MaintenanceDisabled", 1, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Schedule\\Maintenance", "MaintenanceDisabled");
			}, null, on: false);
			y3 = Twk(pageByIndex3, y3, "Disable Scheduled Diagnostics", "Turns off scheduled diagnostic tasks", delegate
			{
				DisTask("\\Microsoft\\Windows\\Diagnosis\\Scheduled");
				DisTask("\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Diagnosis\\Scheduled\" /Enable");
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector\" /Enable");
			}, null, on: false);
			y3 = Twk(pageByIndex3, y3, "Disable UCPD driver", "Disables the User Choice Protection Driver", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\UCPD", "Start", 4, null);
				DisTask("\\Microsoft\\Windows\\AppxDeploymentClient\\UCPD velocity");
			}, delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\UCPD", "Start", 2, null);
			}, null, on: false);
		}
		Panel pageByIndex4 = GetPageByIndex(0);
		if (pageByIndex4 != null)
		{
			int y4 = NextY(pageByIndex4);
			y4 = Head(pageByIndex4, y4, "Privacy & Telemetry (BoosterX)");
			y4 = Twk(pageByIndex4, y4, "Disable .NET CLI telemetry", "Opts out of .NET SDK telemetry", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "DOTNET_CLI_TELEMETRY_OPTOUT", 0, "1");
			}, delegate
			{
				RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "DOTNET_CLI_TELEMETRY_OPTOUT");
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Disable PowerShell telemetry", "Opts out of PowerShell Core telemetry", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "POWERSHELL_TELEMETRY_OPTOUT", 0, "1");
			}, delegate
			{
				RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "POWERSHELL_TELEMETRY_OPTOUT");
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Disable dev tools & CLI telemetry", "Opts out of Azure/Next/Gatsby/Angular CLI telemetry", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "AZURE_CORE_COLLECT_TELEMETRY", 0, "0");
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "NEXT_TELEMETRY_DISABLED", 0, "1");
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "GATSBY_TELEMETRY_DISABLED", 0, "1");
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "NG_CLI_ANALYTICS", 0, "false");
			}, delegate
			{
				RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "AZURE_CORE_COLLECT_TELEMETRY");
				RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "NEXT_TELEMETRY_DISABLED");
				RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "GATSBY_TELEMETRY_DISABLED");
				RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "NG_CLI_ANALYTICS");
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Disable Microsoft Compatibility Appraiser", "Disables the compatibility appraiser task", delegate
			{
				DisTask("\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Application Experience\\Microsoft Compatibility Appraiser\" /Enable");
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Disable CEIP Data Updater tasks", "Disables Customer Experience Improvement tasks", delegate
			{
				DisTask("\\Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator");
				DisTask("\\Microsoft\\Windows\\Customer Experience Improvement Program\\UsbCeip");
				DisTask("\\Microsoft\\Windows\\Customer Experience Improvement Program\\KernelCeipTask");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator\" /Enable");
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Customer Experience Improvement Program\\UsbCeip\" /Enable");
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Disable Application Impact Telemetry Agent", "Disables the AitAgent telemetry task", delegate
			{
				DisTask("\\Microsoft\\Windows\\Application Experience\\AitAgent");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Application Experience\\AitAgent\" /Enable");
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Disable PCA performance reminder", "Disables the Program Compatibility Assistant service", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\PcaSvc", "Start", 4, null);
			}, delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\PcaSvc", "Start", 2, null);
			}, null, on: false);
			y4 = Twk(pageByIndex4, y4, "Disable data collection policy telemetry", "Blocks OneSettings and commercial data pipeline", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "DisableOneSettingsDownloads", 1, null);
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "AllowCommercialDataPipeline", 0, null);
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "LimitEnhancedDiagnosticDataWindowsAnalytics", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "DisableOneSettingsDownloads");
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "AllowCommercialDataPipeline");
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "LimitEnhancedDiagnosticDataWindowsAnalytics");
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Disable Inventory Collector", "Stops app inventory data collection", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat", "DisableInventory", 1, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat", "DisableInventory");
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Disable Steps Recorder", "Turns off the user action recorder (PSR)", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat", "DisableUAR", 1, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppCompat", "DisableUAR");
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Disable voice activation for apps", "Prevents apps/Cortana from listening for wake words", delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\VoiceActivation\\UserPreferenceForAllApps", "AgentActivationEnabled", 0, null);
			}, delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\VoiceActivation\\UserPreferenceForAllApps", "AgentActivationEnabled", 1, null);
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Disable voice activation when locked", "Blocks wake words while the system is locked", delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\VoiceActivation\\UserPreferenceForAllApps", "AgentActivationOnLockScreenEnabled", 0, null);
			}, delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\VoiceActivation\\UserPreferenceForAllApps", "AgentActivationOnLockScreenEnabled", 1, null);
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Disable Windows Search data collection", "Stops device search history collection", delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings", "IsDeviceSearchHistoryEnabled", 0, null);
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search", "AllowSearchToUseLocation", 0, null);
			}, delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\SearchSettings", "IsDeviceSearchHistoryEnabled", 1, null);
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Windows Search", "AllowSearchToUseLocation");
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Disable online (cloud) speech recognition", "Stops sending voice data to Microsoft", delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy", "HasAccepted", 0, null);
			}, delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Speech_OneCore\\Settings\\OnlineSpeechPrivacy", "HasAccepted", 1, null);
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Opt out of tailored experiences", "Disables tailored experiences with diagnostic data", delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 0, null);
			}, delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 1, null);
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Disable Wi-Fi Sense", "Stops auto-connecting to shared/open hotspots", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\WiFi\\AllowWiFiHotSpotReporting", "value", 0, null);
				Reg("HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\WiFi\\AllowAutoConnectToWiFiSenseHotspots", "value", 0, null);
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\WiFi\\AllowWiFiHotSpotReporting", "value", 1, null);
				Reg("HKLM\\SOFTWARE\\Microsoft\\PolicyManager\\default\\WiFi\\AllowAutoConnectToWiFiSenseHotspots", "value", 1, null);
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Disable website access to language list", "Prevents websites from reading your language list", delegate
			{
				Reg("HKCU\\Control Panel\\International\\User Profile", "HttpAcceptLanguageOptOut", 1, null);
			}, delegate
			{
				RegDel("HKCU\\Control Panel\\International\\User Profile", "HttpAcceptLanguageOptOut");
			}, null, on: true);
			y4 = Twk(pageByIndex4, y4, "Disable feedback prompts (Feedback on Write)", "Sets Windows feedback frequency to never", delegate
			{
				Reg("HKCU\\Software\\Microsoft\\Siuf\\Rules", "NumberOfSIUFInPeriod", 0, null);
				Reg("HKCU\\Software\\Microsoft\\Siuf\\Rules", "PeriodInNanoSeconds", 0, null);
			}, delegate
			{
				RegDel("HKCU\\Software\\Microsoft\\Siuf\\Rules", "NumberOfSIUFInPeriod");
				RegDel("HKCU\\Software\\Microsoft\\Siuf\\Rules", "PeriodInNanoSeconds");
			}, null, on: true);
			y4 = Head(pageByIndex4, y4, "App Permissions (BoosterX)");
			y4 = Twk(pageByIndex4, y4, "Deny apps access to call history", "Blocks Windows apps from your call history", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessCallHistory", 2, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessCallHistory");
			}, "Apps using this permission may stop working.", on: false);
			y4 = Twk(pageByIndex4, y4, "Deny apps access to tasks", "Blocks Windows apps from your tasks", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessTasks", 2, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessTasks");
			}, "Apps using this permission may stop working.", on: false);
			y4 = Twk(pageByIndex4, y4, "Deny apps access to messaging", "Blocks Windows apps from reading/sending messages", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessMessaging", 2, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessMessaging");
			}, "Apps using this permission may stop working.", on: false);
			y4 = Twk(pageByIndex4, y4, "Deny apps access to motion data", "Blocks Windows apps from motion/activity data", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessMotion", 2, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessMotion");
			}, "Apps using this permission may stop working.", on: false);
			y4 = Twk(pageByIndex4, y4, "Deny apps access to trusted devices", "Blocks Windows apps from trusted (paired) devices", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessTrustedDevices", 2, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsAccessTrustedDevices");
			}, "Apps using this permission may stop working.", on: false);
			y4 = Twk(pageByIndex4, y4, "Deny apps sync with (wireless) devices", "Blocks apps from syncing with unpaired wireless devices", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsSyncWithDevices", 2, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\AppPrivacy", "LetAppsSyncWithDevices");
			}, "Apps using this permission may stop working.", on: false);
		}
	}

	private void BuildBulkExtras()
	{
		Panel pageByIndex = GetPageByIndex(0);
		if (pageByIndex != null)
		{
			int y = NextY(pageByIndex);
			y = Head(pageByIndex, y, "More: Scheduled tasks (reversible)");
			y = Twk(pageByIndex, y, "Disable task: ProgramDataUpdater", "Compatibility appraiser data", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Application Experience\\ProgramDataUpdater\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Application Experience\\ProgramDataUpdater\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: StartupAppTask", "Startup app tracking", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Application Experience\\StartupAppTask\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Application Experience\\StartupAppTask\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: PcaPatchDbTask", "Program compatibility patching", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Application Experience\\PcaPatchDbTask\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Application Experience\\PcaPatchDbTask\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: Proxy", "SQM autochk proxy", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Autochk\\Proxy\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Autochk\\Proxy\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: Consolidator", "CEIP consolidator", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: UsbCeip", "USB CEIP data", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Customer Experience Improvement Program\\UsbCeip\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Customer Experience Improvement Program\\UsbCeip\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: Microsoft-Windows-DiskDiagnosticDataCollector", "Disk diagnostic data", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\DiskDiagnostic\\Microsoft-Windows-DiskDiagnosticDataCollector\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: DmClient", "Feedback client", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Feedback\\Siuf\\DmClient\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Feedback\\Siuf\\DmClient\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: DmClientOnScenarioDownload", "Feedback scenario client", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Feedback\\Siuf\\DmClientOnScenarioDownload\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Feedback\\Siuf\\DmClientOnScenarioDownload\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: QueueReporting", "Error report queue", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Windows Error Reporting\\QueueReporting\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Windows Error Reporting\\QueueReporting\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: CreateObjectTask", "Cloud experience host", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\CloudExperienceHost\\CreateObjectTask\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\CloudExperienceHost\\CreateObjectTask\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: WinSAT", "System assessment (WinSAT)", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Maintenance\\WinSAT\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Maintenance\\WinSAT\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: Sqm-Tasks", "Perceptive telemetry", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\PI\\Sqm-Tasks\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\PI\\Sqm-Tasks\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: AnalyzeSystem", "Power efficiency analysis", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Power Efficiency Diagnostics\\AnalyzeSystem\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Power Efficiency Diagnostics\\AnalyzeSystem\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: MobilityManager", "RAS mobility manager", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Ras\\MobilityManager\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Ras\\MobilityManager\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: CleanupOfflineContent", "Retail demo cleanup", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Retail Demo\\CleanupOfflineContent\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Retail Demo\\CleanupOfflineContent\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: FamilySafetyMonitor", "Family safety monitor", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Shell\\FamilySafetyMonitor\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Shell\\FamilySafetyMonitor\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: FamilySafetyRefreshTask", "Family safety refresh", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Shell\\FamilySafetyRefreshTask\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Shell\\FamilySafetyRefreshTask\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: MapsToastTask", "Maps toast notifications", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Maps\\MapsToastTask\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Maps\\MapsToastTask\" /Enable");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Disable task: License Validation", "Store license validation", delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Clip\\License Validation\" /Disable");
			}, delegate
			{
				Run("schtasks", "/Change /TN \"\\Microsoft\\Windows\\Clip\\License Validation\" /Enable");
			}, null, on: false);
		}
		Panel pageByIndex2 = GetPageByIndex(1);
		if (pageByIndex2 != null)
		{
			int y2 = NextY(pageByIndex2);
			y2 = Head(pageByIndex2, y2, "More: Power & scheduling");
			y2 = Twk(pageByIndex2, y2, "Disable CPU power throttling", "Stops Windows from slowing background apps", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling", "PowerThrottlingOff", 1, null);
			}, delegate
			{
				RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerThrottling", "PowerThrottlingOff");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Optimize processor scheduling for programs", "Prioritizes foreground apps (Win32PrioritySeparation)", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "Win32PrioritySeparation", 38, null);
			}, delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\PriorityControl", "Win32PrioritySeparation", 2, null);
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Disable dynamic tick", "Can smooth timing on some systems", delegate
			{
				Run("bcdedit", "/set disabledynamictick yes");
			}, delegate
			{
				Run("bcdedit", "/set disabledynamictick no");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Use platform tick", "Forces a consistent timer source", delegate
			{
				Run("bcdedit", "/set useplatformtick yes");
			}, delegate
			{
				Run("bcdedit", "/deletevalue useplatformtick");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Set TSC sync policy to Enhanced", "Timestamp counter sync tuning", delegate
			{
				Run("bcdedit", "/set tscsyncpolicy Enhanced");
			}, delegate
			{
				Run("bcdedit", "/deletevalue tscsyncpolicy");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Enable Ultimate Performance power plan", "Unlocks the hidden high-performance plan", delegate
			{
				Run("powercfg", "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
			}, null, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Disable USB selective suspend", "Keeps USB devices from powering down", delegate
			{
				Run("powercfg", "/SETACVALUEINDEX SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0");
				Run("powercfg", "/SETACTIVE SCHEME_CURRENT");
			}, delegate
			{
				Run("powercfg", "/SETACVALUEINDEX SCHEME_CURRENT 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 1");
				Run("powercfg", "/SETACTIVE SCHEME_CURRENT");
			}, null, on: false);
		}
		Panel pageByIndex3 = GetPageByIndex(2);
		if (pageByIndex3 != null)
		{
			int y3 = NextY(pageByIndex3);
			y3 = Head(pageByIndex3, y3, "More: Graphics performance");
			y3 = Twk(pageByIndex3, y3, "Enable Hardware-Accelerated GPU Scheduling", "Lets the GPU manage its own memory (needs restart)", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "HwSchMode", 2, null);
			}, delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "HwSchMode", 1, null);
			}, null, on: false);
			y3 = Twk(pageByIndex3, y3, "Disable Game DVR / background recording", "Frees GPU/CPU used by Xbox capture", delegate
			{
				Reg("HKCU\\System\\GameConfigStore", "GameDVR_Enabled", 0, null);
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR", "AllowGameDVR", 0, null);
			}, delegate
			{
				Reg("HKCU\\System\\GameConfigStore", "GameDVR_Enabled", 1, null);
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\GameDVR", "AllowGameDVR");
			}, null, on: false);
			y3 = Twk(pageByIndex3, y3, "Disable fullscreen optimizations (global)", "Can reduce input lag in games", delegate
			{
				Reg("HKCU\\System\\GameConfigStore", "GameDVR_FSEBehaviorMode", 2, null);
			}, delegate
			{
				Reg("HKCU\\System\\GameConfigStore", "GameDVR_FSEBehaviorMode", 0, null);
			}, null, on: false);
			y3 = Twk(pageByIndex3, y3, "Increase GPU TDR delay to 10s", "Fewer driver timeout crashes under load", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "TdrDelay", 10, null);
			}, delegate
			{
				RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "TdrDelay");
			}, null, on: false);
			y3 = Twk(pageByIndex3, y3, "Disable Multiplane Overlay (MPO)", "Fixes flicker/stutter on some GPUs", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\Dwm", "OverlayTestMode", 5, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Microsoft\\Windows\\Dwm", "OverlayTestMode");
			}, null, on: false);
			y3 = Twk(pageByIndex3, y3, "Disable Miracast GPU support", "Turns off wireless display GPU support when unused", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "PlatformSupportMiracast", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers", "PlatformSupportMiracast");
			}, null, on: false);
		}
		Panel pageByIndex4 = GetPageByIndex(3);
		if (pageByIndex4 != null)
		{
			int y4 = NextY(pageByIndex4);
			y4 = Head(pageByIndex4, y4, "More: Memory tuning");
			y4 = Twk(pageByIndex4, y4, "Keep kernel in RAM (DisablePagingExecutive)", "Prevents paging of kernel code", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingExecutive", 1, null);
			}, delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "DisablePagingExecutive", 0, null);
			}, null, on: false);
			y4 = Twk(pageByIndex4, y4, "Disable clearing pagefile at shutdown", "Faster shutdowns", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "ClearPageFileAtShutdown", 0, null);
			}, delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "ClearPageFileAtShutdown", 1, null);
			}, null, on: false);
			y4 = Twk(pageByIndex4, y4, "Disable Superfetch/SysMain service", "Reduces disk churn (best on SSDs)", delegate
			{
				SvcDis("SysMain");
			}, delegate
			{
				SvcAuto("SysMain");
			}, "May slow app launches on HDDs.", on: false);
			y4 = Twk(pageByIndex4, y4, "Enable large system cache", "Larger file cache for lots of RAM", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "LargeSystemCache", 1, null);
			}, delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management", "LargeSystemCache", 0, null);
			}, null, on: false);
		}
		Panel pageByIndex5 = GetPageByIndex(4);
		if (pageByIndex5 != null)
		{
			int y5 = NextY(pageByIndex5);
			y5 = Head(pageByIndex5, y5, "More: Remove built-in apps");
			y5 = Twk(pageByIndex5, y5, "Remove Clipchamp", "Uninstalls the Clipchamp app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.Clipchamp* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.Clipchamp* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Bing News", "Uninstalls the Bing News app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.BingNews* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.BingNews* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Bing Weather", "Uninstalls the Bing Weather app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.BingWeather* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.BingWeather* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Get Help", "Uninstalls the Get Help app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.GetHelp* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.GetHelp* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Get Started / Tips", "Uninstalls the Get Started / Tips app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.Getstarted* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.Getstarted* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Media Player (Zune Music)", "Uninstalls the Media Player (Zune Music) app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.ZuneMusic* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.ZuneMusic* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Movies & TV (Zune Video)", "Uninstalls the Movies & TV (Zune Video) app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.ZuneVideo* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.ZuneVideo* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Solitaire Collection", "Uninstalls the Solitaire Collection app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.MicrosoftSolitaireCollection* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.MicrosoftSolitaireCollection* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove People", "Uninstalls the People app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.People* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.People* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Power Automate", "Uninstalls the Power Automate app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.PowerAutomateDesktop* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.PowerAutomateDesktop* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove To Do", "Uninstalls the To Do app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.Todos* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.Todos* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Feedback Hub", "Uninstalls the Feedback Hub app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.WindowsFeedbackHub* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.WindowsFeedbackHub* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Maps", "Uninstalls the Maps app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.WindowsMaps* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.WindowsMaps* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Mail & Calendar", "Uninstalls the Mail & Calendar app", delegate
			{
				RunPS("Get-AppxPackage *microsoft.windowscommunicationsapps* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *microsoft.windowscommunicationsapps* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Mixed Reality Portal", "Uninstalls the Mixed Reality Portal app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.MixedReality.Portal* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.MixedReality.Portal* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove OneNote (Store)", "Uninstalls the OneNote (Store) app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.Office.OneNote* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.Office.OneNote* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Skype", "Uninstalls the Skype app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.SkypeApp* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.SkypeApp* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Wallet", "Uninstalls the Wallet app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.Wallet* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.Wallet* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove 3D Viewer", "Uninstalls the 3D Viewer app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.Microsoft3DViewer* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.Microsoft3DViewer* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Print 3D", "Uninstalls the Print 3D app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.Print3D* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.Print3D* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Phone Link", "Uninstalls the Phone Link app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.YourPhone* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.YourPhone* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Quick Assist", "Uninstalls the Quick Assist app", delegate
			{
				RunPS("Get-AppxPackage *MicrosoftCorporationII.QuickAssist* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *MicrosoftCorporationII.QuickAssist* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Family", "Uninstalls the Family app", delegate
			{
				RunPS("Get-AppxPackage *MicrosoftCorporationII.MicrosoftFamily* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *MicrosoftCorporationII.MicrosoftFamily* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Teams (consumer)", "Uninstalls the Teams (consumer) app", delegate
			{
				RunPS("Get-AppxPackage *MicrosoftTeams* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *MicrosoftTeams* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Xbox Game Bar", "Uninstalls the Xbox Game Bar app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.XboxGamingOverlay* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.XboxGamingOverlay* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Xbox Speech To Text", "Uninstalls the Xbox Speech To Text app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.XboxSpeechToTextOverlay* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.XboxSpeechToTextOverlay* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Xbox TCUI", "Uninstalls the Xbox TCUI app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.Xbox.TCUI* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.Xbox.TCUI* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Xbox Identity Provider", "Uninstalls the Xbox Identity Provider app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.XboxIdentityProvider* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.XboxIdentityProvider* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Xbox Gaming App", "Uninstalls the Xbox Gaming App app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.GamingApp* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.GamingApp* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Dev Home", "Uninstalls the Dev Home app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.Windows.DevHome* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.Windows.DevHome* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Cortana", "Uninstalls the Cortana app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.549981C3F5F10* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.549981C3F5F10* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Sticky Notes", "Uninstalls the Sticky Notes app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.MicrosoftStickyNotes* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.MicrosoftStickyNotes* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Outlook (new)", "Uninstalls the Outlook (new) app", delegate
			{
				RunPS("Get-AppxPackage *Microsoft.OutlookForWindows* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *Microsoft.OutlookForWindows* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
			y5 = Twk(pageByIndex5, y5, "Remove Windows Web Experience (Widgets)", "Uninstalls the Windows Web Experience (Widgets) app", delegate
			{
				RunPS("Get-AppxPackage *MicrosoftWindows.Client.WebExperience* | Remove-AppxPackage -EA 0");
			}, delegate
			{
				RunPS("Get-AppxPackage -AllUsers *MicrosoftWindows.Client.WebExperience* | ForEach {Add-AppxPackage -DisableDevelopmentMode -Register \"$($_.InstallLocation)\\AppXManifest.xml\" -EA 0}");
			}, "Reinstall from Store if needed.", on: false);
		}
		Panel pageByIndex6 = GetPageByIndex(5);
		if (pageByIndex6 != null)
		{
			int y6 = NextY(pageByIndex6);
			y6 = Head(pageByIndex6, y6, "More: Network tuning");
			y6 = Twk(pageByIndex6, y6, "Disable network throttling", "Removes the 10-packet multimedia throttle", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex", uint.MaxValue, null);
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "NetworkThrottlingIndex", 10, null);
			}, null, on: false);
			y6 = Twk(pageByIndex6, y6, "Maximize system responsiveness", "Lets foreground apps use more bandwidth", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "SystemResponsiveness", 0, null);
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile", "SystemResponsiveness", 20, null);
			}, null, on: false);
			y6 = Twk(pageByIndex6, y6, "Disable Update delivery over P2P", "Stops sharing updates with other PCs", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Config", "DODownloadMode", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\DeliveryOptimization\\Config", "DODownloadMode");
			}, null, on: false);
			y6 = Twk(pageByIndex6, y6, "Enable TCP CTCP congestion provider", "Better throughput on fast links", delegate
			{
				Run("netsh", "int tcp set supplemental Internet congestionprovider=ctcp");
			}, delegate
			{
				Run("netsh", "int tcp set supplemental Internet congestionprovider=default");
			}, null, on: false);
			y6 = Twk(pageByIndex6, y6, "Disable TCP heuristics", "Prevents auto-tuning from lowering the receive window", delegate
			{
				Run("netsh", "int tcp set heuristics disabled");
			}, delegate
			{
				Run("netsh", "int tcp set heuristics default");
			}, null, on: false);
			y6 = Twk(pageByIndex6, y6, "Enable Receive Side Scaling (RSS)", "Spreads network load across CPU cores", delegate
			{
				Run("netsh", "int tcp set global rss=enabled");
			}, delegate
			{
				Run("netsh", "int tcp set global rss=default");
			}, null, on: false);
			y6 = Twk(pageByIndex6, y6, "Disable ECN capability", "Compatibility with some routers", delegate
			{
				Run("netsh", "int tcp set global ecncapability=disabled");
			}, delegate
			{
				Run("netsh", "int tcp set global ecncapability=default");
			}, null, on: false);
			y6 = Twk(pageByIndex6, y6, "Set TCP autotuning to normal", "Restores healthy receive-window scaling", delegate
			{
				Run("netsh", "int tcp set global autotuninglevel=normal");
			}, delegate
			{
				Run("netsh", "int tcp set global autotuninglevel=default");
			}, null, on: false);
			y6 = Twk(pageByIndex6, y6, "Disable LLMNR", "Reduces multicast name-resolution chatter", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\DNSClient", "EnableMulticast", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\DNSClient", "EnableMulticast");
			}, null, on: false);
			y6 = Twk(pageByIndex6, y6, "Set QoS reservable bandwidth policy to 0%", "Usually only affects apps that explicitly request QoS bandwidth", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Psched", "NonBestEffortLimit", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Psched", "NonBestEffortLimit");
			}, null, on: false);
			y6 = Twk(pageByIndex6, y6, "Flush DNS resolver cache", "Clears stale DNS entries now", delegate
			{
				Run("ipconfig", "/flushdns");
			}, null, null, on: false);
			y6 = Twk(pageByIndex6, y6, "Set DNS to Cloudflare (1.1.1.1)", "Fast privacy-focused DNS", delegate
			{
				Run("netsh", "interface ip set dns \"Ethernet\" static 1.1.1.1");
				Run("netsh", "interface ip add dns \"Ethernet\" 1.0.0.1 index=2");
			}, delegate
			{
				Run("netsh", "interface ipv4 set dnsservers name=\"Ethernet\" source=dhcp");
			}, "Adapter must be named Ethernet.", on: false);
		}
		Panel pageByIndex7 = GetPageByIndex(6);
		if (pageByIndex7 != null)
		{
			int y7 = NextY(pageByIndex7);
			y7 = Head(pageByIndex7, y7, "More: Gaming tweaks");
			y7 = Twk(pageByIndex7, y7, "Enable Windows Game Mode", "Prioritizes the active game", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\GameBar", "AllowAutoGameMode", 1, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\GameBar", "AutoGameModeEnabled", 1, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\GameBar", "AutoGameModeEnabled", 0, null);
			}, null, on: false);
			y7 = Twk(pageByIndex7, y7, "Disable Game Bar tips / popups", "Stops Game Bar prompts", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\GameBar", "ShowStartupPanel", 0, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\GameBar", "UseNexusForGameBarEnabled", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\GameBar", "ShowStartupPanel", 1, null);
			}, null, on: false);
			y7 = Twk(pageByIndex7, y7, "Set GPU priority high for games", "Raises GPU scheduling priority for the Games task", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "GPU Priority", 8, null);
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "Priority", 6, null);
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "GPU Priority", 8, null);
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "Priority", 2, null);
			}, null, on: false);
			y7 = Twk(pageByIndex7, y7, "Set Games scheduling category to High", "Multimedia scheduler favors games", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "Scheduling Category", "High", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Multimedia\\SystemProfile\\Tasks\\Games", "Scheduling Category", "Medium", "");
			}, null, on: false);
			y7 = Twk(pageByIndex7, y7, "Disable mouse acceleration", "Raw 1:1 mouse movement for aiming", delegate
			{
				Reg("HKCU\\Control Panel\\Mouse", "MouseSpeed", "0", "");
				Reg("HKCU\\Control Panel\\Mouse", "MouseThreshold1", "0", "");
				Reg("HKCU\\Control Panel\\Mouse", "MouseThreshold2", "0", "");
			}, delegate
			{
				Reg("HKCU\\Control Panel\\Mouse", "MouseSpeed", "1", "");
				Reg("HKCU\\Control Panel\\Mouse", "MouseThreshold1", "6", "");
				Reg("HKCU\\Control Panel\\Mouse", "MouseThreshold2", "10", "");
			}, null, on: false);
		}
		Panel pageByIndex8 = GetPageByIndex(7);
		if (pageByIndex8 != null)
		{
			int y8 = NextY(pageByIndex8);
			y8 = Head(pageByIndex8, y8, "More: Privacy hardening");
			y8 = Twk(pageByIndex8, y8, "Disable Advertising ID", "Stops apps using your ad ID", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo", "Enabled", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\AdvertisingInfo", "Enabled", 1, null);
			}, null, on: false);
			y8 = Twk(pageByIndex8, y8, "Disable tailored experiences", "No personalized tips using diagnostic data", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 1, null);
			}, null, on: false);
			y8 = Twk(pageByIndex8, y8, "Disable Start menu suggestions", "No suggested apps in Start", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338388Enabled", 1, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 1, null);
			}, null, on: false);
			y8 = Twk(pageByIndex8, y8, "Disable lock screen tips/ads", "Turns off Spotlight suggestions on lock screen", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338387Enabled", 0, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "RotatingLockScreenOverlayEnabled", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338387Enabled", 1, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "RotatingLockScreenOverlayEnabled", 1, null);
			}, null, on: false);
			y8 = Twk(pageByIndex8, y8, "Disable 'suggested content' in Settings", "No ads inside the Settings app", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338393Enabled", 0, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-353694Enabled", 0, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-353696Enabled", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338393Enabled", 1, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-353694Enabled", 1, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-353696Enabled", 1, null);
			}, null, on: false);
			y8 = Twk(pageByIndex8, y8, "Disable auto-install of suggested apps", "Stops silent app installs", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SilentInstalledAppsEnabled", 0, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "PreInstalledAppsEnabled", 0, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "OemPreInstalledAppsEnabled", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SilentInstalledAppsEnabled", 1, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "PreInstalledAppsEnabled", 1, null);
			}, null, on: false);
			y8 = Twk(pageByIndex8, y8, "Disable 'Get even more out of Windows'", "No setup nag after updates", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-310093Enabled", 0, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\UserProfileEngagement", "ScoobeSystemSettingEnabled", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-310093Enabled", 1, null);
			}, null, on: false);
			y8 = Twk(pageByIndex8, y8, "Disable activity feed / timeline upload", "Stops publishing activity history", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableActivityFeed", 0, null);
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "PublishUserActivities", 0, null);
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "UploadUserActivities", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "EnableActivityFeed");
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "PublishUserActivities");
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "UploadUserActivities");
			}, null, on: false);
			y8 = Twk(pageByIndex8, y8, "Set telemetry to Security (minimum)", "Lowest allowed diagnostic data", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "AllowTelemetry", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection", "AllowTelemetry");
			}, null, on: false);
			y8 = Twk(pageByIndex8, y8, "Disable feedback requests", "Windows stops asking for feedback", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Siuf\\Rules", "NumberOfSIUFInPeriod", 0, null);
			}, delegate
			{
				RegDel("HKCU\\SOFTWARE\\Microsoft\\Siuf\\Rules", "NumberOfSIUFInPeriod");
			}, null, on: false);
			y8 = Twk(pageByIndex8, y8, "Disable typing/inking data collection", "Stops sending typing info to Microsoft", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Input\\TIPC", "Enabled", 0, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\InputPersonalization", "RestrictImplicitInkCollection", 1, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\InputPersonalization", "RestrictImplicitTextCollection", 1, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Input\\TIPC", "Enabled", 1, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\InputPersonalization", "RestrictImplicitInkCollection", 0, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\InputPersonalization", "RestrictImplicitTextCollection", 0, null);
			}, null, on: false);
			y8 = Twk(pageByIndex8, y8, "Disable web/Bing results in Search", "Local-only Start search", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search", "BingSearchEnabled", 0, null);
				Reg("HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer", "DisableSearchBoxSuggestions", 1, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search", "BingSearchEnabled", 1, null);
				RegDel("HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer", "DisableSearchBoxSuggestions");
			}, null, on: false);
			y8 = Twk(pageByIndex8, y8, "Disable cloud search history", "No AAD/MSA cloud search content", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\SearchSettings", "IsAADCloudSearchEnabled", 0, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\SearchSettings", "IsMSACloudSearchEnabled", 0, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\SearchSettings", "IsDeviceSearchHistoryEnabled", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\SearchSettings", "IsDeviceSearchHistoryEnabled", 1, null);
			}, null, on: false);
			y8 = Twk(pageByIndex8, y8, "Disable Windows Copilot", "Removes the Copilot button/feature", delegate
			{
				Reg("HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsCopilot", "TurnOffWindowsCopilot", 1, null);
			}, delegate
			{
				RegDel("HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\WindowsCopilot", "TurnOffWindowsCopilot");
			}, null, on: false);
			y8 = Twk(pageByIndex8, y8, "Disable location access (system)", "Denies location to apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\location", "Value", "Deny", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\location", "Value", "Allow", "");
			}, null, on: false);
			y8 = Twk(pageByIndex8, y8, "Disable app launch tracking", "Stops 'most used' tracking for privacy", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_TrackProgs", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Start_TrackProgs", 1, null);
			}, null, on: false);
		}
		Panel pageByIndex9 = GetPageByIndex(11);
		if (pageByIndex9 != null)
		{
			int y9 = NextY(pageByIndex9);
			y9 = Head(pageByIndex9, y9, "More: System reliability");
			y9 = Twk(pageByIndex9, y9, "Disable Fast Startup", "Cleaner full shutdowns (helps dual-boot)", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power", "HiberbootEnabled", 0, null);
			}, delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power", "HiberbootEnabled", 1, null);
			}, null, on: false);
			y9 = Twk(pageByIndex9, y9, "Disable automatic restart on BSOD", "Lets you read crash info", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\CrashControl", "AutoReboot", 0, null);
			}, delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\CrashControl", "AutoReboot", 1, null);
			}, null, on: false);
			y9 = Twk(pageByIndex9, y9, "Disable Windows Error Reporting", "Stops WER uploads/prompts", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting", "Disabled", 1, null);
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting", "Disabled", 0, null);
			}, null, on: false);
		}
		Panel pageByIndex10 = GetPageByIndex(14);
		if (pageByIndex10 != null)
		{
			int y10 = NextY(pageByIndex10);
			y10 = Head(pageByIndex10, y10, "More: Advanced system");
			y10 = Twk(pageByIndex10, y10, "Disable startup sound", "Silences the Windows boot chime", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Authentication\\LogonUI\\BootAnimation", "DisableStartupSound", 1, null);
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\EditionOverrides", "UserSetting_DisableStartupSound", 1, null);
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Authentication\\LogonUI\\BootAnimation", "DisableStartupSound", 0, null);
			}, null, on: false);
			y10 = Twk(pageByIndex10, y10, "Disable Prefetch (SSD)", "Removes prefetch overhead on SSDs", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnablePrefetcher", 0, null);
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnableSuperfetch", 0, null);
			}, delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnablePrefetcher", 3, null);
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters", "EnableSuperfetch", 3, null);
			}, "Leave on for HDDs.", on: false);
			y10 = Twk(pageByIndex10, y10, "Increase icon cache size", "Fewer icon glitches with many files", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "Max Cached Icons", "4096", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "Max Cached Icons", "512", "");
			}, null, on: false);
			y10 = Twk(pageByIndex10, y10, "Disable Search indexing service", "Saves disk/CPU (search gets slower)", delegate
			{
				SvcDis("WSearch");
			}, delegate
			{
				SvcAuto("WSearch");
			}, "Search results will be slower.", on: false);
			y10 = Twk(pageByIndex10, y10, "Set NTFS memory usage to high", "More cache for busy filesystems", delegate
			{
				Run("fsutil", "behavior set memoryusage 2");
			}, delegate
			{
				Run("fsutil", "behavior set memoryusage 1");
			}, null, on: false);
			y10 = Twk(pageByIndex10, y10, "Disable Remote Assistance", "Closes a remote-help attack surface", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Remote Assistance", "fAllowToGetHelp", 0, null);
			}, delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Control\\Remote Assistance", "fAllowToGetHelp", 1, null);
			}, null, on: false);
		}
		Panel pageByIndex11 = GetPageByIndex(15);
		if (pageByIndex11 != null)
		{
			int y11 = NextY(pageByIndex11);
			y11 = Head(pageByIndex11, y11, "More: Windows UI & Explorer");
			y11 = Twk(pageByIndex11, y11, "Show file name extensions", "Always reveal extensions like .exe", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "HideFileExt", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "HideFileExt", 1, null);
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Show hidden files", "Reveals hidden files and folders", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Hidden", 1, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "Hidden", 2, null);
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Open File Explorer to 'This PC'", "Instead of Home/Quick Access", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "LaunchTo", 1, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "LaunchTo", 2, null);
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Align taskbar to the left", "Classic left-aligned taskbar", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarAl", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarAl", 1, null);
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Hide Task View button", "Removes Task View from taskbar", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowTaskViewButton", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowTaskViewButton", 1, null);
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Hide Widgets button", "Removes the Widgets icon", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarDa", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarDa", 1, null);
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Hide Chat (Teams) button", "Removes the Chat icon", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarMn", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "TaskbarMn", 1, null);
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Collapse taskbar Search to icon", "Smaller search on the taskbar", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search", "SearchboxTaskbarMode", 1, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Search", "SearchboxTaskbarMode", 2, null);
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Restore classic right-click menu", "Full context menu (no 'Show more')", delegate
			{
				Reg("HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\\InprocServer32", "", "", "");
			}, delegate
			{
				Run("reg", "delete HKCU\\Software\\Classes\\CLSID\\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2} /f");
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Show seconds in taskbar clock", "Adds seconds to the clock", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowSecondsInSystemClock", 1, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "ShowSecondsInSystemClock", 0, null);
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Enable 'End task' on taskbar right-click", "Kill hung apps from the taskbar", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\TaskbarDeveloperSettings", "TaskbarEndTask", 1, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced\\TaskbarDeveloperSettings", "TaskbarEndTask", 0, null);
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Disable Aero Shake minimize", "Stops minimize-on-shake", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DisallowShaking", 1, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", "DisallowShaking", 0, null);
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Zero menu show delay", "Snappier menus", delegate
			{
				Reg("HKCU\\Control Panel\\Desktop", "MenuShowDelay", "0", "");
			}, delegate
			{
				Reg("HKCU\\Control Panel\\Desktop", "MenuShowDelay", "400", "");
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Faster app close timeouts", "Shorter hang timeouts on shutdown", delegate
			{
				Reg("HKCU\\Control Panel\\Desktop", "HungAppTimeout", "1000", "");
				Reg("HKCU\\Control Panel\\Desktop", "WaitToKillAppTimeout", "2000", "");
				Reg("HKCU\\Control Panel\\Desktop", "AutoEndTasks", "1", "");
			}, delegate
			{
				Reg("HKCU\\Control Panel\\Desktop", "HungAppTimeout", "5000", "");
				Reg("HKCU\\Control Panel\\Desktop", "WaitToKillAppTimeout", "20000", "");
				Reg("HKCU\\Control Panel\\Desktop", "AutoEndTasks", "0", "");
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Show full path in title bar", "File Explorer shows full folder path", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\CabinetState", "FullPath", 1, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\CabinetState", "FullPath", 0, null);
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Disable Quick Access recent files", "No recent files in Explorer Home", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "ShowRecent", 0, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "ShowFrequent", 0, null);
			}, delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "ShowRecent", 1, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer", "ShowFrequent", 1, null);
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Disable lock screen", "Boot straight to sign-in", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Personalization", "NoLockScreen", 1, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Personalization", "NoLockScreen");
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Reduce startup app delay", "Apps launch immediately at login", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize", "StartupDelayInMSec", 0, null);
			}, delegate
			{
				RegDel("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Serialize", "StartupDelayInMSec");
			}, null, on: false);
			y11 = Twk(pageByIndex11, y11, "Hide 'Recently added' in Start", "Cleaner Start menu", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer", "HideRecentlyAddedApps", 1, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer", "HideRecentlyAddedApps");
			}, null, on: false);
		}
	}

	private void BuildMegaExtras()
	{
		Panel pageByIndex = GetPageByIndex(7);
		if (pageByIndex != null)
		{
			int y = NextY(pageByIndex);
			y = Head(pageByIndex, y, "More: App permission controls");
			y = Twk(pageByIndex, y, "Block app access: Microphone", "Denies microphone access to Windows apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone", "Value", "Deny", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\microphone", "Value", "Allow", "");
			}, "Apps using this permission may stop working.", on: false);
			y = Twk(pageByIndex, y, "Block app access: Camera", "Denies camera access to Windows apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\webcam", "Value", "Deny", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\webcam", "Value", "Allow", "");
			}, "Apps using this permission may stop working.", on: false);
			y = Twk(pageByIndex, y, "Block app access: Contacts", "Denies contacts access to Windows apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\contacts", "Value", "Deny", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\contacts", "Value", "Allow", "");
			}, "Apps using this permission may stop working.", on: false);
			y = Twk(pageByIndex, y, "Block app access: Calendar", "Denies calendar access to Windows apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\appointments", "Value", "Deny", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\appointments", "Value", "Allow", "");
			}, "Apps using this permission may stop working.", on: false);
			y = Twk(pageByIndex, y, "Block app access: Phone calls", "Denies phone calls access to Windows apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\phoneCall", "Value", "Deny", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\phoneCall", "Value", "Allow", "");
			}, "Apps using this permission may stop working.", on: false);
			y = Twk(pageByIndex, y, "Block app access: Email", "Denies email access to Windows apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\email", "Value", "Deny", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\email", "Value", "Allow", "");
			}, "Apps using this permission may stop working.", on: false);
			y = Twk(pageByIndex, y, "Block app access: Account information", "Denies account information access to Windows apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\userAccountInformation", "Value", "Deny", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\userAccountInformation", "Value", "Allow", "");
			}, "Apps using this permission may stop working.", on: false);
			y = Twk(pageByIndex, y, "Block app access: Device radios", "Denies device radios access to Windows apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\radios", "Value", "Deny", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\radios", "Value", "Allow", "");
			}, "Apps using this permission may stop working.", on: false);
			y = Twk(pageByIndex, y, "Block app access: Bluetooth sync", "Denies bluetooth sync access to Windows apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\bluetoothSync", "Value", "Deny", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\bluetoothSync", "Value", "Allow", "");
			}, "Apps using this permission may stop working.", on: false);
			y = Twk(pageByIndex, y, "Block app access: Broad file-system access", "Denies broad file-system access access to Windows apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\broadFileSystemAccess", "Value", "Deny", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\broadFileSystemAccess", "Value", "Allow", "");
			}, "Apps using this permission may stop working.", on: false);
			y = Twk(pageByIndex, y, "Block app access: App diagnostics", "Denies app diagnostics access to Windows apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\appDiagnostics", "Value", "Deny", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\appDiagnostics", "Value", "Allow", "");
			}, "Apps using this permission may stop working.", on: false);
			y = Twk(pageByIndex, y, "Block app access: Documents library", "Denies documents library access to Windows apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\documentsLibrary", "Value", "Deny", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\documentsLibrary", "Value", "Allow", "");
			}, "Apps using this permission may stop working.", on: false);
			y = Twk(pageByIndex, y, "Block app access: Pictures library", "Denies pictures library access to Windows apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\picturesLibrary", "Value", "Deny", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\picturesLibrary", "Value", "Allow", "");
			}, "Apps using this permission may stop working.", on: false);
			y = Twk(pageByIndex, y, "Block app access: Videos library", "Denies videos library access to Windows apps", delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\videosLibrary", "Value", "Deny", "");
			}, delegate
			{
				Reg("HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\CapabilityAccessManager\\ConsentStore\\videosLibrary", "Value", "Allow", "");
			}, "Apps using this permission may stop working.", on: false);
		}
		Panel pageByIndex2 = GetPageByIndex(8);
		if (pageByIndex2 != null)
		{
			int y2 = NextY(pageByIndex2);
			y2 = Head(pageByIndex2, y2, "More: Edge controls");
			y2 = Twk(pageByIndex2, y2, "Disable Edge sidebar", "Removes the sidebar and Discover button", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "HubsSidebarEnabled", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "HubsSidebarEnabled");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Disable Edge shopping assistant", "Stops shopping coupons and price comparison", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "EdgeShoppingAssistantEnabled", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "EdgeShoppingAssistantEnabled");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Disable Edge personalization reporting", "No browsing personalization reports", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "PersonalizationReportingEnabled", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "PersonalizationReportingEnabled");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Disable Edge recommendations", "Removes Microsoft recommendations", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "ShowRecommendationsEnabled", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "ShowRecommendationsEnabled");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Disable Edge spotlight experiences", "No feature spotlight promotions", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "SpotlightExperiencesAndRecommendationsEnabled", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "SpotlightExperiencesAndRecommendationsEnabled");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Disable Edge new-tab feed", "Uses a clean new tab page", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "NewTabPageContentEnabled", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "NewTabPageContentEnabled");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Disable Edge search suggestions", "No typed-query suggestions sent to provider", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "SearchSuggestEnabled", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "SearchSuggestEnabled");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Disable Edge feedback prompts", "Stops feedback surveys", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "UserFeedbackAllowed", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "UserFeedbackAllowed");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Set Edge diagnostic data to minimum", "Reduces browser diagnostic reporting", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "DiagnosticData", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "DiagnosticData");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Disable Edge browser sign-in", "Prevents Microsoft account sign-in to Edge", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "BrowserSignin", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "BrowserSignin");
			}, null, on: false);
		}
		Panel pageByIndex3 = GetPageByIndex(9);
		if (pageByIndex3 != null)
		{
			int y3 = NextY(pageByIndex3);
			y3 = Head(pageByIndex3, y3, "More: Storage maintenance");
			y3 = Twk(pageByIndex3, y3, "Run DISM component cleanup", "Removes superseded Windows component versions", delegate
			{
				Run("dism", "/Online /Cleanup-Image /StartComponentCleanup");
			}, null, null, on: false);
			y3 = Twk(pageByIndex3, y3, "Optimize all fixed volumes", "Runs the correct TRIM/defrag operation per drive", delegate
			{
				RunPS("Get-Volume | Where DriveType -eq Fixed | Optimize-Volume -Verbose -EA 0");
			}, null, null, on: false);
			y3 = Twk(pageByIndex3, y3, "Clear thumbnail cache", "Rebuilds Explorer thumbnail databases", delegate
			{
				RunPS("Stop-Process -Name explorer -Force -EA 0; Remove-Item $env:LOCALAPPDATA\\Microsoft\\Windows\\Explorer\\thumbcache* -Force -EA 0; Start-Process explorer");
			}, null, null, on: false);
			y3 = Twk(pageByIndex3, y3, "Clear Delivery Optimization cache", "Removes cached Windows update delivery files", delegate
			{
				RunPS("Delete-DeliveryOptimizationCache -Force -EA 0");
			}, null, null, on: false);
			y3 = Twk(pageByIndex3, y3, "Empty Recycle Bin", "Frees space across all drives", delegate
			{
				RunPS("Clear-RecycleBin -Force -EA 0");
			}, null, "Permanently deletes Recycle Bin contents.", on: false);
			y3 = Twk(pageByIndex3, y3, "Compact Windows component store", "Analyzes component-store cleanup potential", delegate
			{
				Run("dism", "/Online /Cleanup-Image /AnalyzeComponentStore");
			}, null, null, on: false);
		}
		Panel pageByIndex4 = GetPageByIndex(14);
		if (pageByIndex4 != null)
		{
			int y4 = NextY(pageByIndex4);
			y4 = Head(pageByIndex4, y4, "More: Optional Windows services");
			y4 = Twk(pageByIndex4, y4, "Disable optional service: AJRouter", "AllJoyn device routing", delegate
			{
				Run("sc", "config AJRouter start= disabled");
				Run("sc", "stop AJRouter");
			}, delegate
			{
				Run("sc", "config AJRouter start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: AppVClient", "Microsoft App-V virtualization", delegate
			{
				Run("sc", "config AppVClient start= disabled");
				Run("sc", "stop AppVClient");
			}, delegate
			{
				Run("sc", "config AppVClient start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: AssignedAccessManagerSvc", "Assigned Access / kiosk mode", delegate
			{
				Run("sc", "config AssignedAccessManagerSvc start= disabled");
				Run("sc", "stop AssignedAccessManagerSvc");
			}, delegate
			{
				Run("sc", "config AssignedAccessManagerSvc start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: diagnosticshub.standardcollector.service", "Visual Studio diagnostic collector", delegate
			{
				Run("sc", "config diagnosticshub.standardcollector.service start= disabled");
				Run("sc", "stop diagnosticshub.standardcollector.service");
			}, delegate
			{
				Run("sc", "config diagnosticshub.standardcollector.service start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: icssvc", "Mobile hotspot", delegate
			{
				Run("sc", "config icssvc start= disabled");
				Run("sc", "stop icssvc");
			}, delegate
			{
				Run("sc", "config icssvc start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: irmon", "Infrared monitor", delegate
			{
				Run("sc", "config irmon start= disabled");
				Run("sc", "stop irmon");
			}, delegate
			{
				Run("sc", "config irmon start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: perceptionsimulation", "Mixed-reality simulation", delegate
			{
				Run("sc", "config perceptionsimulation start= disabled");
				Run("sc", "stop perceptionsimulation");
			}, delegate
			{
				Run("sc", "config perceptionsimulation start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: PhoneSvc", "Phone integration", delegate
			{
				Run("sc", "config PhoneSvc start= disabled");
				Run("sc", "stop PhoneSvc");
			}, delegate
			{
				Run("sc", "config PhoneSvc start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: PrintNotify", "Printer notifications", delegate
			{
				Run("sc", "config PrintNotify start= disabled");
				Run("sc", "stop PrintNotify");
			}, delegate
			{
				Run("sc", "config PrintNotify start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: RemoteAccess", "Routing and remote access", delegate
			{
				Run("sc", "config RemoteAccess start= disabled");
				Run("sc", "stop RemoteAccess");
			}, delegate
			{
				Run("sc", "config RemoteAccess start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: RemoteRegistry", "Remote registry editing", delegate
			{
				Run("sc", "config RemoteRegistry start= disabled");
				Run("sc", "stop RemoteRegistry");
			}, delegate
			{
				Run("sc", "config RemoteRegistry start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: RetailDemo", "Retail demo mode", delegate
			{
				Run("sc", "config RetailDemo start= disabled");
				Run("sc", "stop RetailDemo");
			}, delegate
			{
				Run("sc", "config RetailDemo start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: ScDeviceEnum", "Smart-card device enumeration", delegate
			{
				Run("sc", "config ScDeviceEnum start= disabled");
				Run("sc", "stop ScDeviceEnum");
			}, delegate
			{
				Run("sc", "config ScDeviceEnum start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: SCardSvr", "Smart-card support", delegate
			{
				Run("sc", "config SCardSvr start= disabled");
				Run("sc", "stop SCardSvr");
			}, delegate
			{
				Run("sc", "config SCardSvr start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: SEMgrSvc", "Payments and NFC", delegate
			{
				Run("sc", "config SEMgrSvc start= disabled");
				Run("sc", "stop SEMgrSvc");
			}, delegate
			{
				Run("sc", "config SEMgrSvc start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: SensorDataService", "Sensor data delivery", delegate
			{
				Run("sc", "config SensorDataService start= disabled");
				Run("sc", "stop SensorDataService");
			}, delegate
			{
				Run("sc", "config SensorDataService start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: SensorService", "Sensor management", delegate
			{
				Run("sc", "config SensorService start= disabled");
				Run("sc", "stop SensorService");
			}, delegate
			{
				Run("sc", "config SensorService start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: SensrSvc", "Sensor monitoring", delegate
			{
				Run("sc", "config SensrSvc start= disabled");
				Run("sc", "stop SensrSvc");
			}, delegate
			{
				Run("sc", "config SensrSvc start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: SmsRouter", "SMS routing", delegate
			{
				Run("sc", "config SmsRouter start= disabled");
				Run("sc", "stop SmsRouter");
			}, delegate
			{
				Run("sc", "config SmsRouter start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: TabletInputService", "Touch keyboard and handwriting", delegate
			{
				Run("sc", "config TabletInputService start= disabled");
				Run("sc", "stop TabletInputService");
			}, delegate
			{
				Run("sc", "config TabletInputService start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: WbioSrvc", "Windows biometric service", delegate
			{
				Run("sc", "config WbioSrvc start= disabled");
				Run("sc", "stop WbioSrvc");
			}, delegate
			{
				Run("sc", "config WbioSrvc start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: WMPNetworkSvc", "Media Player network sharing", delegate
			{
				Run("sc", "config WMPNetworkSvc start= disabled");
				Run("sc", "stop WMPNetworkSvc");
			}, delegate
			{
				Run("sc", "config WMPNetworkSvc start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: workfolderssvc", "Enterprise Work Folders", delegate
			{
				Run("sc", "config workfolderssvc start= disabled");
				Run("sc", "stop workfolderssvc");
			}, delegate
			{
				Run("sc", "config workfolderssvc start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: WwanSvc", "Mobile broadband", delegate
			{
				Run("sc", "config WwanSvc start= disabled");
				Run("sc", "stop WwanSvc");
			}, delegate
			{
				Run("sc", "config WwanSvc start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: Fax", "Fax service", delegate
			{
				Run("sc", "config Fax start= disabled");
				Run("sc", "stop Fax");
			}, delegate
			{
				Run("sc", "config Fax start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: WalletService", "Windows Wallet", delegate
			{
				Run("sc", "config WalletService start= disabled");
				Run("sc", "stop WalletService");
			}, delegate
			{
				Run("sc", "config WalletService start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: wisvc", "Windows Insider service", delegate
			{
				Run("sc", "config wisvc start= disabled");
				Run("sc", "stop wisvc");
			}, delegate
			{
				Run("sc", "config wisvc start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
			y4 = Twk(pageByIndex4, y4, "Disable optional service: MixedRealityOpenXRSvc", "Mixed Reality OpenXR", delegate
			{
				Run("sc", "config MixedRealityOpenXRSvc start= disabled");
				Run("sc", "stop MixedRealityOpenXRSvc");
			}, delegate
			{
				Run("sc", "config MixedRealityOpenXRSvc start= demand");
			}, "Only enable if you do not use this Windows feature.", on: false);
		}
		Panel pageByIndex5 = GetPageByIndex(16);
		if (pageByIndex5 != null)
		{
			int y5 = NextY(pageByIndex5);
			y5 = Head(pageByIndex5, y5, "More: App installs");
			y5 = Twk(pageByIndex5, y5, "Install GitHub Desktop", "Installs GitHub Desktop using winget", delegate
			{
				Winget("GitHub.GitHubDesktop");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install Docker Desktop", "Installs Docker Desktop using winget", delegate
			{
				Winget("Docker.DockerDesktop");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install WinSCP", "Installs WinSCP using winget", delegate
			{
				Winget("WinSCP.WinSCP");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install PuTTY", "Installs PuTTY using winget", delegate
			{
				Winget("PuTTY.PuTTY");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install FileZilla Client", "Installs FileZilla Client using winget", delegate
			{
				Winget("TimKosse.FileZilla.Client");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install ShareX", "Installs ShareX using winget", delegate
			{
				Winget("ShareX.ShareX");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install Greenshot", "Installs Greenshot using winget", delegate
			{
				Winget("Greenshot.Greenshot");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install Spotify", "Installs Spotify using winget", delegate
			{
				Winget("Spotify.Spotify");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install qBittorrent", "Installs qBittorrent using winget", delegate
			{
				Winget("qBittorrent.qBittorrent");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install KeePassXC", "Installs KeePassXC using winget", delegate
			{
				Winget("KeePassXCTeam.KeePassXC");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install Bitwarden", "Installs Bitwarden using winget", delegate
			{
				Winget("Bitwarden.Bitwarden");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install SumatraPDF", "Installs SumatraPDF using winget", delegate
			{
				Winget("SumatraPDF.SumatraPDF");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install LibreOffice", "Installs LibreOffice using winget", delegate
			{
				Winget("TheDocumentFoundation.LibreOffice");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install EarTrumpet", "Installs EarTrumpet using winget", delegate
			{
				Winget("File-New-Project.EarTrumpet");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install AutoHotkey v2", "Installs AutoHotkey v2 using winget", delegate
			{
				Winget("AutoHotkey.AutoHotkey");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install Sysinternals Suite", "Installs Sysinternals Suite using winget", delegate
			{
				Winget("Microsoft.Sysinternals.Suite");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install CrystalDiskMark", "Installs CrystalDiskMark using winget", delegate
			{
				Winget("CrystalDewWorld.CrystalDiskMark");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install GPU-Z", "Installs GPU-Z using winget", delegate
			{
				Winget("TechPowerUp.GPU-Z");
			}, null, null, on: false);
			y5 = Twk(pageByIndex5, y5, "Install NanaZip", "Installs NanaZip using winget", delegate
			{
				Winget("M2Team.NanaZip");
			}, null, null, on: false);
		}
	}

	private void BuildDeepExtras()
	{
		Panel pageByIndex = GetPageByIndex(5);
		if (pageByIndex != null)
		{
			int y = NextY(pageByIndex);
			y = Head(pageByIndex, y, "More: Deep network latency");
			y = Twk(pageByIndex, y, "TCP DefaultTTL = 64", "Sets a lower, standard packet time-to-live", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "DefaultTTL", 64, null);
			}, delegate
			{
				RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "DefaultTTL");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Increase ephemeral ports (MaxUserPort)", "Raises the number of outbound ports available", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "MaxUserPort", 65534, null);
			}, delegate
			{
				RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "MaxUserPort");
			}, null, on: false);
			y = Twk(pageByIndex, y, "Reduce TcpTimedWaitDelay", "Frees closed connections faster", delegate
			{
				Reg("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "TcpTimedWaitDelay", 30, null);
			}, delegate
			{
				RegDel("HKLM\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters", "TcpTimedWaitDelay");
			}, null, on: false);
		}
		Panel pageByIndex2 = GetPageByIndex(14);
		if (pageByIndex2 != null)
		{
			int y2 = NextY(pageByIndex2);
			y2 = Head(pageByIndex2, y2, "More: Deep responsiveness");
			y2 = Twk(pageByIndex2, y2, "Disable low disk space warning", "Stops the low-disk-space balloon/notification", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoLowDiskSpaceChecks", 1, null);
			}, delegate
			{
				RegDel("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "NoLowDiskSpaceChecks");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Lower input hook timeout (LowLevelHooksTimeout)", "Reduces delay before unresponsive hooks are skipped", delegate
			{
				Reg("HKCU\\Control Panel\\Desktop", "LowLevelHooksTimeout", 1000, "1000");
			}, delegate
			{
				RegDel("HKCU\\Control Panel\\Desktop", "LowLevelHooksTimeout");
			}, null, on: false);
			y2 = Twk(pageByIndex2, y2, "Faster shortcut resolution", "Stops Explorer from searching for moved shortcut targets", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "LinkResolveIgnoreLinkInfo", 1, null);
			}, delegate
			{
				RegDel("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\Explorer", "LinkResolveIgnoreLinkInfo");
			}, null, on: false);
		}
		Panel pageByIndex3 = GetPageByIndex(7);
		if (pageByIndex3 != null)
		{
			int y3 = NextY(pageByIndex3);
			y3 = Head(pageByIndex3, y3, "More: Deep privacy & UI");
			y3 = Twk(pageByIndex3, y3, "Disable Clipboard History", "Stops Windows from storing clipboard history", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "AllowClipboardHistory", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\System", "AllowClipboardHistory");
			}, null, on: false);
			y3 = Twk(pageByIndex3, y3, "Disable Windows Ink Workspace", "Removes the pen/ink workspace overlay", delegate
			{
				Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsInkWorkspace", "AllowWindowsInkWorkspace", 0, null);
			}, delegate
			{
				RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\WindowsInkWorkspace", "AllowWindowsInkWorkspace");
			}, null, on: false);
			y3 = Twk(pageByIndex3, y3, "Disable suggested content in Settings", "Removes ads/suggestions from the Settings app", delegate
			{
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338393Enabled", 0, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338394Enabled", 0, null);
				Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338396Enabled", 0, null);
			}, delegate
			{
				RegDel("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338393Enabled");
				RegDel("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338394Enabled");
				RegDel("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\ContentDeliveryManager", "SubscribedContent-338396Enabled");
			}, null, on: false);
		}
	}

	private void BuildTools()
	{
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Expected O, but got Unknown
		//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fa: Expected O, but got Unknown
		Panel pageByIndex = GetPageByIndex(17);
		int y = 5;
		y = Head(pageByIndex, y, "Safe maintenance");
		y = Twk(pageByIndex, y, "Enable Storage Sense", "Lets Windows automatically remove temporary files", delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy", "01", 1, null);
		}, delegate
		{
			Reg("HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy", "01", 0, null);
		}, null, on: true);
		y = Twk(pageByIndex, y, "Clear DirectX shader cache", "Frees shader-cache space; games rebuild it when needed", delegate
		{
			CleanDir(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"));
		}, null, "The first launch of a game may take longer.", on: true);
		y = Twk(pageByIndex, y, "Clear Windows Update download cache", "Removes already-downloaded update files", delegate
		{
			Run("net", "stop wuauserv");
			CleanDir("C:\\Windows\\SoftwareDistribution\\Download");
			Run("net", "start wuauserv");
		}, null, "Does not remove installed updates.", on: true);
		y = Head(pageByIndex, y, "Microsoft Edge");
		y = Twk(pageByIndex, y, "Disable Edge Startup Boost", "Prevents Edge from loading in the background at sign-in", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "StartupBoostEnabled", 0, null);
		}, delegate
		{
			RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "StartupBoostEnabled");
		}, null, on: true);
		y = Twk(pageByIndex, y, "Disable Edge background mode", "Closes Edge processes when the browser is closed", delegate
		{
			Reg("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "BackgroundModeEnabled", 0, null);
		}, delegate
		{
			RegDel("HKLM\\SOFTWARE\\Policies\\Microsoft\\Edge", "BackgroundModeEnabled");
		}, null, on: true);
		y = Head(pageByIndex, y, "Recovery shortcuts");
		y = Twk(pageByIndex, y, "Restore automatic DNS", "Returns the Ethernet adapter to DHCP-provided DNS", delegate
		{
			Run("netsh", "interface ipv4 set dnsservers name=\"Ethernet\" source=dhcp");
		}, null, "Adapter name must be Ethernet.", on: false);
		y = Twk(pageByIndex, y, "Open Startup Apps settings", "Review and disable startup apps from Windows Settings", delegate
		{
			Process.Start("cmd.exe", "/c start ms-settings:startupapps");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Open System Restore", "Create or use a Windows restore point", delegate
		{
			Process.Start("SystemPropertiesProtection.exe");
		}, null, null, on: false);
		y = Twk(pageByIndex, y, "Open Resource Monitor", "Inspect live CPU, memory, disk, and network activity", delegate
		{
			Process.Start("resmon.exe");
		}, null, null, on: false);
		y = Head(pageByIndex, y, "Quick status");
		Label info = new Label();
		((Control)info).Location = new Point(12, y + 4);
		((Control)info).Size = new Size(720, 46);
		((Control)info).ForeColor = TXT;
		((Control)info).Text = "Select ‘Refresh system status’ to view basic storage and memory information.";
		((Control)pageByIndex).Controls.Add((Control)(object)info);
		Button val = new Button();
		((Control)val).Text = "Refresh system status";
		((Control)val).Location = new Point(12, y + 56);
		((Control)val).Size = new Size(170, 30);
		((ButtonBase)val).FlatStyle = (FlatStyle)0;
		((Control)val).BackColor = Color.FromArgb(50, 50, 70);
		((Control)val).ForeColor = Color.White;
		((ButtonBase)val).FlatAppearance.BorderSize = 0;
		((Control)val).Click += delegate
		{
			try
			{
				DriveInfo driveInfo = new DriveInfo("C");
				((Control)info).Text = "C: " + Math.Round((double)driveInfo.AvailableFreeSpace / 1073741824.0, 1) + " GB free of " + Math.Round((double)driveInfo.TotalSize / 1073741824.0, 1) + " GB  |  System: " + Environment.OSVersion.VersionString;
			}
			catch
			{
				((Control)info).Text = "Status could not be read.";
			}
		};
		((Control)pageByIndex).Controls.Add((Control)(object)val);
	}
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AstryxTweaks;

public partial class MainForm
{
	private sealed class ThemePreset
	{
		public string Name;
		public string Description;
		public Color Background;
		public Color Sidebar;
		public Color Accent;
		public Color AccentSecondary;
		public Color Active;
		public Color Card;
	}

	private const string PreferencesPath = "Software\\AstryxTweaks";
	private const string SteamServicePath = "C:\\Program Files (x86)\\Common Files\\Steam\\steamservice.exe";
	private const string SteamPriorityPath = "HKLM\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Image File Execution Options\\steamservice.exe\\PerfOptions";

	private Panel accountPage;
	private Panel themesPage;
	private Panel steamNetworkPage;
	private Label sidebarProfileNameLabel;
	private Label sidebarProfileInitialLabel;
	private Label homeWelcomeLabel;
	private string currentThemeName = "Astryx Tweaks Blue";
	private readonly Dictionary<Button, Color> buttonRestColors = new Dictionary<Button, Color>();
	private readonly Dictionary<Button, Color> buttonHoverColors = new Dictionary<Button, Color>();
	private readonly Dictionary<Button, Point> buttonRestPositions = new Dictionary<Button, Point>();
	private readonly HashSet<Button> animatedButtons = new HashSet<Button>();

	private List<ThemePreset> GetThemePresets()
	{
		return new List<ThemePreset>
		{
			new ThemePreset
			{
				Name = "Astryx Tweaks Blue",
				Description = "The original deep-blue Astryx Tweaks look.",
				Background = Color.FromArgb(8, 12, 22),
				Sidebar = Color.FromArgb(10, 17, 31),
				Accent = Color.FromArgb(56, 132, 255),
				AccentSecondary = Color.FromArgb(146, 102, 255),
				Active = Color.FromArgb(24, 62, 126),
				Card = Color.FromArgb(15, 32, 62)
			},
			new ThemePreset
			{
				Name = "Midnight Violet",
				Description = "Dark violet with a cool blue secondary accent.",
				Background = Color.FromArgb(12, 8, 24),
				Sidebar = Color.FromArgb(18, 10, 35),
				Accent = Color.FromArgb(146, 102, 255),
				AccentSecondary = Color.FromArgb(71, 147, 255),
				Active = Color.FromArgb(74, 43, 132),
				Card = Color.FromArgb(38, 24, 62)
			},
			new ThemePreset
			{
				Name = "Emerald",
				Description = "A low-glare green theme for long sessions.",
				Background = Color.FromArgb(6, 16, 16),
				Sidebar = Color.FromArgb(8, 25, 23),
				Accent = Color.FromArgb(37, 196, 140),
				AccentSecondary = Color.FromArgb(50, 130, 246),
				Active = Color.FromArgb(18, 84, 68),
				Card = Color.FromArgb(14, 50, 44)
			},
			new ThemePreset
			{
				Name = "Crimson",
				Description = "Warm red and orange highlights on a dark base.",
				Background = Color.FromArgb(18, 8, 13),
				Sidebar = Color.FromArgb(29, 10, 18),
				Accent = Color.FromArgb(235, 78, 113),
				AccentSecondary = Color.FromArgb(255, 153, 77),
				Active = Color.FromArgb(102, 31, 52),
				Card = Color.FromArgb(61, 20, 36)
			},
			new ThemePreset
			{
				Name = "Solar",
				Description = "Amber and coral accents with a warm dark canvas.",
				Background = Color.FromArgb(18, 14, 6),
				Sidebar = Color.FromArgb(30, 23, 9),
				Accent = Color.FromArgb(245, 166, 35),
				AccentSecondary = Color.FromArgb(255, 103, 73),
				Active = Color.FromArgb(103, 70, 18),
				Card = Color.FromArgb(62, 43, 15)
			}
		};
	}

	private void LoadUserPreferences()
	{
		try
		{
			using RegistryKey key = Registry.CurrentUser.OpenSubKey(PreferencesPath);
			if (key != null)
			{
				profileName = (key.GetValue("Name") as string) ?? "";
				currentThemeName = (key.GetValue("Theme") as string) ?? currentThemeName;
			}
		}
		catch
		{
		}
		if (string.IsNullOrWhiteSpace(profileName))
		{
			try
			{
				profileName = Environment.UserName;
			}
			catch
			{
				profileName = "Guest";
			}
		}
		ThemePreset preset = FindTheme(currentThemeName);
		currentThemeName = preset.Name;
		SetThemePalette(preset);
	}

	private ThemePreset FindTheme(string name)
	{
		foreach (ThemePreset preset in GetThemePresets())
		{
			if (string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase))
			{
				return preset;
			}
		}
		return GetThemePresets()[0];
	}

	private void SetThemePalette(ThemePreset preset)
	{
		BG = preset.Background;
		SIDEBAR = preset.Sidebar;
		ACC = preset.Accent;
		ACC2 = preset.AccentSecondary;
		ACTIVE_BG = preset.Active;
		CARD_BOT = preset.Card;
	}

	private void BuildSidebarUtilities()
	{
		Panel container = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 124,
			BackColor = SIDEBAR
		};
		sidebar.Controls.Add(container);

		Panel divider = new Panel
		{
			Location = new Point(12, 0),
			Size = new Size(200, 1),
			BackColor = Color.FromArgb(42, 62, 94)
		};
		container.Controls.Add(divider);

		Button accountButton = CreateUtilityButton("Account", 8, 66);
		accountButton.Click += delegate { ShowUtilityPage(accountPage); };
		container.Controls.Add(accountButton);

		Button themesButton = CreateUtilityButton("Themes", 78, 64);
		themesButton.Click += delegate { ShowUtilityPage(themesPage); };
		container.Controls.Add(themesButton);

		Button networkButton = CreateUtilityButton("Network", 146, 70);
		networkButton.Click += delegate { ShowUtilityPage(steamNetworkPage); };
		container.Controls.Add(networkButton);

		Panel avatar = new Panel
		{
			Location = new Point(14, 66),
			Size = new Size(38, 38),
			BackColor = ACC,
			Cursor = Cursors.Hand
		};
		RoundControl(avatar, 19);
		avatar.Click += delegate { ShowUtilityPage(accountPage); };
		container.Controls.Add(avatar);

		sidebarProfileInitialLabel = new Label
		{
			Dock = DockStyle.Fill,
			Font = FB,
			ForeColor = Color.White,
			TextAlign = ContentAlignment.MiddleCenter,
			BackColor = Color.Transparent,
			Cursor = Cursors.Hand
		};
		sidebarProfileInitialLabel.Click += delegate { ShowUtilityPage(accountPage); };
		avatar.Controls.Add(sidebarProfileInitialLabel);

		sidebarProfileNameLabel = new Label
		{
			Location = new Point(62, 64),
			Size = new Size(146, 20),
			Font = FB,
			ForeColor = TXT,
			AutoEllipsis = true,
			Cursor = Cursors.Hand
		};
		sidebarProfileNameLabel.Click += delegate { ShowUtilityPage(accountPage); };
		container.Controls.Add(sidebarProfileNameLabel);

		Label plan = new Label
		{
			Text = "Astryx Tweaks profile",
			Location = new Point(62, 87),
			Size = new Size(146, 18),
			Font = FS,
			ForeColor = ACC,
			Cursor = Cursors.Hand
		};
		plan.Click += delegate { ShowUtilityPage(accountPage); };
		container.Controls.Add(plan);
		UpdateProfileDisplay();
	}

	private Button CreateUtilityButton(string text, int x, int width)
	{
		Button button = new Button
		{
			Text = text,
			Location = new Point(x, 12),
			Size = new Size(width, 38),
			FlatStyle = FlatStyle.Flat,
			BackColor = Color.FromArgb(14, 29, 52),
			ForeColor = Color.FromArgb(198, 216, 241),
			Font = FS,
			Cursor = Cursors.Hand,
			Tag = "preserve-color"
		};
		button.FlatAppearance.BorderSize = 0;
		RoundControl(button, 9);
		AttachButtonMotion(button, button.BackColor, Blend(button.BackColor, ACC, 0.3f));
		return button;
	}

	private void BuildUtilityPages()
	{
		BuildAccountPage();
		BuildThemesPage();
		BuildSteamNetworkPage();
	}

	private Panel CreateUtilityPage(string title, string subtitle)
	{
		Panel page = new Panel
		{
			Location = new Point(224, 0),
			Size = new Size(840, 700),
			Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
			BackColor = BG,
			Visible = false,
			AutoScroll = true
		};
		Controls.Add(page);

		Label titleLabel = new Label
		{
			Text = title,
			Font = new Font("Segoe UI Semibold", 24f),
			ForeColor = TXT,
			Location = new Point(34, 28),
			AutoSize = true
		};
		page.Controls.Add(titleLabel);

		Label subtitleLabel = new Label
		{
			Text = subtitle,
			Font = FB,
			ForeColor = MUTED,
			Location = new Point(36, 72),
			Size = new Size(760, 40)
		};
		page.Controls.Add(subtitleLabel);

		Panel accent = new Panel
		{
			Location = new Point(20, 31),
			Size = new Size(4, 30),
			BackColor = ACC
		};
		RoundControl(accent, 2);
		page.Controls.Add(accent);
		return page;
	}

	private void BuildAccountPage()
	{
		accountPage = CreateUtilityPage("Account", "Change the display name used throughout Astryx Tweaks.");
		Panel card = CreateUtilityCard(accountPage, 34, 120, 680, 230);

		Label nameLabel = new Label
		{
			Text = "Display name",
			Font = FB,
			ForeColor = TXT,
			Location = new Point(24, 24),
			AutoSize = true
		};
		card.Controls.Add(nameLabel);

		TextBox nameBox = new TextBox
		{
			Text = profileName,
			Location = new Point(24, 55),
			Size = new Size(632, 32),
			BackColor = Color.FromArgb(18, 26, 46),
			ForeColor = TXT,
			Font = new Font("Segoe UI", 11f),
			BorderStyle = BorderStyle.FixedSingle
		};
		card.Controls.Add(nameBox);

		Label help = new Label
		{
			Text = "This is stored locally for your Windows account.",
			Font = FS,
			ForeColor = MUTED,
			Location = new Point(24, 92),
			AutoSize = true
		};
		card.Controls.Add(help);

		Label result = new Label
		{
			Text = "",
			Font = FB,
			ForeColor = GRN,
			Location = new Point(24, 170),
			Size = new Size(350, 24)
		};
		card.Controls.Add(result);

		Button saveButton = CreatePrimaryButton("Save name", 476, 160, 180);
		saveButton.Click += delegate
		{
			string name = nameBox.Text.Trim();
			if (name.Length == 0 || name.Length > 50)
			{
				result.ForeColor = WARN;
				result.Text = "Enter a name from 1 to 50 characters.";
				return;
			}
			profileName = name;
			SaveUserPreferences();
			UpdateProfileDisplay();
			result.ForeColor = GRN;
			result.Text = "Name saved.";
			Status("Account name updated.");
		};
		card.Controls.Add(saveButton);
		StyleCard(card);
	}

	private void BuildThemesPage()
	{
		themesPage = CreateUtilityPage("Themes", "Choose from five color themes. Your selection is saved locally.");
		List<ThemePreset> presets = GetThemePresets();
		for (int i = 0; i < presets.Count; i++)
		{
			ThemePreset preset = presets[i];
			int x = 34 + i % 2 * 350;
			int y = 120 + i / 2 * 154;
			Panel card = CreateUtilityCard(themesPage, x, y, 330, 134);

			Panel swatch = new Panel
			{
				Location = new Point(18, 20),
				Size = new Size(42, 42),
				BackColor = preset.Accent,
				Tag = "theme-preview"
			};
			RoundControl(swatch, 12);
			card.Controls.Add(swatch);

			Label name = new Label
			{
				Text = preset.Name,
				Font = new Font("Segoe UI Semibold", 11f),
				ForeColor = TXT,
				Location = new Point(76, 17),
				AutoSize = true
			};
			card.Controls.Add(name);

			Label description = new Label
			{
				Text = preset.Description,
				Font = FS,
				ForeColor = MUTED,
				Location = new Point(76, 43),
				Size = new Size(230, 36)
			};
			card.Controls.Add(description);

			Button apply = CreatePrimaryButton("Use theme", 196, 90, 116);
			apply.BackColor = preset.Accent;
			apply.Tag = "theme-preview";
			AttachButtonMotion(apply, preset.Accent, Blend(preset.Accent, Color.White, 0.15f));
			apply.Click += delegate
			{
				ApplyTheme(preset);
				Status(preset.Name + " theme applied.");
			};
			card.Controls.Add(apply);
			StyleCard(card);
		}
	}

	private void BuildSteamNetworkPage()
	{
		steamNetworkPage = CreateUtilityPage("Steam Network", "Give the Steam Client Service real-time process priority.");
		Panel card = CreateUtilityCard(steamNetworkPage, 34, 120, 700, 300);

		Label heading = new Label
		{
			Text = "Steam service real-time priority",
			Font = new Font("Segoe UI Semibold", 15f),
			ForeColor = TXT,
			Location = new Point(24, 22),
			AutoSize = true
		};
		card.Controls.Add(heading);

		Label path = new Label
		{
			Text = SteamServicePath,
			Font = new Font("Consolas", 9f),
			ForeColor = ACC,
			Location = new Point(24, 64),
			Size = new Size(650, 24)
		};
		card.Controls.Add(path);

		Label explanation = new Label
		{
			Text = "This stores a Windows process-priority rule and immediately updates the running service when found.",
			Font = FB,
			ForeColor = MUTED,
			Location = new Point(24, 100),
			Size = new Size(640, 40)
		};
		card.Controls.Add(explanation);

		Label warning = new Label
		{
			Text = "Real-time priority can reduce system responsiveness if Steam misbehaves. Use Restore normal priority to undo it.",
			Font = FB,
			ForeColor = WARN,
			Location = new Point(24, 145),
			Size = new Size(640, 40)
		};
		card.Controls.Add(warning);

		Button apply = CreatePrimaryButton("Apply real-time priority", 24, 215, 235);
		apply.Click += delegate
		{
			if (!File.Exists(SteamServicePath))
			{
				MessageBox.Show("Steam Client Service was not found at:\n\n" + SteamServicePath, "Steam service not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			if (MessageBox.Show("Apply real-time priority to Steam Client Service?\n\nThis may affect system responsiveness.", "Steam real-time priority", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
			{
				return;
			}
			Reg(SteamPriorityPath, "CpuPriorityClass", 4, null);
			RunPS("$target='" + SteamServicePath.Replace("'", "''") + "'; Get-CimInstance Win32_Process | Where-Object { $_.ExecutablePath -eq $target } | ForEach-Object { Invoke-CimMethod -InputObject $_ -MethodName SetPriority -Arguments @{Priority=256} | Out-Null }");
			Status("Steam Client Service real-time priority enabled.");
			MessageBox.Show("Steam Client Service is configured for real-time priority.", "Steam priority updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
		};
		card.Controls.Add(apply);

		Button restore = CreateSecondaryButton("Restore normal priority", 275, 215, 220);
		restore.Click += delegate
		{
			RegDel(SteamPriorityPath, "CpuPriorityClass");
			RunPS("$target='" + SteamServicePath.Replace("'", "''") + "'; Get-CimInstance Win32_Process | Where-Object { $_.ExecutablePath -eq $target } | ForEach-Object { Invoke-CimMethod -InputObject $_ -MethodName SetPriority -Arguments @{Priority=32} | Out-Null }");
			Status("Steam Client Service priority restored to normal.");
			MessageBox.Show("Steam Client Service priority was restored to normal.", "Steam priority restored", MessageBoxButtons.OK, MessageBoxIcon.Information);
		};
		card.Controls.Add(restore);
		StyleCard(card);
	}

	private Panel CreateUtilityCard(Panel parent, int x, int y, int width, int height)
	{
		Panel card = new Panel
		{
			Location = new Point(x, y),
			Size = new Size(width, height),
			BackColor = CARD_BOT
		};
		RoundControl(card, 16);
		parent.Controls.Add(card);
		return card;
	}

	private Button CreatePrimaryButton(string text, int x, int y, int width)
	{
		Button button = new Button
		{
			Text = text,
			Location = new Point(x, y),
			Size = new Size(width, 42),
			FlatStyle = FlatStyle.Flat,
			BackColor = ACC,
			ForeColor = Color.White,
			Font = FB,
			Cursor = Cursors.Hand,
			Tag = "preserve-color"
		};
		button.FlatAppearance.BorderSize = 0;
		RoundControl(button, 10);
		AttachButtonMotion(button, button.BackColor, Blend(button.BackColor, Color.White, 0.15f));
		return button;
	}

	private Button CreateSecondaryButton(string text, int x, int y, int width)
	{
		Button button = CreatePrimaryButton(text, x, y, width);
		button.BackColor = Color.FromArgb(34, 40, 58);
		AttachButtonMotion(button, button.BackColor, Blend(button.BackColor, Color.White, 0.13f));
		return button;
	}

	private void AttachButtonMotion(Button button, Color restColor, Color hoverColor)
	{
		buttonRestColors[button] = restColor;
		buttonHoverColors[button] = hoverColor;
		buttonRestPositions[button] = button.Location;
		if (animatedButtons.Contains(button))
		{
			return;
		}
		animatedButtons.Add(button);
		button.MouseEnter += delegate
		{
			AnimateBackColor(button, buttonHoverColors[button], 140);
		};
		button.MouseLeave += delegate
		{
			AnimateBackColor(button, buttonRestColors[button], 190);
			AnimatePosition(button, buttonRestPositions[button], 120);
		};
		button.MouseDown += delegate
		{
			Point restPosition = buttonRestPositions[button];
			AnimatePosition(button, new Point(restPosition.X, restPosition.Y + 1), 70);
		};
		button.MouseUp += delegate
		{
			AnimatePosition(button, buttonRestPositions[button], 100);
		};
	}

	private void ShowUtilityPage(Panel page)
	{
		if (page == null)
		{
			return;
		}
		foreach (Panel navPage in navMap.Values)
		{
			navPage.Visible = false;
		}
		if (homePage != null)
		{
			homePage.Visible = false;
		}
		if (backupPage != null)
		{
			backupPage.Visible = false;
		}
		if (defenderPage != null)
		{
			defenderPage.Visible = false;
		}
		if (powerPage != null)
		{
			powerPage.Visible = false;
		}
		HideUtilityPages();
		activeNav = null;
		secondaryNav.Visible = false;
		page.Location = new Point(sidebar.Width, 0);
		page.Size = new Size(Math.Max(760, ClientSize.Width - sidebar.Width), Math.Max(580, ClientSize.Height - 28));
		page.Visible = true;
		page.BringToFront();
		AnimatePage(page);
	}

	private void HideUtilityPages()
	{
		if (accountPage != null)
		{
			accountPage.Visible = false;
		}
		if (themesPage != null)
		{
			themesPage.Visible = false;
		}
		if (steamNetworkPage != null)
		{
			steamNetworkPage.Visible = false;
		}
	}

	private void SaveUserPreferences()
	{
		try
		{
			using RegistryKey key = Registry.CurrentUser.CreateSubKey(PreferencesPath);
			if (key != null)
			{
				key.SetValue("Name", profileName ?? "");
				key.SetValue("Theme", currentThemeName ?? "Astryx Tweaks Blue");
			}
		}
		catch
		{
		}
	}

	private void UpdateProfileDisplay()
	{
		string name = string.IsNullOrWhiteSpace(profileName) ? "Guest" : profileName.Trim();
		string initial = name.Substring(0, 1).ToUpperInvariant();
		if (sidebarProfileNameLabel != null)
		{
			sidebarProfileNameLabel.Text = name;
		}
		if (sidebarProfileInitialLabel != null)
		{
			sidebarProfileInitialLabel.Text = initial;
		}
		if (homeWelcomeLabel != null)
		{
			homeWelcomeLabel.Text = "Welcome Back, " + name + "!";
		}
	}

	private void ApplyTheme(ThemePreset preset)
	{
		Color oldBackground = BG;
		Color oldSidebar = SIDEBAR;
		Color oldAccent = ACC;
		Color oldAccentSecondary = ACC2;
		Color oldActive = ACTIVE_BG;
		Color oldCard = CARD_BOT;

		SetThemePalette(preset);
		currentThemeName = preset.Name;
		RecolorTree(this, oldBackground, oldSidebar, oldAccent, oldAccentSecondary, oldActive, oldCard);
		foreach (Button button in animatedButtons)
		{
			if (button.IsDisposed || string.Equals(button.Tag as string, "theme-preview", StringComparison.Ordinal))
			{
				continue;
			}
			Color restColor = buttonRestColors[button];
			if (restColor == oldAccent)
			{
				restColor = ACC;
			}
			else if (restColor == oldAccentSecondary)
			{
				restColor = ACC2;
			}
			else if (restColor == oldActive)
			{
				restColor = ACTIVE_BG;
			}
			buttonRestColors[button] = restColor;
			bool sidebarUtility = button.Parent != null && button.Parent.Parent == sidebar;
			buttonHoverColors[button] = sidebarUtility ? Blend(restColor, ACC, 0.3f) : Blend(restColor, Color.White, 0.15f);
		}
		if (sidebar != null)
		{
			sidebar.BackColor = SIDEBAR;
		}
		if (indicator != null)
		{
			indicator.BackColor = ACC;
		}
		SaveUserPreferences();
		Invalidate(true);
	}

	private void RecolorTree(Control root, Color oldBackground, Color oldSidebar, Color oldAccent, Color oldAccentSecondary, Color oldActive, Color oldCard)
	{
		if (string.Equals(root.Tag as string, "theme-preview", StringComparison.Ordinal))
		{
			return;
		}
		if (root.BackColor == oldBackground)
		{
			root.BackColor = BG;
		}
		else if (root.BackColor == oldSidebar)
		{
			root.BackColor = SIDEBAR;
		}
		else if (root.BackColor == oldAccent)
		{
			root.BackColor = ACC;
		}
		else if (root.BackColor == oldAccentSecondary)
		{
			root.BackColor = ACC2;
		}
		else if (root.BackColor == oldActive)
		{
			root.BackColor = ACTIVE_BG;
		}
		else if (root.BackColor == oldCard)
		{
			root.BackColor = CARD_BOT;
		}

		if (root.ForeColor == oldAccent)
		{
			root.ForeColor = ACC;
		}
		else if (root.ForeColor == oldAccentSecondary)
		{
			root.ForeColor = ACC2;
		}

		foreach (Control child in root.Controls)
		{
			RecolorTree(child, oldBackground, oldSidebar, oldAccent, oldAccentSecondary, oldActive, oldCard);
		}
	}
}

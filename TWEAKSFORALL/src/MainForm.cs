using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TweaksForAll
{
    internal sealed class MainForm : Form
    {
        private const string Brand = "TWEAKSFORALL";

        private readonly Panel _content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };
        private readonly Label _status = new Label();
        private readonly Panel _sidebar = new Panel { Dock = DockStyle.Left, Width = 214, BackColor = Theme.Sidebar };

        private readonly Dictionary<string, Panel> _pages = new Dictionary<string, Panel>();
        private readonly Dictionary<string, NavButton> _nav = new Dictionary<string, NavButton>();
        private readonly List<(Tweak tweak, ToggleSwitch toggle)> _all = new List<(Tweak, ToggleSwitch)>();
        private readonly List<Tweak> _catalog = Catalog.All();
        private readonly Telemetry _tele = new Telemetry();
        private readonly Timer _timer = new Timer { Interval = 1000 };

        private string _activeKey;
        private bool _busy;

        // Overview live labels.
        private Label _ovCpu, _ovGpu, _ovMem, _ovUptime, _ovProc;
        private Bar _barCpu, _barGpu, _barMem, _barDisk;

        public MainForm()
        {
            Text = Brand;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            var wa = Screen.FromPoint(Cursor.Position).WorkingArea;
            Size = new Size(Math.Min(1120, wa.Width), Math.Min(700, wa.Height));
            MinimumSize = new Size(Math.Min(980, wa.Width), Math.Min(620, wa.Height));
            BackColor = Theme.Bg;
            DoubleBuffered = true;
            try { Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            BuildChrome();
            BuildSidebar();
            BuildPages();
            Select("home");

            _timer.Tick += (s, e) => TickTelemetry();
            _timer.Start();
            FormClosing += (s, e) => { _timer.Stop(); _tele.Dispose(); };
        }

        // ---------- window chrome (title bar + resize) ----------
        private void BuildChrome()
        {
            var topBar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.Sidebar };
            topBar.MouseDown += DragMove;

            var badge = new Label
            {
                Text = "  \u2605 MAX MODE  ",
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                ForeColor = Theme.AccentBright,
                AutoSize = true,
                Location = new Point(230, 13)
            };
            topBar.Controls.Add(badge);

            var close = WinButton("\uE8BB", Color.FromArgb(232, 17, 35));
            close.Click += (s, e) => Close();
            var min = WinButton("\uE921", Theme.Panel);
            min.Click += (s, e) => WindowState = FormWindowState.Minimized;

            topBar.Resize += (s, e) => { close.Left = topBar.Width - 44; min.Left = topBar.Width - 88; };
            topBar.Controls.Add(close);
            topBar.Controls.Add(min);

            var host = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };
            host.Controls.Add(_content);

            var statusBar = new Panel { Dock = DockStyle.Bottom, Height = 26, BackColor = Theme.Sidebar };
            _status.Dock = DockStyle.Fill;
            _status.ForeColor = Theme.SubText;
            _status.Font = Theme.Small;
            _status.TextAlign = ContentAlignment.MiddleLeft;
            _status.Padding = new Padding(14, 0, 0, 0);
            _status.Text = "Ready. Tuned for Windows 10 IoT Enterprise LTSC.";
            statusBar.Controls.Add(_status);

            Controls.Add(host);
            Controls.Add(statusBar);
            Controls.Add(_sidebar);
            Controls.Add(topBar);
        }

        private Label WinButton(string glyph, Color hover)
        {
            var l = new Label
            {
                Text = glyph,
                Font = new Font("Segoe MDL2 Assets", 9f),
                ForeColor = Theme.SubText,
                Size = new Size(44, 44),
                TextAlign = ContentAlignment.MiddleCenter,
                Top = 0
            };
            l.MouseEnter += (s, e) => { l.BackColor = hover; l.ForeColor = Color.White; };
            l.MouseLeave += (s, e) => { l.BackColor = Color.Transparent; l.ForeColor = Theme.SubText; };
            return l;
        }

        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr h, int m, int w, int l);
        private void DragMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); }
        }

        // ---------- sidebar ----------
        private void BuildSidebar()
        {
            // Account card pinned to the bottom.
            var acct = new Card { Height = 56, Dock = DockStyle.Bottom, Fill = Theme.Panel };
            acct.Padding = new Padding(10, 8, 10, 8);
            var acctIcon = new Label { Text = "\uE77B", Font = new Font("Segoe MDL2 Assets", 13f), ForeColor = Theme.AccentBright, AutoSize = true, Location = new Point(14, 16) };
            var acctName = new Label { Text = "Administrator", Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold), ForeColor = Theme.Text, AutoSize = true, Location = new Point(44, 10) };
            var acctTier = new Label { Text = "MAX MODE \u2022 Local", Font = Theme.Small, ForeColor = Theme.SubText, AutoSize = true, Location = new Point(44, 30) };
            acct.Controls.AddRange(new Control[] { acctIcon, acctName, acctTier });

            // Nav is stacked top-down; add in reverse so order stays correct.
            AddNavHeader("");
            AddNav("settings", "\uE713", "Settings");
            AddNavHeader("");
            AddNav("registry", "\uE74C", "Responsiveness");
            AddNav("memory", "\uEEA0", "Memory");
            AddNav("games", "\uE7FC", "Games");
            AddNav("gpu", "\uE7F4", "GPU");
            AddNav("network", "\uE839", "Network");
            AddNav("cleanup", "\uE74D", "Cleanup");
            AddNav("windows", "\uE770", "Windows");
            AddNavHeader("TWEAKS");
            AddNav("overview", "\uE9D9", "Overview");
            AddNav("home", "\uE80F", "Home");

            var logo = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Theme.Sidebar };
            logo.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                TextRenderer.DrawText(g, "TWEAKS", Theme.Logo, new Rectangle(18, 12, 200, 30), Theme.Text, TextFormatFlags.Left);
                int w = TextRenderer.MeasureText("TWEAKS", Theme.Logo).Width;
                TextRenderer.DrawText(g, "FORALL", Theme.Logo, new Rectangle(16 + w, 12, 200, 30), Theme.AccentBright, TextFormatFlags.Left);
            };

            _sidebar.Controls.Add(acct);
            _sidebar.Controls.Add(logo);
        }

        private void AddNav(string key, string glyph, string label)
        {
            var b = new NavButton(glyph, label);
            b.Click += (s, e) => Select(key);
            _nav[key] = b;
            _sidebar.Controls.Add(b);
        }

        private void AddNavHeader(string text)
        {
            var l = new Label
            {
                Dock = DockStyle.Top,
                Height = string.IsNullOrEmpty(text) ? 12 : 26,
                Text = "   " + text,
                Font = new Font("Segoe UI Semibold", 7.5f, FontStyle.Bold),
                ForeColor = Theme.Muted,
                TextAlign = ContentAlignment.BottomLeft,
                BackColor = Theme.Sidebar
            };
            _sidebar.Controls.Add(l);
        }

        // ---------- page routing ----------
        private void Select(string key)
        {
            _activeKey = key;
            foreach (var kv in _nav) kv.Value.Selected = kv.Key == key;
            foreach (var kv in _pages) kv.Value.Visible = kv.Key == key;
            if (key == "overview") TickTelemetry();
        }

        private void BuildPages()
        {
            _pages["home"] = BuildHome();
            _pages["overview"] = BuildOverview();
            _pages["windows"] = BuildTweakPage("windows", "Windows Tweaks", "Power, CPU scheduling and latency core tuning", "Windows");
            _pages["cleanup"] = BuildTweakPage("cleanup", "Cleanup", "Clear caches and reclaim responsiveness", "Cleanup");
            _pages["network"] = BuildTweakPage("network", "Network Tweaks", "Low-latency, low-jitter online play", "Network");
            _pages["gpu"] = BuildTweakPage("gpu", "GPU Tweaks", "Frame pacing, scheduling and input latency", "GPU");
            _pages["games"] = BuildTweakPage("games", "Games", "Max FPS, game mode and live process booster", "Games");
            _pages["memory"] = BuildTweakPage("memory", "Memory", "RAM and paging behaviour for gaming", "Memory");
            _pages["registry"] = BuildTweakPage("registry", "Responsiveness", "Instant menus, fast kills, snappy shell", "Registry");
            _pages["settings"] = BuildSettings();
            foreach (var p in _pages.Values) { p.Visible = false; _content.Controls.Add(p); }
        }

        private Panel NewPage()
        {
            return new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Padding = new Padding(28, 22, 28, 14), AutoScroll = true };
        }

        private static void Header(Panel page, string title, string sub, ref int y)
        {
            var t = new Label { Text = title, Font = Theme.H1, ForeColor = Theme.Text, AutoSize = true, Location = new Point(2, y) };
            page.Controls.Add(t);
            y += 34;
            var s = new Label { Text = sub, Font = Theme.Body, ForeColor = Theme.SubText, AutoSize = true, Location = new Point(3, y) };
            page.Controls.Add(s);
            y += 34;
        }

        // ---------- HOME ----------
        private Panel BuildHome()
        {
            var page = NewPage();
            int y = 4;
            Header(page, "TWEAKSFORALL", "One-click maximum optimization for Windows 10 IoT Enterprise LTSC", ref y);

            var hero = new Card { Fill = Theme.Panel, Location = new Point(2, y), Size = new Size(760, 168) };
            hero.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            hero.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var b = new LinearGradientBrush(new Rectangle(0, 0, hero.Width, hero.Height), Theme.AccentGlow, Theme.Panel, 25f))
                using (var path = Card.Round(new Rectangle(0, 0, hero.Width - 1, hero.Height - 1), 12))
                    g.FillPath(b, path);
                using (var p = new Pen(Theme.AccentDim)) using (var path = Card.Round(new Rectangle(0, 0, hero.Width - 1, hero.Height - 1), 12)) g.DrawPath(p, path);
            };
            var hTitle = new Label { Text = "MAXIMUM. MAXIMUM.", Font = new Font("Segoe UI", 17f, FontStyle.Bold), ForeColor = Theme.Text, AutoSize = true, BackColor = Color.Transparent, Location = new Point(24, 20) };
            var hDesc = new Label { Text = "Apply every recommended low-latency and max-FPS tweak in one pass.\nA System Restore point is created first so you can always roll back.", Font = Theme.Body, ForeColor = Theme.SubText, AutoSize = true, BackColor = Color.Transparent, Location = new Point(26, 54) };
            var maxBtn = new PillButton { Text = "\u26A1  MAXIMUM OPTIMIZE", Size = new Size(250, 48), Location = new Point(24, 104), Filled = true };
            var restore = new CheckBox { Text = "  Create restore point first (recommended)", ForeColor = Theme.SubText, Font = Theme.Small, AutoSize = true, Checked = true, BackColor = Color.Transparent, FlatStyle = FlatStyle.Flat, Location = new Point(288, 118) };
            maxBtn.Click += (s, e) => RunMaximum(restore.Checked);
            hero.Controls.AddRange(new Control[] { hTitle, hDesc, restore, maxBtn });
            page.Controls.Add(hero);
            y += 184;

            // quick action tiles
            AddQuickTile(page, ref y, 0, "\uE770", "Windows", "Power & CPU core tuning", "windows");
            AddQuickTile(page, ref y, 1, "\uE839", "Network", "Kill lag & jitter", "network");
            AddQuickTile(page, ref y, 2, "\uE7F4", "GPU", "Frame pacing & input", "gpu");
            AddQuickTile(page, ref y, 3, "\uE7FC", "Games", "Max FPS & booster", "games");
            return page;
        }

        private void AddQuickTile(Panel page, ref int y, int index, string glyph, string title, string sub, string key)
        {
            int col = index % 2, row = index / 2;
            var tile = new Card { Fill = Theme.Card, Size = new Size(366, 78), Cursor = Cursors.Hand };
            tile.Location = new Point(2 + col * 384, y + row * 92);
            tile.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            var ic = new Label { Text = glyph, Font = new Font("Segoe MDL2 Assets", 17f), ForeColor = Theme.AccentBright, AutoSize = true, BackColor = Color.Transparent, Location = new Point(18, 24) };
            var tt = new Label { Text = title, Font = Theme.H2, ForeColor = Theme.Text, AutoSize = true, BackColor = Color.Transparent, Location = new Point(58, 16) };
            var ss = new Label { Text = sub, Font = Theme.Small, ForeColor = Theme.SubText, AutoSize = true, BackColor = Color.Transparent, Location = new Point(60, 42) };
            EventHandler go = (s, e) => Select(key);
            tile.Click += go; ic.Click += go; tt.Click += go; ss.Click += go;
            tile.Controls.AddRange(new Control[] { ic, tt, ss });
            page.Controls.Add(tile);
            if (index == 3) y += 200;
        }

        // ---------- OVERVIEW ----------
        private Panel BuildOverview()
        {
            var page = NewPage();
            int y = 4;
            Header(page, "System Overview", "Live temps, usage and hardware at a glance", ref y);

            var util = new Card { Fill = Theme.Panel, Location = new Point(2, y), Size = new Size(430, 210) };
            var utilTitle = new Label { Text = "UTILIZATION", Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = Theme.SubText, AutoSize = true, BackColor = Color.Transparent, Location = new Point(18, 14) };
            util.Controls.Add(utilTitle);
            _barCpu = AddBar(util, "CPU", 44); _barGpu = AddBar(util, "GPU", 84);
            _barMem = AddBar(util, "MEMORY", 124); _barDisk = AddBar(util, "DISK", 164);
            page.Controls.Add(util);

            var stat = new Card { Fill = Theme.Panel, Location = new Point(448, y), Size = new Size(300, 210) };
            var st = new Label { Text = "LIVE", Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = Theme.SubText, AutoSize = true, BackColor = Color.Transparent, Location = new Point(18, 14) };
            stat.Controls.Add(st);
            _ovCpu = BigStat(stat, "CPU load", 44);
            _ovGpu = BigStat(stat, "GPU load", 88);
            _ovMem = BigStat(stat, "Memory", 132);
            _ovUptime = SmallStat(stat, "Uptime", 172, 18);
            _ovProc = SmallStat(stat, "Processes", 172, 168);
            page.Controls.Add(stat);
            y += 226;

            var spec = new Card { Fill = Theme.Panel, Location = new Point(2, y), Size = new Size(746, 176) };
            spec.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            var sTitle = new Label { Text = "PC SPECIFICATIONS", Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = Theme.SubText, AutoSize = true, BackColor = Color.Transparent, Location = new Point(18, 14) };
            spec.Controls.Add(sTitle);
            SpecRow(spec, "CPU", $"{_tele.Cpu}   ({_tele.Cores}C / {_tele.Threads}T)", 44);
            SpecRow(spec, "GPU", _tele.Gpu, 74);
            SpecRow(spec, "RAM", _tele.Ram, 104);
            SpecRow(spec, "BOARD", _tele.Board, 134);
            SpecRow(spec, "OS", _tele.Os, 164);
            page.Controls.Add(spec);
            y += 200;
            return page;
        }

        private Bar AddBar(Card host, string label, int top)
        {
            var l = new Label { Text = label, Font = Theme.Small, ForeColor = Theme.Text, AutoSize = true, BackColor = Color.Transparent, Location = new Point(18, top) };
            host.Controls.Add(l);
            var bar = new Bar { Location = new Point(18, top + 18), Size = new Size(390, 8) };
            bar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            host.Controls.Add(bar);
            return bar;
        }

        private Label BigStat(Card host, string label, int top)
        {
            var l = new Label { Text = label, Font = Theme.Small, ForeColor = Theme.SubText, AutoSize = true, BackColor = Color.Transparent, Location = new Point(18, top) };
            var v = new Label { Text = "--", Font = new Font("Segoe UI", 15f, FontStyle.Bold), ForeColor = Theme.AccentBright, AutoSize = true, BackColor = Color.Transparent, Location = new Point(150, top - 4) };
            host.Controls.Add(l); host.Controls.Add(v);
            return v;
        }

        private Label SmallStat(Card host, string label, int top, int left)
        {
            var l = new Label { Text = label, Font = Theme.Small, ForeColor = Theme.SubText, AutoSize = true, BackColor = Color.Transparent, Location = new Point(left, top) };
            var v = new Label { Text = "--", Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold), ForeColor = Theme.Text, AutoSize = true, BackColor = Color.Transparent, Location = new Point(left, top + 16) };
            host.Controls.Add(l); host.Controls.Add(v);
            return v;
        }

        private void SpecRow(Card host, string k, string v, int top)
        {
            var kl = new Label { Text = k, Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold), ForeColor = Theme.SubText, AutoSize = true, BackColor = Color.Transparent, Location = new Point(18, top) };
            var vl = new Label { Text = v, Font = Theme.Body, ForeColor = Theme.Text, AutoSize = true, BackColor = Color.Transparent, Location = new Point(90, top - 1) };
            host.Controls.Add(kl); host.Controls.Add(vl);
        }

        private void TickTelemetry()
        {
            if (_activeKey != "overview") return;
            _tele.Sample();
            if (_barCpu == null) return;
            _barCpu.Value = _tele.CpuPercent; _barGpu.Value = _tele.GpuPercent;
            _barMem.Value = _tele.MemoryPercent; _barDisk.Value = _tele.DiskPercent;
            _ovCpu.Text = $"{_tele.CpuPercent:0}%";
            _ovGpu.Text = $"{_tele.GpuPercent:0}%";
            _ovMem.Text = $"{_tele.UsedMemMB / 1024.0:0.0}/{_tele.TotalMemMB / 1024.0:0.0} GB";
            _ovUptime.Text = _tele.Uptime;
            _ovProc.Text = _tele.ProcessCount.ToString();
        }

        // ---------- TWEAK PAGES ----------
        private Panel BuildTweakPage(string key, string title, string sub, string category)
        {
            var page = NewPage();
            int y = 4;
            Header(page, title, sub, ref y);

            var tweaks = _catalog.Where(x => x.Category == category).ToList();

            // Controls whose width should track the page; rows carry a toggle on the right.
            var fillControls = new List<Control>();
            var rows = new List<Card>();

            // action bar
            var bar = new Panel { Location = new Point(2, y), Size = new Size(760, 44), BackColor = Color.Transparent };
            var apply = new PillButton { Text = "Apply selected", Size = new Size(140, 36), Location = new Point(2, 4), Filled = true };
            var revert = new PillButton { Text = "Revert selected", Size = new Size(140, 36), Location = new Point(150, 4), Filled = false };
            var all = new PillButton { Text = "Select all", Size = new Size(104, 36), Location = new Point(298, 4), Filled = false };
            var none = new PillButton { Text = "Clear", Size = new Size(84, 36), Location = new Point(406, 4), Filled = false };
            bar.Controls.AddRange(new Control[] { apply, revert, all, none });
            page.Controls.Add(bar);
            fillControls.Add(bar);
            y += 54;

            var rowToggles = new List<(Tweak, ToggleSwitch)>();
            foreach (var tw in tweaks)
            {
                var row = new Card { Fill = Theme.Card, Location = new Point(2, y), Size = new Size(760, 60) };
                var name = new Label { Text = tw.Name, Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold), ForeColor = Theme.Text, AutoSize = true, BackColor = Color.Transparent, Location = new Point(18, 10) };
                var desc = new Label { Text = tw.Warning == null ? tw.Desc : tw.Desc + "   \u26A0 " + tw.Warning, Font = Theme.Small, ForeColor = tw.Warning == null ? Theme.SubText : Theme.Warn, AutoSize = true, BackColor = Color.Transparent, Location = new Point(18, 32) };
                var tog = new ToggleSwitch { Location = new Point(700, 18) };
                tog.SetSilent(tw.DefaultOn);
                row.Controls.AddRange(new Control[] { name, desc, tog });
                page.Controls.Add(row);
                fillControls.Add(row);
                rows.Add(row);
                rowToggles.Add((tw, tog));
                _all.Add((tw, tog));
                y += 68;
            }

            EventHandler layout = (s, e) =>
            {
                int w = page.ClientSize.Width - 34;
                if (w < 300) return;
                foreach (var c in fillControls) c.Width = w;
                foreach (var r in rows)
                    if (r.Controls.Count > 0 && r.Controls[r.Controls.Count - 1] is ToggleSwitch t)
                        t.Left = r.Width - 62;
            };
            page.ClientSizeChanged += layout;
            page.VisibleChanged += layout;

            apply.Click += (s, e) => ApplySet(rowToggles.Where(x => x.Item2.On).Select(x => x.Item1).ToList(), false, "Applied");
            revert.Click += (s, e) => ApplySet(rowToggles.Where(x => x.Item2.On).Select(x => x.Item1).ToList(), true, "Reverted");
            all.Click += (s, e) => { foreach (var x in rowToggles) x.Item2.SetSilent(true); };
            none.Click += (s, e) => { foreach (var x in rowToggles) x.Item2.SetSilent(false); };
            return page;
        }

        // ---------- SETTINGS ----------
        private Panel BuildSettings()
        {
            var page = NewPage();
            int y = 4;
            Header(page, "Settings", "About TWEAKSFORALL", ref y);
            var card = new Card { Fill = Theme.Panel, Location = new Point(2, y), Size = new Size(746, 210), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var about = new Label
            {
                Text = "TWEAKSFORALL v1.0\n\nA single-file, self-contained Windows optimizer built for Windows 10 IoT\nEnterprise LTSC. Every tweak is reversible and a System Restore point is\ncreated before a MAXIMUM run.\n\nRun as Administrator. Some tweaks (timers, mitigations, drivers) take\nfull effect after a reboot.",
                Font = Theme.Body, ForeColor = Theme.SubText, AutoSize = true, BackColor = Color.Transparent, Location = new Point(20, 18)
            };
            var revertAll = new PillButton { Text = "Revert everything to Windows defaults", Size = new Size(300, 40), Location = new Point(20, 150), Filled = false };
            revertAll.Click += (s, e) =>
            {
                if (MessageBox.Show("Revert every tweak that has a defined revert action?", Brand, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    ApplySet(_catalog.Where(x => x.Revert != null).ToList(), true, "Reverted");
            };
            card.Controls.Add(about); card.Controls.Add(revertAll);
            page.Controls.Add(card);
            return page;
        }

        // ---------- apply engine ----------
        private async void ApplySet(List<Tweak> tweaks, bool revert, string verb)
        {
            if (_busy) return;
            if (tweaks.Count == 0) { SetStatus("Nothing selected."); return; }
            _busy = true;
            int done = 0;
            await Task.Run(() =>
            {
                foreach (var tw in tweaks)
                {
                    try
                    {
                        if (revert) tw.Revert?.Invoke();
                        else tw.Apply?.Invoke();
                        done++;
                        BeginInvoke((Action)(() => SetStatus($"{verb} {done}/{tweaks.Count}: {tw.Name}")));
                    }
                    catch { }
                }
            });
            _busy = false;
            SetStatus($"{verb} {done} tweak(s). A reboot is recommended for full effect.");
        }

        private async void RunMaximum(bool restore)
        {
            if (_busy) return;
            if (MessageBox.Show("Apply ALL recommended MAXIMUM tweaks now?\n\nThis changes power, CPU, GPU, network, memory and gaming settings.",
                Brand, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
            _busy = true;
            var max = _catalog.Where(x => x.Maximum).ToList();
            int done = 0;
            await Task.Run(() =>
            {
                if (restore)
                {
                    BeginInvoke((Action)(() => SetStatus("Creating System Restore point...")));
                    Engine.CreateRestorePoint("TWEAKSFORALL - before MAXIMUM");
                }
                foreach (var tw in max)
                {
                    try { tw.Apply?.Invoke(); done++; BeginInvoke((Action)(() => SetStatus($"MAXIMUM {done}/{max.Count}: {tw.Name}"))); }
                    catch { }
                }
            });
            _busy = false;
            SetStatus($"MAXIMUM complete \u2014 {done} tweaks applied. Reboot to lock in timer/driver changes.");
            MessageBox.Show($"MAXIMUM optimization applied ({done} tweaks).\n\nReboot your PC for timer, driver and mitigation changes to take full effect.",
                Brand, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SetStatus(string s) { _status.Text = "  " + s; }
    }

    // Thin animated usage bar in the accent colour.
    internal sealed class Bar : Control
    {
        private float _value;
        public float Value { get => _value; set { _value = Math.Max(0, Math.Min(100, value)); Invalidate(); } }
        public Bar()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var b = new SolidBrush(Theme.Border)) using (var p = Card.Round(r, Height / 2)) g.FillPath(b, p);
            int w = (int)((Width - 2) * (_value / 100f));
            if (w > Height)
            {
                var fr = new Rectangle(0, 0, w, Height - 1);
                using (var b = new LinearGradientBrush(fr, Theme.Accent, Theme.AccentBright, 0f))
                using (var p = Card.Round(fr, Height / 2)) g.FillPath(b, p);
            }
        }
    }
}

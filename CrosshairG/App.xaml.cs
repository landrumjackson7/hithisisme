using System.Reflection;
using System.Windows;
using Application = System.Windows.Application;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace CrosshairG;

/// <summary>
/// Application entry point. Owns the crosshair settings, the overlay window and
/// the system-tray icon. The app uses <c>OnExplicitShutdown</c>, so closing any
/// window (including Settings) leaves it running in the tray — it only exits via
/// the tray's "Exit" command or when the process is ended from Task Manager.
/// </summary>
public partial class App : Application
{
    private const string MutexName = "CrosshairG_SingleInstance_Mutex";

    private Mutex? _instanceMutex;
    private CrosshairSettings _settings = new();
    private OverlayWindow? _overlay;
    private SettingsWindow? _settingsWindow;
    private Forms.NotifyIcon? _tray;
    private Forms.ToolStripMenuItem? _toggleItem;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        // Enforce a single running instance.
        _instanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        _settings = CrosshairSettings.Load();

        _overlay = new OverlayWindow(_settings);
        _overlay.ApplyVisibility(); // shows the overlay if enabled

        BuildTrayIcon();

        _settings.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(CrosshairSettings.OverlayEnabled) && _toggleItem != null)
                _toggleItem.Checked = _settings.OverlayEnabled;
        };
    }

    private void BuildTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();

        var settingsItem = new Forms.ToolStripMenuItem("Settings…");
        settingsItem.Font = new Drawing.Font(settingsItem.Font, Drawing.FontStyle.Bold);
        settingsItem.Click += (_, _) => OpenSettings();

        _toggleItem = new Forms.ToolStripMenuItem("Show crosshair")
        {
            CheckOnClick = true,
            Checked = _settings.OverlayEnabled,
        };
        _toggleItem.Click += (_, _) => _settings.OverlayEnabled = _toggleItem.Checked;

        var resetItem = new Forms.ToolStripMenuItem("Reset to defaults");
        resetItem.Click += (_, _) => _settings.CopyFrom(new CrosshairSettings());

        var exitItem = new Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitApp();

        menu.Items.Add(settingsItem);
        menu.Items.Add(_toggleItem);
        menu.Items.Add(resetItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        _tray = new Forms.NotifyIcon
        {
            Icon = LoadTrayIcon(),
            Visible = true,
            Text = "Crosshair G",
            ContextMenuStrip = menu,
        };
        _tray.DoubleClick += (_, _) => OpenSettings();
    }

    private void OpenSettings()
    {
        if (_settingsWindow != null)
        {
            if (_settingsWindow.WindowState == WindowState.Minimized)
                _settingsWindow.WindowState = WindowState.Normal;
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings);
        _settingsWindow.Closed += (_, _) =>
        {
            _settingsWindow = null;
            _settings.Save();
        };
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void ExitApp()
    {
        _settings.Save();
        Shutdown();
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        try { _settings.Save(); } catch { /* best effort */ }

        if (_tray != null)
        {
            _tray.Visible = false;
            _tray.Dispose();
            _tray = null;
        }

        _overlay?.Close();

        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        _instanceMutex = null;
    }

    private static Drawing.Icon LoadTrayIcon()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            string? name = Array.Find(asm.GetManifestResourceNames(),
                n => n.EndsWith("app.ico", StringComparison.OrdinalIgnoreCase));
            if (name != null)
            {
                using var stream = asm.GetManifestResourceStream(name);
                if (stream != null) return new Drawing.Icon(stream);
            }
        }
        catch
        {
            // Fall through to the system default icon.
        }
        return Drawing.SystemIcons.Application;
    }
}

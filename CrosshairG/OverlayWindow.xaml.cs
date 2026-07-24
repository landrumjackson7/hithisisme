using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace CrosshairG;

/// <summary>
/// The always-on-top, click-through, transparent window that paints the
/// crosshair over every other window. It never takes focus or receives mouse
/// input, so games and other apps behave exactly as if it were not there.
/// </summary>
public partial class OverlayWindow : Window
{
    private readonly CrosshairSettings _settings;
    private readonly DispatcherTimer _topmostTimer;
    private IntPtr _hwnd = IntPtr.Zero;

    public OverlayWindow(CrosshairSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        _settings.PropertyChanged += OnSettingsChanged;

        // Some full-screen games periodically grab the top z-order; re-assert it.
        _topmostTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _topmostTimer.Tick += (_, _) => ReassertTopmost();

        Loaded += (_, _) => { PositionToPrimaryScreen(); Redraw(); };
        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        MakeClickThrough(_hwnd);
        _topmostTimer.Start();
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => OnSettingsChanged(sender, e));
            return;
        }

        if (e.PropertyName == nameof(CrosshairSettings.OverlayEnabled))
        {
            ApplyVisibility();
            return;
        }
        Redraw();
    }

    public void ApplyVisibility()
    {
        if (_settings.OverlayEnabled)
        {
            Show();
            ReassertTopmost();
        }
        else
        {
            Hide();
        }
    }

    private void PositionToPrimaryScreen()
    {
        // SystemParameters are in DIPs, so the crosshair stays centred regardless
        // of the monitor's DPI scaling.
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;
    }

    private void Redraw()
    {
        if (Width <= 0 || Height <= 0) PositionToPrimaryScreen();
        double cx = Width / 2.0 + _settings.OffsetX;
        double cy = Height / 2.0 + _settings.OffsetY;
        CrosshairRenderer.Render(RootCanvas, _settings, cx, cy);
    }

    private void ReassertTopmost()
    {
        if (_hwnd == IntPtr.Zero || !_settings.OverlayEnabled) return;
        SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _topmostTimer.Stop();
        _settings.PropertyChanged -= OnSettingsChanged;
        base.OnClosing(e);
    }

    // ----- Win32 interop for click-through / no-activate -----

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;

    private static void MakeClickThrough(IntPtr hwnd)
    {
        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        ex |= WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
        SetWindowLong(hwnd, GWL_EXSTYLE, ex);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);
}

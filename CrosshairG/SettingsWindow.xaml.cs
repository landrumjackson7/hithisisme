using System.ComponentModel;
using System.Windows;
using Button = System.Windows.Controls.Button;
using SizeChangedEventArgs = System.Windows.SizeChangedEventArgs;

namespace CrosshairG;

public partial class SettingsWindow : Window
{
    private readonly CrosshairSettings _settings;

    public SettingsWindow(CrosshairSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        DataContext = _settings;
        _settings.PropertyChanged += OnSettingsChanged;
        Loaded += (_, _) => RenderPreview();
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(RenderPreview);
            return;
        }
        RenderPreview();
    }

    private void PreviewCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => RenderPreview();

    private void RenderPreview()
    {
        double w = PreviewCanvas.ActualWidth;
        double h = PreviewCanvas.ActualHeight;
        if (w <= 0 || h <= 0) return;
        // Preview ignores the on-screen position offset so the crosshair stays
        // centred in the preview box.
        CrosshairRenderer.Render(PreviewCanvas, _settings, w / 2.0, h / 2.0);
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string tag)
        {
            var parts = tag.Split(',');
            if (parts.Length == 3
                && byte.TryParse(parts[0], out var r)
                && byte.TryParse(parts[1], out var g)
                && byte.TryParse(parts[2], out var bl))
            {
                _settings.R = r;
                _settings.G = g;
                _settings.B = bl;
            }
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _settings.CopyFrom(new CrosshairSettings());
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        // Closing the settings window must NOT quit the app — it keeps running in
        // the tray with the overlay active. Persist the latest settings on close.
        _settings.PropertyChanged -= OnSettingsChanged;
        _settings.Save();
        base.OnClosing(e);
    }
}

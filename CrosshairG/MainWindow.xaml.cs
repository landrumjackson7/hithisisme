using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Brush = System.Windows.Media.Brush;
using Button = System.Windows.Controls.Button;
using Cursors = System.Windows.Input.Cursors;
using Orientation = System.Windows.Controls.Orientation;
using RadioButton = System.Windows.Controls.RadioButton;

namespace CrosshairG;

public partial class MainWindow : Window
{
    private readonly CrosshairSettings _settings;
    private readonly List<CrosshairSettings> _saved;
    private string _selectedCategory = "All Crosshairs";
    private CrosshairPreset? _selectedPreset;
    private Border? _selectedThumb;

    public MainWindow(CrosshairSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        DataContext = _settings;
        _saved = SavedStore.Load();

        _settings.PropertyChanged += OnSettingsChanged;

        BuildCategories();
        BuildGallery();
        BuildSaved();

        Loaded += (_, _) => RenderDesignPreview();
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(() => OnSettingsChanged(sender, e)); return; }
        RenderDesignPreview();
    }

    // ===== window chrome =====

    private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void CloseToTray_Click(object sender, RoutedEventArgs e) => Hide();

    protected override void OnClosing(CancelEventArgs e)
    {
        // Closing keeps the app alive in the tray unless we're really exiting.
        if (!App.IsExiting)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        _settings.PropertyChanged -= OnSettingsChanged;
        base.OnClosing(e);
    }

    // ===== navigation =====

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (ViewBrowse == null) return; // during init
        string tag = (string)((FrameworkElement)sender).Tag;
        ViewBrowse.Visibility = tag == "Browse" ? Visibility.Visible : Visibility.Collapsed;
        ViewDesign.Visibility = tag == "Design" ? Visibility.Visible : Visibility.Collapsed;
        ViewSaved.Visibility = tag == "Saved" ? Visibility.Visible : Visibility.Collapsed;
        ViewSettings.Visibility = tag == "Settings" ? Visibility.Visible : Visibility.Collapsed;
        if (tag == "Design") RenderDesignPreview();
        if (tag == "Saved") BuildSaved();
    }

    public void GoToDesign()
    {
        TabDesign.IsChecked = true;
    }

    // ===== browse: sidebar =====

    private void BuildCategories()
    {
        CategoryList.Children.Clear();
        AddCategory("All Crosshairs", CrosshairPresets.All.Count, isFirst: true);
        foreach (var cat in CrosshairPresets.Categories)
        {
            int count = CrosshairPresets.All.Count(p => p.Category == cat);
            AddCategory(cat, count);
        }
    }

    private void AddCategory(string name, int count, bool isFirst = false)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var label = new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(label, 0);

        var badge = new Border
        {
            Background = (Brush)FindResource("BgPanel3"),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 1, 6, 1),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = count.ToString(), FontSize = 11, Foreground = (Brush)FindResource("TextDim") },
        };
        Grid.SetColumn(badge, 1);

        grid.Children.Add(label);
        grid.Children.Add(badge);

        var rb = new RadioButton
        {
            Style = (Style)FindResource("CategoryItem"),
            GroupName = "cat",
            Content = grid,
            Tag = name,
            IsChecked = isFirst,
        };
        rb.Checked += (_, _) => { _selectedCategory = name; BuildGallery(); };
        CategoryList.Children.Add(rb);
    }

    // ===== browse: gallery =====

    private void Search_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (SearchPlaceholder != null)
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        BuildGallery();
    }

    private void BuildGallery()
    {
        if (GalleryPanel == null) return;
        GalleryPanel.Children.Clear();
        string query = SearchBox?.Text?.Trim() ?? string.Empty;

        IEnumerable<string> cats = _selectedCategory == "All Crosshairs"
            ? CrosshairPresets.Categories
            : new[] { _selectedCategory };

        foreach (var cat in cats)
        {
            var items = CrosshairPresets.All
                .Where(p => p.Category == cat)
                .Where(p => query.Length == 0 || p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (items.Count == 0) continue;

            var header = new TextBlock { Margin = new Thickness(2, 14, 0, 8) };
            header.Inlines.Add(new System.Windows.Documents.Run(cat.ToUpperInvariant())
            { FontWeight = FontWeights.Bold, FontSize = 15 });
            header.Inlines.Add(new System.Windows.Documents.Run($"   {items.Count} STYLES")
            { Foreground = (Brush)FindResource("TextDim"), FontSize = 11 });
            GalleryPanel.Children.Add(header);

            var wrap = new WrapPanel();
            foreach (var preset in items)
                wrap.Children.Add(BuildThumb(preset));
            GalleryPanel.Children.Add(wrap);
        }
    }

    private Border BuildThumb(CrosshairPreset preset)
    {
        var canvas = new Canvas { Width = 150, Height = 84, ClipToBounds = true };
        CrosshairRenderer.Render(canvas, preset.Settings, 75, 42);

        var name = new TextBlock
        {
            Text = preset.Name,
            Foreground = (Brush)FindResource("TextPrimary"),
            FontSize = 12,
            Margin = new Thickness(8, 4, 8, 8),
        };

        var stack = new StackPanel();
        stack.Children.Add(canvas);
        stack.Children.Add(name);

        var border = new Border
        {
            Width = 150,
            Background = (Brush)FindResource("BgPanel2"),
            BorderBrush = (Brush)FindResource("BorderBrushX"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 12, 12),
            Cursor = Cursors.Hand,
            Tag = preset,
            Child = stack,
        };
        border.MouseLeftButtonUp += (_, _) => SelectPreset(preset, border);
        return border;
    }

    private void SelectPreset(CrosshairPreset preset, Border thumb)
    {
        _selectedPreset = preset;

        if (_selectedThumb != null)
            _selectedThumb.BorderBrush = (Brush)FindResource("BorderBrushX");
        thumb.BorderBrush = (Brush)FindResource("Accent");
        _selectedThumb = thumb;

        CrosshairRenderer.Render(BrowsePreview, preset.Settings, 28, 28);
        BrowseSelName.Text = $"{preset.Name}  ·  {preset.Category}";
        BrowseSelName.Foreground = (Brush)FindResource("TextPrimary");
        UseBtn.IsEnabled = true;
    }

    private void UseCrosshair_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPreset == null) return;
        _settings.CopyFrom(_selectedPreset.Settings);
        _settings.OverlayEnabled = true;
    }

    // ===== design =====

    private void DesignPreview_SizeChanged(object sender, SizeChangedEventArgs e) => RenderDesignPreview();

    private void RenderDesignPreview()
    {
        if (DesignPreview == null) return;
        double w = DesignPreview.ActualWidth, h = DesignPreview.ActualHeight;
        if (w <= 0 || h <= 0) return;
        CrosshairRenderer.Render(DesignPreview, _settings, w / 2.0, h / 2.0);
    }

    private void Thin_Click(object sender, RoutedEventArgs e) => _settings.Thickness = 1.5;
    private void Thick_Click(object sender, RoutedEventArgs e) => _settings.Thickness = 5;

    private void Smaller_Click(object sender, RoutedEventArgs e)
    {
        _settings.Size = Math.Max(0, _settings.Size * 0.82);
        _settings.Gap = Math.Max(0, _settings.Gap * 0.82);
        _settings.DotSize = Math.Max(0.5, _settings.DotSize * 0.82);
        _settings.CircleRadius = Math.Max(2, _settings.CircleRadius * 0.82);
    }

    private void Bigger_Click(object sender, RoutedEventArgs e)
    {
        _settings.Size = Math.Min(60, _settings.Size * 1.2 + 1);
        _settings.Gap = Math.Min(40, _settings.Gap * 1.2);
        _settings.DotSize = Math.Min(20, _settings.DotSize * 1.2);
        _settings.CircleRadius = Math.Min(80, _settings.CircleRadius * 1.2);
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string tag)
        {
            var p = tag.Split(',');
            if (p.Length == 3 && byte.TryParse(p[0], out var r) && byte.TryParse(p[1], out var g) && byte.TryParse(p[2], out var bl))
            {
                _settings.R = r; _settings.G = g; _settings.B = bl;
            }
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e) => _settings.CopyFrom(new CrosshairSettings());

    // ===== saved =====

    private void SaveCurrent_Click(object sender, RoutedEventArgs e)
    {
        var clone = _settings.Clone();
        if (string.IsNullOrWhiteSpace(clone.Name) || clone.Name == "Custom")
            clone.Name = $"My {_settings.Style} {_saved.Count + 1}";
        _saved.Add(clone);
        SavedStore.Save(_saved);
        BuildSaved();
        TabSaved.IsChecked = true;
    }

    private void BuildSaved()
    {
        if (SavedPanel == null) return;
        SavedPanel.Children.Clear();
        SavedEmpty.Visibility = _saved.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (var item in _saved.ToList())
            SavedPanel.Children.Add(BuildSavedCard(item));
    }

    private Border BuildSavedCard(CrosshairSettings item)
    {
        var canvas = new Canvas { Width = 150, Height = 84, ClipToBounds = true };
        CrosshairRenderer.Render(canvas, item, 75, 42);

        var name = new TextBlock
        {
            Text = item.Name,
            Foreground = (Brush)FindResource("TextPrimary"),
            FontSize = 12,
            Margin = new Thickness(8, 4, 8, 4),
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        var loadBtn = new Button { Content = "Load", Style = (Style)FindResource("GhostButton"), Margin = new Thickness(0, 0, 6, 0) };
        loadBtn.Click += (_, _) => { _settings.CopyFrom(item); _settings.OverlayEnabled = true; };
        var delBtn = new Button { Content = "Delete", Style = (Style)FindResource("GhostButton") };
        delBtn.Click += (_, _) => { _saved.Remove(item); SavedStore.Save(_saved); BuildSaved(); };

        var btns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 8, 8) };
        btns.Children.Add(loadBtn);
        btns.Children.Add(delBtn);

        var stack = new StackPanel();
        stack.Children.Add(canvas);
        stack.Children.Add(name);
        stack.Children.Add(btns);

        return new Border
        {
            Width = 150,
            Background = (Brush)FindResource("BgPanel2"),
            BorderBrush = (Brush)FindResource("BorderBrushX"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 12, 12),
            Child = stack,
        };
    }
}

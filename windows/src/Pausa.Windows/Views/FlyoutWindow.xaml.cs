using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Pausa.Core;
using Pausa.Windows.Interop;
using static Pausa.Windows.Interop.Native;

namespace Pausa.Windows.Views;

/// <summary>The left-click flyout: every setting from the macOS menu, laid out as a Windows 11 panel.</summary>
public partial class FlyoutWindow : Window
{
    private readonly PausaController _controller;
    private readonly List<(FlashMode Mode, RadioButton Card, ModePreview Preview)> _modeCards = [];
    private readonly List<(FlashColor Color, RadioButton Swatch)> _swatches = [];
    private bool _syncing;
    private bool _allowClose;

    internal FlyoutWindow(PausaController controller)
    {
        _controller = controller;
        InitializeComponent();
        BuildModeCards();
        BuildSwatches();

        SyncSettings();
        SyncStatus();
        SyncUpdate();
        FirstRunHint.Visibility = controller.IsFirstRun ? Visibility.Visible : Visibility.Collapsed;

        controller.StatusChanged += SyncStatus;
        controller.SettingsChanged += SyncSettings;
        controller.UpdateChanged += SyncUpdate;

        SourceInitialized += (_, _) => SetDwmInt(new WindowInteropHelper(this).Handle, DWMWA_WINDOW_CORNER_PREFERENCE, DWMWCP_ROUND);
        Deactivated += (_, _) =>
        {
            if (!IsVisible) return;
            Hide();
            _controller.FlyoutHiddenUtc = DateTime.UtcNow;
        };
        IsVisibleChanged += (_, _) =>
        {
            if (!IsVisible) _controller.CancelHoverPreview();
        };
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape) return;
            Hide();
            e.Handled = true;
        };
    }

    // ---- Building ----

    private void BuildModeCards()
    {
        foreach (var mode in FlashModes.All)
        {
            var preview = new ModePreview { Mode = mode, Height = 52, Margin = new Thickness(0, 0, 0, 6) };
            var content = new StackPanel();
            content.Children.Add(preview);
            content.Children.Add(new TextBlock { Text = mode.Title(), FontSize = 13, FontWeight = FontWeights.SemiBold });
            content.Children.Add(new TextBlock { Text = mode.Description(), Style = (Style)FindResource("CaptionStyle") });

            var card = new RadioButton
            {
                GroupName = "FlashMode",
                Style = (Style)FindResource("ModeCardStyle"),
                Margin = new Thickness(4, 0, 4, 0),
                Content = content,
            };
            AutomationProperties.SetName(card, $"{mode.Title()}: {mode.Description()}");
            card.Checked += (_, _) =>
            {
                if (!_syncing) _controller.SetMode(mode);
            };
            card.MouseEnter += (_, _) => _controller.HoverPreview(mode: mode);
            card.MouseLeave += (_, _) => _controller.CancelHoverPreview();

            _modeCards.Add((mode, card, preview));
            ModeCards.Children.Add(card);
        }
    }

    private void BuildSwatches()
    {
        foreach (var color in FlashColor.All)
        {
            var brush = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
            brush.Freeze();
            var swatch = new RadioButton
            {
                GroupName = "FlashColor",
                Style = (Style)FindResource("SwatchStyle"),
                Background = brush,
                ToolTip = color.Name,
                Margin = new Thickness(0, 0, 4, 0),
            };
            AutomationProperties.SetName(swatch, color.Name);
            swatch.Checked += (_, _) =>
            {
                if (!_syncing) _controller.SetColor(color);
            };
            swatch.MouseEnter += (_, _) => _controller.HoverPreview(color: color);
            swatch.MouseLeave += (_, _) => _controller.CancelHoverPreview();

            _swatches.Add((color, swatch));
            Swatches.Children.Add(swatch);
        }
    }

    // ---- Syncing from the controller ----

    private void SyncSettings()
    {
        _syncing = true;
        try
        {
            var s = _controller.Settings;

            var interval = Steps.NearestIndex(Steps.IntervalSeconds, s.IntervalSeconds);
            IntervalSlider.Value = interval;
            IntervalValue.Text = PausaTimer.FormatInterval(Steps.IntervalSeconds[interval]);

            var intensity = Steps.NearestIndex(Steps.Intensity, s.Intensity);
            IntensitySlider.Value = intensity;
            IntensityValue.Text = Steps.Intensity[intensity].Name;

            var duration = Steps.NearestIndex(Steps.Duration, s.DurationSeconds);
            DurationSlider.Value = duration;
            DurationValue.Text = Steps.Duration[duration].Name;

            var color = FlashColor.FromName(s.FlashColor);
            var glow = Color.FromRgb(color.R, color.G, color.B);
            foreach (var (mode, card, preview) in _modeCards)
            {
                card.IsChecked = mode == s.FlashMode;
                preview.GlowColor = glow;
            }
            foreach (var (swatchColor, swatch) in _swatches)
            {
                swatch.IsChecked = swatchColor.Name == color.Name;
            }

            ActiveScreenToggle.IsChecked = s.ActiveScreenOnly;
            FullscreenToggle.IsChecked = s.PauseDuringFullscreen;
            StartupToggle.IsChecked = _controller.LaunchAtSignIn;
        }
        finally
        {
            _syncing = false;
        }
    }

    private void SyncStatus()
    {
        StatusText.Text = _controller.StatusLine;
        var paused = _controller.Timer.IsPaused;
        PauseButton.Visibility = paused ? Visibility.Collapsed : Visibility.Visible;
        ResumeButton.Visibility = paused ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SyncUpdate()
    {
        if (_controller.Update is { } update)
        {
            UpdateText.Text = $"Pausa v{update.Version} available";
            UpdateBanner.Visibility = Visibility.Visible;
        }
        else
        {
            UpdateBanner.Visibility = Visibility.Collapsed;
        }
    }

    // ---- Events ----

    private void OnIntervalChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || !IsInitialized) return;
        _controller.SetInterval(Steps.IntervalSeconds[(int)Math.Round(e.NewValue)]);
    }

    private void OnIntensityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || !IsInitialized) return;
        _controller.SetIntensity(Steps.Intensity[(int)Math.Round(e.NewValue)].Value);
    }

    private void OnDurationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing || !IsInitialized) return;
        _controller.SetDuration(Steps.Duration[(int)Math.Round(e.NewValue)].Value);
    }

    private void OnPauseClick(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu { PlacementTarget = PauseButton, Placement = PlacementMode.Bottom };
        foreach (var option in Steps.PauseOptions)
        {
            menu.Items.Add(MenuItem(option.Title, () => _controller.Pause(option.Duration)));
        }
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuItem("Until I resume", _controller.PauseIndefinitely));
        menu.Items.Add(MenuItem("Until next restart", _controller.PauseUntilNextRestart));
        menu.IsOpen = true;
    }

    private static MenuItem MenuItem(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private void OnResumeClick(object sender, RoutedEventArgs e) => _controller.Resume();
    private void OnTestFlashClick(object sender, RoutedEventArgs e) => _controller.TestFlash();
    private void OnUpdateClick(object sender, RoutedEventArgs e) => _controller.OpenUpdate();
    private void OnAboutClick(object sender, RoutedEventArgs e) => _controller.ShowAbout();
    private void OnQuitClick(object sender, RoutedEventArgs e) => _controller.Quit();
    private void OnDismissHint(object sender, RoutedEventArgs e) => FirstRunHint.Visibility = Visibility.Collapsed;
    private void OnActiveScreenClick(object sender, RoutedEventArgs e) => _controller.SetActiveScreenOnly(ActiveScreenToggle.IsChecked == true);
    private void OnFullscreenClick(object sender, RoutedEventArgs e) => _controller.SetPauseDuringFullscreen(FullscreenToggle.IsChecked == true);
    private void OnStartupClick(object sender, RoutedEventArgs e) => _controller.LaunchAtSignIn = StartupToggle.IsChecked == true;

    // ---- Showing ----

    /// <summary>Opens beside the tray icon (or the cursor when the icon is hidden in the overflow).</summary>
    public void ShowAtTray()
    {
        var hwnd = new WindowInteropHelper(this).EnsureHandle();
        var anchor = _controller.Tray?.GetIconRect()?.ToPixelRect() ?? CursorAnchor();
        var monitor = Monitors.FromPoint(anchor.X + anchor.Width / 2, anchor.Y + anchor.Height / 2) ?? Monitors.Primary();
        if (monitor is null)
        {
            Show();
            Activate();
            return;
        }

        MaxHeight = Math.Max(240, monitor.WorkArea.Height / monitor.Scale - 24);

        // Lay out on the target monitor while cloaked so WPF picks up its DPI before we measure.
        SetDwmInt(hwnd, DWMWA_CLOAK, 1);
        if (!IsVisible) Show();
        SetWindowPos(hwnd, IntPtr.Zero, monitor.WorkArea.X, monitor.WorkArea.Y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        UpdateLayout();
        GetWindowRect(hwnd, out var rect);

        var (x, y) = FlyoutPlacement.Compute(anchor, rect.Width, rect.Height, monitor.Bounds, monitor.WorkArea, monitor.Scale);
        SetWindowPos(hwnd, HWND_TOPMOST, x, y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
        SetDwmInt(hwnd, DWMWA_CLOAK, 0);

        Activate();
        SetForegroundWindow(hwnd);
    }

    private static PixelRect CursorAnchor() =>
        GetCursorPos(out var p) ? new PixelRect(p.X, p.Y, 1, 1) : new PixelRect(0, 0, 1, 1);

    internal void CloseForGood()
    {
        _allowClose = true;
        _controller.StatusChanged -= SyncStatus;
        _controller.SettingsChanged -= SyncSettings;
        _controller.UpdateChanged -= SyncUpdate;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Alt+F4 just hides the flyout; the app keeps running in the tray.
        if (!_allowClose)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}

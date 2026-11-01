using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using Pausa.Core;
using Pausa.Windows.Interop;
using Pausa.Windows.Overlay;
using Pausa.Windows.Services;
using Pausa.Windows.Views;

namespace Pausa.Windows;

/// <summary>Command-line driven switches. Smoke tests never touch the network or the registry.</summary>
internal sealed record AppOptions(bool SmokeTest, string SettingsPath)
{
    public bool UseNetwork => !SmokeTest;
    public bool UseRegistry => !SmokeTest;
}

/// <summary>
/// Owns the timer, settings, tray icon, overlay and windows: the Windows counterpart of the
/// macOS AppDelegate. Everything runs on the WPF dispatcher thread.
/// </summary>
internal sealed class PausaController : IDisposable
{
    private static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(24);
    private static readonly TimeSpan HoverPreviewDelay = TimeSpan.FromMilliseconds(180);
    private static readonly TimeSpan IntensityPreviewDelay = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan ClickDebounce = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan ReopenGuard = TimeSpan.FromMilliseconds(300);

    private readonly AppOptions _options;
    private readonly SettingsStore _store;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly DispatcherTimer _secondTimer;
    private readonly DispatcherTimer _countdownTimer;
    private readonly DispatcherTimer _updateTimer;
    private readonly DispatcherTimer _hoverTimer;
    private readonly DispatcherTimer _intensityTimer;
    private readonly TrayGlyph _glyph = new();

    private TrayIcon? _tray;
    private FlyoutWindow? _flyout;
    private AboutWindow? _about;
    private ContextMenu? _menu;
    private Action? _pendingHoverPreview;
    private DateTime _flashEndsUtc;
    private DateTime _lastTrayClickUtc;
    private bool _launchAtSignInFake;

    public PausaController(AppOptions options)
    {
        _options = options;
        _store = new SettingsStore(options.SettingsPath);
        Settings = _store.Load();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd($"Pausa-Windows/{AppInfo.Version}");

        Timer = new PausaTimer { IntervalSeconds = Settings.IntervalSeconds };
        Timer.Ticked += RaiseStatusChanged;
        Timer.PauseStateChanged += RaiseStatusChanged;
        Timer.Fired += OnTimerFired;

        Overlay = new FlashOverlay();
        Overlay.FlashStarted += OnFlashStarted;
        Overlay.FlashEnded += OnFlashEnded;

        _secondTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(1) };
        _secondTimer.Tick += (_, _) => Timer.Tick();
        _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _countdownTimer.Tick += (_, _) => RaiseStatusChanged();
        _updateTimer = new DispatcherTimer { Interval = UpdateCheckInterval };
        _updateTimer.Tick += async (_, _) => await CheckForUpdateAsync();
        _hoverTimer = new DispatcherTimer { Interval = HoverPreviewDelay };
        _hoverTimer.Tick += (_, _) =>
        {
            _hoverTimer.Stop();
            _pendingHoverPreview?.Invoke();
            _pendingHoverPreview = null;
        };
        _intensityTimer = new DispatcherTimer { Interval = IntensityPreviewDelay };
        _intensityTimer.Tick += (_, _) =>
        {
            _intensityTimer.Stop();
            Overlay.Preview(CurrentStyle, pulses: 4);
        };
    }

    public PausaSettings Settings { get; }
    public PausaTimer Timer { get; }
    public FlashOverlay Overlay { get; }
    public AvailableUpdate? Update { get; private set; }
    public TrayIcon? Tray => _tray;
    public FlyoutWindow? Flyout => _flyout;
    public bool IsFirstRun { get; private set; }
    public DateTime FlyoutHiddenUtc { get; set; }

    /// <summary>Raised whenever the status line or pause state may have changed.</summary>
    public event Action? StatusChanged;
    public event Action? UpdateChanged;
    public event Action? SettingsChanged;

    public FlashStyle CurrentStyle => new(Settings.FlashMode, FlashColor.FromName(Settings.FlashColor), Settings.Intensity, Settings.ActiveScreenOnly);

    public bool IsFlashing => Overlay.IsFlashing;

    public int FlashSecondsRemaining =>
        Math.Max(0, (int)Math.Ceiling((_flashEndsUtc - DateTime.UtcNow).TotalSeconds));

    public void Start()
    {
        IsFirstRun = !Settings.HasLaunchedBefore;
        if (IsFirstRun)
        {
            Settings.HasLaunchedBefore = true;
            Save();
        }

        _tray = new TrayIcon();
        _tray.LeftClicked += OnTrayLeftClicked;
        _tray.RightClicked += (_, _) => ShowContextMenu();
        _tray.ActivateRequested += ShowFlyout;
        _tray.SessionLocked += OnSessionLocked;
        _tray.SessionUnlocked += Timer.HandleScreenUnlocked;
        _tray.DisplaySettingsChanged += OnDisplayChanged;
        _tray.ThemeChanged += RefreshTrayIcon;
        _tray.TaskbarRecreated += () =>
        {
            _iconKey = null;
            _tray.Show(CurrentIcon(), TooltipText);
        };
        _tray.Show(CurrentIcon(), TooltipText);

        if (_options.UseRegistry) StartupManager.RefreshPathIfEnabled();

        _secondTimer.Start();

        if (_options.UseNetwork)
        {
            if (Settings.ShareUsage) _ = Telemetry.SendLaunchAsync(_http, AppInfo.Version, _store.EnsureDeviceId(Settings));
            _updateTimer.Start();
        }
        _ = CheckForUpdateAsync();

        if (IsFirstRun && !_options.SmokeTest) ShowFlyout();
    }

    // ---- Timer and flashing ----

    private void OnTimerFired()
    {
        if (Settings.PauseDuringFullscreen && FullscreenDetector.IsFullscreenAppActive()) return;
        Overlay.Flash(CurrentStyle, Settings.DurationSeconds);
    }

    public void TestFlash()
    {
        _intensityTimer.Stop();
        CancelHoverPreview();
        Overlay.Flash(CurrentStyle, Settings.DurationSeconds);
    }

    private void OnFlashStarted()
    {
        _flashEndsUtc = DateTime.UtcNow.AddSeconds(Settings.DurationSeconds);
        _countdownTimer.Start();
        RefreshTray();
        StatusChanged?.Invoke();
    }

    private void OnFlashEnded()
    {
        _countdownTimer.Stop();
        RefreshTray();
        StatusChanged?.Invoke();
    }

    private void OnSessionLocked()
    {
        Timer.HandleScreenLocked();
        Overlay.Cancel();
    }

    private void OnDisplayChanged()
    {
        // Glow windows are rebuilt per flash, so only the tray glyph needs a new size.
        RefreshTrayIcon();
    }

    // ---- Pause ----

    public void Pause(TimeSpan duration) => Timer.Pause(duration);
    public void PauseIndefinitely() => Timer.PauseIndefinitely();
    public void PauseUntilNextRestart() => Timer.PauseUntilNextRestart();
    public void Resume() => Timer.Resume();

    // ---- Settings ----

    public void SetInterval(int seconds)
    {
        if (Settings.IntervalSeconds == seconds) return;
        Settings.IntervalSeconds = seconds;
        Timer.IntervalSeconds = seconds;
        SaveAndNotify();
    }

    public void SetIntensity(double intensity)
    {
        if (Math.Abs(Settings.Intensity - intensity) < 0.0001) return;
        Settings.Intensity = intensity;
        SaveAndNotify();
        // Like the Mac: drop any running preview right away, then show the new level once the slider settles.
        Overlay.CancelPreview();
        _intensityTimer.Stop();
        _intensityTimer.Start();
    }

    public void SetDuration(double seconds)
    {
        if (Math.Abs(Settings.DurationSeconds - seconds) < 0.0001) return;
        Settings.DurationSeconds = seconds;
        SaveAndNotify();
    }

    public void SetMode(FlashMode mode)
    {
        if (Settings.FlashMode == mode) return;
        Settings.FlashMode = mode;
        SaveAndNotify();
    }

    public void SetColor(FlashColor color)
    {
        if (Settings.FlashColor == color.Name) return;
        Settings.FlashColor = color.Name;
        SaveAndNotify();
    }

    public void SetActiveScreenOnly(bool value)
    {
        Settings.ActiveScreenOnly = value;
        SaveAndNotify();
    }

    public void SetPauseDuringFullscreen(bool value)
    {
        Settings.PauseDuringFullscreen = value;
        SaveAndNotify();
    }

    /// <summary>Off stops the launch count and deletes the install ID.</summary>
    public void SetShareUsage(bool value)
    {
        if (Settings.ShareUsage == value) return;
        Settings.ShareUsage = value;
        if (!value) Settings.DeviceId = null;
        SaveAndNotify();
    }

    public bool LaunchAtSignIn
    {
        get => _options.UseRegistry ? StartupManager.IsEnabled : _launchAtSignInFake;
        set
        {
            if (_options.UseRegistry) StartupManager.SetEnabled(value);
            else _launchAtSignInFake = value;
            SettingsChanged?.Invoke();
        }
    }

    /// <summary>Hovering a mode card or swatch previews it (two pulses) without changing the saved choice.</summary>
    public void HoverPreview(FlashMode? mode = null, FlashColor? color = null)
    {
        if (Overlay.IsFlashing) return;
        var style = CurrentStyle;
        style = style with
        {
            Mode = mode ?? style.Mode,
            Color = color ?? style.Color,
        };
        _pendingHoverPreview = () => Overlay.Preview(style, pulses: 2);
        _hoverTimer.Stop();
        _hoverTimer.Start();
    }

    public void CancelHoverPreview()
    {
        _hoverTimer.Stop();
        _pendingHoverPreview = null;
    }

    private void SaveAndNotify()
    {
        Save();
        SettingsChanged?.Invoke();
    }

    private void Save()
    {
        try
        {
            _store.Save(Settings);
        }
        catch (Exception ex)
        {
            App.Log($"Saving settings failed: {ex.Message}");
        }
    }

    // ---- Updates ----

    private async Task CheckForUpdateAsync()
    {
        // PAUSA_FAKE_UPDATE=1.2 shows the update banner without a network call (for screenshots).
        var fake = Environment.GetEnvironmentVariable("PAUSA_FAKE_UPDATE");
        if (string.IsNullOrWhiteSpace(fake) && !_options.UseNetwork) return;
        var update = !string.IsNullOrWhiteSpace(fake)
            ? new AvailableUpdate(fake.Trim(), Endpoints.ReleasesPage)
            : await UpdateChecker.CheckAsync(_http, AppInfo.Version);
        if (update is null || update == Update) return;
        Update = update;
        UpdateChanged?.Invoke();
    }

    public void OpenUpdate() => OpenUrl((Update?.ReleaseUrl ?? Endpoints.ReleasesPage).ToString());

    public void SendFeedback() => OpenUrl(Endpoints.FeedbackForm(AppInfo.Version).ToString());

    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            App.Log($"Opening {url} failed: {ex.Message}");
        }
    }

    // ---- Tray ----

    public string TooltipText => $"Pausa: {StatusLine}";

    /// <summary>Status shown in the flyout, menu and tooltip; counts down while a break flash runs.</summary>
    public string StatusLine => Overlay.IsFlashing ? $"Look away, {FlashSecondsRemaining}s left" : Timer.StatusText;

    private void RaiseStatusChanged()
    {
        RefreshTray();
        StatusChanged?.Invoke();
    }

    private IntPtr _icon;
    private string? _iconKey;

    /// <summary>The glyph for the current state, re-rendered only when paused/countdown state changes.</summary>
    private IntPtr CurrentIcon()
    {
        var countdown = Overlay.IsFlashing ? FlashSecondsRemaining.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
        var key = $"{Timer.IsPaused}|{countdown}";
        if (key != _iconKey || _icon == IntPtr.Zero)
        {
            _icon = _glyph.Create(Timer.IsPaused, countdown);
            _iconKey = key;
        }
        return _icon;
    }

    private void RefreshTray() => _tray?.Update(CurrentIcon(), TooltipText);

    private void RefreshTrayIcon()
    {
        _iconKey = null;
        RefreshTray();
    }

    private void OnTrayLeftClicked()
    {
        var now = DateTime.UtcNow;
        if (now - _lastTrayClickUtc < ClickDebounce) return;
        _lastTrayClickUtc = now;

        if (_flyout is { IsVisible: true })
        {
            _flyout.Hide();
            return;
        }
        // Clicking the icon deactivates an open flyout first; don't bounce it straight back open.
        if (now - FlyoutHiddenUtc < ReopenGuard) return;
        ShowFlyout();
    }

    public void ShowFlyout()
    {
        _menu?.SetCurrentValue(ContextMenu.IsOpenProperty, false);
        _flyout ??= new FlyoutWindow(this);
        _flyout.ShowAtTray();
    }

    public void HideFlyout() => _flyout?.Hide();

    public FlyoutWindow EnsureFlyout() => _flyout ??= new FlyoutWindow(this);

    private void ShowContextMenu()
    {
        if (_tray is null) return;
        _flyout?.Hide();
        _menu = TrayMenu.Build(this);
        // The menu only dismisses on outside clicks if our process owns the foreground.
        Native.SetForegroundWindow(_tray.Handle);
        _menu.Opened += (_, _) =>
        {
            if (PresentationSource.FromVisual(_menu) is HwndSource source) Native.SetForegroundWindow(source.Handle);
        };
        _menu.IsOpen = true;
    }

    public void ShowAbout()
    {
        _flyout?.Hide();
        if (_about is null)
        {
            _about = new AboutWindow();
            _about.Closed += (_, _) => _about = null;
            _about.Show();
        }
        _about.Activate();
    }

    public AboutWindow CreateAboutWindowForCapture() => new();

    public void Quit()
    {
        Overlay.Cancel();
        Application.Current.Shutdown();
    }

    public void Dispose()
    {
        _secondTimer.Stop();
        _countdownTimer.Stop();
        _updateTimer.Stop();
        _hoverTimer.Stop();
        _intensityTimer.Stop();
        Overlay.Cancel();
        _flyout?.CloseForGood();
        _about?.Close();
        _tray?.Dispose();
        _glyph.Dispose();
        _http.Dispose();
    }
}

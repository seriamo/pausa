using System.Diagnostics;
using System.Windows.Threading;
using Pausa.Core;
using Pausa.Windows.Interop;

namespace Pausa.Windows.Overlay;

public sealed record FlashStyle(FlashMode Mode, FlashColor Color, double Intensity, bool ActiveScreenOnly);

/// <summary>Builds glow windows on demand and animates their opacity. UI thread only.</summary>
internal sealed class FlashOverlay
{
    private readonly DispatcherTimer _frameTimer;
    private readonly Stopwatch _clock = new();
    private readonly List<LayeredStripWindow> _windows = [];
    private FlashAnimation? _animation;

    public FlashOverlay()
    {
        _frameTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(15) };
        _frameTimer.Tick += (_, _) => RenderFrame();
    }

    public event Action? FlashStarted;
    public event Action? FlashEnded;

    public bool IsFlashing => _animation is { IsPreview: false };
    public int LastWindowCount { get; private set; }

    public void Flash(FlashStyle style, double durationSeconds)
    {
        Start(style, FlashAnimation.Flash(durationSeconds, style.Intensity));
        FlashStarted?.Invoke();
    }

    public void Preview(FlashStyle style, int pulses = 2) =>
        Start(style, FlashAnimation.Preview(pulses, style.Intensity));

    /// <summary>Stops a running preview immediately. Real flashes keep going.</summary>
    public void CancelPreview()
    {
        if (_animation is { IsPreview: true }) Stop();
    }

    public void Cancel() => Stop();

    private void Start(FlashStyle style, FlashAnimation animation)
    {
        Stop();
        foreach (var monitor in TargetMonitors(style.ActiveScreenOnly))
        {
            foreach (var strip in OverlayLayout.Strips(style.Mode, monitor.Bounds, monitor.Scale))
            {
                _windows.Add(new LayeredStripWindow(strip, style.Color));
            }
        }
        LastWindowCount = _windows.Count;
        foreach (var window in _windows) window.Show();

        _animation = animation;
        _clock.Restart();
        _frameTimer.Start();
    }

    private void RenderFrame()
    {
        if (_animation?.AlphaAt(_clock.Elapsed) is { } alpha)
        {
            foreach (var window in _windows) window.SetOpacity(alpha);
            return;
        }
        Stop();
    }

    private void Stop()
    {
        _frameTimer.Stop();
        _clock.Reset();
        foreach (var window in _windows) window.Dispose();
        _windows.Clear();

        var wasFlash = _animation is { IsPreview: false };
        _animation = null;
        if (wasFlash) FlashEnded?.Invoke();
    }

    private static IEnumerable<MonitorInfo> TargetMonitors(bool activeScreenOnly)
    {
        if (activeScreenOnly && Monitors.UnderCursor() is { } active) return [active];
        return Monitors.All();
    }
}

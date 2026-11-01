namespace Pausa.Core;

/// <summary>
/// Countdown and pause state. A straight port of PausaTimer.swift: the host calls
/// <see cref="Tick"/> once per second on the UI thread.
/// </summary>
public sealed class PausaTimer(TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private int _intervalSeconds = 20 * 60;

    public int IntervalSeconds
    {
        get => _intervalSeconds;
        set
        {
            _intervalSeconds = value;
            Reset();
        }
    }

    public int SecondsRemaining { get; private set; } = 20 * 60;
    public bool IsPaused { get; private set; }
    public bool IsScreenLocked { get; private set; }

    /// <summary>If set, auto-resume once the wall clock passes this instant.</summary>
    public DateTimeOffset? PauseUntil { get; private set; }

    /// <summary>Paused until the app restarts (no auto-resume).</summary>
    public bool PauseUntilRestart { get; private set; }

    public event Action? Ticked;
    public event Action? Fired;
    public event Action? PauseStateChanged;

    public void Reset()
    {
        SecondsRemaining = _intervalSeconds;
        Ticked?.Invoke();
    }

    public void HandleScreenLocked() => IsScreenLocked = true;

    public void HandleScreenUnlocked()
    {
        IsScreenLocked = false;
        Reset();
    }

    public void Pause(TimeSpan duration)
    {
        IsPaused = true;
        PauseUntil = _time.GetUtcNow() + duration;
        PauseUntilRestart = false;
        PauseStateChanged?.Invoke();
        Ticked?.Invoke();
    }

    public void PauseIndefinitely()
    {
        IsPaused = true;
        PauseUntil = null;
        PauseUntilRestart = false;
        PauseStateChanged?.Invoke();
        Ticked?.Invoke();
    }

    public void PauseUntilNextRestart()
    {
        IsPaused = true;
        PauseUntil = null;
        PauseUntilRestart = true;
        PauseStateChanged?.Invoke();
        Ticked?.Invoke();
    }

    public void Resume()
    {
        IsPaused = false;
        PauseUntil = null;
        PauseUntilRestart = false;
        Reset();
        PauseStateChanged?.Invoke();
    }

    public void Tick()
    {
        if (PauseUntil is { } until && _time.GetUtcNow() >= until)
        {
            PauseUntil = null;
            IsPaused = false;
            PauseStateChanged?.Invoke();
        }

        // Keep ticking while a timed pause runs so "resumes in X:XX" counts down live.
        if (IsPaused)
        {
            if (PauseUntil is not null) Ticked?.Invoke();
            return;
        }
        if (IsScreenLocked) return;

        SecondsRemaining -= 1;
        Ticked?.Invoke();

        if (SecondsRemaining <= 0)
        {
            Fired?.Invoke();
            Reset();
        }
    }

    public string FormattedTimeRemaining => FormatClock(SecondsRemaining);

    /// <summary>The one-line status shown in the flyout, tray menu and tooltip.</summary>
    public string StatusText
    {
        get
        {
            if (!IsPaused) return $"Next Pausa in {FormattedTimeRemaining}";
            if (PauseUntil is { } until)
            {
                // Round up so a 10-minute pause reads 10:00 at t=0, matching the active countdown.
                var remaining = Math.Max(0, (int)Math.Ceiling((until - _time.GetUtcNow()).TotalSeconds));
                return $"Paused, resumes in {FormatClock(remaining)}";
            }
            return PauseUntilRestart ? "Paused until restart" : "Paused";
        }
    }

    public static string FormatClock(int seconds) => $"{seconds / 60}:{seconds % 60:00}";

    public static string FormatInterval(int seconds) => seconds < 60 ? $"{seconds}s" : $"{seconds / 60} min";
}

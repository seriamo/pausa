namespace Pausa.Core.Tests;

public class PausaTimerTests
{
    private static (PausaTimer timer, FakeTime time) Create(int interval = 5)
    {
        var time = new FakeTime(new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero));
        var timer = new PausaTimer(time) { IntervalSeconds = interval };
        return (timer, time);
    }

    [Fact]
    public void Defaults_match_macos()
    {
        var timer = new PausaTimer();
        Assert.Equal(1200, timer.IntervalSeconds);
        Assert.Equal(1200, timer.SecondsRemaining);
        Assert.Equal("Next Pausa in 20:00", timer.StatusText);
    }

    [Fact]
    public void Fires_after_interval_then_resets()
    {
        var (timer, _) = Create(3);
        var fired = 0;
        timer.Fired += () => fired++;

        timer.Tick();
        timer.Tick();
        Assert.Equal(0, fired);
        Assert.Equal(1, timer.SecondsRemaining);

        timer.Tick();
        Assert.Equal(1, fired);
        Assert.Equal(3, timer.SecondsRemaining);
    }

    [Fact]
    public void Setting_interval_resets_countdown()
    {
        var (timer, _) = Create(10);
        timer.Tick();
        timer.Tick();
        timer.IntervalSeconds = 600;
        Assert.Equal(600, timer.SecondsRemaining);
    }

    [Fact]
    public void Locked_screen_freezes_and_unlock_restarts()
    {
        var (timer, _) = Create(10);
        timer.Tick();
        timer.HandleScreenLocked();
        timer.Tick();
        timer.Tick();
        Assert.Equal(9, timer.SecondsRemaining);

        timer.HandleScreenUnlocked();
        Assert.Equal(10, timer.SecondsRemaining);
        timer.Tick();
        Assert.Equal(9, timer.SecondsRemaining);
    }

    [Fact]
    public void Timed_pause_counts_down_and_auto_resumes()
    {
        var (timer, time) = Create(10);
        var ticks = 0;
        var changes = 0;
        timer.Ticked += () => ticks++;
        timer.PauseStateChanged += () => changes++;

        timer.Pause(TimeSpan.FromMinutes(10));
        Assert.True(timer.IsPaused);
        Assert.Equal("Paused, resumes in 10:00", timer.StatusText);
        Assert.Equal(1, changes);

        ticks = 0;
        time.Advance(TimeSpan.FromSeconds(0.4));
        timer.Tick();
        Assert.Equal(1, ticks);
        Assert.Equal(10, timer.SecondsRemaining);
        Assert.Equal("Paused, resumes in 10:00", timer.StatusText);

        time.Advance(TimeSpan.FromMinutes(10));
        timer.Tick();
        Assert.False(timer.IsPaused);
        Assert.Equal(2, changes);
        Assert.Equal(9, timer.SecondsRemaining);
    }

    [Fact]
    public void Indefinite_pause_is_silent_until_resumed()
    {
        var (timer, _) = Create(10);
        timer.PauseIndefinitely();
        var ticks = 0;
        timer.Ticked += () => ticks++;

        timer.Tick();
        timer.Tick();
        Assert.Equal(0, ticks);
        Assert.Equal("Paused", timer.StatusText);

        timer.Resume();
        Assert.False(timer.IsPaused);
        Assert.Equal(10, timer.SecondsRemaining);
    }

    [Fact]
    public void Pause_until_restart_has_own_status()
    {
        var (timer, _) = Create();
        timer.PauseUntilNextRestart();
        Assert.True(timer.PauseUntilRestart);
        Assert.Equal("Paused until restart", timer.StatusText);
    }

    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(59, "0:59")]
    [InlineData(61, "1:01")]
    [InlineData(1200, "20:00")]
    [InlineData(3600, "60:00")]
    public void Formats_clock(int seconds, string expected) => Assert.Equal(expected, PausaTimer.FormatClock(seconds));

    [Theory]
    [InlineData(300, "5 min")]
    [InlineData(3600, "60 min")]
    [InlineData(30, "30s")]
    public void Formats_interval(int seconds, string expected) => Assert.Equal(expected, PausaTimer.FormatInterval(seconds));
}

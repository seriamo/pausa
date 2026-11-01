namespace Pausa.Core.Tests;

public class FlashAnimationTests
{
    [Theory]
    [InlineData(1, 3)]
    [InlineData(3, 4)]
    [InlineData(10, 15)]
    [InlineData(20, 30)]
    public void Pulse_count_matches_macos(double duration, int expected) =>
        Assert.Equal(expected, FlashAnimation.FlashPulseCount(duration));

    [Fact]
    public void Peaks_fade_by_thirty_percent()
    {
        var anim = FlashAnimation.Flash(20, 0.4);
        Assert.Equal(30, anim.PulseCount);
        Assert.Equal(0.4, anim.PeakAlpha(0), 6);
        Assert.Equal(0.4 * (1 - 0.3 * 29 / 30), anim.PeakAlpha(29), 6);
        Assert.Equal(TimeSpan.FromSeconds(30 * 0.65).TotalSeconds, anim.TotalDuration.TotalSeconds, 6);
    }

    [Fact]
    public void Alpha_rises_to_peak_and_falls_to_zero()
    {
        var anim = FlashAnimation.Flash(5, 0.7);
        Assert.Equal(0, anim.AlphaAt(TimeSpan.Zero)!.Value, 6);
        Assert.Equal(0.7, anim.AlphaAt(TimeSpan.FromSeconds(0.30))!.Value, 6);
        Assert.Equal(0.35, anim.AlphaAt(TimeSpan.FromSeconds(0.30 + 0.175))!.Value, 6);
        Assert.Equal(0, anim.AlphaAt(TimeSpan.FromSeconds(0.6499999))!.Value, 4);
        Assert.Null(anim.AlphaAt(anim.TotalDuration + TimeSpan.FromMilliseconds(1)));
    }

    [Fact]
    public void Alpha_never_exceeds_intensity()
    {
        var anim = FlashAnimation.Flash(12, 0.4);
        for (var ms = 0; ms < anim.TotalDuration.TotalMilliseconds; ms += 7)
        {
            var a = anim.AlphaAt(TimeSpan.FromMilliseconds(ms));
            Assert.NotNull(a);
            Assert.InRange(a!.Value, 0, 0.4 + 1e-9);
        }
    }

    [Fact]
    public void Preview_is_faster_and_full_strength()
    {
        var anim = FlashAnimation.Preview(4, 0.15);
        Assert.True(anim.IsPreview);
        Assert.Equal(4, anim.PulseCount);
        Assert.Equal(2.0, anim.TotalDuration.TotalSeconds, 6);
        Assert.Equal(0.15, anim.PeakAlpha(3), 6);
        Assert.Equal(0.15, anim.AlphaAt(TimeSpan.FromSeconds(0.2))!.Value, 6);
    }
}

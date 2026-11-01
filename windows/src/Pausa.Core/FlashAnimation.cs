namespace Pausa.Core;

/// <summary>
/// Opacity over time for a flash or a settings preview. Same rhythm as the macOS app:
/// a real flash pulses (0.30s in, 0.35s out) with each peak fading slightly; a preview
/// pulses faster (0.2s in, 0.3s out) at full intensity.
/// </summary>
public sealed class FlashAnimation
{
    public const double FlashRiseSeconds = 0.30;
    public const double FlashFallSeconds = 0.35;
    public const double PreviewRiseSeconds = 0.20;
    public const double PreviewFallSeconds = 0.30;

    private readonly double _rise;
    private readonly double _fall;
    private readonly double[] _peaks;

    private FlashAnimation(double rise, double fall, double[] peaks)
    {
        _rise = rise;
        _fall = fall;
        _peaks = peaks;
    }

    public bool IsPreview { get; private init; }
    public int PulseCount => _peaks.Length;
    public TimeSpan TotalDuration => TimeSpan.FromSeconds(_peaks.Length * (_rise + _fall));
    public double PeakAlpha(int pulse) => _peaks[pulse];

    public const double FlashPulseSeconds = 0.65;

    public static int FlashPulseCount(double durationSeconds) =>
        Math.Max(3, (int)(durationSeconds / FlashPulseSeconds));

    public static FlashAnimation Flash(double durationSeconds, double intensity)
    {
        var count = FlashPulseCount(durationSeconds);
        var peaks = new double[count];
        for (var i = 0; i < count; i++)
        {
            peaks[i] = intensity * (1.0 - 0.3 * i / count);
        }
        return new FlashAnimation(FlashRiseSeconds, FlashFallSeconds, peaks);
    }

    public static FlashAnimation Preview(int pulses, double intensity)
    {
        var peaks = Enumerable.Repeat(intensity, Math.Max(1, pulses)).ToArray();
        return new FlashAnimation(PreviewRiseSeconds, PreviewFallSeconds, peaks) { IsPreview = true };
    }

    /// <summary>Window opacity (0-1) at <paramref name="elapsed"/>, or null once the animation has finished.</summary>
    public double? AlphaAt(TimeSpan elapsed)
    {
        var t = Math.Max(0, elapsed.TotalSeconds);
        var period = _rise + _fall;
        var index = (int)(t / period);
        if (index >= _peaks.Length) return null;

        var local = t - index * period;
        var peak = _peaks[index];
        return local < _rise
            ? peak * EaseInOut(local / _rise)
            : peak * (1.0 - EaseInOut((local - _rise) / _fall));
    }

    private static double EaseInOut(double x)
    {
        x = Math.Clamp(x, 0, 1);
        return x < 0.5 ? 4 * x * x * x : 1 - Math.Pow(-2 * x + 2, 3) / 2;
    }
}

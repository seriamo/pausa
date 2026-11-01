namespace Pausa.Core;

public readonly record struct NamedStep(string Name, double Value);

public readonly record struct PauseOption(string Title, TimeSpan Duration);

/// <summary>Discrete slider stops and pause choices: the macOS values, with Windows sentence-case labels.</summary>
public static class Steps
{
    public static IReadOnlyList<int> IntervalSeconds { get; } =
        [5 * 60, 10 * 60, 15 * 60, 20 * 60, 30 * 60, 45 * 60, 60 * 60];

    public static IReadOnlyList<NamedStep> Intensity { get; } =
        [new("Soft", 0.15), new("Medium", 0.4), new("Strong", 0.7)];

    public static IReadOnlyList<NamedStep> Duration { get; } =
        [new("3s", 3), new("5s", 5), new("7s", 7), new("10s", 10), new("12s", 12), new("15s", 15), new("20s", 20)];

    public static IReadOnlyList<PauseOption> PauseOptions { get; } =
    [
        new("Pause for 5 minutes", TimeSpan.FromMinutes(5)),
        new("Pause for 10 minutes", TimeSpan.FromMinutes(10)),
        new("Pause for 30 minutes", TimeSpan.FromMinutes(30)),
        new("Pause for 1 hour", TimeSpan.FromHours(1)),
    ];

    public static int NearestIndex(IReadOnlyList<int> steps, int value)
    {
        var best = 0;
        for (var i = 1; i < steps.Count; i++)
        {
            if (Math.Abs(steps[i] - value) < Math.Abs(steps[best] - value)) best = i;
        }
        return best;
    }

    public static int NearestIndex(IReadOnlyList<NamedStep> steps, double value)
    {
        var best = 0;
        for (var i = 1; i < steps.Count; i++)
        {
            if (Math.Abs(steps[i].Value - value) < Math.Abs(steps[best].Value - value)) best = i;
        }
        return best;
    }
}

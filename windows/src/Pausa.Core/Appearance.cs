namespace Pausa.Core;

public enum FlashMode
{
    TopGlow,
    ScreenBorder,
}

public static class FlashModes
{
    public static IReadOnlyList<FlashMode> All { get; } = [FlashMode.TopGlow, FlashMode.ScreenBorder];

    public static string Title(this FlashMode mode) => mode switch
    {
        FlashMode.ScreenBorder => "Screen Border",
        _ => "Top Glow",
    };

    public static string Description(this FlashMode mode) => mode switch
    {
        FlashMode.ScreenBorder => "Gradient glow on all edges",
        _ => "Soft glow along the top edge",
    };
}

/// <summary>Flash colors, matching the macOS palette (NSColor red/orange/yellow/green/systemBlue/purple).</summary>
public sealed record FlashColor(string Name, byte R, byte G, byte B)
{
    public static IReadOnlyList<FlashColor> All { get; } =
    [
        new("Red", 255, 0, 0),
        new("Orange", 255, 128, 0),
        new("Yellow", 255, 255, 0),
        new("Green", 0, 255, 0),
        new("Blue", 0, 122, 255),
        new("Purple", 128, 0, 128),
    ];

    public static FlashColor Default => All[0];

    public static FlashColor FromName(string? name) =>
        All.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)) ?? Default;
}

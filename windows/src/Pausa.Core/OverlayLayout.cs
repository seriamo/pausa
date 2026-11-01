namespace Pausa.Core;

public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;
    public bool Contains(int px, int py) => px >= X && px < Right && py >= Y && py < Bottom;
}

public enum GlowEdge { Top, Bottom, Left, Right }

/// <summary>One overlay window: a gradient strip hugging a monitor edge.</summary>
public readonly record struct OverlayStrip(PixelRect Bounds, GlowEdge Edge);

public static class OverlayLayout
{
    /// <summary>Glow depth in device-independent pixels (same 100pt as macOS).</summary>
    public const double GlowThicknessDips = 100;

    public static int GlowThicknessPixels(double scale) => Math.Max(1, (int)Math.Round(GlowThicknessDips * scale));

    /// <param name="monitor">Monitor bounds in physical pixels.</param>
    /// <param name="scale">Monitor DPI scale (1.0 = 96 DPI).</param>
    public static IReadOnlyList<OverlayStrip> Strips(FlashMode mode, PixelRect monitor, double scale)
    {
        var t = Math.Min(GlowThicknessPixels(scale), Math.Min(monitor.Width, monitor.Height) / 2);
        var top = new OverlayStrip(new PixelRect(monitor.X, monitor.Y, monitor.Width, t), GlowEdge.Top);
        if (mode == FlashMode.TopGlow) return [top];

        return
        [
            top,
            new(new PixelRect(monitor.X, monitor.Bottom - t, monitor.Width, t), GlowEdge.Bottom),
            new(new PixelRect(monitor.X, monitor.Y, t, monitor.Height), GlowEdge.Left),
            new(new PixelRect(monitor.Right - t, monitor.Y, t, monitor.Height), GlowEdge.Right),
        ];
    }

    /// <summary>
    /// Renders a strip as top-down, premultiplied BGRA: full color at the screen edge
    /// fading linearly to transparent, like the macOS GradientBorderView.
    /// </summary>
    public static byte[] RenderStrip(int width, int height, GlowEdge edge, FlashColor color)
    {
        var pixels = new byte[width * height * 4];
        var depth = edge is GlowEdge.Top or GlowEdge.Bottom ? height : width;

        // Precompute one premultiplied pixel per distance-from-edge.
        var ramp = new byte[depth * 4];
        for (var d = 0; d < depth; d++)
        {
            var a = 1.0 - (d + 0.5) / depth;
            ramp[d * 4 + 0] = (byte)Math.Round(color.B * a);
            ramp[d * 4 + 1] = (byte)Math.Round(color.G * a);
            ramp[d * 4 + 2] = (byte)Math.Round(color.R * a);
            ramp[d * 4 + 3] = (byte)Math.Round(255 * a);
        }

        for (var y = 0; y < height; y++)
        {
            var row = y * width * 4;
            for (var x = 0; x < width; x++)
            {
                var d = edge switch
                {
                    GlowEdge.Top => y,
                    GlowEdge.Bottom => height - 1 - y,
                    GlowEdge.Left => x,
                    _ => width - 1 - x,
                };
                Buffer.BlockCopy(ramp, d * 4, pixels, row + x * 4, 4);
            }
        }
        return pixels;
    }
}

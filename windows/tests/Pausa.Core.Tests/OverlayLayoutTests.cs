namespace Pausa.Core.Tests;

public class OverlayLayoutTests
{
    private static readonly PixelRect Monitor = new(-1920, 0, 1920, 1080);

    [Fact]
    public void Top_glow_is_one_strip_along_top()
    {
        var strips = OverlayLayout.Strips(FlashMode.TopGlow, Monitor, 1.0);
        var strip = Assert.Single(strips);
        Assert.Equal(GlowEdge.Top, strip.Edge);
        Assert.Equal(new PixelRect(-1920, 0, 1920, 100), strip.Bounds);
    }

    [Fact]
    public void Screen_border_hugs_all_four_edges_scaled_by_dpi()
    {
        var monitor = new PixelRect(0, 0, 2880, 1800);
        var strips = OverlayLayout.Strips(FlashMode.ScreenBorder, monitor, 1.5);
        Assert.Equal(4, strips.Count);
        Assert.Contains(new OverlayStrip(new PixelRect(0, 0, 2880, 150), GlowEdge.Top), strips);
        Assert.Contains(new OverlayStrip(new PixelRect(0, 1650, 2880, 150), GlowEdge.Bottom), strips);
        Assert.Contains(new OverlayStrip(new PixelRect(0, 0, 150, 1800), GlowEdge.Left), strips);
        Assert.Contains(new OverlayStrip(new PixelRect(2730, 0, 150, 1800), GlowEdge.Right), strips);
    }

    [Fact]
    public void Thickness_never_exceeds_half_a_tiny_monitor()
    {
        var strips = OverlayLayout.Strips(FlashMode.ScreenBorder, new PixelRect(0, 0, 300, 120), 2.0);
        Assert.All(strips, s => Assert.True(Math.Min(s.Bounds.Width, s.Bounds.Height) <= 60));
    }

    [Fact]
    public void Gradient_is_opaque_at_edge_and_clear_inside_premultiplied()
    {
        var orange = FlashColor.FromName("Orange");
        const int w = 4, h = 100;
        var px = OverlayLayout.RenderStrip(w, h, GlowEdge.Top, orange);
        Assert.Equal(w * h * 4, px.Length);

        // Row 0, BGRA.
        Assert.Equal(254, px[3]);
        Assert.Equal(254, px[2]);
        Assert.Equal(127, px[1]);
        Assert.Equal(0, px[0]);

        var last = (h - 1) * w * 4;
        Assert.Equal(1, px[last + 3]);

        for (var i = 0; i < px.Length; i += 4)
        {
            Assert.True(px[i] <= px[i + 3] && px[i + 1] <= px[i + 3] && px[i + 2] <= px[i + 3]);
        }
    }

    [Theory]
    [InlineData(GlowEdge.Bottom)]
    [InlineData(GlowEdge.Left)]
    [InlineData(GlowEdge.Right)]
    public void Each_edge_is_brightest_at_its_side(GlowEdge edge)
    {
        const int w = 50, h = 50;
        var px = OverlayLayout.RenderStrip(w, h, edge, FlashColor.Default);
        int Alpha(int x, int y) => px[(y * w + x) * 4 + 3];
        var (edgeAlpha, innerAlpha) = edge switch
        {
            GlowEdge.Bottom => (Alpha(25, h - 1), Alpha(25, 0)),
            GlowEdge.Left => (Alpha(0, 25), Alpha(w - 1, 25)),
            _ => (Alpha(w - 1, 25), Alpha(0, 25)),
        };
        Assert.True(edgeAlpha > 240);
        Assert.True(innerAlpha < 10);
    }

    [Fact]
    public void Pixel_rect_contains_is_half_open()
    {
        Assert.True(Monitor.Contains(-1920, 0));
        Assert.True(Monitor.Contains(-1, 1079));
        Assert.False(Monitor.Contains(0, 0));
    }
}

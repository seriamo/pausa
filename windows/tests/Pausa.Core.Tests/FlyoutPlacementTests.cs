using Pausa.Core;

namespace Pausa.Core.Tests;

public class FlyoutPlacementTests
{
    private static readonly PixelRect Monitor = new(0, 0, 1920, 1080);

    [Fact]
    public void BottomTaskbar_OpensAboveTaskbarCenteredOnIconButInsideWorkArea()
    {
        var work = new PixelRect(0, 0, 1920, 1032);
        var icon = new PixelRect(1700, 1040, 24, 24);

        var (x, y) = FlyoutPlacement.Compute(icon, 340, 600, Monitor, work, 1.0);

        Assert.Equal(1712 - 170, x);
        Assert.Equal(1032 - 600 - 12, y);
    }

    [Fact]
    public void IconNearScreenCorner_ClampsToWorkAreaWithMargin()
    {
        var work = new PixelRect(0, 0, 1920, 1032);
        var icon = new PixelRect(1890, 1040, 24, 24);

        var (x, _) = FlyoutPlacement.Compute(icon, 340, 600, Monitor, work, 1.0);

        Assert.Equal(1920 - 340 - 12, x);
    }

    [Fact]
    public void TopTaskbar_OpensBelowIt()
    {
        var work = new PixelRect(0, 48, 1920, 1032);
        var icon = new PixelRect(1700, 12, 24, 24);

        var (_, y) = FlyoutPlacement.Compute(icon, 340, 600, Monitor, work, 1.0);

        Assert.Equal(48 + 12, y);
    }

    [Fact]
    public void LeftAndRightTaskbars_OpenBesideThem()
    {
        var icon = new PixelRect(12, 500, 24, 24);
        var (leftX, leftY) = FlyoutPlacement.Compute(icon, 340, 400, Monitor, new PixelRect(48, 0, 1872, 1080), 1.0);
        Assert.Equal(48 + 12, leftX);
        Assert.Equal(512 - 200, leftY);

        var (_, lowY) = FlyoutPlacement.Compute(new PixelRect(12, 900, 24, 24), 340, 400, Monitor, new PixelRect(48, 0, 1872, 1080), 1.0);
        Assert.Equal(1080 - 400 - 12, lowY);

        var (rightX, _) = FlyoutPlacement.Compute(new PixelRect(1884, 900, 24, 24), 340, 400, Monitor, new PixelRect(0, 0, 1872, 1080), 1.0);
        Assert.Equal(1872 - 340 - 12, rightX);
    }

    [Fact]
    public void AutoHideTaskbar_OpensAwayFromTheIconEdge()
    {
        var icon = new PixelRect(1700, 1040, 24, 24);
        var (_, y) = FlyoutPlacement.Compute(icon, 340, 600, Monitor, Monitor, 1.0);
        Assert.Equal(1040 - 600 - 12, y);
    }

    [Fact]
    public void MarginScalesWithDpi()
    {
        var monitor = new PixelRect(0, 0, 3840, 2160);
        var work = new PixelRect(0, 0, 3840, 2064);
        var (_, y) = FlyoutPlacement.Compute(new PixelRect(3400, 2080, 48, 48), 680, 1200, monitor, work, 2.0);
        Assert.Equal(2064 - 1200 - 24, y);
    }

    [Fact]
    public void SecondaryMonitorWithNegativeOrigin_StaysOnThatMonitor()
    {
        var monitor = new PixelRect(-1920, 0, 1920, 1080);
        var work = new PixelRect(-1920, 0, 1920, 1032);
        var (x, y) = FlyoutPlacement.Compute(new PixelRect(-40, 1040, 24, 24), 340, 600, monitor, work, 1.0);
        Assert.Equal(-340 - 12, x);
        Assert.Equal(1032 - 600 - 12, y);
    }
}

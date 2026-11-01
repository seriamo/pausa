namespace Pausa.Core;

/// <summary>Where a tray flyout opens: next to the icon, on the taskbar's side, kept inside the work area.</summary>
public static class FlyoutPlacement
{
    public const double MarginDips = 12;

    /// <param name="anchor">Tray icon (or cursor) rectangle, physical pixels.</param>
    /// <param name="width">Flyout width, physical pixels.</param>
    /// <param name="height">Flyout height, physical pixels.</param>
    /// <param name="monitor">Full monitor bounds.</param>
    /// <param name="workArea">Monitor bounds minus the taskbar.</param>
    /// <param name="scale">Monitor DPI scale (1.0 = 96 DPI).</param>
    public static (int X, int Y) Compute(PixelRect anchor, int width, int height, PixelRect monitor, PixelRect workArea, double scale)
    {
        var margin = (int)Math.Round(MarginDips * scale);
        var cx = anchor.X + anchor.Width / 2;
        var cy = anchor.Y + anchor.Height / 2;
        int x, y;

        if (workArea.Y > monitor.Y)
        {
            // Taskbar on top.
            x = cx - width / 2;
            y = workArea.Y + margin;
        }
        else if (workArea.X > monitor.X)
        {
            // Taskbar on the left.
            x = workArea.X + margin;
            y = cy - height / 2;
        }
        else if (workArea.Right < monitor.Right)
        {
            // Taskbar on the right.
            x = workArea.Right - width - margin;
            y = cy - height / 2;
        }
        else if (workArea.Bottom < monitor.Bottom)
        {
            // Taskbar at the bottom (the usual case).
            x = cx - width / 2;
            y = workArea.Bottom - height - margin;
        }
        else
        {
            // Auto-hide taskbar: open away from whichever edge the icon is near.
            x = cx - width / 2;
            y = cy > monitor.Y + monitor.Height / 2 ? anchor.Y - height - margin : anchor.Bottom + margin;
        }

        x = Math.Clamp(x, workArea.X + margin, Math.Max(workArea.X + margin, workArea.Right - width - margin));
        y = Math.Clamp(y, workArea.Y + margin, Math.Max(workArea.Y + margin, workArea.Bottom - height - margin));
        return (x, y);
    }
}

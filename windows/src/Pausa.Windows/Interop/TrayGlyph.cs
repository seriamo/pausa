using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static Pausa.Windows.Interop.Native;

namespace Pausa.Windows.Interop;

/// <summary>The Pausa eye-and-star mark, shared by the tray icon and the flyout header.</summary>
public static class Glyph
{
    // Same geometry as the macOS menu bar icon (22-unit box), flipped to y-down.
    public static readonly Geometry Eye = Freeze(Geometry.Parse(
        "M2.072,10.726 C1.906,10.871 1.906,11.130 2.072,11.275 C7.596,16.074 10.456,15.841 11,15.841 " +
        "C11.544,15.841 14.403,16.074 19.928,11.275 C20.094,11.130 20.094,10.871 19.928,10.726 " +
        "C14.404,5.927 11.544,6.160 11,6.160 C10.456,6.160 7.597,5.927 2.072,10.726 Z"));

    public static readonly Geometry Star = Freeze(Geometry.Parse(
        "M11.317,7.507 L11.879,10.121 L14.493,10.683 C14.834,10.757 14.834,11.243 14.493,11.316 " +
        "L11.879,11.878 L11.317,14.492 C11.243,14.833 10.757,14.833 10.684,14.492 L10.122,11.878 " +
        "L7.508,11.316 C7.167,11.242 7.167,10.756 7.508,10.683 L10.122,10.121 L10.684,7.507 " +
        "C10.758,7.166 11.244,7.166 11.317,7.507 Z"));

    private static readonly Point SlashStart = new(2.2, 2.2);
    private static readonly Point SlashEnd = new(19.8, 19.8);

    private static Geometry Freeze(Geometry g)
    {
        g.Freeze();
        return g;
    }

    /// <summary>Draws the glyph into a square of <paramref name="size"/> device pixels.</summary>
    public static void Draw(DrawingContext dc, double size, Brush brush, bool paused, string? countdown)
    {
        if (countdown is not null)
        {
            DrawCountdown(dc, size, brush, countdown);
            return;
        }

        // The eye spans x 1.9-20.1 in the 22-unit box; crop to 1-21 so it fills small icons.
        var scale = size / 20.0;
        dc.PushTransform(new MatrixTransform(scale, 0, 0, scale, -1 * scale, -1 * scale));

        var stroke = new Pen(brush, 1.45) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if (paused)
        {
            // Cut a gap around the slash so it reads clearly at 16px.
            var gap = new LineGeometry(SlashStart, SlashEnd).GetWidenedPathGeometry(new Pen(Brushes.Black, 3.6)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
            });
            dc.PushClip(Geometry.Combine(new RectangleGeometry(new Rect(0, 0, 22, 22)), gap, GeometryCombineMode.Exclude, null));
        }

        dc.DrawGeometry(null, stroke, Eye);
        dc.DrawGeometry(brush, null, Star);

        if (paused)
        {
            dc.Pop();
            dc.DrawLine(stroke, SlashStart, SlashEnd);
        }
        dc.Pop();
    }

    private static void DrawCountdown(DrawingContext dc, double size, Brush brush, string text)
    {
        var emSize = size * (text.Length > 1 ? 0.68 : 0.82);
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.SemiCondensed),
            emSize, brush, 1.0);
        dc.DrawText(formatted, new Point((size - formatted.Width) / 2, (size - formatted.Height) / 2));
    }

    public static BitmapSource Render(int pixels, Brush brush, bool paused, string? countdown)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            Draw(dc, pixels, brush, paused, countdown);
        }
        var bitmap = new RenderTargetBitmap(pixels, pixels, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}

/// <summary>Turns the glyph into HICONs for the notification area.</summary>
internal sealed class TrayGlyph : IDisposable
{
    private IntPtr _current;

    /// <summary>Renders a new icon and destroys the previous one once the caller has swapped it in.</summary>
    public IntPtr Create(bool paused, string? countdown)
    {
        var size = IconSize();
        var brush = TaskbarUsesLightTheme() ? new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)) : Brushes.White;
        var icon = ToIcon(Glyph.Render(size, brush, paused, countdown));
        if (_current != IntPtr.Zero) DestroyIcon(_current);
        _current = icon;
        return icon;
    }

    public static int IconSize()
    {
        var primary = Monitors.Primary();
        var dpi = (uint)Math.Round((primary?.Scale ?? 1.0) * 96);
        var size = GetSystemMetricsForDpi(SM_CXSMICON, dpi);
        return size > 0 ? size : 16;
    }

    public static bool TaskbarUsesLightTheme() => ReadPersonalize("SystemUsesLightTheme");
    public static bool AppsUseLightTheme() => ReadPersonalize("AppsUseLightTheme", fallback: true);

    private static bool ReadPersonalize(string name, bool fallback = false)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue(name) is int value ? value != 0 : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static IntPtr ToIcon(BitmapSource source)
    {
        // Icons want straight (non-premultiplied) alpha in a 32-bit DIB section.
        var straight = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
        int w = straight.PixelWidth, h = straight.PixelHeight, stride = w * 4;
        var pixels = new byte[stride * h];
        straight.CopyPixels(pixels, stride, 0);

        var header = BITMAPINFOHEADER.TopDown32(w, h);
        var color = CreateDIBSection(IntPtr.Zero, ref header, 0, out var bits, IntPtr.Zero, 0);
        Marshal.Copy(pixels, 0, bits, pixels.Length);
        var mask = CreateBitmap(w, h, 1, 1, new byte[((w + 15) / 16) * 2 * h]);
        try
        {
            var info = new ICONINFO { fIcon = true, hbmColor = color, hbmMask = mask };
            return CreateIconIndirect(ref info);
        }
        finally
        {
            DeleteObject(color);
            DeleteObject(mask);
        }
    }

    public void Dispose()
    {
        if (_current != IntPtr.Zero) DestroyIcon(_current);
        _current = IntPtr.Zero;
    }
}

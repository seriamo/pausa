using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pausa.Core;
using static Pausa.Windows.Interop.Native;

namespace Pausa.Windows.Services;

/// <summary>Screen and visual captures for the smoke test.</summary>
internal static class ScreenCapture
{
    public static PixelRect VirtualScreen() => new(
        GetSystemMetrics(SM_XVIRTUALSCREEN), GetSystemMetrics(SM_YVIRTUALSCREEN),
        GetSystemMetrics(SM_CXVIRTUALSCREEN), GetSystemMetrics(SM_CYVIRTUALSCREEN));

    /// <summary>Copies a region of the composed desktop, layered windows included (CAPTUREBLT).</summary>
    public static BitmapSource Capture(PixelRect rect)
    {
        int w = Math.Max(1, rect.Width), h = Math.Max(1, rect.Height), stride = w * 4;
        var screen = GetDC(IntPtr.Zero);
        var memory = CreateCompatibleDC(screen);
        var header = BITMAPINFOHEADER.TopDown32(w, h);
        var bitmap = CreateDIBSection(screen, ref header, 0, out var bits, IntPtr.Zero, 0);
        var previous = SelectObject(memory, bitmap);
        try
        {
            BitBlt(memory, 0, 0, w, h, screen, rect.X, rect.Y, SRCCOPY | CAPTUREBLT);
            var pixels = new byte[stride * h];
            Marshal.Copy(bits, pixels, 0, pixels.Length);
            // GDI leaves the alpha byte at zero.
            for (var i = 3; i < pixels.Length; i += 4) pixels[i] = 255;
            var source = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
            source.Freeze();
            return source;
        }
        finally
        {
            SelectObject(memory, previous);
            DeleteObject(bitmap);
            DeleteDC(memory);
            ReleaseDC(IntPtr.Zero, screen);
        }
    }

    /// <summary>Renders a laid-out element at its monitor's DPI, independent of what's on screen.</summary>
    public static BitmapSource Render(FrameworkElement element)
    {
        var dpi = VisualTreeHelper.GetDpi(element);
        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY),
            dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(element);
        bitmap.Freeze();
        return bitmap;
    }

    public static void SavePng(BitmapSource source, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}

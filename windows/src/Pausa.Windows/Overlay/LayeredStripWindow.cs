using System.Runtime.InteropServices;
using Pausa.Core;
using static Pausa.Windows.Interop.Native;

namespace Pausa.Windows.Overlay;

/// <summary>
/// A click-through, never-activating, topmost layered window showing one gradient strip.
/// The bitmap is uploaded once; each animation frame only changes the window's alpha.
/// </summary>
internal sealed class LayeredStripWindow : IDisposable
{
    private const string ClassName = "Seriamo.Pausa.Glow";

    // Kept in a static field so the GC never collects the delegate Windows calls into.
    private static readonly WndProc s_wndProc = WindowProc;
    private static bool s_classRegistered;

    private readonly PixelRect _bounds;
    private IntPtr _hwnd;
    private IntPtr _memDc;
    private IntPtr _bitmap;
    private IntPtr _previousBitmap;

    /// <summary>False once an alpha-only update has failed; from then on every frame re-sends the bitmap.</summary>
    public static bool AlphaOnlyUpdates { get; private set; } = true;

    public LayeredStripWindow(OverlayStrip strip, FlashColor color)
    {
        _bounds = strip.Bounds;
        EnsureClass();

        _hwnd = CreateWindowEx(
            WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST,
            ClassName, "Pausa", WS_POPUP,
            _bounds.X, _bounds.Y, _bounds.Width, _bounds.Height,
            IntPtr.Zero, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);
        if (_hwnd == IntPtr.Zero) throw new InvalidOperationException($"CreateWindowEx failed ({Marshal.GetLastWin32Error()}).");

        var pixels = OverlayLayout.RenderStrip(_bounds.Width, _bounds.Height, strip.Edge, color);
        var screen = GetDC(IntPtr.Zero);
        try
        {
            _memDc = CreateCompatibleDC(screen);
            var header = BITMAPINFOHEADER.TopDown32(_bounds.Width, _bounds.Height);
            _bitmap = CreateDIBSection(screen, ref header, 0, out var bits, IntPtr.Zero, 0);
            if (_bitmap == IntPtr.Zero) throw new InvalidOperationException("CreateDIBSection failed.");
            Marshal.Copy(pixels, 0, bits, pixels.Length);
            _previousBitmap = SelectObject(_memDc, _bitmap);
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, screen);
        }

        FullUpdate(0);
    }

    public void Show() =>
        SetWindowPos(_hwnd, HWND_TOPMOST, _bounds.X, _bounds.Y, _bounds.Width, _bounds.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);

    public void SetOpacity(double opacity)
    {
        var alpha = (byte)Math.Clamp(Math.Round(opacity * 255), 0, 255);
        if (AlphaOnlyUpdates)
        {
            var blend = Blend(alpha);
            if (UpdateLayeredWindow(_hwnd, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, ref blend, ULW_ALPHA)) return;
            AlphaOnlyUpdates = false;
        }
        FullUpdate(alpha);
    }

    private bool FullUpdate(byte alpha)
    {
        var blend = Blend(alpha);
        var dst = new POINT { X = _bounds.X, Y = _bounds.Y };
        var size = new SIZE { Cx = _bounds.Width, Cy = _bounds.Height };
        var src = new POINT();
        return UpdateLayeredWindow(_hwnd, IntPtr.Zero, ref dst, ref size, _memDc, ref src, 0, ref blend, ULW_ALPHA);
    }

    private static BLENDFUNCTION Blend(byte alpha) => new()
    {
        BlendOp = AC_SRC_OVER,
        SourceConstantAlpha = alpha,
        AlphaFormat = AC_SRC_ALPHA,
    };

    private static void EnsureClass()
    {
        if (s_classRegistered) return;
        var wc = new WNDCLASSEX
        {
            cbSize = Marshal.SizeOf<WNDCLASSEX>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_wndProc),
            hInstance = GetModuleHandle(null),
            lpszClassName = ClassName,
        };
        if (RegisterClassEx(ref wc) == 0) throw new InvalidOperationException("RegisterClassEx failed.");
        s_classRegistered = true;
    }

    private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam) => msg switch
    {
        WM_NCHITTEST => new IntPtr(HTTRANSPARENT),
        WM_MOUSEACTIVATE => new IntPtr(MA_NOACTIVATE),
        _ => DefWindowProc(hwnd, msg, wParam, lParam),
    };

    public void Dispose()
    {
        if (_hwnd != IntPtr.Zero)
        {
            DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
        if (_memDc != IntPtr.Zero)
        {
            SelectObject(_memDc, _previousBitmap);
            DeleteDC(_memDc);
            _memDc = IntPtr.Zero;
        }
        if (_bitmap != IntPtr.Zero)
        {
            DeleteObject(_bitmap);
            _bitmap = IntPtr.Zero;
        }
    }
}

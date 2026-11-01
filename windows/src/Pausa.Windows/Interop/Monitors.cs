using Pausa.Core;
using static Pausa.Windows.Interop.Native;

namespace Pausa.Windows.Interop;

internal sealed record MonitorInfo(IntPtr Handle, PixelRect Bounds, PixelRect WorkArea, double Scale, bool IsPrimary);

internal static class Monitors
{
    public static IReadOnlyList<MonitorInfo> All()
    {
        var result = new List<MonitorInfo>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (handle, _, _, _) =>
        {
            if (Describe(handle) is { } info) result.Add(info);
            return true;
        }, IntPtr.Zero);
        return result;
    }

    public static MonitorInfo? Describe(IntPtr handle)
    {
        var info = new MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(handle, ref info)) return null;
        var scale = GetDpiForMonitor(handle, MDT_EFFECTIVE_DPI, out var dpi, out _) == 0 ? dpi / 96.0 : 1.0;
        return new MonitorInfo(handle, info.rcMonitor.ToPixelRect(), info.rcWork.ToPixelRect(), scale, (info.dwFlags & 1) != 0);
    }

    public static MonitorInfo? FromPoint(int x, int y) =>
        Describe(MonitorFromPoint(new POINT { X = x, Y = y }, MONITOR_DEFAULTTONEAREST));

    public static MonitorInfo? Primary() =>
        Describe(MonitorFromPoint(new POINT(), MONITOR_DEFAULTTOPRIMARY));

    public static MonitorInfo? UnderCursor() =>
        GetCursorPos(out var p) ? FromPoint(p.X, p.Y) : Primary();
}

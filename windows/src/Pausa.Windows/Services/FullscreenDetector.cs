using System.Diagnostics;
using Pausa.Windows.Interop;
using static Pausa.Windows.Interop.Native;

namespace Pausa.Windows.Services;

/// <summary>
/// True while something is presenting, playing fullscreen video or running a fullscreen game,
/// so a reminder doesn't land on top of a slideshow or a movie. Checked when a flash is due.
/// </summary>
internal static class FullscreenDetector
{
    private const int QUNS_BUSY = 2;
    private const int QUNS_RUNNING_D3D_FULL_SCREEN = 3;
    private const int QUNS_PRESENTATION_MODE = 4;

    private static readonly HashSet<string> ShellClasses = new(StringComparer.Ordinal)
    {
        "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd",
    };

    public static bool IsFullscreenAppActive()
    {
        if (SHQueryUserNotificationState(out var state) == 0
            && state is QUNS_BUSY or QUNS_RUNNING_D3D_FULL_SCREEN or QUNS_PRESENTATION_MODE)
        {
            return true;
        }
        return AnyWindowCoversItsMonitor();
    }

    private static bool AnyWindowCoversItsMonitor()
    {
        var ownPid = (uint)Environment.ProcessId;
        var found = false;
        var className = new char[64];

        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd) || IsIconic(hwnd)) return true;
            if (DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out var cloaked, sizeof(int)) == 0 && cloaked != 0) return true;

            GetWindowThreadProcessId(hwnd, out var pid);
            if (pid == ownPid) return true;

            var exStyle = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
            if ((exStyle & WS_EX_TOOLWINDOW) != 0) return true;

            // A maximized window still has a caption. Fullscreen video, slideshows and games drop it.
            var style = (long)GetWindowLongPtr(hwnd, GWL_STYLE);
            if ((style & WS_CAPTION) == WS_CAPTION) return true;

            var length = GetClassName(hwnd, className, className.Length);
            if (length > 0 && ShellClasses.Contains(new string(className, 0, length))) return true;

            if (!GetWindowRect(hwnd, out var rect)) return true;
            if (Monitors.Describe(MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST)) is not { } monitor) return true;

            var m = monitor.Bounds;
            if (rect.Left <= m.X && rect.Top <= m.Y && rect.Right >= m.Right && rect.Bottom >= m.Bottom)
            {
                found = true;
                return false;
            }
            return true;
        }, IntPtr.Zero);

        return found;
    }
}

using System.Runtime.InteropServices;
using System.Windows.Interop;
using static Pausa.Windows.Interop.Native;

namespace Pausa.Windows.Interop;

/// <summary>
/// Notification-area icon plus the hidden top-level window that receives its callbacks
/// and the system broadcasts Pausa cares about (lock/unlock, display and theme changes).
/// </summary>
internal sealed class TrayIcon : IDisposable
{
    public const string HostWindowTitle = "Seriamo.Pausa.TrayHost";

    /// <summary>Posted by a second Pausa launch so the running copy opens its flyout.</summary>
    public const int WM_PAUSA_ACTIVATE = WM_APP + 2;

    private const int WM_TRAY_CALLBACK = WM_APP + 1;
    private const uint IconId = 1;

    private readonly HwndSource _host;
    private readonly uint _taskbarCreatedMessage;
    private IntPtr _icon;
    private string _tooltip = "Pausa";
    private bool _added;

    public TrayIcon()
    {
        // A real (hidden) top-level window, not message-only: message-only windows miss the
        // TaskbarCreated broadcast we need to restore the icon after Explorer restarts.
        _host = new HwndSource(new HwndSourceParameters(HostWindowTitle)
        {
            WindowStyle = 0,
            ExtendedWindowStyle = WS_EX_TOOLWINDOW,
            Width = 0,
            Height = 0,
        });
        _host.AddHook(WndProc);
        _taskbarCreatedMessage = RegisterWindowMessage("TaskbarCreated");
        WTSRegisterSessionNotification(_host.Handle, 0);
    }

    public IntPtr Handle => _host.Handle;
    public bool IsAdded => _added;

    public event Action? LeftClicked;
    public event Action<int, int>? RightClicked;
    public event Action? ActivateRequested;
    public event Action? SessionLocked;
    public event Action? SessionUnlocked;
    public event Action? DisplaySettingsChanged;
    public event Action? ThemeChanged;
    public event Action? TaskbarRecreated;

    public void Show(IntPtr icon, string tooltip)
    {
        _icon = icon;
        _tooltip = tooltip;
        var data = CreateData(NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_SHOWTIP);
        _added = Shell_NotifyIcon(NIM_ADD, ref data);
        if (_added)
        {
            data.uVersion = NOTIFYICON_VERSION_4;
            Shell_NotifyIcon(NIM_SETVERSION, ref data);
        }
    }

    public void Update(IntPtr icon, string tooltip)
    {
        if (icon == _icon && tooltip == _tooltip) return;
        _icon = icon;
        _tooltip = tooltip;
        if (!_added) return;
        var data = CreateData(NIF_ICON | NIF_TIP | NIF_SHOWTIP);
        Shell_NotifyIcon(NIM_MODIFY, ref data);
    }

    /// <summary>Icon bounds in physical pixels, or null when it's hidden in the overflow area.</summary>
    public RECT? GetIconRect()
    {
        var id = new NOTIFYICONIDENTIFIER
        {
            cbSize = Marshal.SizeOf<NOTIFYICONIDENTIFIER>(),
            hWnd = _host.Handle,
            uID = IconId,
        };
        return Shell_NotifyIconGetRect(ref id, out var rect) == 0 && rect.Width > 0 ? rect : null;
    }

    private NOTIFYICONDATA CreateData(uint flags) => new()
    {
        cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
        hWnd = _host.Handle,
        uID = IconId,
        uFlags = flags,
        uCallbackMessage = WM_TRAY_CALLBACK,
        hIcon = _icon,
        szTip = _tooltip.Length > 127 ? _tooltip[..127] : _tooltip,
        szInfo = string.Empty,
        szInfoTitle = string.Empty,
    };

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WM_TRAY_CALLBACK:
                switch (LoWord(lParam))
                {
                    case NIN_SELECT:
                    case NIN_KEYSELECT:
                        LeftClicked?.Invoke();
                        break;
                    case WM_CONTEXTMENU:
                        RightClicked?.Invoke(LoWord(wParam), HiWord(wParam));
                        break;
                }
                handled = true;
                break;
            case WM_PAUSA_ACTIVATE:
                ActivateRequested?.Invoke();
                handled = true;
                break;
            case WM_WTSSESSION_CHANGE:
                if (wParam == WTS_SESSION_LOCK) SessionLocked?.Invoke();
                else if (wParam == WTS_SESSION_UNLOCK) SessionUnlocked?.Invoke();
                break;
            case WM_DISPLAYCHANGE:
            case WM_DPICHANGED:
                DisplaySettingsChanged?.Invoke();
                break;
            case WM_SETTINGCHANGE:
                if (lParam != IntPtr.Zero && Marshal.PtrToStringUni(lParam) == "ImmersiveColorSet") ThemeChanged?.Invoke();
                break;
            default:
                if (_taskbarCreatedMessage != 0 && msg == (int)_taskbarCreatedMessage)
                {
                    _added = false;
                    TaskbarRecreated?.Invoke();
                }
                break;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_added)
        {
            var data = CreateData(0);
            Shell_NotifyIcon(NIM_DELETE, ref data);
            _added = false;
        }
        WTSUnRegisterSessionNotification(_host.Handle);
        _host.RemoveHook(WndProc);
        _host.Dispose();
    }
}

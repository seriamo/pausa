using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Pausa.Core;
using Pausa.Windows.Interop;

namespace Pausa.Windows;

public partial class App : Application
{
    private const string SingleInstanceName = @"Local\Seriamo.Pausa.SingleInstance";
    private static readonly Lock LogLock = new();
    private static readonly TimeSpan SmokeTestTimeout = TimeSpan.FromMinutes(2);

    private Mutex? _singleInstance;
    private bool _ownsSingleInstance;
    private PausaController? _controller;
    private Timer? _smokeWatchdog;

    public static string LogPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Seriamo", "Pausa", "pausa.log");

    public static void Log(string message)
    {
        try
        {
            lock (LogLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                var info = new FileInfo(LogPath);
                if (info.Exists && info.Length > 1_000_000) File.Move(LogPath, LogPath + ".old", overwrite: true);
                File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => Log($"Unhandled: {args.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log($"Unobserved task exception: {args.Exception}");
            args.SetObserved();
        };

        var smokeIndex = Array.IndexOf(e.Args, "--smoke-test");
        if (smokeIndex >= 0)
        {
            var directory = smokeIndex + 1 < e.Args.Length ? e.Args[smokeIndex + 1] : Path.Combine(Environment.CurrentDirectory, "smoke");
            RunSmokeTest(Path.GetFullPath(directory));
            return;
        }

        _singleInstance = new Mutex(initiallyOwned: true, SingleInstanceName, out _ownsSingleInstance);
        if (!_ownsSingleInstance)
        {
            ActivateRunningInstance();
            Shutdown();
            return;
        }

        Log($"Pausa {AppInfo.Version} starting");
        _controller = new PausaController(new AppOptions(SmokeTest: false, SettingsStore.DefaultPath));
        _controller.Start();
    }

    /// <summary>Launching Pausa again (Start menu, installer) opens the running copy's flyout.</summary>
    private static void ActivateRunningInstance()
    {
        var host = Native.FindWindow(null, TrayIcon.HostWindowTitle);
        if (host == IntPtr.Zero) return;
        Native.GetWindowThreadProcessId(host, out var processId);
        Native.AllowSetForegroundWindow(processId);
        Native.PostMessage(host, TrayIcon.WM_PAUSA_ACTIVATE, IntPtr.Zero, IntPtr.Zero);
    }

    private async void RunSmokeTest(string directory)
    {
        _smokeWatchdog = new Timer(_ =>
        {
            Log("Smoke test timed out");
            Environment.Exit(3);
        }, null, SmokeTestTimeout, Timeout.InfiniteTimeSpan);

        var exitCode = 2;
        try
        {
            exitCode = await SmokeTest.RunAsync(directory);
        }
        catch (Exception ex)
        {
            Log($"Smoke test crashed: {ex}");
            await File.WriteAllTextAsync(Path.Combine(directory, "crash.txt"), ex.ToString());
        }
        Shutdown(exitCode);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log($"Unhandled UI exception: {e.Exception}");
        // A tray utility should keep running through a bad frame rather than vanish silently.
        e.Handled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _smokeWatchdog?.Dispose();
        _controller?.Dispose();
        if (_ownsSingleInstance) _singleInstance?.ReleaseMutex();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}

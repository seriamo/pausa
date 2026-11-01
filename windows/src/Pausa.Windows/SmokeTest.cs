using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Pausa.Core;
using Pausa.Windows.Interop;
using Pausa.Windows.Overlay;
using Pausa.Windows.Services;
using Pausa.Windows.Views;

namespace Pausa.Windows;

/// <summary>
/// <c>Pausa.exe --smoke-test &lt;dir&gt;</c>: starts the real app against throwaway settings, drives
/// every surface once and writes screenshots plus report.json. No network, no registry writes.
/// Exits non-zero only if a step throws.
/// </summary>
internal static class SmokeTest
{
    public static async Task<int> RunAsync(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var report = new Dictionary<string, object?>();
        var errors = new List<string>();
        var settingsPath = Path.Combine(Path.GetTempPath(), $"pausa-smoke-{Guid.NewGuid():N}", "settings.json");
        var controller = new PausaController(new AppOptions(SmokeTest: true, settingsPath));
        string Out(string name) => Path.Combine(outputDirectory, name);

        async Task Step(string name, Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                errors.Add($"{name}: {ex}");
                App.Log($"Smoke step {name} failed: {ex}");
            }
        }

        report["version"] = AppInfo.Version;
        report["os"] = Environment.OSVersion.VersionString;
        report["processArchitecture"] = RuntimeInformation.ProcessArchitecture.ToString();
        report["osArchitecture"] = RuntimeInformation.OSArchitecture.ToString();
        report["monitors"] = Monitors.All().Select(m => new { m.Bounds, m.WorkArea, m.Scale, m.IsPrimary }).ToList();
        report["virtualScreen"] = ScreenCapture.VirtualScreen();

        // Strong so the glow is easy to see in the captures.
        controller.Settings.Intensity = Steps.Intensity[Steps.Intensity.Count - 1].Value;

        await Step("start", () =>
        {
            controller.Start();
            return Task.CompletedTask;
        });
        report["trayAdded"] = controller.Tray?.IsAdded;
        report["trayIconRect"] = controller.Tray?.GetIconRect()?.ToPixelRect();
        report["statusLine"] = controller.StatusLine;
        report["firstRun"] = controller.IsFirstRun;

        await Step("glyphs", () =>
        {
            var ink = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
            ScreenCapture.SavePng(Glyph.Render(64, ink, paused: false, null), Out("glyph-light.png"));
            ScreenCapture.SavePng(Glyph.Render(64, ink, paused: true, null), Out("glyph-light-paused.png"));
            ScreenCapture.SavePng(Glyph.Render(64, Brushes.White, paused: false, null), Out("glyph-dark.png"));
            ScreenCapture.SavePng(Glyph.Render(64, Brushes.White, paused: false, "12"), Out("glyph-dark-countdown.png"));
            report["trayIconSize"] = TrayGlyph.IconSize();
            return Task.CompletedTask;
        });

        foreach (var (name, mode) in new[] { ("light", ThemeMode.Light), ("dark", ThemeMode.Dark) })
        {
            await Step($"theme-{name}", async () =>
            {
                Application.Current.ThemeMode = mode;
                await Settle(200);
            });

            await Step($"flyout-{name}", async () =>
            {
                controller.ShowFlyout();
                await Settle(700);
                var flyout = controller.Flyout!;
                ScreenCapture.SavePng(ScreenCapture.Render(flyout), Out($"flyout-{name}.png"));
                var rect = WindowRect(flyout);
                ScreenCapture.SavePng(ScreenCapture.Capture(Inflate(rect, 32)), Out($"flyout-{name}-screen.png"));
                report[$"flyout-{name}"] = new { rect, flyout.ActualWidth, flyout.ActualHeight, dpi = VisualTreeHelper.GetDpi(flyout).PixelsPerDip };
                controller.HideFlyout();
            });

            await Step($"about-{name}", async () =>
            {
                var about = controller.CreateAboutWindowForCapture();
                about.Show();
                about.ApplyLinkColor();
                await Settle(500);
                ScreenCapture.SavePng(ScreenCapture.Render(about), Out($"about-{name}.png"));
                ScreenCapture.SavePng(ScreenCapture.Capture(Inflate(WindowRect(about), 16)), Out($"about-{name}-screen.png"));
                about.Close();
            });

            await Step($"menu-{name}", async () =>
            {
                var menu = TrayMenu.Build(controller);
                menu.IsOpen = true;
                await Settle(500);
                if (PresentationSource.FromVisual(menu) is HwndSource source && Native.GetWindowRect(source.Handle, out var r))
                {
                    ScreenCapture.SavePng(ScreenCapture.Capture(Inflate(r.ToPixelRect(), 16)), Out($"menu-{name}.png"));
                }
                menu.IsOpen = false;
            });
        }
        Application.Current.ThemeMode = ThemeMode.System;

        foreach (var mode in FlashModes.All)
        {
            await Step($"flash-{mode}", async () =>
            {
                controller.Settings.FlashMode = mode;
                controller.Overlay.Flash(controller.CurrentStyle, 3);
                // First peak lands at FlashRiseSeconds.
                await Task.Delay(TimeSpan.FromSeconds(FlashAnimation.FlashRiseSeconds));
                ScreenCapture.SavePng(ScreenCapture.Capture(ScreenCapture.VirtualScreen()), Out($"flash-{mode}.png"));
                report[$"flash-{mode}"] = new
                {
                    windows = controller.Overlay.LastWindowCount,
                    flashing = controller.Overlay.IsFlashing,
                    status = controller.StatusLine,
                };
                controller.Overlay.Cancel();
            });
        }

        await Step("flash-lifecycle", async () =>
        {
            var ended = false;
            controller.Overlay.FlashEnded += () => ended = true;
            controller.Overlay.Flash(controller.CurrentStyle, 2);
            await Task.Delay(TimeSpan.FromSeconds(2.5));
            report["flash-lifecycle"] = new { ended, stillFlashing = controller.Overlay.IsFlashing };
            if (!ended || controller.Overlay.IsFlashing) throw new InvalidOperationException("A 2s flash did not end on its own.");
        });

        report["alphaOnlyUpdates"] = LayeredStripWindow.AlphaOnlyUpdates;
        report["fullscreenAppActive"] = FullscreenDetector.IsFullscreenAppActive();
        report["errors"] = errors;

        controller.Dispose();
        await File.WriteAllTextAsync(Out("report.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        TryDelete(Path.GetDirectoryName(settingsPath)!);
        return errors.Count == 0 ? 0 : 1;
    }

    private static async Task Settle(int milliseconds)
    {
        await Task.Delay(milliseconds);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    }

    private static PixelRect WindowRect(Window window) =>
        Native.GetWindowRect(new WindowInteropHelper(window).Handle, out var r) ? r.ToPixelRect() : default;

    private static PixelRect Inflate(PixelRect r, int by) => new(r.X - by, r.Y - by, r.Width + by * 2, r.Height + by * 2);

    private static void TryDelete(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}

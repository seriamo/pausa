using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Pausa.Core;

namespace Pausa.Windows.Views;

/// <summary>The right-click menu on the tray icon. Rebuilt each time so it always reflects current state.</summary>
internal static class TrayMenu
{
    public static ContextMenu Build(PausaController controller)
    {
        var menu = new ContextMenu { Placement = PlacementMode.MousePoint, StaysOpen = false };

        if (controller.Update is { } update)
        {
            var updateItem = Item($"Pausa v{update.Version} available", controller.OpenUpdate);
            updateItem.FontWeight = FontWeights.SemiBold;
            updateItem.SetResourceReference(Control.ForegroundProperty, "AccentTextFillColorPrimaryBrush");
            menu.Items.Add(updateItem);
            menu.Items.Add(new Separator());
        }

        var status = new MenuItem { Header = controller.StatusLine, IsEnabled = false };
        menu.Items.Add(status);

        if (controller.Timer.IsPaused)
        {
            menu.Items.Add(Item("Resume", controller.Resume));
        }
        else
        {
            var pause = new MenuItem { Header = "Pause" };
            foreach (var option in Steps.PauseOptions)
            {
                pause.Items.Add(Item(option.Title, () => controller.Pause(option.Duration)));
            }
            pause.Items.Add(new Separator());
            pause.Items.Add(Item("Until I resume", controller.PauseIndefinitely));
            pause.Items.Add(Item("Until next restart", controller.PauseUntilNextRestart));
            menu.Items.Add(pause);
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(Item("Test flash now", controller.TestFlash));
        menu.Items.Add(Item("Settings", controller.ShowFlyout));
        menu.Items.Add(new Separator());
        menu.Items.Add(Item("About Pausa", controller.ShowAbout));
        menu.Items.Add(Item("Quit", controller.Quit));

        void OnStatusChanged() => status.Header = controller.StatusLine;
        menu.Opened += (_, _) => controller.StatusChanged += OnStatusChanged;
        menu.Closed += (_, _) => controller.StatusChanged -= OnStatusChanged;
        return menu;
    }

    private static MenuItem Item(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }
}

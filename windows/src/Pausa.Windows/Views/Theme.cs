using System.Windows;
using System.Windows.Media;

namespace Pausa.Windows.Views;

internal static class Theme
{
    /// <summary>True when the applied Fluent theme is dark, judged by its primary text color.</summary>
    public static bool IsDark(FrameworkElement element) =>
        element.TryFindResource("TextFillColorPrimaryBrush") is SolidColorBrush { Color: var c }
        && (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255 > 0.5;
}

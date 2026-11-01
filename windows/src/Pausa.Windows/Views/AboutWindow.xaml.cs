using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Pausa.Core;

namespace Pausa.Windows.Views;

public partial class AboutWindow : Window
{
    private static readonly Color BrandLink = Color.FromRgb(0x2E, 0x5C, 0x7C);
    private static readonly Color BrandLinkOnDark = Color.FromRgb(0x7F, 0xB3, 0xD5);

    public AboutWindow()
    {
        InitializeComponent();
        VersionText.Text = $"v{AppInfo.Version}";
        AppIcon.Source = LargestIconFrame();
        ApplyLinkColor();
        // Hyperlinks don't follow the theme on their own; recolor when Windows switches light/dark.
        Activated += (_, _) => ApplyLinkColor();
    }

    public void ApplyLinkColor() =>
        SeriamoLink.Foreground = new SolidColorBrush(Theme.IsDark(this) ? BrandLinkOnDark : BrandLink);

    private static BitmapSource? LargestIconFrame()
    {
        try
        {
            var decoder = BitmapDecoder.Create(new Uri("pack://application:,,,/Assets/Pausa.ico"),
                BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            return decoder.Frames.MaxBy(f => f.PixelWidth);
        }
        catch (Exception ex)
        {
            App.Log($"Loading the About icon failed: {ex.Message}");
            return null;
        }
    }

    private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        PausaController.OpenUrl(e.Uri.AbsoluteUri);
        e.Handled = true;
    }
}

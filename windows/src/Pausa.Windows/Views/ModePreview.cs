using System.Windows;
using System.Windows.Media;
using Pausa.Core;

namespace Pausa.Windows.Views;

/// <summary>A tiny "screen" showing where each flash mode glows, drawn in the chosen color.</summary>
public sealed class ModePreview : FrameworkElement
{
    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode), typeof(FlashMode), typeof(ModePreview),
        new FrameworkPropertyMetadata(FlashMode.TopGlow, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty GlowColorProperty = DependencyProperty.Register(
        nameof(GlowColor), typeof(Color), typeof(ModePreview),
        new FrameworkPropertyMetadata(Colors.Red, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Brush ScreenBrush = Frozen(new LinearGradientBrush(
        Color.FromRgb(0x33, 0x3B, 0x46), Color.FromRgb(0x1D, 0x23, 0x2B), 90));
    private static readonly Brush WindowBrush = Frozen(new SolidColorBrush(Color.FromArgb(0x38, 0xFF, 0xFF, 0xFF)));
    private static readonly Pen BezelPen = Frozen(new Pen(new SolidColorBrush(Color.FromArgb(0x66, 0x80, 0x80, 0x80)), 1));

    public FlashMode Mode
    {
        get => (FlashMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public Color GlowColor
    {
        get => (Color)GetValue(GlowColorProperty);
        set => SetValue(GlowColorProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        if (w <= 0 || h <= 0) return;
        var bounds = new Rect(0, 0, w, h);

        dc.PushClip(new RectangleGeometry(bounds, 4, 4));
        dc.DrawRectangle(ScreenBrush, null, bounds);
        dc.DrawRoundedRectangle(WindowBrush, null, new Rect(w * 0.10, h * 0.26, w * 0.48, h * 0.52), 2, 2);
        dc.DrawRoundedRectangle(WindowBrush, null, new Rect(w * 0.50, h * 0.40, w * 0.40, h * 0.46), 2, 2);

        if (Mode == FlashMode.TopGlow)
        {
            dc.DrawRectangle(Edge(0, 0, 0, 1), null, new Rect(0, 0, w, h * 0.34));
        }
        else
        {
            var t = Math.Min(w, h) * 0.26;
            dc.DrawRectangle(Edge(0, 0, 0, 1), null, new Rect(0, 0, w, t));
            dc.DrawRectangle(Edge(0, 1, 0, 0), null, new Rect(0, h - t, w, t));
            dc.DrawRectangle(Edge(0, 0, 1, 0), null, new Rect(0, 0, t, h));
            dc.DrawRectangle(Edge(1, 0, 0, 0), null, new Rect(w - t, 0, t, h));
        }
        dc.Pop();
        dc.DrawRoundedRectangle(null, BezelPen, new Rect(0.5, 0.5, w - 1, h - 1), 4, 4);
    }

    private LinearGradientBrush Edge(double x1, double y1, double x2, double y2)
    {
        var c = GlowColor;
        var brush = new LinearGradientBrush(
            [new GradientStop(Color.FromArgb(0xE6, c.R, c.G, c.B), 0), new GradientStop(Color.FromArgb(0, c.R, c.G, c.B), 1)],
            new Point(x1, y1), new Point(x2, y2));
        brush.Freeze();
        return brush;
    }

    private static T Frozen<T>(T freezable) where T : Freezable
    {
        freezable.Freeze();
        return freezable;
    }
}

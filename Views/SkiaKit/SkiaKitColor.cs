#nullable enable
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

internal static class SkiaKitColor
{
    public static SKColor Blend(SKColor a, SKColor b, float t)
    {
        t = Math.Clamp(t, 0, 1);
        return new SKColor(
            (byte)(a.Red + (b.Red - a.Red) * t),
            (byte)(a.Green + (b.Green - a.Green) * t),
            (byte)(a.Blue + (b.Blue - a.Blue) * t),
            (byte)(a.Alpha + (b.Alpha - a.Alpha) * t));
    }

    public static SKColor Darken(SKColor color, float amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return new SKColor(
            (byte)(color.Red * amount),
            (byte)(color.Green * amount),
            (byte)(color.Blue * amount),
            color.Alpha);
    }

    public static bool TrySkColor(StyledElement element, string key, out SKColor color)
    {
        if (element.TryGetResource(key, element.ActualThemeVariant, out var resource) && resource is IBrush brush)
        {
            color = BrushToSkColor(brush);
            return true;
        }

        color = default;
        return false;
    }

    public static SKColor BrushToSkColor(IBrush brush) =>
        brush is SolidColorBrush solid
            ? new SKColor(solid.Color.R, solid.Color.G, solid.Color.B, solid.Color.A)
            : new SKColor(45, 45, 48);
}

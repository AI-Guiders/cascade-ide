#nullable enable
using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Единые настройки Skia-текста: subpixel AA, hinting, снап baseline к пиксельной сетке.</summary>
internal static class SkiaKitFonts
{
    public static SKFont CreateUi(float size, bool bold = false, bool italic = false)
    {
        var weight = bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        var slant = italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
        var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI", new SKFontStyle(weight, SKFontStyleWidth.Normal, slant)), size);
        ApplyTextQuality(font);
        return font;
    }

    public static SKFont CreateMono(float size)
    {
        var typeface = SKTypeface.FromFamilyName("Cascadia Mono", SKFontStyle.Normal)
                       ?? SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal)
                       ?? SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal);
        var font = new SKFont(typeface, size);
        ApplyTextQuality(font);
        return font;
    }

    public static SKPaint CreateTextPaint(SKColor color) =>
        new()
        {
            IsAntialias = true,
            Color = color,
            Style = SKPaintStyle.Fill,
        };

    public static void DrawText(
        SKCanvas canvas,
        string text,
        float x,
        float y,
        SKTextAlign align,
        SKFont font,
        SKPaint paint,
        float layoutScale = 1f)
    {
        if (layoutScale > 0f && layoutScale != 1f)
        {
            x = SnapForScale(x, layoutScale);
            y = SnapForScale(y, layoutScale);
        }

        canvas.DrawText(text, x, y, align, font, paint);
    }

    private static void ApplyTextQuality(SKFont font)
    {
        font.Edging = SKFontEdging.SubpixelAntialias;
        font.Subpixel = true;
        font.Hinting = SKFontHinting.Normal;
        font.LinearMetrics = false;
    }

    /// <summary>Снап координаты к физическому пикселю при известном layoutScale (RenderScaling).</summary>
    internal static float SnapForScale(float logical, float layoutScale) =>
        MathF.Round(logical * layoutScale) / layoutScale;
}

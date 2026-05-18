#nullable enable
using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Схлопнутый mono-блок кода в ленте чата (ADR 0123 фаза 3).</summary>
internal static class SkiaMonoCodeStrip
{
    public const float Padding = 8f;
    public const float LineHeight = 14f;
    public const int DefaultMaxLines = 8;
    public const float CornerRadius = 6f;

    public static float MeasureHeight(string code, float contentWidth, int maxLines = DefaultMaxLines)
    {
        var lines = WrapLines(code, contentWidth, maxLines);
        return Padding * 2 + Math.Max(1, lines.Count) * LineHeight;
    }

    public static void Draw(
        SKCanvas canvas,
        SKRect bounds,
        ISkiaKitPaintTheme theme,
        string code,
        float contentWidth,
        int maxLines = DefaultMaxLines)
    {
        using var fill = new SKPaint
        {
            Color = SkiaKitColor.Blend(theme.Surface, theme.Border, 0.55f),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        canvas.DrawRoundRect(bounds, CornerRadius, CornerRadius, fill);

        using var border = new SKPaint
        {
            Color = theme.Border,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
        };
        canvas.DrawRoundRect(bounds, CornerRadius, CornerRadius, border);

        var lines = WrapLines(code, contentWidth, maxLines);
        var typeface = SKTypeface.FromFamilyName("Cascadia Mono", SKFontStyle.Normal)
                       ?? SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal)
                       ?? SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal);
        using var font = new SKFont(typeface, 10.5f);

        using var paint = new SKPaint { IsAntialias = true, Color = theme.Content };
        var y = bounds.Top + Padding + LineHeight - 3f;
        foreach (var line in lines)
        {
            canvas.DrawText(line, bounds.Left + Padding, y, SKTextAlign.Left, font, paint);
            y += LineHeight;
        }
    }

    private static List<string> WrapLines(string code, float contentWidth, int maxLines)
    {
        var maxChars = Math.Max(16, (int)(contentWidth / 6.2f));
        var raw = code.Replace("\r", "").Split('\n');
        var lines = new List<string>();
        foreach (var row in raw)
        {
            if (lines.Count >= maxLines)
                break;
            if (row.Length <= maxChars)
            {
                lines.Add(row);
                continue;
            }

            for (var i = 0; i < row.Length && lines.Count < maxLines; i += maxChars)
            {
                var take = Math.Min(maxChars, row.Length - i);
                lines.Add(row.Substring(i, take));
            }
        }

        if (raw.Length > maxLines || code.Split('\n').Length > maxLines)
            lines[^1] = lines[^1].TrimEnd() + " …";

        return lines.Count == 0 ? [""] : lines;
    }
}

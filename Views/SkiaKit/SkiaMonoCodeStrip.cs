#nullable enable
using CascadeIDE.Views.Chat.Skia;
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
        var innerWidth = Math.Max(40f, contentWidth - Padding * 2);
        var rich = SkiaRichTextKitMarkdown.TryMeasurePlain(
            code,
            innerWidth,
            fontSize: 10.5f,
            color: new SKColor(220, 225, 235),
            maxLines: maxLines,
            lineHeight: LineHeight,
            fontFamily: "Cascadia Mono");
        return Padding * 2 + (rich?.BodyHeight ?? LineHeight);
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

        var innerWidth = Math.Max(40f, contentWidth - Padding * 2);
        var rich = SkiaRichTextKitMarkdown.TryMeasurePlain(
            code,
            innerWidth,
            10.5f,
            theme.Content,
            maxLines,
            LineHeight,
            "Cascadia Mono");
        if (rich is null)
            return;

        SkiaRichTextKitMarkdown.Paint(
            canvas,
            new SKPoint(bounds.Left + Padding, bounds.Top + Padding + LineHeight - 3f - 10.5f * 0.85f),
            rich,
            theme.Content,
            theme.Content);
    }
}

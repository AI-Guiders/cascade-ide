#nullable enable

using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Семантика pill-рамки (CodeAnchors, slash TCI, attach-chip, …).</summary>
internal enum SkiaStatusChipSeverity
{
    None = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
    Pending = 4,
    Info = 5,
}

/// <summary>Палитра pill: рамка, заливка, иконка, акцентный текст снаружи/внутри.</summary>
internal readonly record struct SkiaStatusChipColors(
    SKColor Border,
    SKColor Fill,
    SKColor Icon,
    SKColor Accent);

/// <summary>
/// Примитив Skia: скруглённая рамка + опциональная иконка слева (как attach-chip / slash TCI).
/// Текст рисует вызывающий код с <see cref="ContentLeftInRect"/> и <see cref="Accent"/> из палитры.
/// </summary>
internal static class SkiaStatusChip
{
    public const float PadX = 8f;
    public const float PadY = 4f;
    public const float IconBox = 14f;
    public const float IconGap = 5f;
    public const float Corner = 6f;
    public const float MinHeight = 22f;
    public const float BorderStroke = 1.25f;

    /// <summary>Ширина зоны иконки слева от начала текста (pill рисуется левее <c>/</c>).</summary>
    public const float IconLeadingOverhang = PadX + IconBox + IconGap;

    public static float ContentWidthPadding(bool withIcon = true) =>
        PadX * 2f + (withIcon ? IconBox + IconGap : 0f);

    public static float ComputeContentWidth(float labelWidth, bool withIcon = true) =>
        ContentWidthPadding(withIcon) + labelWidth;

    /// <summary>
    /// Pill вокруг контента, начинающегося в <paramref name="textLeft"/> (иконка слева от текста).
    /// </summary>
    public static SKRect ComputeRectAroundTextStart(
        float textLeft,
        float textTop,
        float lineHeight,
        float labelWidth,
        bool withIcon = true)
    {
        var chipW = ComputeContentWidth(labelWidth, withIcon);
        var chipH = Math.Max(lineHeight + PadY * 2f, MinHeight);
        var chipTop = textTop + (lineHeight - chipH) * 0.5f;
        var chipLeft = withIcon
            ? textLeft - PadX - IconBox - IconGap
            : textLeft - PadX;
        return new SKRect(chipLeft, chipTop, chipLeft + chipW, chipTop + chipH);
    }

    public static float ContentLeftInRect(SKRect chipRect, bool withIcon = true) =>
        chipRect.Left + (withIcon ? PadX + IconBox + IconGap : PadX);

    public static SKPoint IconCenterInRect(SKRect chipRect) =>
        new(chipRect.Left + PadX + IconBox * 0.5f, chipRect.MidY);

    public static SkiaStatusChipColors ResolveColors(
        ISkiaKitPaintTheme theme,
        SkiaStatusChipSeverity severity,
        SKColor? mutedContent = null) =>
        ResolveColors(theme, severity, mutedContent ?? theme.EmptyHint);

    public static void DrawFrame(SKCanvas canvas, SKRect chipRect, in SkiaStatusChipColors colors)
    {
        using var fillPaint = new SKPaint { Color = colors.Fill, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawRoundRect(chipRect, Corner, Corner, fillPaint);
        using var borderPaint = new SKPaint
        {
            Color = colors.Border,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = BorderStroke,
        };
        canvas.DrawRoundRect(chipRect, Corner, Corner, borderPaint);
    }

    public static void DrawIcon(
        SKCanvas canvas,
        SKPoint center,
        SkiaStatusChipSeverity severity,
        SKColor color,
        float fontSize)
    {
        var glyph = GlyphFor(severity);
        if (glyph.Length == 0)
            return;

        DrawIconGlyph(canvas, center, glyph, color, fontSize);
    }

    public static void DrawIconGlyph(
        SKCanvas canvas,
        SKPoint center,
        string glyph,
        SKColor color,
        float fontSize)
    {
        if (string.IsNullOrEmpty(glyph))
            return;

        using var font = new SKFont(
            SKTypeface.FromFamilyName("Segoe UI Symbol", SKFontStyle.Normal),
            fontSize);
        using var paint = new SKPaint { IsAntialias = true, Color = color };
        canvas.DrawText(glyph, center.X, center.Y + 4f, SKTextAlign.Center, font, paint);
    }

    public static string GlyphFor(SkiaStatusChipSeverity severity) =>
        severity switch
        {
            SkiaStatusChipSeverity.Success => "\u2713",
            SkiaStatusChipSeverity.Warning => "\u26A0",
            SkiaStatusChipSeverity.Error => "\u2715",
            SkiaStatusChipSeverity.Pending => "\u23F1",
            SkiaStatusChipSeverity.Info => "\u2139",
            _ => "",
        };

    public static void DrawChrome(
        SKCanvas canvas,
        SKRect chipRect,
        ISkiaKitPaintTheme theme,
        SkiaStatusChipSeverity severity,
        float iconFontSize,
        bool withIcon = true)
    {
        if (severity == SkiaStatusChipSeverity.None)
            return;

        var colors = ResolveColors(theme, severity);
        DrawFrame(canvas, chipRect, colors);
        if (withIcon)
            DrawIcon(canvas, IconCenterInRect(chipRect), severity, colors.Icon, iconFontSize);
    }

    private static SkiaStatusChipColors ResolveColors(
        ISkiaKitPaintTheme theme,
        SkiaStatusChipSeverity severity,
        SKColor mutedContent) =>
        severity switch
        {
            SkiaStatusChipSeverity.Success => new(
                new SKColor(72, 160, 110, 200),
                SkiaKitColor.Blend(theme.Surface, new SKColor(72, 160, 110), 0.12f),
                new SKColor(100, 200, 140),
                new SKColor(120, 200, 255)),
            SkiaStatusChipSeverity.Warning => new(
                new SKColor(200, 150, 70, 210),
                SkiaKitColor.Blend(theme.Surface, new SKColor(200, 150, 70), 0.14f),
                new SKColor(230, 180, 90),
                new SKColor(140, 190, 255)),
            SkiaStatusChipSeverity.Error => new(
                new SKColor(190, 90, 90, 200),
                SkiaKitColor.Blend(theme.Surface, new SKColor(190, 90, 90), 0.12f),
                new SKColor(220, 110, 110),
                SkiaKitColor.Blend(theme.EmptyHint, theme.Content, 0.35f)),
            SkiaStatusChipSeverity.Pending => new(
                SkiaKitColor.Blend(theme.Border, mutedContent, 0.5f),
                SkiaKitColor.Blend(theme.Surface, theme.Border, 0.2f),
                mutedContent,
                SkiaKitColor.Blend(theme.Content, theme.HoverBorder, 0.45f)),
            SkiaStatusChipSeverity.Info => new(
                SkiaKitColor.Blend(theme.Border, theme.EmptyHint, 0.5f),
                SkiaKitColor.Blend(theme.Surface, theme.Border, 0.2f),
                theme.EmptyHint,
                SkiaKitColor.Blend(theme.Content, theme.HoverBorder, 0.45f)),
            _ => new(theme.Border, theme.Surface, theme.EmptyHint, theme.Content),
        };
}

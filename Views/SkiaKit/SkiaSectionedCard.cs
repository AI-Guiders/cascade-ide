#nullable enable
using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>
/// Карточка с отсеками (UML Class–style compartments): подпись секции, разделитель, строки.
/// Для чата, graph-backed surfaces и прочих IDE-Skia поверхностей.
/// </summary>
internal static class SkiaSectionedCard
{
    public const float CornerRadius = 12f;
    public const float CompartmentLabelHeight = 18f;
    public const float CompartmentPaddingY = 6f;
    public const float LineHeight = 16f;
    public const float HorizontalPadding = 12f;
    public const float MinHeight = 132f;

    public static SkiaSectionedCardModel FromThreeCompartments(
        string topicLabel,
        IReadOnlyList<string> topicLines,
        string tagsLabel,
        IReadOnlyList<string> tagLines,
        string summaryLabel,
        IReadOnlyList<string> summaryLines) =>
        new([
            new SkiaSectionedCardSection(topicLabel, topicLines, SkiaSectionedSectionStyle.Header),
            new SkiaSectionedCardSection(tagsLabel, tagLines),
            new SkiaSectionedCardSection(summaryLabel, summaryLines)
        ]);

    public static float MeasureCompartmentHeight(int lineCount) =>
        CompartmentLabelHeight
        + CompartmentPaddingY
        + Math.Max(1, lineCount) * LineHeight
        + CompartmentPaddingY;

    public static float MeasureTotalHeight(in SkiaSectionedCardModel model)
    {
        var inner = model.Sections.Sum(s => MeasureCompartmentHeight(Math.Max(1, s.Lines.Count)));
        return Math.Max(MinHeight, HorizontalPadding * 2 + inner);
    }

    public static void Draw(
        SKCanvas canvas,
        ISkiaKitPaintTheme theme,
        SKRect bounds,
        float contentLeft,
        float contentWidth,
        in SkiaSectionedCardModel model,
        in SkiaSectionedCardDrawState state)
    {
        DrawShadow(canvas, bounds, CornerRadius);
        using (var fill = new SKPaint
               {
                   Color = state.FillColor,
                   IsAntialias = true,
                   Style = SKPaintStyle.Fill
               })
            canvas.DrawRoundRect(bounds, CornerRadius, CornerRadius, fill);

        DrawFrame(canvas, theme, bounds, in state);

        var y = bounds.Top + HorizontalPadding;
        foreach (var section in model.Sections)
        {
            y = DrawCompartment(
                canvas,
                theme,
                contentLeft,
                bounds.Right,
                y,
                section.Label,
                section.Lines,
                section.Style == SkiaSectionedSectionStyle.Header);
        }
    }

    private static void DrawShadow(SKCanvas canvas, SKRect bounds, float corner)
    {
        var shadowRect = bounds;
        shadowRect.Offset(0, 4);
        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 88),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRoundRect(shadowRect, corner + 1, corner + 1, shadowPaint);
    }

    private static void DrawFrame(SKCanvas canvas, ISkiaKitPaintTheme theme, SKRect bounds, in SkiaSectionedCardDrawState state)
    {
        var borderColor = state.IsFocused
            ? theme.SelectedBorder
            : state.IsHovered
                ? theme.HoverBorder
                : SkiaKitColor.Blend(theme.Border, theme.Content, 0.62f);
        using var stroke = new SKPaint
        {
            Color = borderColor,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = state.IsFocused ? 2.4f : state.IsHovered ? 2f : 1.6f
        };
        canvas.DrawRoundRect(bounds, CornerRadius, CornerRadius, stroke);
    }

    private static float DrawCompartment(
        SKCanvas canvas,
        ISkiaKitPaintTheme theme,
        float contentLeft,
        float boundsRight,
        float top,
        string label,
        IReadOnlyList<string> lines,
        bool isHeader)
    {
        var compartmentTop = top;
        var labelBaseline = compartmentTop + CompartmentLabelHeight - 5f;

        using var labelFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 8.5f);
        using var labelPaint = new SKPaint
        {
            IsAntialias = true,
            Color = SkiaKitColor.Blend(theme.Role, theme.EmptyHint, 0.22f)
        };
        canvas.DrawText(label, contentLeft, labelBaseline, SKTextAlign.Left, labelFont, labelPaint);

        var sepY = compartmentTop + CompartmentLabelHeight;
        using var sepPaint = new SKPaint
        {
            Color = SkiaKitColor.Blend(theme.Border, theme.Content, 0.48f),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.2f
        };
        canvas.DrawLine(contentLeft, sepY, boundsRight - HorizontalPadding, sepY, sepPaint);

        if (isHeader)
        {
            using var titleFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 13f);
            using var titlePaint = new SKPaint { IsAntialias = true, Color = theme.Content };
            var titleY = sepY + CompartmentPaddingY + 12f;
            foreach (var line in lines)
            {
                canvas.DrawText(line, contentLeft, titleY, SKTextAlign.Left, titleFont, titlePaint);
                titleY += LineHeight;
            }

            return compartmentTop + MeasureCompartmentHeight(lines.Count);
        }

        using var bodyFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 11f);
        var textY = sepY + CompartmentPaddingY + 12f;
        foreach (var line in lines)
        {
            var isPlaceholder = IsPlaceholderLine(line);
            using var linePaint = new SKPaint
            {
                IsAntialias = true,
                Color = isPlaceholder
                    ? theme.EmptyHint
                    : SkiaKitColor.Blend(theme.Content, theme.EmptyHint, 0.18f)
            };
            canvas.DrawText(line, contentLeft, textY, SKTextAlign.Left, bodyFont, linePaint);
            textY += LineHeight;
        }

        return compartmentTop + MeasureCompartmentHeight(lines.Count);
    }

    internal static bool IsPlaceholderLine(string line) =>
        line is "—" or "нет тегов"
        || line.StartsWith("Нет краткого", StringComparison.Ordinal)
        || line.StartsWith("Пока без сообщений", StringComparison.Ordinal)
        || line.StartsWith("Задай фокус", StringComparison.Ordinal);
}

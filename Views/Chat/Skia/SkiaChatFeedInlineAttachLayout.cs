#nullable enable

using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>
/// Prose + attach-chip в одной строке, когда хватает ширины (маркер не на отдельной строке).
/// </summary>
internal static class SkiaChatFeedInlineAttachLayout
{
    public const float Gap = 6f;

    public static bool TryMeasurePair(
        string proseText,
        string chipLabel,
        string? anchorShortId,
        float bodyColumnWidth,
        in SkiaChatFeedLayout layout,
        in SkiaChatBubbleSpec proseSpec,
        out float proseBlockWidth,
        out float rowHeight)
    {
        proseBlockWidth = 0f;
        rowHeight = 0f;
        if (string.IsNullOrEmpty(proseText)
            || proseText.Contains('\n', StringComparison.Ordinal)
            || proseText.Contains('\r', StringComparison.Ordinal))
        {
            return false;
        }

        using var font = SkiaChatFeedFontResolver.CreateFont(layout.ProseFamily, layout.ProseFontSize);
        var proseIntrinsicW = font.MeasureText(proseText);
        proseBlockWidth = proseIntrinsicW + layout.TextInset * 2f;

        var narrowMaxChars = layout.MaxCharsForWidth(proseBlockWidth);
        var narrowCtx = new SkiaChatMeasureContext(narrowMaxChars, proseBlockWidth);
        var metrics = SkiaChatBubbleRenderer.Measure(narrowCtx, proseSpec);
        if (metrics.ContentLines.Count > 1)
            return false;

        var proseH = SkiaChatBubbleRenderer.MeasureHeight(proseSpec, metrics);
        if (metrics.RichTextBody is { } rich
            && rich.BodyHeight > layout.ProseLineHeight * 1.35f)
        {
            return false;
        }

        if (proseH > layout.ProseLineHeight * 1.35f)
            return false;

        var chipH = SkiaIntercomAttachLinkChip.MeasureHeight(layout.ForwardHost, layout.AttachChipFontSize);
        var chipW = SkiaIntercomAttachLinkChip.MeasureIntrinsicWidth(
            chipLabel,
            anchorShortId,
            layout.AttachChipFontSize,
            layout.ChipFamily,
            layout.ChipIdFamily);
        if (proseBlockWidth + Gap + chipW > bodyColumnWidth + 0.5f)
            return false;

        rowHeight = Math.Max(proseH, chipH);
        return true;
    }

    public static float ChipTop(float rowTop, float rowHeight, float chipHeight) =>
        rowTop + (rowHeight - chipHeight) * 0.5f;
}

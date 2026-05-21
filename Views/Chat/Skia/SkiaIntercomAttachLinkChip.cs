#nullable enable

using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Attach-ссылка в ленте: pill + рамка/иконка статуса resolve + label (ADR 0134).</summary>
internal static class SkiaIntercomAttachLinkChip
{
    private const float PadX = 8f;
    private const float PadY = 4f;
    private const float IconBox = 14f;
    private const float IconGap = 5f;
    private const float Corner = 6f;
    private const float DefaultLabelFontSize = 11f;
    private const float MinChipHeight = 22f;

    public static IntercomAttachLinkVisualStatus Classify(AttachmentAnchor? anchor, bool messagePending)
    {
        if (messagePending)
            return IntercomAttachLinkVisualStatus.Pending;

        if (anchor is null)
            return IntercomAttachLinkVisualStatus.Failed;

        var outcome = anchor.ResolveOutcome?.Trim();
        if (string.Equals(outcome, IntercomAttachmentRevealPlan.OutcomeFileMissing, StringComparison.OrdinalIgnoreCase))
            return IntercomAttachLinkVisualStatus.Failed;

        if (string.IsNullOrWhiteSpace(anchor.File))
            return IntercomAttachLinkVisualStatus.Pending;

        if (string.Equals(outcome, IntercomAttachmentRevealPlan.OutcomeMemberNotFound, StringComparison.OrdinalIgnoreCase))
            return IntercomAttachLinkVisualStatus.Degraded;

        return IntercomAttachLinkVisualStatus.Resolved;
    }

    public static float MeasureHeight(bool forwardHost, float labelFontSize = DefaultLabelFontSize)
    {
        var baseH = forwardHost ? MinChipHeight - 2f : MinChipHeight;
        return baseH * (labelFontSize / DefaultLabelFontSize);
    }

    public static float MeasureWidth(
        string label,
        string? anchorShortId,
        float maxContentWidth,
        float labelFontSize = DefaultLabelFontSize,
        string? chipFamily = null,
        string? chipIdFamily = null)
    {
        using var font = CreateLabelFont(labelFontSize, chipFamily);
        using var idFont = CreateIdFont(labelFontSize, chipIdFamily);
        var text = normalizeLabel(label);
        var textW = font.MeasureText(text);
        var idW = string.IsNullOrWhiteSpace(anchorShortId) ? 0f : idFont.MeasureText(formatIdSuffix(anchorShortId)) + 4f;
        var w = PadX * 2f + IconBox + IconGap + textW + idW;
        return Math.Min(Math.Max(48f, w), Math.Max(80f, maxContentWidth));
    }

    /// <summary>Ширина chip без усечения под колонку (для inline-раскладки prose+chip).</summary>
    public static float MeasureIntrinsicWidth(
        string label,
        string? anchorShortId,
        float labelFontSize = DefaultLabelFontSize,
        string? chipFamily = null,
        string? chipIdFamily = null)
    {
        using var font = CreateLabelFont(labelFontSize, chipFamily);
        using var idFont = CreateIdFont(labelFontSize, chipIdFamily);
        var text = normalizeLabel(label);
        var textW = font.MeasureText(text);
        var idW = string.IsNullOrWhiteSpace(anchorShortId)
            ? 0f
            : idFont.MeasureText(formatIdSuffix(anchorShortId)) + 4f;
        var w = PadX * 2f + IconBox + IconGap + textW + idW;
        return Math.Max(48f, w);
    }

    public static void Draw(
        SKCanvas canvas,
        SkiaChatTheme theme,
        SKRect chipRect,
        string label,
        IntercomAttachLinkVisualStatus status,
        string? anchorShortId = null,
        float labelFontSize = DefaultLabelFontSize,
        string? chipFamily = null,
        string? chipIdFamily = null)
    {
        var (border, fill, iconColor, linkColor) = colorsFor(theme, status);
        using var fillPaint = new SKPaint { Color = fill, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawRoundRect(chipRect, Corner, Corner, fillPaint);
        using var borderPaint = new SKPaint
        {
            Color = border,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.25f,
        };
        canvas.DrawRoundRect(chipRect, Corner, Corner, borderPaint);

        var iconCenterX = chipRect.Left + PadX + IconBox * 0.5f;
        var iconCenterY = chipRect.MidY;
        DrawStatusIcon(canvas, iconCenterX, iconCenterY, status, iconColor, labelFontSize);

        using var labelFont = CreateLabelFont(labelFontSize, chipFamily);
        var text = normalizeLabel(label);
        var textLeft = chipRect.Left + PadX + IconBox + IconGap;
        var baseline = chipRect.MidY + labelFont.Size * 0.35f;
        using var labelPaint = new SKPaint { IsAntialias = true, Color = linkColor };
        canvas.DrawText(text, textLeft, baseline, SKTextAlign.Left, labelFont, labelPaint);

        if (!string.IsNullOrWhiteSpace(anchorShortId))
        {
            using var idFont = CreateIdFont(labelFontSize, chipIdFamily);
            var idText = formatIdSuffix(anchorShortId);
            var idLeft = textLeft + labelFont.MeasureText(text) + 4f;
            using var idPaint = new SKPaint { IsAntialias = true, Color = theme.MutedContent };
            canvas.DrawText(idText, idLeft, baseline, SKTextAlign.Left, idFont, idPaint);
        }
    }

    public static SKRect ComputeHitRect(SKRect chipRect) =>
        chipRect;

    private static string normalizeLabel(string label)
    {
        var t = label.Trim();
        if (t.Length >= 2 && t[0] == '[' && t[^1] == ']')
            return t[1..^1];
        return t;
    }

    private static SKFont CreateLabelFont(float labelFontSize, string? chipFamily) =>
        SkiaChatFeedFontResolver.CreateFont(
            string.IsNullOrWhiteSpace(chipFamily) ? "Segoe UI" : chipFamily,
            labelFontSize);

    private static SKFont CreateIdFont(float labelFontSize, string? chipIdFamily) =>
        SkiaChatFeedFontResolver.CreateFont(
            string.IsNullOrWhiteSpace(chipIdFamily) ? "Consolas" : chipIdFamily,
            labelFontSize - 1f);

    private static string formatIdSuffix(string anchorShortId) =>
        $"a:{anchorShortId.Trim().ToLowerInvariant()}";

    private static (SKColor Border, SKColor Fill, SKColor Icon, SKColor Link) colorsFor(
        SkiaChatTheme theme,
        IntercomAttachLinkVisualStatus status) =>
        status switch
        {
            IntercomAttachLinkVisualStatus.Resolved => (
                new SKColor(72, 160, 110, 200),
                SkiaKitColor.Blend(theme.Surface, new SKColor(72, 160, 110), 0.12f),
                new SKColor(100, 200, 140),
                new SKColor(120, 200, 255)),
            IntercomAttachLinkVisualStatus.Degraded => (
                new SKColor(200, 150, 70, 210),
                SkiaKitColor.Blend(theme.Surface, new SKColor(200, 150, 70), 0.14f),
                new SKColor(230, 180, 90),
                new SKColor(140, 190, 255)),
            IntercomAttachLinkVisualStatus.Pending => (
                SkiaKitColor.Blend(theme.Border, theme.MutedContent, 0.5f),
                SkiaKitColor.Blend(theme.Surface, theme.Border, 0.2f),
                theme.MutedContent,
                SkiaKitColor.Blend(theme.Content, theme.HoverBorder, 0.45f)),
            _ => (
                new SKColor(190, 90, 90, 200),
                SkiaKitColor.Blend(theme.Surface, new SKColor(190, 90, 90), 0.12f),
                new SKColor(220, 110, 110),
                SkiaKitColor.Blend(theme.MutedContent, theme.Content, 0.35f)),
        };

    private static void DrawStatusIcon(
        SKCanvas canvas,
        float cx,
        float cy,
        IntercomAttachLinkVisualStatus status,
        SKColor color,
        float labelFontSize)
    {
        var glyph = status switch
        {
            IntercomAttachLinkVisualStatus.Resolved => "\u2713",
            IntercomAttachLinkVisualStatus.Degraded => "\u26A0",
            IntercomAttachLinkVisualStatus.Pending => "\u23F1",
            _ => "\u2715",
        };

        using var font = new SKFont(
            SKTypeface.FromFamilyName("Segoe UI Symbol", SKFontStyle.Normal),
            labelFontSize);
        using var paint = new SKPaint { IsAntialias = true, Color = color };
        canvas.DrawText(glyph, cx, cy + 4f, SKTextAlign.Center, font, paint);
    }

}

internal enum IntercomAttachLinkVisualStatus
{
    Resolved = 0,
    Degraded = 1,
    Failed = 2,
    Pending = 3,
}

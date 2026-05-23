#nullable enable

using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Attach-ссылка в ленте: pill + рамка/иконка статуса resolve + label (ADR 0134).</summary>
internal static class SkiaIntercomAttachLinkChip
{
    private const float DefaultLabelFontSize = 11f;

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
        var baseH = forwardHost ? SkiaStatusChip.MinHeight - 2f : SkiaStatusChip.MinHeight;
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
        var w = SkiaStatusChip.ComputeContentWidth(textW + idW);
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
        return Math.Max(48f, SkiaStatusChip.ComputeContentWidth(textW + idW));
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
        var colors = SkiaStatusChip.ResolveColors(theme, ToSeverity(status), theme.MutedContent);
        SkiaStatusChip.DrawFrame(canvas, chipRect, colors);
        SkiaStatusChip.DrawIcon(
            canvas,
            SkiaStatusChip.IconCenterInRect(chipRect, SkiaStatusChipIconPlacement.Left),
            ToSeverity(status),
            colors.Icon,
            labelFontSize);

        using var labelFont = CreateLabelFont(labelFontSize, chipFamily);
        var text = normalizeLabel(label);
        var textLeft = SkiaStatusChip.ContentLeftInRect(chipRect);
        var baseline = chipRect.MidY + labelFont.Size * 0.35f;
        using var labelPaint = new SKPaint { IsAntialias = true, Color = colors.Accent };
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

    private static SkiaStatusChipSeverity ToSeverity(IntercomAttachLinkVisualStatus status) =>
        status switch
        {
            IntercomAttachLinkVisualStatus.Resolved => SkiaStatusChipSeverity.Success,
            IntercomAttachLinkVisualStatus.Degraded => SkiaStatusChipSeverity.Warning,
            IntercomAttachLinkVisualStatus.Pending => SkiaStatusChipSeverity.Pending,
            _ => SkiaStatusChipSeverity.Error,
        };
}

internal enum IntercomAttachLinkVisualStatus
{
    Resolved = 0,
    Degraded = 1,
    Failed = 2,
    Pending = 3,
}

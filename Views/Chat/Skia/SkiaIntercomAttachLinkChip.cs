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
    private const float FontSize = 11f;
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

    public static float MeasureHeight(bool compactLayout) =>
        compactLayout ? MinChipHeight - 2f : MinChipHeight;

    public static float MeasureWidth(string label, float maxContentWidth)
    {
        using var font = CreateLabelFont();
        var text = normalizeLabel(label);
        var textW = font.MeasureText(text);
        var w = PadX * 2f + IconBox + IconGap + textW;
        return Math.Min(Math.Max(48f, w), Math.Max(80f, maxContentWidth));
    }

    public static void Draw(
        SKCanvas canvas,
        SkiaChatTheme theme,
        SKRect chipRect,
        string label,
        IntercomAttachLinkVisualStatus status)
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
        DrawStatusIcon(canvas, iconCenterX, iconCenterY, status, iconColor);

        using var labelFont = CreateLabelFont();
        var text = normalizeLabel(label);
        var textLeft = chipRect.Left + PadX + IconBox + IconGap;
        var baseline = chipRect.MidY + labelFont.Size * 0.35f;
        using var labelPaint = new SKPaint { IsAntialias = true, Color = linkColor };
        canvas.DrawText(text, textLeft, baseline, SKTextAlign.Left, labelFont, labelPaint);
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

    private static SKFont CreateLabelFont() =>
        new(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal), FontSize);

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
        SKColor color)
    {
        var glyph = status switch
        {
            IntercomAttachLinkVisualStatus.Resolved => "\u2713",
            IntercomAttachLinkVisualStatus.Degraded => "\u26A0",
            IntercomAttachLinkVisualStatus.Pending => "\u23F1",
            _ => "\u2715",
        };

        using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI Symbol", SKFontStyle.Normal), 11f);
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

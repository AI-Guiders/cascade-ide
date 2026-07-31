#nullable enable
using CascadeIDE.Models.Intercom;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

internal static partial class SkiaChatBubbleRenderer
{
    private const float FeedLinkHitPadX = 2f;
    private const float FeedLinkHitPadY = 2f;

    /// <summary>Узкий hit по фактической строке ссылки в feed-сегменте (не на всю ширину bubble).</summary>
    public static SKRect ComputeFeedLinkHitRect(
        SKRect segmentRect,
        string linkText,
        in SkiaChatBubbleMetrics metrics,
        in SkiaChatFeedLayout feed)
    {
        var baseline = feed.FirstLineBaselineY(segmentRect.Top, metrics.TitleHeight);
        var left = segmentRect.Left + feed.TextInset;
        return computeFeedLinkRunHitRect(left, baseline, linkText, metrics.LineHeight, feed.ProseFamily, feed.ProseFontSize);
    }

    /// <summary>Регистрация hit по каждому <see cref="SkiaMarkdownStyle.Link"/> run в feed-prose (через <see cref="SkiaChatDrawContext.HitRegistry"/>).</summary>
    public static void RegisterFeedMarkdownLinkHits(
        SkiaChatDrawContext context,
        SKRect segmentRect,
        in SkiaChatBubbleMetrics metrics,
        in SkiaChatFeedLayout feed,
        int? messageIndex,
        Func<string, AttachmentAnchor?> tryResolveAnchor)
    {
        if (tryResolveAnchor is null)
            return;

        var textY = feed.FirstLineBaselineY(segmentRect.Top, metrics.TitleHeight);
        var xStart = segmentRect.Left + feed.TextInset;

        using var bodyFont = SkiaChatFeedFontResolver.CreateFont(feed.ProseFamily, feed.ProseFontSize);

        foreach (var line in metrics.ContentLines)
        {
            var x = xStart;
            foreach (var run in line.Runs)
            {
                if (run.Text.Length == 0)
                    continue;

                if (run.Style == SkiaMarkdownStyle.Link
                    && tryResolveAnchor(run.Text) is { } anchor)
                {
                    context.RegisterHit(
                        computeFeedLinkRunHitRect(x, textY, run.Text, metrics.LineHeight, feed.ProseFamily, feed.ProseFontSize),
                        new SkiaChatHit(
                            messageIndex,
                            null,
                            ResetDetailMode: false,
                            RevealAttachment: anchor));
                }

                x += bodyFont.MeasureText(run.Text);
            }

            textY += metrics.LineHeight;
        }
    }

    /// <summary>RTK/plain feed: link hit по разбору inline markdown (когда <see cref="SkiaChatBubbleMetrics.RichTextBody"/> задействован).</summary>
    public static void RegisterFeedMarkdownLinkHitsFromText(
        SkiaChatDrawContext context,
        SKRect segmentRect,
        string proseText,
        in SkiaChatFeedLayout feed,
        int? messageIndex,
        Func<string, AttachmentAnchor?> tryResolveAnchor)
    {
        if (tryResolveAnchor is null || string.IsNullOrWhiteSpace(proseText))
            return;

        var maxChars = feed.MaxCharsForWidth(segmentRect.Width);
        var runs = SkiaMarkdownLayout.ParseInline(proseText);
        var lines = SkiaMarkdownLayout.WrapLines(runs, maxChars);
        if (lines.Count == 0)
            return;

        var metrics = new SkiaChatBubbleMetrics(
            lines,
            Footer: null,
            TitleHeight: 0,
            FooterHeight: 0,
            feed.ProseLineHeight);
        RegisterFeedMarkdownLinkHits(context, segmentRect, metrics, feed, messageIndex, tryResolveAnchor);
    }

    private static SKRect computeFeedLinkRunHitRect(
        float left,
        float top,
        string linkText,
        float lineHeight,
        string proseFamily,
        float proseFontSize)
    {
        using var bodyFont = SkiaChatFeedFontResolver.CreateFont(proseFamily, proseFontSize);
        var width = string.IsNullOrWhiteSpace(linkText)
            ? 24f
            : bodyFont.MeasureText(linkText);
        var height = Math.Max(lineHeight, bodyFont.Size + 4f);
        return new SKRect(
            left - FeedLinkHitPadX,
            top - FeedLinkHitPadY,
            left + width + FeedLinkHitPadX,
            top + height + FeedLinkHitPadY);
    }
}

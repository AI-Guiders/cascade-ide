#nullable enable
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

internal enum SkiaChatBubbleKind
{
    Standard,
    /// <summary>Плоская строка ленты Intercom (ADR 0123): meta + тело, без messenger-пузыря.</summary>
    Feed,
    CardPanel,
    OverviewHeader,
    SpineStrip
}

internal readonly record struct SkiaChatBubbleSpec(
    string Title,
    string Body,
    string? Footer,
    SkiaChatBubbleKind Kind,
    SkiaBubbleFillRole FillRole,
    SkiaChatBodyTone BodyTone,
    bool IsPending,
    bool IsSelected,
    bool StartsBranch,
    int? MessageIndex,
    float MinHeight = 0,
    int MaxBodyLines = int.MaxValue,
    float GapAfter = 8,
    float Padding = 10,
    float TitleHeight = 16,
    float FooterHeight = 16,
    float LineHeight = 16,
    float CornerRadius = 7,
    bool ForwardFeedMetrics = false,
    IntercomFontsSettings? IntercomFonts = null);

internal static partial class SkiaChatBubbleRenderer
{
    public static SkiaChatBubbleMetrics Measure(SkiaChatMeasureContext context, in SkiaChatBubbleSpec spec)
    {
        var feed = FeedLayout(spec);
        var maxChars = spec.Kind == SkiaChatBubbleKind.Feed
            ? feed.MaxCharsForWidth(context.ContentWidth)
            : Math.Max(24, context.MaxChars);
        var body = Trim(spec.Body, SkiaChatRenderLimits.MaxProseBodyChars);
        var maxBodyLines = EffectiveMaxBodyLines(spec);
        var titleHeight = string.IsNullOrWhiteSpace(spec.Title) ? 0 : spec.TitleHeight;
        var footerHeight = string.IsNullOrWhiteSpace(spec.Footer) ? 0 : spec.FooterHeight;

        var bodyWidth = BodyWidthForMeasure(context, spec, feed);
        if (spec.BodyTone == SkiaChatBodyTone.Link)
        {
            // Attach label в ленте — один кликабельный токен [label]; не разбираем как code-ref markdown.
            var linkRuns = new List<SkiaMarkdownRun> { new(body, SkiaMarkdownStyle.Link) };
            var linkLines = SkiaMarkdownLayout.WrapLines(linkRuns, maxChars);
            if (linkLines.Count == 0)
                linkLines = [new SkiaMarkdownLine([new SkiaMarkdownRun("", SkiaMarkdownStyle.Plain)])];
            if (linkLines.Count > maxBodyLines)
                linkLines = linkLines.Take(maxBodyLines).ToList();

            return new SkiaChatBubbleMetrics(
                linkLines,
                spec.Footer,
                titleHeight,
                footerHeight,
                spec.LineHeight);
        }

        var bodyColor = ResolveBodyColor(spec.BodyTone);
        var codeColor = new SKColor(180, 190, 210);

        // Intercom feed: WrapLines only — RichTextKit-per-bubble retained multi-GB WS.
        if (spec.Kind == SkiaChatBubbleKind.Feed)
        {
            var feedRuns = SkiaMarkdownLayout.ParseInline(body);
            var feedLines = SkiaMarkdownLayout.WrapLines(feedRuns, maxChars);
            if (feedLines.Count == 0)
                feedLines = [new SkiaMarkdownLine([new SkiaMarkdownRun("", SkiaMarkdownStyle.Plain)])];
            if (feedLines.Count > maxBodyLines)
                feedLines = feedLines.Take(maxBodyLines).ToList();

            return new SkiaChatBubbleMetrics(feedLines, spec.Footer, titleHeight, footerHeight, spec.LineHeight);
        }

        if (ChatMessageBodyPresentation.ShouldUseDocumentLayout(body))
        {
            var document = SkiaRichTextKitMarkdown.TryMeasureDocument(
                body,
                bodyWidth,
                baseFontSize: ProseFontSize(spec, feed),
                bodyColor,
                codeColor,
                maxBodyLines,
                spec.LineHeight,
                forwardHost: spec.ForwardFeedMetrics,
                fontFamily: feed.ProseFamily,
                monoFamily: feed.MonoFamily);
            if (document is not null)
            {
                var placeholder = new SkiaMarkdownLine([new SkiaMarkdownRun("", SkiaMarkdownStyle.Plain)]);
                return new SkiaChatBubbleMetrics(
                    [placeholder],
                    spec.Footer,
                    titleHeight,
                    footerHeight,
                    spec.LineHeight,
                    document);
            }
        }

        var rich = SkiaRichTextKitMarkdown.TryMeasure(
            body,
            bodyWidth,
            fontSize: ProseFontSize(spec, feed),
            bodyColor,
            codeColor,
            maxBodyLines,
            spec.LineHeight,
            fontFamily: feed.ProseFamily,
            monoFamily: feed.MonoFamily);
        if (rich is not null)
        {
            var placeholder = new SkiaMarkdownLine([new SkiaMarkdownRun("", SkiaMarkdownStyle.Plain)]);
            return new SkiaChatBubbleMetrics(
                [placeholder],
                spec.Footer,
                titleHeight,
                footerHeight,
                spec.LineHeight,
                rich);
        }

        var runs = SkiaMarkdownLayout.ParseInline(body);
        var lines = SkiaMarkdownLayout.WrapLines(runs, maxChars);
        if (lines.Count == 0)
            lines = [new SkiaMarkdownLine([new SkiaMarkdownRun("", SkiaMarkdownStyle.Plain)])];
        if (lines.Count > maxBodyLines)
            lines = lines.Take(maxBodyLines).ToList();

        return new SkiaChatBubbleMetrics(lines, spec.Footer, titleHeight, footerHeight, spec.LineHeight);
    }

    private static int EffectiveMaxBodyLines(in SkiaChatBubbleSpec spec)
    {
        if (spec.MaxBodyLines != int.MaxValue)
            return spec.MaxBodyLines;

        return SkiaChatRenderLimits.MaxProseBodyLines;
    }

    private static SkiaChatFeedLayout FeedLayout(in SkiaChatBubbleSpec spec) =>
        SkiaChatFeedLayout.For(spec.ForwardFeedMetrics, spec.IntercomFonts);

    private static float BodyWidthForMeasure(
        SkiaChatMeasureContext context,
        in SkiaChatBubbleSpec spec,
        in SkiaChatFeedLayout feed) =>
        spec.Kind switch
        {
            SkiaChatBubbleKind.Feed => Math.Max(
                SkiaChatFeedLayout.MinColumnWidth,
                context.ContentWidth - feed.ProseMeasureWidthTrim),
            SkiaChatBubbleKind.CardPanel => Math.Max(80f, context.ContentWidth - 40f),
            _ => Math.Max(80f, context.ContentWidth - 24f),
        };

    private static float ProseFontSize(in SkiaChatBubbleSpec spec, in SkiaChatFeedLayout feed) =>
        spec.Kind == SkiaChatBubbleKind.Feed ? feed.ProseFontSize : BodyFontSize(spec.Kind);

    private static float BodyFontSize(SkiaChatBubbleKind kind) =>
        kind switch
        {
            SkiaChatBubbleKind.CardPanel => 11.5f,
            _ => 11f,
        };

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

    public static float MeasureHeight(in SkiaChatBubbleSpec spec, in SkiaChatBubbleMetrics metrics)
    {
        var bodyHeight = metrics.RichTextBody?.BodyHeight
                         ?? metrics.ContentLines.Count * metrics.LineHeight;
        return spec.Kind == SkiaChatBubbleKind.Feed
            ? Math.Max(
                spec.MinHeight,
                2f + metrics.TitleHeight + bodyHeight + metrics.FooterHeight + 2f)
            : Math.Max(
                spec.MinHeight,
                spec.Padding + metrics.TitleHeight + bodyHeight + metrics.FooterHeight + spec.Padding);
    }

    public static void Draw(
        SkiaChatDrawContext ctx,
        SKRect rect,
        in SkiaChatBubbleSpec spec,
        in SkiaChatBubbleMetrics metrics)
    {
        var corner = spec.Kind is SkiaChatBubbleKind.CardPanel ? 12f : spec.Kind is SkiaChatBubbleKind.OverviewHeader ? 8f : 7f;
        var insetX = spec.Kind is SkiaChatBubbleKind.CardPanel ? 20f : 12f;
        var contentLeft = ctx.ContentLeft + insetX;

        if (spec.Kind is SkiaChatBubbleKind.CardPanel)
            DrawCardShadow(ctx.Canvas, rect, corner);

        if (spec.Kind == SkiaChatBubbleKind.SpineStrip)
            DrawSpineStripFrame(ctx, rect, corner, spec);
        else if (spec.Kind == SkiaChatBubbleKind.OverviewHeader)
            DrawOverviewHeaderFrame(ctx, rect);
        else if (spec.Kind == SkiaChatBubbleKind.Feed)
            DrawFeedAccent(ctx, rect, spec);
        else
            DrawStandardFrame(ctx, rect, corner, spec, metrics);

        var feed = FeedLayout(spec);
        var textInset = spec.Kind == SkiaChatBubbleKind.Feed ? feed.TextInset : insetX;
        var textLeft = spec.Kind == SkiaChatBubbleKind.Feed ? rect.Left + textInset : contentLeft;
        DrawText(ctx, rect, textLeft, textInset, spec, metrics, feed);
    }
}

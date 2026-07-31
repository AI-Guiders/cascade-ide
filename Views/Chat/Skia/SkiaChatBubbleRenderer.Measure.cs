#nullable enable
using CascadeIDE.Features.Chat;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

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
}

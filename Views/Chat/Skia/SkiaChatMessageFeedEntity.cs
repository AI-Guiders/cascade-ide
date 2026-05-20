#nullable enable
using CascadeIDE.Features.Chat;
using CascadeIDE.Features.Chat.Application;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Сообщение ленты: prose (flat feed, ADR 0123) + mono code strips, attach-метки, thinking.</summary>
internal sealed class SkiaChatMessageFeedEntity : ISkiaChatEntity
{
    private const float FeedTextInset = 6f;
    private const float TitleGapAfter = 4f;

    private readonly ChatSurfaceEntry _entry;
    private readonly bool _compactLayout;
    private readonly bool _suppressTitle;
    private readonly float _gapAfter;
    private readonly IReadOnlyList<ChatMessageBodySegment> _segments;
    private readonly IReadOnlyList<AttachmentAnchor> _attachments;
    private readonly int _feedOrdinal;

    public SkiaChatMessageFeedEntity(
        ChatSurfaceEntry entry,
        bool compactLayout,
        bool suppressTitle,
        float gapAfter,
        int feedOrdinal = 0)
    {
        _entry = entry;
        _compactLayout = compactLayout;
        _suppressTitle = suppressTitle;
        _gapAfter = gapAfter;
        _feedOrdinal = feedOrdinal;
        _attachments = entry.Attachments ?? [];
        _segments = ChatMessageBodyPresentation.SplitSegments(entry.Body);
    }

    public SkiaChatMeasuredLayout Measure(SkiaChatMeasureContext context)
    {
        var height = MessageTitleBandHeight();
        var gap = 4f;
        foreach (var segment in _segments)
        {
            if (segment.Kind == ChatMessageBodySegmentKind.Code)
            {
                height += SkiaMonoCodeStrip.MeasureHeight(segment.Text, context.ContentWidth) + gap;
                continue;
            }

            foreach (var feedSeg in IntercomFeedProjector.ProjectProse(segment.Text, _attachments))
            {
                if (feedSeg.Kind == IntercomAttachmentFeedSegmentKind.Attachment)
                {
                    height += SkiaIntercomAttachLinkChip.MeasureHeight(_compactLayout) + gap;
                    continue;
                }

                var spec = BuildProseSpec(feedSeg.Text, isAttachment: false);
                var metrics = SkiaChatBubbleRenderer.Measure(context, spec);
                height += SkiaChatBubbleRenderer.MeasureHeight(spec, metrics) + gap;
            }
        }

        return new SkiaChatMeasuredLayout(Math.Max(8f, height - gap), _gapAfter);
    }

    public void Draw(SkiaChatDrawContext context, float top, in SkiaChatMeasuredLayout layout)
    {
        var rowBottom = top + layout.Height;
        var isSelected = _entry.MessageIndex == context.SelectedMessageIndex;
        if (isSelected)
            DrawMessageRowSelection(context, top, rowBottom, includeGutter: _feedOrdinal > 0);
        if (_feedOrdinal > 0)
            DrawFeedGutterOrdinal(context, top, rowBottom, isSelected, _feedOrdinal);

        var y = top;
        if (!_suppressTitle && !string.IsNullOrWhiteSpace(_entry.Title))
        {
            var titleH = MessageTitleBandHeight();
            DrawMessageTitle(context, context.ContentLeft + FeedTextInset, top);
            y = top + titleH + TitleGapAfter;
        }

        var gap = 4f;
        foreach (var segment in _segments)
        {
            if (segment.Kind == ChatMessageBodySegmentKind.Code)
            {
                var h = SkiaMonoCodeStrip.MeasureHeight(segment.Text, context.ContentWidth);
                var rect = new SKRect(context.ContentLeft, y, context.ContentLeft + context.ContentWidth, y + h);
                SkiaMonoCodeStrip.Draw(context.Canvas, rect, context.Theme, segment.Text, context.ContentWidth);
                y += h + gap;
                continue;
            }

            foreach (var feedSeg in IntercomFeedProjector.ProjectProse(segment.Text, _attachments))
            {
                if (feedSeg.Kind == IntercomAttachmentFeedSegmentKind.Attachment)
                {
                    var canReveal = tryResolveAttachmentAnchorForRevealHit(feedSeg, out var anchor);
                    var status = SkiaIntercomAttachLinkChip.Classify(anchor, _entry.IsPending);
                    var chipH = SkiaIntercomAttachLinkChip.MeasureHeight(_compactLayout);
                    var chipW = SkiaIntercomAttachLinkChip.MeasureWidth(feedSeg.Text, context.ContentWidth);
                    var chipRect = new SKRect(context.ContentLeft, y, context.ContentLeft + chipW, y + chipH);
                    SkiaIntercomAttachLinkChip.Draw(
                        context.Canvas,
                        context.Theme,
                        chipRect,
                        feedSeg.Text,
                        status);

                    if (canReveal)
                    {
                        context.RegisterHit(
                            SkiaIntercomAttachLinkChip.ComputeHitRect(chipRect),
                            new SkiaChatHit(
                                _entry.MessageIndex,
                                null,
                                ResetDetailMode: false,
                                RevealAttachment: anchor,
                                RevealAttachmentSelect: false));
                    }

                    y += chipH + gap;
                    continue;
                }

                var spec = BuildProseSpec(feedSeg.Text, isAttachment: false);
                var measure = new SkiaChatMeasureContext(
                    Math.Max(12, (int)(context.ContentWidth / 7.1f)),
                    context.ContentWidth);
                var metrics = SkiaChatBubbleRenderer.Measure(measure, spec);
                var h2 = SkiaChatBubbleRenderer.MeasureHeight(spec, metrics);
                var rect2 = new SKRect(context.ContentLeft, y, context.ContentLeft + context.ContentWidth, y + h2);
                SkiaChatBubbleRenderer.Draw(context, rect2, spec, metrics);
                if (metrics.RichTextBody is not null)
                {
                    SkiaChatBubbleRenderer.RegisterFeedMarkdownLinkHitsFromText(
                        context,
                        rect2,
                        feedSeg.Text,
                        context.ContentWidth,
                        metrics.LineHeight,
                        _entry.MessageIndex,
                        tryResolveLinkRunAnchor);
                }
                else
                {
                    SkiaChatBubbleRenderer.RegisterFeedMarkdownLinkHits(
                        context,
                        rect2,
                        metrics,
                        _entry.MessageIndex,
                        tryResolveLinkRunAnchor);
                }

                y += h2 + gap;
            }
        }

        if (_entry.MessageIndex is { } messageIndex)
        {
            var rowRect = new SKRect(
                _feedOrdinal > 0 ? context.FeedGutterLeft : context.ContentLeft,
                top,
                context.ContentLeft + context.ContentWidth,
                rowBottom);
            context.RegisterHit(
                rowRect,
                new SkiaChatHit(
                    messageIndex,
                    null,
                    ResetDetailMode: false,
                    ToggleThinking: _entry.VisualRole == ChatMessageVisualRole.Thinking));
        }
    }

    public SkiaChatHit? CreateHit(in SkiaChatMeasuredLayout layout) => null;

    private static SKRect MessageRowRect(SkiaChatDrawContext context, float top, float bottom, bool includeGutter)
    {
        var left = includeGutter ? context.FeedGutterLeft : context.ContentLeft;
        return new SKRect(left, top, context.ContentLeft + context.ContentWidth, bottom);
    }

    /// <summary>Сплошная подсветка всей строки сообщения (gutter + контент), без отдельного «огрызка» в gutter.</summary>
    private static void DrawMessageRowSelection(
        SkiaChatDrawContext context,
        float top,
        float bottom,
        bool includeGutter)
    {
        var rect = MessageRowRect(context, top, bottom, includeGutter);
        using var rowFill = new SKPaint
        {
            Color = SkiaKit.SkiaKitColor.Blend(context.Theme.Surface, context.Theme.SelectedBorder, 0.26f),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        context.Canvas.DrawRect(rect, rowFill);

        using var rowStroke = new SKPaint
        {
            Color = context.Theme.SelectedBorder.WithAlpha(140),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
        };
        context.Canvas.DrawRect(rect, rowStroke);
    }

    private static void DrawFeedGutterOrdinal(SkiaChatDrawContext context, float top, float bottom, bool isSelected, int ordinal)
    {
        var gutterRect = new SKRect(context.FeedGutterLeft, top, context.ContentLeft, bottom);

        using var numFont = new SKFont(SKTypeface.FromFamilyName("Cascadia Mono", SKFontStyle.Normal), 10f);
        using var numPaint = new SKPaint
        {
            IsAntialias = true,
            Color = isSelected
                ? context.Theme.SelectedBorder
                : SkiaKit.SkiaKitColor.Blend(context.Theme.Role, context.Theme.Content, 0.55f),
        };
        var baseline = top + numFont.Size * 0.9f + 2f;
        context.Canvas.DrawText(
            ordinal.ToString(),
            gutterRect.Right - 6f,
            baseline,
            SKTextAlign.Right,
            numFont,
            numPaint);
    }

    private bool tryResolveAttachmentAnchorForRevealHit(
        in IntercomAttachmentFeedSegment feedSeg,
        out AttachmentAnchor anchor)
    {
        anchor = feedSeg.Anchor ?? new AttachmentAnchor();
        if (!string.IsNullOrWhiteSpace(anchor.File))
            return true;

        if (tryResolveByMarkerShortId(feedSeg.MarkerShortId, out anchor))
            return true;

        if (tryResolveByDisplayLabel(feedSeg.Text, out anchor))
            return true;

        if (!string.IsNullOrWhiteSpace(feedSeg.MarkerShortId))
        {
            anchor = new AttachmentAnchor { Id = feedSeg.MarkerShortId };
            return true;
        }

        return false;
    }

    private bool tryResolveByMarkerShortId(string? markerShortId, out AttachmentAnchor anchor)
    {
        anchor = new AttachmentAnchor();
        if (string.IsNullOrWhiteSpace(markerShortId))
            return false;

        foreach (var candidate in _attachments)
        {
            if (string.IsNullOrWhiteSpace(candidate.Id)
                || !string.Equals(candidate.Id, markerShortId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            anchor = candidate;
            return true;
        }

        return false;
    }

    private AttachmentAnchor? tryResolveLinkRunAnchor(string linkRunText)
    {
        if (tryResolveByDisplayLabel(linkRunText, out var byLabel)
            || tryResolveByDisplayLabel($"[{linkRunText.Trim()}]", out byLabel))
        {
            return byLabel;
        }

        if (!BracketCodeReferenceParser.TryParse(linkRunText.Trim(), out var reference, out _))
            return null;

        foreach (var candidate in _attachments)
        {
            if (attachmentMatchesBracketReference(candidate, reference))
                return candidate;
        }

        return IntercomAttachmentResolveAtSend.TryResolveBracketDraft(
            reference,
            activeFilePath: null,
            workspaceRoot: null,
            out var draft,
            out _)
            && !string.IsNullOrWhiteSpace(draft.File)
            ? draft
            : null;
    }

    private static bool attachmentMatchesBracketReference(AttachmentAnchor anchor, BracketCodeReference reference)
    {
        if (string.IsNullOrWhiteSpace(anchor.File))
            return false;

        var fileMatch = string.IsNullOrWhiteSpace(reference.File)
            || anchor.File.Replace('\\', '/').EndsWith(
                reference.File!.Replace('\\', '/'),
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                anchor.File.Replace('\\', '/'),
                reference.File.Replace('\\', '/'),
                StringComparison.OrdinalIgnoreCase);

        if (!fileMatch)
            return false;

        if (string.IsNullOrWhiteSpace(reference.MemberKey))
            return string.IsNullOrWhiteSpace(anchor.MemberKey);

        return string.Equals(anchor.MemberKey, reference.MemberKey, StringComparison.Ordinal);
    }

    private bool tryResolveByDisplayLabel(string feedText, out AttachmentAnchor anchor)
    {
        anchor = new AttachmentAnchor();
        var label = stripFeedLinkBrackets(feedText);
        if (string.IsNullOrWhiteSpace(label))
            return false;

        foreach (var candidate in _attachments)
        {
            if (string.IsNullOrWhiteSpace(candidate.DisplayLabel)
                || !string.Equals(candidate.DisplayLabel, label, StringComparison.Ordinal))
            {
                continue;
            }

            anchor = candidate;
            return true;
        }

        return false;
    }

    private static string stripFeedLinkBrackets(string text)
    {
        var t = text.Trim();
        if (t.Length >= 2 && t[0] == '[' && t[^1] == ']')
            return t[1..^1];
        return t;
    }

    private SkiaChatBubbleSpec BuildProseSpec(string body, bool isAttachment)
    {
        var fillRole = SkiaBubbleFillRoleMapping.FromMessageRole(_entry.VisualRole);
        var footer = BuildThinkingFooter();
        var spec = new SkiaChatBubbleSpec(
            Title: "",
            body,
            footer,
            SkiaChatBubbleKind.Feed,
            fillRole,
            isAttachment ? SkiaChatBodyTone.Link : SkiaChatBodyTone.Normal,
            _entry.IsPending,
            _entry.IsSelected,
            _entry.StartsBranch,
            _entry.MessageIndex,
            GapAfter: 0,
            Padding: 0,
            TitleHeight: 0,
            LineHeight: _compactLayout ? 14 : 15,
            MaxBodyLines: SkiaChatRenderLimits.MaxProseBodyLines);
        return SkiaChatDensity.Apply(spec, _compactLayout);
    }

    private float MessageTitleBandHeight() =>
        _suppressTitle || string.IsNullOrWhiteSpace(_entry.Title)
            ? 0f
            : _compactLayout
                ? 14f + TitleGapAfter
                : 16f + TitleGapAfter;

    private void DrawMessageTitle(SkiaChatDrawContext context, float textLeft, float top)
    {
        using var titleFont = new SKFont(
            SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold),
            _compactLayout ? 9.5f : 10f);
        using var titlePaint = new SKPaint { IsAntialias = true, Color = context.Theme.Role };
        var baseline = top + titleFont.Size * 0.85f + 2f;
        context.Canvas.DrawText(_entry.Title, textLeft, baseline, SKTextAlign.Left, titleFont, titlePaint);
    }

    private string? BuildThinkingFooter()
    {
        if (_entry.VisualRole != ChatMessageVisualRole.Thinking)
            return null;

        return ChatMessageBodyPresentation.IsCollapsedThinking(_entry.Body)
            ? "Двойной щелчок — развернуть"
            : "Двойной щелчок — свернуть";
    }
}

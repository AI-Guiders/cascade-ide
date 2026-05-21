#nullable enable
using CascadeIDE.Models;
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
    private readonly ChatSurfaceEntry _entry;
    private readonly SkiaChatFeedLayout _layout;
    private readonly bool _forwardHost;
    private readonly bool _suppressTitle;
    private readonly float _gapAfter;
    private readonly IReadOnlyList<ChatMessageBodySegment> _segments;
    private readonly IReadOnlyList<AttachmentAnchor> _attachments;
    private readonly int _feedOrdinal;

    public SkiaChatMessageFeedEntity(
        ChatSurfaceEntry entry,
        bool forwardHost,
        bool suppressTitle,
        float gapAfter,
        int feedOrdinal = 0,
        IntercomFontsSettings? intercomFonts = null)
    {
        _entry = entry;
        _forwardHost = forwardHost;
        _layout = SkiaChatFeedLayout.For(forwardHost, intercomFonts);
        _suppressTitle = suppressTitle;
        _gapAfter = gapAfter;
        _feedOrdinal = feedOrdinal;
        _attachments = entry.Attachments ?? [];
        _segments = ChatMessageBodyPresentation.SplitSegments(entry.Body);
    }

    private bool ShowRoleRail =>
        _layout.ShouldShowRoleRail(_entry.Title, _suppressTitle);

    private FeedBodyColumn BodyColumn(SkiaChatDrawContext context) =>
        _layout.BodyColumn(context.ContentLeft, context.ContentWidth, ShowRoleRail);

    public SkiaChatMeasuredLayout Measure(SkiaChatMeasureContext context)
    {
        var bodyContext = _layout.NarrowMeasureContext(context, ShowRoleRail);
        var height = 0f;
        var gap = _layout.SegmentGap;
        foreach (var segment in _segments)
        {
            if (segment.Kind == ChatMessageBodySegmentKind.Code)
            {
                height += SkiaMonoCodeStrip.MeasureHeight(segment.Text, bodyContext.ContentWidth) + gap;
                continue;
            }

            var feedSegs = IntercomFeedProjector.ProjectProse(segment.Text, _attachments);
            for (var fi = 0; fi < feedSegs.Count; fi++)
            {
                var feedSeg = feedSegs[fi];
                if (feedSeg.Kind == IntercomAttachmentFeedSegmentKind.Attachment)
                {
                    height += SkiaIntercomAttachLinkChip.MeasureHeight(_forwardHost, _layout.AttachChipFontSize) + gap;
                    continue;
                }

                if (fi + 1 < feedSegs.Count
                    && feedSegs[fi + 1].Kind == IntercomAttachmentFeedSegmentKind.Attachment)
                {
                    var attachSeg = feedSegs[fi + 1];
                    var proseSpec = BuildProseSpec(feedSeg.Text, isAttachment: false);
                    var anchorId = attachSeg.Anchor?.Id ?? attachSeg.MarkerShortId;
                    if (SkiaChatFeedInlineAttachLayout.TryMeasurePair(
                            feedSeg.Text,
                            attachSeg.Text,
                            anchorId,
                            bodyContext.ContentWidth,
                            _layout,
                            proseSpec,
                            out _,
                            out var rowH))
                    {
                        height += rowH + gap;
                        fi++;
                        continue;
                    }
                }

                var spec = BuildProseSpec(feedSeg.Text, isAttachment: false);
                var metrics = SkiaChatBubbleRenderer.Measure(bodyContext, spec);
                height += SkiaChatBubbleRenderer.MeasureHeight(spec, metrics) + gap;
            }
        }

        return new SkiaChatMeasuredLayout(Math.Max(8f, height - gap), _gapAfter);
    }

    public void Draw(SkiaChatDrawContext context, float top, in SkiaChatMeasuredLayout layout)
    {
        var rowBottom = top + layout.Height;
        var isSelected = context.IsMessageHighlighted(_entry.MessageIndex);
        if (isSelected)
            DrawMessageRowSelection(context, top, rowBottom, includeGutter: _feedOrdinal > 0);
        if (_feedOrdinal > 0)
            SkiaChatFeedGutter.DrawOrdinal(context, top, rowBottom, _feedOrdinal, _layout, isSelected);

        if (ShowRoleRail)
        {
            SkiaChatFeedRoleRail.Draw(
                context.Canvas,
                context.Theme,
                context.ContentLeft,
                top,
                rowBottom,
                _entry.Title,
                _layout);
        }

        var bodyColumn = BodyColumn(context);
        var y = top;
        var gap = _layout.SegmentGap;
        foreach (var segment in _segments)
        {
            if (segment.Kind == ChatMessageBodySegmentKind.Code)
            {
                var h = SkiaMonoCodeStrip.MeasureHeight(segment.Text, bodyColumn.Width);
                var rect = _layout.SegmentRect(bodyColumn, y, h);
                SkiaMonoCodeStrip.Draw(context.Canvas, rect, context.Theme, segment.Text, bodyColumn.Width);
                y += h + gap;
                continue;
            }

            var feedSegs = IntercomFeedProjector.ProjectProse(segment.Text, _attachments);
            for (var fi = 0; fi < feedSegs.Count; fi++)
            {
                var feedSeg = feedSegs[fi];
                if (feedSeg.Kind == IntercomAttachmentFeedSegmentKind.Attachment)
                {
                    y += drawAttachmentChip(context, bodyColumn, y, feedSeg) + gap;
                    continue;
                }

                if (fi + 1 < feedSegs.Count
                    && feedSegs[fi + 1].Kind == IntercomAttachmentFeedSegmentKind.Attachment)
                {
                    var attachSeg = feedSegs[fi + 1];
                    var proseSpec = BuildProseSpec(feedSeg.Text, isAttachment: false);
                    var anchorId = attachSeg.Anchor?.Id ?? attachSeg.MarkerShortId;
                    if (SkiaChatFeedInlineAttachLayout.TryMeasurePair(
                            feedSeg.Text,
                            attachSeg.Text,
                            anchorId,
                            bodyColumn.Width,
                            _layout,
                            proseSpec,
                            out var proseBlockW,
                            out var rowH))
                    {
                        y += drawInlineProseAttach(
                                context,
                                bodyColumn,
                                y,
                                feedSeg,
                                attachSeg,
                                proseSpec,
                                proseBlockW,
                                rowH)
                            + gap;
                        fi++;
                        continue;
                    }
                }

                y += drawProseSegment(context, bodyColumn, y, feedSeg) + gap;
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

    private float drawAttachmentChip(
        SkiaChatDrawContext context,
        FeedBodyColumn bodyColumn,
        float y,
        in IntercomAttachmentFeedSegment feedSeg)
    {
        var canReveal = tryResolveAttachmentAnchorForRevealHit(feedSeg, out var anchor);
        var status = SkiaIntercomAttachLinkChip.Classify(anchor, _entry.IsPending);
        var chipH = SkiaIntercomAttachLinkChip.MeasureHeight(_forwardHost, _layout.AttachChipFontSize);
        var chipW = SkiaIntercomAttachLinkChip.MeasureWidth(
            feedSeg.Text,
            anchor.Id,
            bodyColumn.Width,
            _layout.AttachChipFontSize,
            _layout.ChipFamily,
            _layout.ChipIdFamily);
        var chipRect = new SKRect(bodyColumn.Left, y, bodyColumn.Left + chipW, y + chipH);
        SkiaIntercomAttachLinkChip.Draw(
            context.Canvas,
            context.Theme,
            chipRect,
            feedSeg.Text,
            status,
            anchor.Id,
            _layout.AttachChipFontSize,
            _layout.ChipFamily,
            _layout.ChipIdFamily);

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

        return chipH;
    }

    private float drawInlineProseAttach(
        SkiaChatDrawContext context,
        FeedBodyColumn bodyColumn,
        float y,
        in IntercomAttachmentFeedSegment proseSeg,
        in IntercomAttachmentFeedSegment attachSeg,
        in SkiaChatBubbleSpec proseSpec,
        float proseBlockWidth,
        float rowHeight)
    {
        var narrowMaxChars = _layout.MaxCharsForWidth(proseBlockWidth);
        var narrowCtx = new SkiaChatMeasureContext(narrowMaxChars, proseBlockWidth);
        var metrics = SkiaChatBubbleRenderer.Measure(narrowCtx, proseSpec);
        var proseH = SkiaChatBubbleRenderer.MeasureHeight(proseSpec, metrics);
        var proseRect = new SKRect(
            bodyColumn.Left,
            y,
            bodyColumn.Left + proseBlockWidth,
            y + proseH);
        SkiaChatBubbleRenderer.Draw(context, proseRect, proseSpec, metrics);
        if (metrics.RichTextBody is not null)
        {
            SkiaChatBubbleRenderer.RegisterFeedMarkdownLinkHitsFromText(
                context,
                proseRect,
                proseSeg.Text,
                _layout,
                _entry.MessageIndex,
                tryResolveLinkRunAnchor);
        }
        else
        {
            SkiaChatBubbleRenderer.RegisterFeedMarkdownLinkHits(
                context,
                proseRect,
                metrics,
                _layout,
                _entry.MessageIndex,
                tryResolveLinkRunAnchor);
        }

        var canReveal = tryResolveAttachmentAnchorForRevealHit(attachSeg, out var anchor);
        var status = SkiaIntercomAttachLinkChip.Classify(anchor, _entry.IsPending);
        var chipH = SkiaIntercomAttachLinkChip.MeasureHeight(_forwardHost, _layout.AttachChipFontSize);
        var chipW = SkiaIntercomAttachLinkChip.MeasureIntrinsicWidth(
            attachSeg.Text,
            anchor.Id,
            _layout.AttachChipFontSize,
            _layout.ChipFamily,
            _layout.ChipIdFamily);
        var chipLeft = bodyColumn.Left + proseBlockWidth + SkiaChatFeedInlineAttachLayout.Gap;
        var chipTop = SkiaChatFeedInlineAttachLayout.ChipTop(y, rowHeight, chipH);
        var chipRect = new SKRect(chipLeft, chipTop, chipLeft + chipW, chipTop + chipH);
        SkiaIntercomAttachLinkChip.Draw(
            context.Canvas,
            context.Theme,
            chipRect,
            attachSeg.Text,
            status,
            anchor.Id,
            _layout.AttachChipFontSize,
            _layout.ChipFamily,
            _layout.ChipIdFamily);

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

        return rowHeight;
    }

    private float drawProseSegment(
        SkiaChatDrawContext context,
        FeedBodyColumn bodyColumn,
        float y,
        in IntercomAttachmentFeedSegment feedSeg)
    {
        var spec = BuildProseSpec(feedSeg.Text, isAttachment: false);
        var measure = new SkiaChatMeasureContext(
            _layout.MaxCharsForWidth(bodyColumn.Width),
            bodyColumn.Width);
        var metrics = SkiaChatBubbleRenderer.Measure(measure, spec);
        var h2 = SkiaChatBubbleRenderer.MeasureHeight(spec, metrics);
        var rect2 = _layout.SegmentRect(bodyColumn, y, h2);
        SkiaChatBubbleRenderer.Draw(context, rect2, spec, metrics);
        if (metrics.RichTextBody is not null)
        {
            SkiaChatBubbleRenderer.RegisterFeedMarkdownLinkHitsFromText(
                context,
                rect2,
                feedSeg.Text,
                _layout,
                _entry.MessageIndex,
                tryResolveLinkRunAnchor);
        }
        else
        {
            SkiaChatBubbleRenderer.RegisterFeedMarkdownLinkHits(
                context,
                rect2,
                metrics,
                _layout,
                _entry.MessageIndex,
                tryResolveLinkRunAnchor);
        }

        return h2;
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
            LineHeight: _layout.ProseLineHeight,
            MaxBodyLines: SkiaChatRenderLimits.MaxProseBodyLines,
            ForwardFeedMetrics: _forwardHost,
            IntercomFonts: _layout.Fonts);
        return SkiaChatDensity.Apply(spec, _forwardHost);
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

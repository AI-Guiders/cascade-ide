#nullable enable
using CascadeIDE.Features.Chat;
using CascadeIDE.Models.Intercom;
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

    public SkiaChatMessageFeedEntity(
        ChatSurfaceEntry entry,
        bool compactLayout,
        bool suppressTitle,
        float gapAfter)
    {
        _entry = entry;
        _compactLayout = compactLayout;
        _suppressTitle = suppressTitle;
        _gapAfter = gapAfter;
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
                var prose = feedSeg.Text;
                var spec = BuildProseSpec(prose, feedSeg.Kind == IntercomAttachmentFeedSegmentKind.Attachment);
                var metrics = SkiaChatBubbleRenderer.Measure(context, spec);
                height += SkiaChatBubbleRenderer.MeasureHeight(spec, metrics) + gap;
            }
        }

        return new SkiaChatMeasuredLayout(Math.Max(8f, height - gap), _gapAfter);
    }

    public void Draw(SkiaChatDrawContext context, float top, in SkiaChatMeasuredLayout layout)
    {
        var y = top;
        if (!_suppressTitle && !string.IsNullOrWhiteSpace(_entry.Title))
        {
            var titleH = MessageTitleBandHeight();
            var titleRect = new SKRect(
                context.ContentLeft,
                top,
                context.ContentLeft + context.ContentWidth,
                top + titleH);
            DrawMessageTitle(context, context.ContentLeft + FeedTextInset, top);
            context.RegisterHit(
                titleRect,
                new SkiaChatHit(_entry.MessageIndex, null, ResetDetailMode: false));
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
                var spec = BuildProseSpec(feedSeg.Text, feedSeg.Kind == IntercomAttachmentFeedSegmentKind.Attachment);
                var measure = new SkiaChatMeasureContext(
                    Math.Max(12, (int)(context.ContentWidth / 7.1f)),
                    context.ContentWidth);
                var metrics = SkiaChatBubbleRenderer.Measure(measure, spec);
                var h2 = SkiaChatBubbleRenderer.MeasureHeight(spec, metrics);
                var rect2 = new SKRect(context.ContentLeft, y, context.ContentLeft + context.ContentWidth, y + h2);
                SkiaChatBubbleRenderer.Draw(context, rect2, spec, metrics);

                if (feedSeg.Kind == IntercomAttachmentFeedSegmentKind.Attachment
                    && tryResolveAttachmentAnchorForHit(feedSeg, out var anchor))
                {
                    context.RegisterHit(
                        rect2,
                        new SkiaChatHit(
                            _entry.MessageIndex,
                            null,
                            ResetDetailMode: false,
                            RevealAttachment: anchor,
                            RevealAttachmentSelect: false));
                }

                y += h2 + gap;
            }
        }
    }

    public SkiaChatHit? CreateHit(in SkiaChatMeasuredLayout layout) => null;

    private bool tryResolveAttachmentAnchorForHit(
        in IntercomAttachmentFeedSegment feedSeg,
        out AttachmentAnchor anchor)
    {
        anchor = feedSeg.Anchor ?? new AttachmentAnchor();
        if (!string.IsNullOrWhiteSpace(anchor.File))
            return true;

        if (string.IsNullOrWhiteSpace(feedSeg.MarkerShortId))
            return false;

        foreach (var candidate in _attachments)
        {
            if (string.IsNullOrWhiteSpace(candidate.Id)
                || !string.Equals(candidate.Id, feedSeg.MarkerShortId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            anchor = candidate;
            return true;
        }

        return false;
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

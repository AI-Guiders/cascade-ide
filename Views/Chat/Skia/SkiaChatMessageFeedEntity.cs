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
        var height = 0f;
        var gap = 4f;
        foreach (var segment in _segments)
        {
            if (segment.Kind == ChatMessageBodySegmentKind.Code)
            {
                height += SkiaMonoCodeStrip.MeasureHeight(segment.Text, context.ContentWidth) + gap;
                continue;
            }

            foreach (var feedSeg in buildFeedProseSegments(segment.Text))
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

            foreach (var feedSeg in buildFeedProseSegments(segment.Text))
            {
                var spec = BuildProseSpec(feedSeg.Text, feedSeg.Kind == IntercomAttachmentFeedSegmentKind.Attachment);
                var measure = new SkiaChatMeasureContext(
                    Math.Max(12, (int)(context.ContentWidth / 7.1f)),
                    context.ContentWidth);
                var metrics = SkiaChatBubbleRenderer.Measure(measure, spec);
                var h2 = SkiaChatBubbleRenderer.MeasureHeight(spec, metrics);
                var rect2 = new SKRect(context.ContentLeft, y, context.ContentLeft + context.ContentWidth, y + h2);
                SkiaChatBubbleRenderer.Draw(context, rect2, spec, metrics);

                if (feedSeg is { Kind: IntercomAttachmentFeedSegmentKind.Attachment, Anchor: { } anchor })
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

    public SkiaChatHit? CreateHit(in SkiaChatMeasuredLayout layout)
    {
        var canToggle = ChatMessageBodyPresentation.CanToggleThinking(_entry.VisualRole);
        return new SkiaChatHit(
            _entry.MessageIndex,
            null,
            ResetDetailMode: false,
            ToggleThinking: canToggle);
    }

    private IEnumerable<IntercomAttachmentFeedSegment> buildFeedProseSegments(string prose) =>
        IntercomAttachmentMarkers.SplitFeedSegments(prose, _attachments);

    private SkiaChatBubbleSpec BuildProseSpec(string body, bool isAttachment)
    {
        var fillRole = SkiaBubbleFillRoleMapping.FromMessageRole(_entry.VisualRole);
        var footer = BuildThinkingFooter();
        var spec = new SkiaChatBubbleSpec(
            _suppressTitle ? "" : _entry.Title,
            isAttachment ? "📎 " + body : body,
            footer,
            SkiaChatBubbleKind.Feed,
            fillRole,
            isAttachment ? SkiaChatBodyTone.Placeholder : SkiaChatBodyTone.Normal,
            _entry.IsPending,
            _entry.IsSelected,
            _entry.StartsBranch,
            _entry.MessageIndex,
            GapAfter: 0,
            Padding: 0,
            TitleHeight: _suppressTitle ? 0 : _compactLayout ? 14 : 16,
            LineHeight: _compactLayout ? 14 : 15,
            MaxBodyLines: SkiaChatRenderLimits.MaxProseBodyLines);
        return SkiaChatDensity.Apply(spec, _compactLayout);
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

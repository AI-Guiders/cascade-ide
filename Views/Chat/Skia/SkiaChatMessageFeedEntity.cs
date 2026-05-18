#nullable enable
using CascadeIDE.Features.Chat;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Сообщение ленты: prose bubble + опциональные mono code strips, группировка, thinking.</summary>
internal sealed class SkiaChatMessageFeedEntity : ISkiaChatEntity
{
    private readonly ChatSurfaceEntry _entry;
    private readonly bool _compactLayout;
    private readonly bool _suppressTitle;
    private readonly float _gapAfter;
    private readonly IReadOnlyList<ChatMessageBodySegment> _segments;

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

            var prose = BuildProseSpec(segment.Text);
            var metrics = SkiaChatBubbleRenderer.Measure(context, prose);
            height += SkiaChatBubbleRenderer.MeasureHeight(prose, metrics) + gap;
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

            var spec = BuildProseSpec(segment.Text);
            var measure = new SkiaChatMeasureContext(
                Math.Max(12, (int)(context.ContentWidth / 7.1f)),
                context.ContentWidth);
            var metrics = SkiaChatBubbleRenderer.Measure(measure, spec);
            var h2 = SkiaChatBubbleRenderer.MeasureHeight(spec, metrics);
            var rect2 = new SKRect(context.ContentLeft, y, context.ContentLeft + context.ContentWidth, y + h2);
            SkiaChatBubbleRenderer.Draw(context, rect2, spec, metrics);
            y += h2 + gap;
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

    private SkiaChatBubbleSpec BuildProseSpec(string body)
    {
        var fillRole = SkiaBubbleFillRoleMapping.FromMessageRole(_entry.VisualRole);
        var footer = BuildThinkingFooter();
        var spec = new SkiaChatBubbleSpec(
            _suppressTitle ? "" : _entry.Title,
            body,
            footer,
            SkiaChatBubbleKind.Standard,
            fillRole,
            SkiaChatBodyTone.Normal,
            _entry.IsPending,
            _entry.IsSelected,
            _entry.StartsBranch,
            _entry.MessageIndex,
            GapAfter: 0,
            Padding: _compactLayout ? 8 : 10,
            TitleHeight: _suppressTitle ? 0 : _compactLayout ? 14 : 16,
            LineHeight: _compactLayout ? 14 : 15);
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

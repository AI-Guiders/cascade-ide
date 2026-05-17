#nullable enable
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Универсальная Skia-сущность на базе <see cref="SkiaChatBubbleSpec"/> (без дублирования Measure/Draw).</summary>
internal sealed class SkiaChatBubbleEntity : ISkiaChatEntity
{
    private readonly SkiaChatBubbleSpec _spec;
    private readonly Func<SkiaChatHit?> _createHit;

    public SkiaChatBubbleEntity(SkiaChatBubbleSpec spec, Func<SkiaChatHit?> createHit)
    {
        _spec = spec;
        _createHit = createHit;
    }

    public SkiaChatMeasuredLayout Measure(SkiaChatMeasureContext context)
    {
        var metrics = SkiaChatBubbleRenderer.Measure(context, _spec);
        return new SkiaChatMeasuredLayout(
            SkiaChatBubbleRenderer.MeasureHeight(_spec, metrics),
            _spec.GapAfter,
            Bubble: metrics);
    }

    public void Draw(SkiaChatDrawContext context, float top, in SkiaChatMeasuredLayout layout)
    {
        var rect = new SKRect(context.ContentLeft, top, context.ContentLeft + context.ContentWidth, top + layout.Height);
        SkiaChatBubbleRenderer.Draw(context, rect, _spec, layout.Bubble!.Value);
    }

    public SkiaChatHit? CreateHit(in SkiaChatMeasuredLayout layout) => _createHit();
}

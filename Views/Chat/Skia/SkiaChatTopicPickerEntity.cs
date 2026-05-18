#nullable enable
using CascadeIDE.Features.Chat;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Интерактивный список/дерево тем после <c>/topic list|tree</c>.</summary>
internal sealed class SkiaChatTopicPickerEntity(
    TopicPickerPresentation mode,
    IReadOnlyList<ChatThreadNode> threads,
    IReadOnlyDictionary<Guid, int> messageCounts) : ISkiaChatEntity
{
    private const float Padding = 10f;
    private const float RowHeight = 34f;
    private const float RowGap = 4f;
    private const float IndentPerDepth = 14f;

    private readonly IReadOnlyList<ChatThreadPresentation.PickerRow> _rows =
        ChatThreadPresentation.BuildPickerRows(mode, threads, messageCounts);

    public SkiaChatMeasuredLayout Measure(SkiaChatMeasureContext context)
    {
        if (_rows.Count == 0)
            return new SkiaChatMeasuredLayout(40f, 8f);

        var height = Padding + 20f + _rows.Count * RowHeight + (_rows.Count - 1) * RowGap + Padding;
        return new SkiaChatMeasuredLayout(height, 10f);
    }

    public void Draw(SkiaChatDrawContext context, float top, in SkiaChatMeasuredLayout layout)
    {
        var rect = new SKRect(context.ContentLeft, top, context.ContentLeft + context.ContentWidth, top + layout.Height);
        using var panelFill = new SKPaint
        {
            Color = SkiaKitColor.Blend(context.Theme.Surface, context.Theme.Border, 0.12f),
            IsAntialias = true,
        };
        context.Canvas.DrawRoundRect(rect, 8, 8, panelFill);

        using var headerFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), 12f);
        using var headerPaint = new SKPaint { IsAntialias = true, Color = context.Theme.MutedContent };
        var header = mode == TopicPickerPresentation.Tree
            ? "Дерево тем — клик для открытия"
            : "Список тем — клик для открытия";
        context.Canvas.DrawText(header, rect.Left + Padding, rect.Top + Padding + 12f, SKTextAlign.Left, headerFont, headerPaint);

        var y = rect.Top + Padding + 20f;
        foreach (var row in _rows)
        {
            var rowRect = new SKRect(rect.Left + Padding, y, rect.Right - Padding, y + RowHeight);
            using var rowFill = new SKPaint
            {
                Color = SkiaKitColor.Blend(context.Theme.Surface, context.Theme.Border, 0.2f),
                IsAntialias = true,
            };
            context.Canvas.DrawRoundRect(rowRect, 6, 6, rowFill);

            using var titleFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal), 12.5f);
            using var titlePaint = new SKPaint { IsAntialias = true, Color = context.Theme.Content };
            var titleX = rowRect.Left + 8f + row.Depth * IndentPerDepth;
            context.Canvas.DrawText(row.Title, titleX, rowRect.MidY + 4f, SKTextAlign.Left, titleFont, titlePaint);

            using var metaFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal), 10.5f);
            using var metaPaint = new SKPaint { IsAntialias = true, Color = context.Theme.MutedContent };
            context.Canvas.DrawText(row.Meta, rowRect.Right - 8f, rowRect.MidY + 4f, SKTextAlign.Right, metaFont, metaPaint);

            context.RegisterHit(rowRect, new SkiaChatHit(null, row.ThreadId, ResetDetailMode: false));
            y += RowHeight + RowGap;
        }
    }

    public SkiaChatHit? CreateHit(in SkiaChatMeasuredLayout layout) => null;
}

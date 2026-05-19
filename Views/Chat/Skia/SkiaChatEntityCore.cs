#nullable enable
using Avalonia;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

internal readonly record struct SkiaChatMeasureContext(int MaxChars, float ContentWidth)
{
    public SkiaChatMeasureContext WithContentWidth(float width) =>
        new(Math.Max(12, (int)(width / 7.1f)), Math.Max(120f, width));
}

internal readonly record struct SkiaChatHit(
    int? MessageIndex,
    Guid? SelectThreadId,
    bool ResetDetailMode,
    bool ToggleThinking = false);

internal readonly record struct SkiaChatMeasuredLayout(
    float Height,
    float GapAfter,
    SkiaChatBubbleMetrics? Bubble = null,
    SkiaTopicCardModel? TopicCard = null,
    SkiaTopicCardLayout? TopicCardLayout = null);

internal readonly record struct SkiaChatBubbleMetrics(
    IReadOnlyList<SkiaMarkdownLine> ContentLines,
    string? Footer,
    float TitleHeight,
    float FooterHeight,
    float LineHeight);

internal readonly record struct SkiaChatPlacedEntity(
    ISkiaChatEntity Entity,
    float Top,
    SkiaChatMeasuredLayout Layout,
    float Left = float.NaN,
    float Width = float.NaN);

internal sealed class SkiaChatDrawContext
{
    public required SKCanvas Canvas { get; init; }
    public required SkiaChatTheme Theme { get; init; }
    public required float ContentLeft { get; init; }
    public required float ContentWidth { get; init; }
    public required float ScrollOffset { get; init; }
    public required int ItemIndex { get; init; }
    public required int HoveredItemIndex { get; init; }
    public required int SelectedMessageIndex { get; init; }
    public required List<(Rect Bounds, SkiaChatHit Hit)> HitTargets { get; init; }

    public bool IsHovered => ItemIndex == HoveredItemIndex;

    public void RegisterHit(SKRect rect, SkiaChatHit hit) =>
        HitTargets.Add((
            new Rect(rect.Left, rect.Top - ScrollOffset, rect.Width, rect.Height),
            hit));
}

internal interface ISkiaChatEntity
{
    SkiaChatMeasuredLayout Measure(SkiaChatMeasureContext context);
    void Draw(SkiaChatDrawContext context, float top, in SkiaChatMeasuredLayout layout);
    SkiaChatHit? CreateHit(in SkiaChatMeasuredLayout layout);
}

internal interface IGridTileSkiaChatEntity
{
    bool IsGridTile { get; }
}

internal static class SkiaChatLayoutEngine
{
    private static readonly SkiaTileGridOptions GridOptions = new(OriginX: 12f);

    public static IReadOnlyList<SkiaChatPlacedEntity> Layout(
        IReadOnlyList<ISkiaChatEntity> entities,
        SkiaChatMeasureContext context)
    {
        var placed = new List<SkiaChatPlacedEntity>(entities.Count);
        var y = 8f;
        for (var i = 0; i < entities.Count;)
        {
            if (entities[i] is IGridTileSkiaChatEntity { IsGridTile: true } && TryCollectGridRun(entities, i, out var run))
            {
                PlaceGridTiles(placed, run, context, ref y);
                i += run.Count;
                continue;
            }

            var entity = entities[i];
            var layout = entity.Measure(context);
            placed.Add(new SkiaChatPlacedEntity(entity, y, layout));
            y += layout.Height + layout.GapAfter;
            i++;
        }

        return placed;
    }

    private static bool TryCollectGridRun(
        IReadOnlyList<ISkiaChatEntity> entities,
        int start,
        out List<ISkiaChatEntity> run)
    {
        run = [];
        for (var i = start; i < entities.Count; i++)
        {
            if (entities[i] is not IGridTileSkiaChatEntity { IsGridTile: true })
                break;
            run.Add(entities[i]);
        }

        return run.Count > 0;
    }

    private static void PlaceGridTiles(
        List<SkiaChatPlacedEntity> placed,
        IReadOnlyList<ISkiaChatEntity> tiles,
        SkiaChatMeasureContext context,
        ref float y)
    {
        var columns = SkiaTileGridLayout.ComputeColumnCount(context.ContentWidth, in GridOptions);
        var tileWidth = SkiaTileGridLayout.TileWidth(context.ContentWidth, columns, in GridOptions);
        var tileContext = context.WithContentWidth(tileWidth);
        var col = 0;
        var rowStartY = y;
        var rowMaxHeight = 0f;

        for (var i = 0; i < tiles.Count; i++)
        {
            var entity = tiles[i];
            var layout = entity.Measure(tileContext);
            var left = GridOptions.OriginX + col * (tileWidth + GridOptions.GapX);
            placed.Add(new SkiaChatPlacedEntity(entity, rowStartY, layout, left, tileWidth));
            rowMaxHeight = Math.Max(rowMaxHeight, layout.Height);
            col++;
            if (col < columns)
                continue;

            col = 0;
            rowStartY += rowMaxHeight + GridOptions.GapY;
            rowMaxHeight = 0f;
        }

        if (col > 0)
            rowStartY += rowMaxHeight;

        y = rowStartY + (tiles.Count > 0 ? GridOptions.GapY : 0f);
    }

    public static double TotalHeight(IReadOnlyList<SkiaChatPlacedEntity> placed)
    {
        if (placed.Count == 0)
            return 56;
        var last = placed[^1];
        return last.Top + last.Layout.Height + 8;
    }
}

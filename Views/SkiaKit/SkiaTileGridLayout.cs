#nullable enable

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Раскладка плиток в сетке для Skia-surfaces (картотека тем, graph cards, …).</summary>
internal readonly record struct SkiaTileGridOptions(
    float MinTileWidth = 200f,
    float GapX = 12f,
    float GapY = 12f,
    float OriginX = 12f,
    int MaxColumns = 4);

internal readonly record struct SkiaTilePlacement(float Left, float Top, float Width);

internal static class SkiaTileGridLayout
{
    public static int ComputeColumnCount(float contentWidth, in SkiaTileGridOptions options)
    {
        if (contentWidth <= options.MinTileWidth)
            return 1;
        var columns = (int)((contentWidth + options.GapX) / (options.MinTileWidth + options.GapX));
        return Math.Clamp(columns, 1, options.MaxColumns);
    }

    public static float TileWidth(float contentWidth, int columns, in SkiaTileGridOptions options) =>
        (contentWidth - options.GapX * (columns - 1)) / columns;

    /// <summary>
    /// Раскладывает элементы в сетку; <paramref name="measureHeight"/> вызывается с шириной плитки.
    /// Возвращает новую Y после блока и список размещений (порядок совпадает с <paramref name="items"/>).
    /// </summary>
    public static (float NextY, IReadOnlyList<SkiaTilePlacement> Placements) Layout<T>(
        IReadOnlyList<T> items,
        float contentWidth,
        float startY,
        in SkiaTileGridOptions options,
        Func<T, float, float> measureHeight)
    {
        if (items.Count == 0)
            return (startY, Array.Empty<SkiaTilePlacement>());

        var columns = ComputeColumnCount(contentWidth, in options);
        var tileWidth = TileWidth(contentWidth, columns, in options);
        var placements = new SkiaTilePlacement[items.Count];
        var col = 0;
        var rowStartY = startY;
        var rowMaxHeight = 0f;

        for (var i = 0; i < items.Count; i++)
        {
            var height = measureHeight(items[i], tileWidth);
            var left = options.OriginX + col * (tileWidth + options.GapX);
            placements[i] = new SkiaTilePlacement(left, rowStartY, tileWidth);
            rowMaxHeight = Math.Max(rowMaxHeight, height);
            col++;
            if (col < columns)
                continue;

            col = 0;
            rowStartY += rowMaxHeight + options.GapY;
            rowMaxHeight = 0f;
        }

        if (col > 0)
            rowStartY += rowMaxHeight;

        return (rowStartY + options.GapY, placements);
    }
}

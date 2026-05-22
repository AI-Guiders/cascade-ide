#nullable enable
using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Всплывающий список подсказок (slash) над composer. ADR 0123 фаза 2.</summary>
internal static class SkiaPopupList
{
    public const float RowHeight = 52f;
    public const float MaxVisibleRows = 6f;
    public const float HorizontalPadding = 8f;
    public const float HierarchyHeaderHeight = 34f;
    public const float CornerRadius = 8f;

    public static int ViewportRowCount(int rowCount) =>
        rowCount <= 0 ? 0 : (int)Math.Min(MaxVisibleRows, rowCount);

    public static int MaxScrollOffset(int rowCount) =>
        Math.Max(0, rowCount - ViewportRowCount(rowCount));

    public static int ClampScrollOffset(int scrollOffset, int rowCount) =>
        Math.Clamp(scrollOffset, 0, MaxScrollOffset(rowCount));

    /// <summary>Сдвинуть окно так, чтобы <paramref name="selectedIndex"/> был виден (для стрелок).</summary>
    public static int EnsureSelectionVisible(int selectedIndex, int scrollOffset, int rowCount)
    {
        if (rowCount <= 0 || selectedIndex < 0)
            return 0;

        scrollOffset = ClampScrollOffset(scrollOffset, rowCount);
        var viewport = ViewportRowCount(rowCount);
        if (selectedIndex < scrollOffset)
            return selectedIndex;
        if (selectedIndex >= scrollOffset + viewport)
            return ClampScrollOffset(selectedIndex - viewport + 1, rowCount);
        return scrollOffset;
    }

    public static float MeasureHeight(int rowCount, bool showHierarchyHeader = false)
    {
        if (rowCount <= 0)
            return 0f;

        var header = showHierarchyHeader ? HierarchyHeaderHeight : 0f;
        return header + ViewportRowCount(rowCount) * RowHeight + HorizontalPadding * 2;
    }

    public static void Draw(
        SKCanvas canvas,
        SKRect bounds,
        ISkiaKitPaintTheme theme,
        IReadOnlyList<SkiaPopupListRow> rows,
        int selectedIndex,
        int scrollOffset,
        float layoutScale = 1f,
        string? hierarchyPathPrefix = null,
        string? hierarchyNextStep = null,
        string? hierarchyBreadcrumb = null)
    {
        if (rows.Count == 0)
            return;

        scrollOffset = ClampScrollOffset(scrollOffset, rows.Count);
        var showHeader = !string.IsNullOrWhiteSpace(hierarchyPathPrefix)
                         || !string.IsNullOrWhiteSpace(hierarchyNextStep)
                         || !string.IsNullOrWhiteSpace(hierarchyBreadcrumb);

        using var fill = new SKPaint { Color = theme.Surface, IsAntialias = true, Style = SKPaintStyle.Fill };
        canvas.DrawRoundRect(bounds, CornerRadius, CornerRadius, fill);

        using var border = new SKPaint
        {
            Color = theme.Border,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
        };
        canvas.DrawRoundRect(bounds, CornerRadius, CornerRadius, border);

        canvas.Save();
        canvas.ClipRect(bounds);

        var y = bounds.Top + HorizontalPadding;
        if (showHeader)
        {
            DrawHierarchyHeader(
                canvas,
                new SKRect(bounds.Left + 4f, y, bounds.Right - 4f, y + HierarchyHeaderHeight - 2f),
                theme,
                hierarchyPathPrefix,
                hierarchyNextStep,
                hierarchyBreadcrumb,
                layoutScale);
            y += HierarchyHeaderHeight;
        }

        var visible = ViewportRowCount(rows.Count);
        for (var slot = 0; slot < visible; slot++)
        {
            var rowIndex = scrollOffset + slot;
            if (rowIndex >= rows.Count)
                break;

            var row = rows[rowIndex];
            var rowRect = new SKRect(bounds.Left + 4f, y, bounds.Right - 4f, y + RowHeight - 4f);
            if (rowIndex == selectedIndex)
            {
                using var sel = new SKPaint { Color = theme.HoverBorder.WithAlpha(56), IsAntialias = true };
                canvas.DrawRoundRect(rowRect, 4f, 4f, sel);
            }

            if (!string.IsNullOrWhiteSpace(row.Group))
            {
                using var groupFont = SkiaKitFonts.CreateUi(10);
                using var groupPaint = SkiaKitFonts.CreateTextPaint(theme.EmptyHint);
                SkiaKitFonts.DrawText(
                    canvas,
                    row.Group,
                    rowRect.Left + 6f,
                    rowRect.Top + 12f,
                    SKTextAlign.Left,
                    groupFont,
                    groupPaint,
                    layoutScale);
            }

            using var titleFont = SkiaKitFonts.CreateUi(12, bold: true);
            using var titlePaint = SkiaKitFonts.CreateTextPaint(theme.Content);
            SkiaKitFonts.DrawText(
                canvas,
                row.Title,
                rowRect.Left + 6f,
                rowRect.Top + 28f,
                SKTextAlign.Left,
                titleFont,
                titlePaint,
                layoutScale);

            using var subFont = SkiaKitFonts.CreateUi(10);
            using var subPaint = SkiaKitFonts.CreateTextPaint(theme.EmptyHint);
            var subtitle = Truncate(row.Subtitle, 72);
            SkiaKitFonts.DrawText(
                canvas,
                subtitle,
                rowRect.Left + 6f,
                rowRect.Top + 44f,
                SKTextAlign.Left,
                subFont,
                subPaint,
                layoutScale);

            y += RowHeight;
        }

        canvas.Restore();
    }

    public static int HitTestRow(
        SKRect bounds,
        float x,
        float y,
        int rowCount,
        int scrollOffset,
        bool showHierarchyHeader = false)
    {
        if (rowCount <= 0 || !bounds.Contains(x, y))
            return -1;

        scrollOffset = ClampScrollOffset(scrollOffset, rowCount);

        var headerOffset = showHierarchyHeader ? HierarchyHeaderHeight : 0f;
        var localY = y - bounds.Top - HorizontalPadding - headerOffset;
        if (localY < 0)
            return -1;

        var slot = (int)(localY / RowHeight);
        var viewport = ViewportRowCount(rowCount);
        if (slot < 0 || slot >= viewport)
            return -1;

        var index = scrollOffset + slot;
        return index >= 0 && index < rowCount ? index : -1;
    }

    private static void DrawHierarchyHeader(
        SKCanvas canvas,
        SKRect headerRect,
        ISkiaKitPaintTheme theme,
        string? pathPrefix,
        string? nextStep,
        string? breadcrumb,
        float layoutScale)
    {
        using var divider = new SKPaint
        {
            Color = theme.Border.WithAlpha(140),
            IsAntialias = true,
            StrokeWidth = 1f,
        };
        canvas.DrawLine(headerRect.Left, headerRect.Bottom, headerRect.Right, headerRect.Bottom, divider);

        if (!string.IsNullOrWhiteSpace(breadcrumb))
        {
            using var crumbFont = SkiaKitFonts.CreateUi(9);
            using var crumbPaint = SkiaKitFonts.CreateTextPaint(theme.EmptyHint);
            SkiaKitFonts.DrawText(
                canvas,
                Truncate(breadcrumb, 80),
                headerRect.Left + 6f,
                headerRect.Top + 10f,
                SKTextAlign.Left,
                crumbFont,
                crumbPaint,
                layoutScale);
        }

        if (!string.IsNullOrWhiteSpace(pathPrefix))
        {
            using var pathFont = SkiaKitFonts.CreateUi(11, bold: true);
            using var pathPaint = SkiaKitFonts.CreateTextPaint(theme.Content);
            SkiaKitFonts.DrawText(
                canvas,
                Truncate(pathPrefix, 48),
                headerRect.Left + 6f,
                headerRect.Top + 22f,
                SKTextAlign.Left,
                pathFont,
                pathPaint,
                layoutScale);
        }

        if (!string.IsNullOrWhiteSpace(nextStep))
        {
            using var stepFont = SkiaKitFonts.CreateUi(10);
            using var stepPaint = SkiaKitFonts.CreateTextPaint(theme.EmptyHint);
            var stepLabel = $"→ {nextStep}";
            SkiaKitFonts.DrawText(
                canvas,
                stepLabel,
                headerRect.Right - 6f,
                headerRect.Top + 22f,
                SKTextAlign.Right,
                stepFont,
                stepPaint,
                layoutScale);
        }
    }

    private static string Truncate(string text, int maxChars) =>
        text.Length <= maxChars ? text : text[..(maxChars - 1)] + "…";
}

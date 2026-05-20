#nullable enable
using SkiaSharp;

namespace CascadeIDE.Views.SkiaKit;

/// <summary>Всплывающий список подсказок (slash) над composer. ADR 0123 фаза 2.</summary>
internal static class SkiaPopupList
{
    public const float RowHeight = 52f;
    public const float MaxVisibleRows = 4f;
    public const float HorizontalPadding = 8f;
    public const float CornerRadius = 8f;

    public static float MeasureHeight(int rowCount) =>
        rowCount <= 0 ? 0f : Math.Min(MaxVisibleRows, rowCount) * RowHeight + HorizontalPadding * 2;

    public static void Draw(
        SKCanvas canvas,
        SKRect bounds,
        ISkiaKitPaintTheme theme,
        IReadOnlyList<SkiaPopupListRow> rows,
        int selectedIndex,
        float layoutScale = 1f)
    {
        if (rows.Count == 0)
            return;

        using var shadow = new SKPaint { Color = new SKColor(0, 0, 0, 48), IsAntialias = true };
        canvas.DrawRoundRect(bounds, CornerRadius, CornerRadius, shadow);

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

        var y = bounds.Top + HorizontalPadding;
        var visible = (int)Math.Min(MaxVisibleRows, rows.Count);
        for (var i = 0; i < visible; i++)
        {
            var row = rows[i];
            var rowRect = new SKRect(bounds.Left + 4f, y, bounds.Right - 4f, y + RowHeight - 4f);
            if (i == selectedIndex)
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
    }

    public static int HitTestRow(SKRect bounds, float x, float y, int rowCount)
    {
        if (rowCount <= 0 || !bounds.Contains(x, y))
            return -1;

        var localY = y - bounds.Top - HorizontalPadding;
        if (localY < 0)
            return -1;

        var index = (int)(localY / RowHeight);
        return index >= 0 && index < rowCount ? index : -1;
    }

    private static string Truncate(string text, int maxChars) =>
        text.Length <= maxChars ? text : text[..(maxChars - 1)] + "…";
}

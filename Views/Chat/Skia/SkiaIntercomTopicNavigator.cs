#nullable enable
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Боковая панель Topic Navigator: поиск + дерево тем (ADR 0127-E).</summary>
internal static class SkiaIntercomTopicNavigator
{
    public const float PanelWidth = 220f;
    public const float SearchHeight = 30f;
    public const float RowHeight = 30f;
    public const float RowGap = 3f;
    public const float Pad = 8f;

    /// <summary>Геометрия панели: один расчёт для отрисовки и pointer-hit (без дублирования listTop/scroll).</summary>
    internal sealed record PanelLayout(
        SKRect SearchBounds,
        SKRect ListClipRect,
        float ListTop,
        float ContentHeight);

    public sealed record RowHit(Guid ThreadId, SKRect Bounds);

    public sealed record LayoutResult(
        IReadOnlyList<RowHit> RowHits,
        PanelLayout Layout,
        SKRect SearchBounds,
        float ContentHeight);

    /// <summary>Единая точка: координаты строки в пространстве списка (после Translate) → координаты контрола.</summary>
    internal static SKRect MapRowBoundsToPanel(SKRect rowInListSpace, PanelLayout layout, float scrollOffset)
    {
        var bounds = rowInListSpace;
        bounds.Offset(0f, layout.ListTop - scrollOffset);
        return bounds;
    }

    internal static PanelLayout ComputePanelLayout(float left, float top, float height, int rowCount)
    {
        var searchTop = top + Pad;
        var searchBounds = new SKRect(left + Pad, searchTop, left + PanelWidth - Pad, searchTop + SearchHeight);
        var listTop = searchBounds.Bottom + Pad;
        var listBottom = top + height - Pad;
        var contentHeight = rowCount > 0
            ? Pad + SearchHeight + Pad + rowCount * RowHeight + Math.Max(0, rowCount - 1) * RowGap + Pad
            : Pad + SearchHeight + Pad + 24f;
        var listClip = new SKRect(left, listTop, left + PanelWidth, listBottom);
        return new PanelLayout(searchBounds, listClip, listTop, contentHeight);
    }

    public static LayoutResult Draw(
        SKCanvas canvas,
        float left,
        float top,
        float height,
        SkiaChatTheme theme,
        IntercomFontsSettings fonts,
        IReadOnlyList<ChatThreadPresentation.PickerRow> rows,
        Guid selectedThreadId,
        string? searchQuery,
        float scrollOffset)
    {
        var panelLayout = ComputePanelLayout(left, top, height, rows.Count);
        var panelRect = new SKRect(left, top, left + PanelWidth, top + height);
        using (var panelFill = new SKPaint
        {
            Color = SkiaKit.SkiaKitColor.Blend(theme.Surface, theme.Border, 0.1f),
            IsAntialias = true,
        })
            canvas.DrawRect(panelRect, panelFill);

        using (var divider = new SKPaint { Color = theme.Border, StrokeWidth = 1, IsAntialias = true })
            canvas.DrawLine(left + PanelWidth - 0.5f, top, left + PanelWidth - 0.5f, top + height, divider);

        DrawSearchField(canvas, panelLayout.SearchBounds, theme, fonts, searchQuery);

        canvas.Save();
        canvas.ClipRect(panelLayout.ListClipRect, antialias: false);
        canvas.Translate(0, panelLayout.ListTop - scrollOffset);

        var hits = new List<RowHit>();
        var y = 0f;
        var rowPt = Math.Max(10f, fonts.ResolveChromeSubtitlePt());
        using var titleFont = new SKFont(SKTypeface.FromFamilyName(fonts.ResolveProseFamily()), rowPt);
        using var titlePaint = new SKPaint { IsAntialias = true, Color = theme.Content };
        using var mutedPaint = new SKPaint { IsAntialias = true, Color = theme.MutedContent };
        using var selBg = new SKPaint
        {
            IsAntialias = true,
            Color = SkiaKit.SkiaKitColor.Blend(theme.BubbleUser, theme.Border, 0.22f),
        };

        foreach (var row in rows)
        {
            var rowInListSpace = new SKRect(left + Pad, y, left + PanelWidth - Pad, y + RowHeight);
            if (row.ThreadId == selectedThreadId)
                canvas.DrawRoundRect(rowInListSpace, 5, 5, selBg);
            else
            {
                using var rowFill = new SKPaint
                {
                    Color = SkiaKit.SkiaKitColor.Blend(theme.Surface, theme.Border, 0.18f),
                    IsAntialias = true,
                };
                canvas.DrawRoundRect(rowInListSpace, 5, 5, rowFill);
            }

            var titleX = rowInListSpace.Left + 6f + row.Depth * 12f;
            canvas.DrawText(Truncate(row.Title, 22), titleX, rowInListSpace.MidY + 4f, SKTextAlign.Left, titleFont, titlePaint);
            canvas.DrawText(row.Meta, rowInListSpace.Right - 6f, rowInListSpace.MidY + 4f, SKTextAlign.Right, titleFont, mutedPaint);
            hits.Add(new RowHit(row.ThreadId, MapRowBoundsToPanel(rowInListSpace, panelLayout, scrollOffset)));
            y += RowHeight + RowGap;
        }

        if (rows.Count == 0)
        {
            using var hintFont = new SKFont(SKTypeface.FromFamilyName(fonts.ResolveProseFamily()), rowPt - 1f);
            canvas.DrawText(
                "Нет тем",
                left + Pad,
                8f,
                SKTextAlign.Left,
                hintFont,
                mutedPaint);
        }

        canvas.Restore();
        return new LayoutResult(hits, panelLayout, panelLayout.SearchBounds, panelLayout.ContentHeight);
    }

    private static void DrawSearchField(
        SKCanvas canvas,
        SKRect bounds,
        SkiaChatTheme theme,
        IntercomFontsSettings fonts,
        string? query)
    {
        using var fill = new SKPaint
        {
            Color = SkiaKit.SkiaKitColor.Blend(theme.Surface, theme.Border, 0.28f),
            IsAntialias = true,
        };
        canvas.DrawRoundRect(bounds, 6, 6, fill);
        using var stroke = new SKPaint
        {
            Color = theme.Border,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
        };
        canvas.DrawRoundRect(bounds, 6, 6, stroke);

        var pt = Math.Max(10f, fonts.ResolveChromeSubtitlePt());
        using var font = new SKFont(SKTypeface.FromFamilyName(fonts.ResolveProseFamily()), pt);
        var text = string.IsNullOrWhiteSpace(query) ? "Поиск…" : query.Trim();
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = string.IsNullOrWhiteSpace(query) ? theme.MutedContent : theme.Content,
        };
        canvas.DrawText(Truncate(text, 28), bounds.Left + 8f, bounds.MidY + 4f, SKTextAlign.Left, font, paint);
    }

    private static string Truncate(string text, int maxChars) =>
        text.Length <= maxChars ? text : text[..maxChars] + "…";
}

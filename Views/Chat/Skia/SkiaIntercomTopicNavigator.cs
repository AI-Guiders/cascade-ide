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

    public sealed record RowHit(Guid ThreadId, SKRect Bounds);

    public sealed record LayoutResult(
        IReadOnlyList<RowHit> RowHits,
        SKRect SearchBounds,
        float ContentHeight);

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
        var panelRect = new SKRect(left, top, left + PanelWidth, top + height);
        using (var panelFill = new SKPaint
        {
            Color = SkiaKit.SkiaKitColor.Blend(theme.Surface, theme.Border, 0.1f),
            IsAntialias = true,
        })
            canvas.DrawRect(panelRect, panelFill);

        using (var divider = new SKPaint { Color = theme.Border, StrokeWidth = 1, IsAntialias = true })
            canvas.DrawLine(left + PanelWidth - 0.5f, top, left + PanelWidth - 0.5f, top + height, divider);

        var searchTop = top + Pad;
        var searchBounds = new SKRect(left + Pad, searchTop, left + PanelWidth - Pad, searchTop + SearchHeight);
        DrawSearchField(canvas, searchBounds, theme, fonts, searchQuery);

        var listTop = searchBounds.Bottom + Pad;
        var listBottom = top + height - Pad;
        var listHeight = Math.Max(0, listBottom - listTop);
        var contentHeight = rows.Count > 0
            ? Pad + SearchHeight + Pad + rows.Count * RowHeight + Math.Max(0, rows.Count - 1) * RowGap + Pad
            : Pad + SearchHeight + Pad + 24f;

        canvas.Save();
        canvas.ClipRect(new SKRect(left, listTop, left + PanelWidth, listBottom), antialias: false);
        canvas.Translate(0, listTop - scrollOffset);

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
            var rowRect = new SKRect(left + Pad, y, left + PanelWidth - Pad, y + RowHeight);
            if (row.ThreadId == selectedThreadId)
                canvas.DrawRoundRect(rowRect, 5, 5, selBg);
            else
            {
                using var rowFill = new SKPaint
                {
                    Color = SkiaKit.SkiaKitColor.Blend(theme.Surface, theme.Border, 0.18f),
                    IsAntialias = true,
                };
                canvas.DrawRoundRect(rowRect, 5, 5, rowFill);
            }

            var titleX = rowRect.Left + 6f + row.Depth * 12f;
            canvas.DrawText(Truncate(row.Title, 22), titleX, rowRect.MidY + 4f, SKTextAlign.Left, titleFont, titlePaint);
            canvas.DrawText(row.Meta, rowRect.Right - 6f, rowRect.MidY + 4f, SKTextAlign.Right, titleFont, mutedPaint);
            hits.Add(new RowHit(row.ThreadId, rowRect));
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
        return new LayoutResult(hits, searchBounds, contentHeight);
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

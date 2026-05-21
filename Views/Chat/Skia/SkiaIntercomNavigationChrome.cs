#nullable enable
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Spine row + topic tab bar (ADR 0127 фазы A–B).</summary>
internal static class SkiaIntercomNavigationChrome
{
    public const float SpineRowHeight = 26f;
    public const float TabBarHeight = 30f;
    public const int DefaultMaxVisibleTabs = 7;
    public const float HorizontalPad = 12f;
    public const float TabGap = 6f;
    public const float TabPadX = 10f;

    public sealed record TabHit(Guid ThreadId, SKRect Bounds);

    public sealed record LayoutResult(
        IReadOnlyList<TabHit> TabHits,
        SKRect CreateButtonBounds,
        SKRect? OverflowBounds,
        int OverflowHiddenCount);

    public static float ResolveNavigationHeight(bool forwardHost, bool overviewMode, int topicCount)
    {
        if (!forwardHost || topicCount <= 0)
            return 0f;
        var height = SpineRowHeight;
        if (!overviewMode)
            height += TabBarHeight;
        return height;
    }

    public static float ResolveTopChromeHeight(
        bool forwardHost,
        bool showOverviewCatalog,
        bool showStatusSubtitle,
        bool overviewMode,
        int topicCount) =>
        SkiaChatChromeRenderer.ResolveToolbarHeight(forwardHost, showStatusSubtitle)
        + ResolveNavigationHeight(forwardHost, overviewMode, topicCount)
        + (showOverviewCatalog ? SkiaChatChromeRenderer.OverviewCatalogBandHeight : 0f);

    public static float DrawSpineRow(
        SKCanvas canvas,
        float width,
        float top,
        SkiaChatTheme theme,
        ChatProductSpine spine,
        IntercomFontsSettings fonts)
    {
        var bottom = top + SpineRowHeight;
        using (var fill = new SKPaint
        {
            Color = SkiaKit.SkiaKitColor.Blend(theme.Surface, theme.Border, 0.06f),
            IsAntialias = true,
        })
            canvas.DrawRect(new SKRect(0, top, width, bottom), fill);

        using (var line = new SKPaint { Color = theme.Border, StrokeWidth = 1, IsAntialias = true })
            canvas.DrawLine(0, bottom - 0.5f, width, bottom - 0.5f, line);

        var lineTitle = ChatProductSpinePresentation.ResolveLineTitle(spine);
        var focus = string.IsNullOrWhiteSpace(spine.CurrentFocus)
            ? null
            : ChatProductSpinePresentation.FormatDetailStripFocus(spine.CurrentFocus);
        var spineText = focus is null
            ? $"Spine: {Truncate(lineTitle, 40)}"
            : $"Spine: {Truncate(lineTitle, 28)} · {Truncate(focus, 36)}";

        var pt = Math.Max(10f, fonts.ResolveChromeSubtitlePt());
        using var font = new SKFont(SKTypeface.FromFamilyName(fonts.ResolveProseFamily()), pt);
        using var paint = new SKPaint { IsAntialias = true, Color = theme.MutedContent };
        canvas.DrawText(spineText, HorizontalPad, top + pt * 0.9f + 4f, SKTextAlign.Left, font, paint);
        return bottom;
    }

    public static LayoutResult DrawTopicTabBar(
        SKCanvas canvas,
        float width,
        float top,
        SkiaChatTheme theme,
        IReadOnlyList<ChatThreadOverviewItem> overview,
        Guid selectedThreadId,
        IntercomFontsSettings fonts,
        int maxVisibleTabs = DefaultMaxVisibleTabs)
    {
        var bottom = top + TabBarHeight;
        using (var fill = new SKPaint
        {
            Color = theme.Surface,
            IsAntialias = true,
        })
            canvas.DrawRect(new SKRect(0, top, width, bottom), fill);

        var tabPt = Math.Max(10f, fonts.ResolveChromeSubtitlePt() + 1f);
        using var font = new SKFont(SKTypeface.FromFamilyName(fonts.ResolveProseFamily()), tabPt);
        using var paint = new SKPaint { IsAntialias = true, Color = theme.Content };
        using var muted = new SKPaint { IsAntialias = true, Color = theme.MutedContent };
        using var selBg = new SKPaint
        {
            IsAntialias = true,
            Color = SkiaKit.SkiaKitColor.Blend(theme.BubbleUser, theme.Border, 0.2f),
        };

        var hits = new List<TabHit>();
        var x = HorizontalPad;
        var maxX = width - HorizontalPad - 36f;
        var overflowCount = 0;
        SKRect? overflowBounds = null;

        var visible = overview;
        if (overview.Count > maxVisibleTabs)
        {
            overflowCount = overview.Count - (maxVisibleTabs - 1);
            visible = overview.Take(maxVisibleTabs - 1).ToList();
        }

        foreach (var item in visible)
        {
            var label = item.IsMainThread ? "★ " + item.Title : item.Title;
            label = Truncate(label, 22);
            var textWidth = font.MeasureText(label);
            var tabWidth = textWidth + TabPadX * 2;
            if (x + tabWidth > maxX)
                break;

            var rect = new SKRect(x, top + 4f, x + tabWidth, bottom - 4f);
            var selected = item.ThreadId == selectedThreadId;
            if (selected)
                canvas.DrawRoundRect(rect, 6, 6, selBg);
            else
            {
                using var bg = new SKPaint
                {
                    IsAntialias = true,
                    Color = SkiaKit.SkiaKitColor.Blend(theme.Surface, theme.Border, 0.25f),
                };
                canvas.DrawRoundRect(rect, 6, 6, bg);
            }

            canvas.DrawText(
                label,
                rect.MidX,
                rect.MidY + tabPt * 0.35f,
                SKTextAlign.Center,
                font,
                selected ? paint : muted);
            hits.Add(new TabHit(item.ThreadId, rect));
            x = rect.Right + TabGap;
        }

        if (overflowCount > 0)
        {
            var label = $"+{overflowCount}";
            var textWidth = font.MeasureText(label);
            var rect = new SKRect(x, top + 4f, x + textWidth + TabPadX * 2, bottom - 4f);
            using var bg = new SKPaint
            {
                IsAntialias = true,
                Color = SkiaKit.SkiaKitColor.Blend(theme.Surface, theme.Border, 0.35f),
            };
            canvas.DrawRoundRect(rect, 6, 6, bg);
            canvas.DrawText(label, rect.MidX, rect.MidY + tabPt * 0.35f, SKTextAlign.Center, font, muted);
            overflowBounds = rect;
            x = rect.Right + TabGap;
        }

        var createLabel = "+";
        var createWidth = font.MeasureText(createLabel) + 14f;
        var createRect = new SKRect(Math.Min(x, width - HorizontalPad - createWidth), top + 4f, width - HorizontalPad, bottom - 4f);
        using (var createBg = new SKPaint
        {
            IsAntialias = true,
            Color = SkiaKit.SkiaKitColor.Blend(theme.BubbleAssistant, theme.Border, 0.3f),
        })
            canvas.DrawRoundRect(createRect, 6, 6, createBg);
        canvas.DrawText(createLabel, createRect.MidX, createRect.MidY + tabPt * 0.35f, SKTextAlign.Center, font, paint);

        using (var line = new SKPaint { Color = theme.Border, StrokeWidth = 1, IsAntialias = true })
            canvas.DrawLine(0, bottom - 0.5f, width, bottom - 0.5f, line);

        return new LayoutResult(hits, createRect, overflowBounds, overflowCount);
    }

    private static string Truncate(string text, int maxChars) =>
        text.Length <= maxChars ? text : text[..maxChars] + "…";
}

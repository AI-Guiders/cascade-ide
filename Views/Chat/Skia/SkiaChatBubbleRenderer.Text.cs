#nullable enable
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Text / markdown paint for chat bubbles (line-gate peel from hub).</summary>
internal static partial class SkiaChatBubbleRenderer
{
    private static void DrawText(
        SkiaChatDrawContext ctx,
        SKRect rect,
        float contentLeft,
        float insetX,
        in SkiaChatBubbleSpec spec,
        in SkiaChatBubbleMetrics metrics,
        in SkiaChatFeedLayout feed)
    {
        var uiFamily = spec.Kind == SkiaChatBubbleKind.Feed ? feed.ProseFamily : "Segoe UI";
        using var titleFont = SkiaChatFeedFontResolver.CreateFont(
            uiFamily,
            spec.Kind is SkiaChatBubbleKind.CardPanel or SkiaChatBubbleKind.OverviewHeader ? 13.5f : 10,
            SKFontStyle.Bold);
        using var bodyFont = SkiaChatFeedFontResolver.CreateFont(uiFamily, ProseFontSize(spec, feed));
        using var titlePaint = new SKPaint
        {
            IsAntialias = true,
            Color = spec.Kind is SkiaChatBubbleKind.CardPanel ? ctx.Theme.Content : ctx.Theme.Role
        };
        using var footerPaint = new SKPaint { IsAntialias = true, Color = ctx.Theme.FooterMuted };

        var titleTopInset = spec.Kind switch
        {
            SkiaChatBubbleKind.Feed => feed.BodyTopPad - 2f,
            SkiaChatBubbleKind.CardPanel => 12f,
            SkiaChatBubbleKind.OverviewHeader => 10f,
            SkiaChatBubbleKind.SpineStrip => 6f,
            _ => 8f
        };
        var titleBaseline = rect.Top + titleTopInset + titleFont.Size * SkiaChatFeedLayout.TextBaselineFactor;
        if (!string.IsNullOrWhiteSpace(spec.Title))
            ctx.Canvas.DrawText(spec.Title, contentLeft, titleBaseline, SKTextAlign.Left, titleFont, titlePaint);

        var textY = spec.Kind == SkiaChatBubbleKind.Feed
            ? feed.FirstLineBaselineY(rect.Top, metrics.TitleHeight)
            : rect.Top + metrics.TitleHeight + (spec.Kind switch
            {
                SkiaChatBubbleKind.OverviewHeader => 6f,
                SkiaChatBubbleKind.CardPanel => 10f,
                _ => 12f
            });
        if (metrics.RichTextBody is { } richBody)
        {
            var bodyColor = ResolveBodyColor(ctx.Theme, spec.BodyTone);
            var codeColor = SkiaKitColor.Blend(ctx.Theme.Content, ctx.Theme.HoverBorder, 0.35f);
            var paintOrigin = spec.Kind == SkiaChatBubbleKind.Feed
                ? feed.RichTextPaintOrigin(contentLeft, textY, bodyFont.Size)
                : new SKPoint(contentLeft, textY - bodyFont.Size * SkiaChatFeedLayout.TextBaselineFactor);
            SkiaRichTextKitMarkdown.Paint(
                ctx.Canvas,
                paintOrigin,
                richBody,
                bodyColor,
                codeColor);
            if (spec.BodyTone == SkiaChatBodyTone.Link)
            {
                var linkWidth = bodyFont.MeasureText(richBody.Body);
                DrawLinkUnderline(ctx.Canvas, contentLeft, textY, linkWidth, bodyColor);
            }
            if (string.IsNullOrWhiteSpace(metrics.Footer))
                return;
            DrawFooter(ctx, rect, contentLeft, spec, metrics, footerPaint);
            return;
        }

        foreach (var line in metrics.ContentLines)
        {
            var x = contentLeft;
            foreach (var run in line.Runs)
            {
                if (run.Text.Length == 0)
                    continue;

                var (font, color, disposeFont) = ResolveRunStyle(
                    ctx,
                    spec,
                    feed,
                    bodyFont,
                    run.Style);
                try
                {
                    using var linePaint = new SKPaint { IsAntialias = true, Color = color };
                    ctx.Canvas.DrawText(run.Text, x, textY, SKTextAlign.Left, font, linePaint);
                    var runWidth = font.MeasureText(run.Text);
                    if (run.Style == SkiaMarkdownStyle.Link)
                        DrawLinkUnderline(ctx.Canvas, x, textY, runWidth, color);
                    x += runWidth;
                }
                finally
                {
                    if (disposeFont)
                        font.Dispose();
                }
            }

            textY += metrics.LineHeight;
        }

        if (string.IsNullOrWhiteSpace(metrics.Footer))
            return;

        DrawFooter(ctx, rect, contentLeft, spec, metrics, footerPaint);
    }

    private static void DrawFooter(
        SkiaChatDrawContext ctx,
        SKRect rect,
        float contentLeft,
        in SkiaChatBubbleSpec spec,
        in SkiaChatBubbleMetrics metrics,
        SKPaint footerPaint)
    {
        if (spec.Kind == SkiaChatBubbleKind.CardPanel)
        {
            var sepY = rect.Bottom - metrics.FooterHeight - 6;
            using var sepPaint = new SKPaint
            {
                Color = SkiaKitColor.Blend(ctx.Theme.Border, ctx.Theme.Content, 0.25f),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1
            };
            ctx.Canvas.DrawLine(contentLeft, sepY, rect.Right - 12, sepY, sepPaint);
        }

        using var footerFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), spec.Kind == SkiaChatBubbleKind.CardPanel ? 9.5f : 9);
        ctx.Canvas.DrawText(metrics.Footer!, contentLeft, rect.Bottom - 11, SKTextAlign.Left, footerFont, footerPaint);
    }

    private static (SKFont Font, SKColor Color, bool DisposeFont) ResolveRunStyle(
        SkiaChatDrawContext ctx,
        in SkiaChatBubbleSpec spec,
        in SkiaChatFeedLayout feed,
        SKFont bodyFont,
        SkiaMarkdownStyle style)
    {
        var bodyColor = ResolveBodyColor(ctx.Theme, spec.BodyTone);
        var proseFamily = spec.Kind == SkiaChatBubbleKind.Feed ? feed.ProseFamily : "Segoe UI";
        var monoFamily = spec.Kind == SkiaChatBubbleKind.Feed ? feed.MonoFamily : "Cascadia Mono,Consolas";
        return style switch
        {
            SkiaMarkdownStyle.Link => (
                bodyFont,
                bodyColor,
                false),
            SkiaMarkdownStyle.Bold => (
                SkiaChatFeedFontResolver.CreateFont(proseFamily, bodyFont.Size, SKFontStyle.Bold),
                bodyColor,
                true),
            SkiaMarkdownStyle.Italic => (
                SkiaChatFeedFontResolver.CreateFont(proseFamily, bodyFont.Size, SKFontStyle.Italic),
                bodyColor,
                true),
            SkiaMarkdownStyle.Code => (
                SkiaChatFeedFontResolver.CreateFont(monoFamily, bodyFont.Size * 0.95f),
                SkiaKitColor.Blend(ctx.Theme.Content, ctx.Theme.HoverBorder, 0.35f),
                true),
            _ => (bodyFont, bodyColor, false)
        };
    }

    private static string Trim(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..maxLen] + "...";

    private static SKColor ResolveBodyColor(SkiaChatBodyTone tone) =>
        tone switch
        {
            SkiaChatBodyTone.Placeholder => new SKColor(160, 165, 175),
            SkiaChatBodyTone.Link => new SKColor(120, 185, 255),
            _ => new SKColor(220, 225, 235)
        };

    private static SKColor ResolveBodyColor(SkiaChatTheme theme, SkiaChatBodyTone tone) =>
        tone switch
        {
            SkiaChatBodyTone.Placeholder => theme.MutedContent,
            SkiaChatBodyTone.Link => SkiaKitColor.Blend(theme.Content, theme.HoverBorder, 0.55f),
            _ => theme.Content
        };

    private static void DrawLinkUnderline(SKCanvas canvas, float x, float textBaselineY, float width, SKColor color)
    {
        if (width <= 0f)
            return;

        var y = textBaselineY + 2f;
        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f
        };
        canvas.DrawLine(x, y, x + width, y, paint);
    }
}

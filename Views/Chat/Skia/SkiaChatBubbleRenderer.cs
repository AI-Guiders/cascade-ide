#nullable enable
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

internal enum SkiaChatBubbleKind
{
    Standard,
    CardPanel,
    OverviewHeader,
    SpineStrip
}

internal readonly record struct SkiaChatBubbleSpec(
    string Title,
    string Body,
    string? Footer,
    SkiaChatBubbleKind Kind,
    SkiaBubbleFillRole FillRole,
    SkiaChatBodyTone BodyTone,
    bool IsPending,
    bool IsSelected,
    bool StartsBranch,
    int? MessageIndex,
    float MinHeight = 0,
    int MaxBodyLines = int.MaxValue,
    float GapAfter = 8,
    float Padding = 10,
    float TitleHeight = 16,
    float FooterHeight = 16,
    float LineHeight = 16,
    float CornerRadius = 7);

internal static class SkiaChatBubbleRenderer
{
    public static SkiaChatBubbleMetrics Measure(SkiaChatMeasureContext context, in SkiaChatBubbleSpec spec)
    {
        var maxChars = Math.Max(24, context.MaxChars);
        var runs = SkiaMarkdownLayout.ParseInline(Trim(spec.Body, 32_000));
        var lines = SkiaMarkdownLayout.WrapLines(runs, maxChars);
        if (lines.Count == 0)
            lines = [new SkiaMarkdownLine([new SkiaMarkdownRun("", SkiaMarkdownStyle.Plain)])];
        if (lines.Count > spec.MaxBodyLines)
            lines = lines.Take(spec.MaxBodyLines).ToList();

        var titleHeight = string.IsNullOrWhiteSpace(spec.Title) ? 0 : spec.TitleHeight;
        var footerHeight = string.IsNullOrWhiteSpace(spec.Footer) ? 0 : spec.FooterHeight;
        return new SkiaChatBubbleMetrics(lines, spec.Footer, titleHeight, footerHeight, spec.LineHeight);
    }

    public static float MeasureHeight(in SkiaChatBubbleSpec spec, in SkiaChatBubbleMetrics metrics) =>
        Math.Max(
            spec.MinHeight,
            spec.Padding + metrics.TitleHeight + metrics.ContentLines.Count * metrics.LineHeight + metrics.FooterHeight + spec.Padding);

    public static void Draw(
        SkiaChatDrawContext ctx,
        SKRect rect,
        in SkiaChatBubbleSpec spec,
        in SkiaChatBubbleMetrics metrics)
    {
        var corner = spec.Kind is SkiaChatBubbleKind.CardPanel ? 12f : spec.Kind is SkiaChatBubbleKind.OverviewHeader ? 8f : 7f;
        var insetX = spec.Kind is SkiaChatBubbleKind.CardPanel ? 20f : 12f;
        var contentLeft = ctx.ContentLeft + insetX;

        if (spec.Kind is SkiaChatBubbleKind.CardPanel)
            DrawCardShadow(ctx.Canvas, rect, corner);

        if (spec.Kind == SkiaChatBubbleKind.SpineStrip)
            DrawSpineStripFrame(ctx, rect, corner, spec);
        else if (spec.Kind == SkiaChatBubbleKind.OverviewHeader)
            DrawOverviewHeaderFrame(ctx, rect);
        else
            DrawStandardFrame(ctx, rect, corner, spec, metrics);

        DrawText(ctx, rect, contentLeft, insetX, spec, metrics);
    }

    private static void DrawCardShadow(SKCanvas canvas, SKRect rect, float corner)
    {
        var shadowRect = rect;
        shadowRect.Offset(0, 3);
        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 72),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRoundRect(shadowRect, corner + 1, corner + 1, shadowPaint);
    }

    private static void DrawSpineStripFrame(SkiaChatDrawContext ctx, SKRect rect, float corner, in SkiaChatBubbleSpec spec)
    {
        using var fill = new SKPaint
        {
            Color = ResolveFill(ctx.Theme, spec),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        ctx.Canvas.DrawRoundRect(rect, corner, corner, fill);
        using var stroke = FrameStroke(ctx.Theme.Border);
        ctx.Canvas.DrawRoundRect(rect, corner, corner, stroke);
        if (ctx.IsHovered)
            ctx.Canvas.DrawRoundRect(rect, corner, corner, FrameStroke(ctx.Theme.HoverBorder, 2));
    }

    private static void DrawOverviewHeaderFrame(SkiaChatDrawContext ctx, SKRect rect)
    {
        using var headerFill = new SKPaint
        {
            Color = SkiaKitColor.Blend(ctx.Theme.Surface, ctx.Theme.HoverBorder, 0.12f),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        ctx.Canvas.DrawRoundRect(rect, 8, 8, headerFill);
        using var headerStroke = new SKPaint
        {
            Color = SkiaKitColor.Blend(ctx.Theme.Border, ctx.Theme.HoverBorder, 0.45f),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1
        };
        ctx.Canvas.DrawRoundRect(rect, 8, 8, headerStroke);
    }

    private static void DrawStandardFrame(
        SkiaChatDrawContext ctx,
        SKRect rect,
        float corner,
        in SkiaChatBubbleSpec spec,
        in SkiaChatBubbleMetrics metrics)
    {
        using var fill = new SKPaint
        {
            Color = ResolveFill(ctx.Theme, spec),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        ctx.Canvas.DrawRoundRect(rect, corner, corner, fill);

        if (spec.Kind == SkiaChatBubbleKind.CardPanel)
        {
            var accent = spec.FillRole == SkiaBubbleFillRole.SpineCard
                ? SkiaKitColor.Blend(ctx.Theme.HoverBorder, ctx.Theme.SelectedBorder, 0.35f)
                : SkiaKitColor.Blend(ctx.Theme.HoverBorder, new SKColor(255, 210, 120), 0.45f);
            var barRect = new SKRect(rect.Left + 6, rect.Top + 10, rect.Left + 10, rect.Bottom - 10);
            using var barPaint = new SKPaint { Color = accent, IsAntialias = true, Style = SKPaintStyle.Fill };
            ctx.Canvas.DrawRoundRect(barRect, 2, 2, barPaint);
            ctx.Canvas.DrawRoundRect(rect, corner, corner, FrameStroke(
                ctx.IsHovered ? ctx.Theme.HoverBorder : SkiaKitColor.Blend(ctx.Theme.Border, ctx.Theme.Content, 0.35f),
                ctx.IsHovered ? 2f : 1.35f));
            return;
        }

        ctx.Canvas.DrawRoundRect(rect, corner, corner, FrameStroke(ctx.Theme.Border));
        if (ctx.IsHovered)
            ctx.Canvas.DrawRoundRect(rect, corner, corner, FrameStroke(ctx.Theme.HoverBorder, 2));
        if (spec.IsSelected || (spec.MessageIndex is not null && spec.MessageIndex == ctx.SelectedMessageIndex))
            ctx.Canvas.DrawRoundRect(rect, corner, corner, FrameStroke(ctx.Theme.SelectedBorder, 2.2f));
    }

    private static void DrawText(
        SkiaChatDrawContext ctx,
        SKRect rect,
        float contentLeft,
        float insetX,
        in SkiaChatBubbleSpec spec,
        in SkiaChatBubbleMetrics metrics)
    {
        using var titleFont = new SKFont(
            SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold),
            spec.Kind is SkiaChatBubbleKind.CardPanel or SkiaChatBubbleKind.OverviewHeader ? 13.5f : 10);
        using var bodyFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), spec.Kind == SkiaChatBubbleKind.CardPanel ? 11.5f : 11);
        using var titlePaint = new SKPaint
        {
            IsAntialias = true,
            Color = spec.Kind is SkiaChatBubbleKind.CardPanel ? ctx.Theme.Content : ctx.Theme.Role
        };
        using var footerPaint = new SKPaint { IsAntialias = true, Color = ctx.Theme.FooterMuted };

        var titleBaseline = rect.Top + (spec.Kind switch
        {
            SkiaChatBubbleKind.CardPanel => 20f,
            SkiaChatBubbleKind.OverviewHeader => 18f,
            SkiaChatBubbleKind.SpineStrip => 14f,
            _ => 14f
        });
        if (!string.IsNullOrWhiteSpace(spec.Title))
            ctx.Canvas.DrawText(spec.Title, contentLeft, titleBaseline, SKTextAlign.Left, titleFont, titlePaint);

        var textY = rect.Top + metrics.TitleHeight + (spec.Kind switch
        {
            SkiaChatBubbleKind.OverviewHeader => 6f,
            SkiaChatBubbleKind.CardPanel => 10f,
            _ => 12f
        });
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
                    bodyFont,
                    run.Style);
                try
                {
                    using var linePaint = new SKPaint { IsAntialias = true, Color = color };
                    ctx.Canvas.DrawText(run.Text, x, textY, SKTextAlign.Left, font, linePaint);
                    x += font.MeasureText(run.Text);
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

    private static SKColor ResolveFill(SkiaChatTheme theme, in SkiaChatBubbleSpec spec) =>
        spec.FillRole switch
        {
            SkiaBubbleFillRole.SpineCard => SkiaKitColor.Blend(theme.BubbleAssistant, theme.Content, 0.22f),
            SkiaBubbleFillRole.SpineStrip => SkiaKitColor.Blend(theme.Surface, theme.Border, 0.32f),
            SkiaBubbleFillRole.OverviewNav => SkiaKitColor.Blend(theme.Surface, theme.Border, 0.45f),
            SkiaBubbleFillRole.ThreadRow => SkiaKitColor.Blend(theme.Surface, theme.Border, 0.45f),
            SkiaBubbleFillRole.ThreadRowActive => SkiaKitColor.Blend(theme.Surface, theme.HoverBorder, 0.22f),
            SkiaBubbleFillRole.ThreadHeader => SkiaKitColor.Blend(theme.Surface, theme.HoverBorder, 0.22f),
            SkiaBubbleFillRole.ThreadHeaderActive => SkiaKitColor.Blend(theme.Surface, theme.HoverBorder, 0.28f),
            SkiaBubbleFillRole.ClarificationPending => SkiaKitColor.Blend(theme.BubbleAssistant, theme.HoverBorder, 0.32f),
            SkiaBubbleFillRole.ClarificationResolved => SkiaKitColor.Blend(theme.BubbleAssistant, theme.HoverBorder, 0.18f),
            SkiaBubbleFillRole.MessageThinking => SkiaKitColor.Blend(theme.BubbleAssistant, theme.HoverBorder, 0.26f),
            SkiaBubbleFillRole.MessageTool => SkiaKitColor.Blend(theme.BubbleAssistant, theme.Border, 0.35f),
            SkiaBubbleFillRole.MessageUser => theme.BubbleUser,
            SkiaBubbleFillRole.MessageAssistant when spec.StartsBranch =>
                SkiaKitColor.Blend(theme.BubbleAssistant, theme.SelectedBorder, 0.24f),
            SkiaBubbleFillRole.MessageAssistant => theme.BubbleAssistant,
            _ => spec.Kind switch
            {
                SkiaChatBubbleKind.CardPanel => SkiaKitColor.Blend(theme.BubbleAssistant, theme.Content, 0.22f),
                SkiaChatBubbleKind.SpineStrip => SkiaKitColor.Blend(theme.Surface, theme.Border, 0.32f),
                SkiaChatBubbleKind.OverviewHeader => SkiaKitColor.Blend(theme.Surface, theme.Border, 0.2f),
                _ => theme.BubbleAssistant
            }
        };

    private static SKPaint FrameStroke(SKColor color, float width = 1) =>
        new()
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = width
        };

    private static (SKFont Font, SKColor Color, bool DisposeFont) ResolveRunStyle(
        SkiaChatDrawContext ctx,
        in SkiaChatBubbleSpec spec,
        SKFont bodyFont,
        SkiaMarkdownStyle style)
    {
        var bodyColor = spec.BodyTone == SkiaChatBodyTone.Placeholder
            ? ctx.Theme.MutedContent
            : ctx.Theme.Content;
        return style switch
        {
            SkiaMarkdownStyle.Bold => (
                new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), bodyFont.Size),
                bodyColor,
                true),
            SkiaMarkdownStyle.Italic => (
                new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Italic), bodyFont.Size),
                bodyColor,
                true),
            SkiaMarkdownStyle.Code => (
                new SKFont(SKTypeface.FromFamilyName("Cascadia Mono", SKFontStyle.Normal), bodyFont.Size * 0.95f),
                SkiaKitColor.Blend(ctx.Theme.Content, ctx.Theme.HoverBorder, 0.35f),
                true),
            _ => (bodyFont, bodyColor, false)
        };
    }

    private static string Trim(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..maxLen] + "...";
}

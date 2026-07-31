#nullable enable
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

internal static partial class SkiaChatBubbleRenderer
{
    private static void DrawFeedAccent(SkiaChatDrawContext ctx, SKRect rect, in SkiaChatBubbleSpec spec)
    {
        if (!spec.StartsBranch)
            return;

        var branchBar = new SKRect(rect.Left, rect.Top + 2f, rect.Left + 3f, rect.Bottom - 2f);
        using var branchPaint = new SKPaint
        {
            Color = SkiaKitColor.Blend(ctx.Theme.HoverBorder, new SKColor(255, 210, 120), 0.45f),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        ctx.Canvas.DrawRoundRect(branchBar, 1.5f, 1.5f, branchPaint);
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
        var messageSelected = spec.MessageIndex is not null && spec.MessageIndex == ctx.SelectedMessageIndex;
        if (spec.Kind != SkiaChatBubbleKind.Feed
            && (spec.IsSelected || messageSelected))
        {
            ctx.Canvas.DrawRoundRect(rect, corner, corner, FrameStroke(ctx.Theme.SelectedBorder, 2.2f));
        }
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
            SkiaBubbleFillRole.SedmCard => SkiaKitColor.Blend(theme.Surface, theme.HoverBorder, 0.14f),
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
}

#nullable enable
using CascadeIDE.Models;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

internal enum SkiaChatBubbleKind
{
    Standard,
    /// <summary>Плоская строка ленты Intercom (ADR 0123): meta + тело, без messenger-пузыря.</summary>
    Feed,
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
    float CornerRadius = 7,
    bool ForwardFeedMetrics = false,
    IntercomFontsSettings? IntercomFonts = null);

internal static partial class SkiaChatBubbleRenderer
{
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
        else if (spec.Kind == SkiaChatBubbleKind.Feed)
            DrawFeedAccent(ctx, rect, spec);
        else
            DrawStandardFrame(ctx, rect, corner, spec, metrics);

        var feed = FeedLayout(spec);
        var textInset = spec.Kind == SkiaChatBubbleKind.Feed ? feed.TextInset : insetX;
        var textLeft = spec.Kind == SkiaChatBubbleKind.Feed ? rect.Left + textInset : contentLeft;
        DrawText(ctx, rect, textLeft, textInset, spec, metrics, feed);
    }
}

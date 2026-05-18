#nullable enable
using CascadeIDE.Features.Chat;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Пузырь локальной слэш-команды: путь, аргументы, иконка статуса.</summary>
internal sealed class SkiaChatSlashCommandEntity(
    string slashPath,
    string? args,
    string? detail,
    ChatSlashCommandStatus status,
    int? messageIndex) : ISkiaChatEntity
{
    private const float Padding = 12f;
    private const float GapAfter = 8f;
    private const float IconReserve = SkiaSlashCommandStatusIconRenderer.IconSize + SkiaSlashCommandStatusIconRenderer.IconMargin + 4f;
    private const float MinHeight = 44f;

    public SkiaChatMeasuredLayout Measure(SkiaChatMeasureContext context)
    {
        var maxChars = Math.Max(16, (int)((context.ContentWidth - IconReserve - Padding * 2) / 6.8f));
        var pathLines = SkiaTextLayout.Wrap(slashPath, maxChars);
        var argsLines = string.IsNullOrWhiteSpace(args)
            ? []
            : SkiaTextLayout.Wrap(args!, maxChars);
        var detailLines = string.IsNullOrWhiteSpace(detail)
            ? []
            : SkiaTextLayout.Wrap(detail!, maxChars);

        var lineHeight = 15f;
        var height = Padding;
        height += pathLines.Count * 17f;
        if (argsLines.Count > 0)
            height += 4f + argsLines.Count * lineHeight;
        if (detailLines.Count > 0)
            height += 6f + detailLines.Count * lineHeight;
        height += Padding;

        return new SkiaChatMeasuredLayout(Math.Max(MinHeight, height), GapAfter);
    }

    public void Draw(SkiaChatDrawContext context, float top, in SkiaChatMeasuredLayout layout)
    {
        var rect = new SKRect(context.ContentLeft, top, context.ContentLeft + context.ContentWidth, top + layout.Height);
        using var fill = new SKPaint
        {
            Color = SkiaKitColor.Blend(context.Theme.Surface, context.Theme.Border, 0.22f),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        context.Canvas.DrawRoundRect(rect, 8, 8, fill);

        using var stroke = new SKPaint
        {
            Color = SkiaKitColor.Blend(context.Theme.Border, context.Theme.HoverBorder, 0.35f),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
        };
        context.Canvas.DrawRoundRect(rect, 8, 8, stroke);

        var textRight = rect.Right - IconReserve;
        var y = rect.Top + Padding + 13f;

        using var pathFont = new SKFont(SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold), 13.5f);
        using var pathPaint = new SKPaint { IsAntialias = true, Color = context.Theme.Content };
        context.Canvas.DrawText(slashPath, rect.Left + Padding, y, SKTextAlign.Left, pathFont, pathPaint);
        y += 18f;

        if (!string.IsNullOrWhiteSpace(args))
        {
            using var argsFont = new SKFont(SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal), 11.5f);
            using var argsPaint = new SKPaint { IsAntialias = true, Color = context.Theme.MutedContent };
            context.Canvas.DrawText(args!, rect.Left + Padding, y, SKTextAlign.Left, argsFont, argsPaint);
            y += 15f;
        }

        if (!string.IsNullOrWhiteSpace(detail))
        {
            using var detailFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 11);
            using var detailPaint = new SKPaint
            {
                IsAntialias = true,
                Color = status == ChatSlashCommandStatus.Failed
                    ? new SKColor(240, 160, 160)
                    : context.Theme.MutedContent,
            };
            var maxChars = Math.Max(16, (int)((textRight - rect.Left - Padding) / 6.5f));
            foreach (var line in SkiaTextLayout.Wrap(detail, maxChars))
            {
                context.Canvas.DrawText(line, rect.Left + Padding, y, SKTextAlign.Left, detailFont, detailPaint);
                y += 14f;
            }
        }

        var iconRect = SkiaSlashCommandStatusIconRenderer.ResolveIconRect(
            rect,
            ChatSlashCommandPresentation.DefaultStatusIconPlacement);
        SkiaSlashCommandStatusIconRenderer.Draw(context.Canvas, iconRect, context.Theme, status);
    }

    public SkiaChatHit? CreateHit(in SkiaChatMeasuredLayout layout) =>
        messageIndex is { } index
            ? new SkiaChatHit(index, null, ResetDetailMode: false)
            : null;
}

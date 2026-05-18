#nullable enable
using CascadeIDE.Features.Chat;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Иконка статуса слэш-команды (часы / галочка / крест).</summary>
internal static class SkiaSlashCommandStatusIconRenderer
{
    public const float IconSize = 16f;
    public const float IconMargin = 10f;

    public static SKRect ResolveIconRect(
        SKRect bubbleRect,
        ChatSlashCommandStatusIconPlacement placement)
    {
        var size = IconSize;
        return placement switch
        {
            ChatSlashCommandStatusIconPlacement.TopLeft => new SKRect(
                bubbleRect.Left + IconMargin,
                bubbleRect.Top + IconMargin,
                bubbleRect.Left + IconMargin + size,
                bubbleRect.Top + IconMargin + size),
            ChatSlashCommandStatusIconPlacement.BottomLeft => new SKRect(
                bubbleRect.Left + IconMargin,
                bubbleRect.Bottom - IconMargin - size,
                bubbleRect.Left + IconMargin + size,
                bubbleRect.Bottom - IconMargin),
            ChatSlashCommandStatusIconPlacement.BottomRight => new SKRect(
                bubbleRect.Right - IconMargin - size,
                bubbleRect.Bottom - IconMargin - size,
                bubbleRect.Right - IconMargin,
                bubbleRect.Bottom - IconMargin),
            _ => new SKRect(
                bubbleRect.Right - IconMargin - size,
                bubbleRect.Top + IconMargin,
                bubbleRect.Right - IconMargin,
                bubbleRect.Top + IconMargin + size),
        };
    }

    public static void Draw(
        SKCanvas canvas,
        in SKRect iconRect,
        SkiaChatTheme theme,
        ChatSlashCommandStatus status)
    {
        var color = status switch
        {
            ChatSlashCommandStatus.Succeeded => theme.HoverBorder,
            ChatSlashCommandStatus.Failed or ChatSlashCommandStatus.Cancelled => new SKColor(240, 110, 110),
            _ => theme.MutedContent,
        };

        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.6f,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
        };

        var cx = iconRect.MidX;
        var cy = iconRect.MidY;
        var r = iconRect.Width * 0.42f;

        switch (status)
        {
            case ChatSlashCommandStatus.Running:
                canvas.DrawCircle(cx, cy, r, paint);
                canvas.DrawLine(cx, cy, cx, cy - r * 0.55f, paint);
                canvas.DrawLine(cx, cy, cx + r * 0.45f, cy + r * 0.12f, paint);
                break;
            case ChatSlashCommandStatus.Succeeded:
                canvas.DrawLine(cx - r * 0.55f, cy, cx - r * 0.1f, cy + r * 0.5f, paint);
                canvas.DrawLine(cx - r * 0.1f, cy + r * 0.5f, cx + r * 0.62f, cy - r * 0.55f, paint);
                break;
            case ChatSlashCommandStatus.Failed:
            case ChatSlashCommandStatus.Cancelled:
                canvas.DrawLine(cx - r * 0.45f, cy - r * 0.45f, cx + r * 0.45f, cy + r * 0.45f, paint);
                canvas.DrawLine(cx + r * 0.45f, cy - r * 0.45f, cx - r * 0.45f, cy + r * 0.45f, paint);
                break;
        }
    }
}

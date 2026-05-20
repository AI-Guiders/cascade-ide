#nullable enable

using CascadeIDE.Features.Chat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>
/// Результат локальной слэш-команды в ленте — <b>flat feed</b> (ADR 0123, UX ref: без messenger-пузыря).
/// Meta-строка + тело; рамка/inset только при ошибке; <c>audience: self</c> — тонкая полоска слева.
/// </summary>
internal sealed class SkiaChatSlashCommandEntity(
    ChatSurfaceEntry entry,
    bool compactLayout) : ISkiaChatEntity
{
    private const float GapAfter = 6f;
    private const float AccentWidth = 3f;
    private const float IconReserve = SkiaSlashCommandStatusIconRenderer.IconSize + SkiaSlashCommandStatusIconRenderer.IconMargin + 4f;

    private readonly string _slashPath = entry.SlashCommandPath ?? "";
    private readonly string? _args = entry.SlashCommandArgs;
    private readonly string? _detail = string.IsNullOrWhiteSpace(entry.Body) ? null : entry.Body;
    private readonly ChatSlashCommandStatus _status = entry.SlashCommandStatus ?? ChatSlashCommandStatus.Running;
    private readonly bool _isLocalSelfOnly = entry.Audience == IntercomMessageAudience.SelfOnly;
    private readonly string _metaTitle = entry.Title;

    public SkiaChatMeasuredLayout Measure(SkiaChatMeasureContext context)
    {
        var textWidth = context.ContentWidth - AccentWidth - 4f - IconReserve;

        var height = MetaLineHeight(compactLayout) + 2f;
        if (!string.IsNullOrWhiteSpace(_args))
            height += ArgsLineHeight(compactLayout) + 2f;
        SkiaRichTextKitBodyLayout? richDetail = null;
        if (!string.IsNullOrWhiteSpace(_detail))
        {
            richDetail = SkiaRichTextKitMarkdown.TryMeasureDocument(
                _detail,
                textWidth,
                baseFontSize: compactLayout ? 10.5f : 11f,
                contentColor: new SKColor(220, 225, 235),
                codeColor: new SKColor(180, 190, 210),
                maxRows: SkiaChatRenderLimits.MaxDocumentRows,
                lineHeight: compactLayout ? 14f : 15f,
                compactLayout);
            height += 4f + (richDetail?.BodyHeight ?? 0f);
        }

        return new SkiaChatMeasuredLayout(Math.Max(20f, height), GapAfter, RichTextDetail: richDetail);
    }

    public void Draw(SkiaChatDrawContext context, float top, in SkiaChatMeasuredLayout layout)
    {
        var rect = new SKRect(context.ContentLeft, top, context.ContentLeft + context.ContentWidth, top + layout.Height);
        var textLeft = rect.Left + AccentWidth + 4f;
        var textRight = rect.Right - IconReserve;

        DrawAccentIfNeeded(context, rect);

        var y = rect.Top + MetaBaseline(compactLayout);
        DrawMetaLine(context, textLeft, textRight, y);

        var iconRect = SkiaSlashCommandStatusIconRenderer.ResolveIconRect(
            new SKRect(textRight, rect.Top, rect.Right, rect.Top + MetaLineHeight(compactLayout) + 4f),
            ChatSlashCommandPresentation.DefaultStatusIconPlacement);
        SkiaSlashCommandStatusIconRenderer.Draw(context.Canvas, iconRect, context.Theme, _status);

        y += MetaLineHeight(compactLayout) - 10f;
        if (!string.IsNullOrWhiteSpace(_args))
        {
            y += 4f;
            using var argsFont = new SKFont(SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal), compactLayout ? 10.5f : 11f);
            using var argsPaint = new SKPaint { IsAntialias = true, Color = context.Theme.MutedContent };
            context.Canvas.DrawText(_args, textLeft, y, SKTextAlign.Left, argsFont, argsPaint);
            y += ArgsLineHeight(compactLayout);
        }

        if (!string.IsNullOrWhiteSpace(_detail))
        {
            y += 6f;
            var bodyColor = _status == ChatSlashCommandStatus.Failed
                ? new SKColor(240, 160, 160)
                : context.Theme.Content;
            var codeColor = SkiaKitColor.Blend(context.Theme.Content, context.Theme.HoverBorder, 0.35f);
            if (layout.RichTextDetail is { } rich)
            {
                SkiaRichTextKitMarkdown.Paint(
                    context.Canvas,
                    new SKPoint(textLeft, y - (compactLayout ? 10f : 11f) * 0.85f),
                    rich,
                    bodyColor,
                    codeColor);
                y += rich.BodyHeight;
            }
            else
            {
                var maxChars = Math.Max(16, (int)((textRight - textLeft) / 6.5f));
                var detailRows = SkiaMarkdownDocument.Layout(_detail, maxChars);
                y = SkiaMarkdownPainter.Draw(context, textLeft, textRight, y, detailRows, compactLayout, bodyColor);
            }
        }
    }

    public SkiaChatHit? CreateHit(in SkiaChatMeasuredLayout layout) =>
        entry.MessageIndex is { } index
            ? new SkiaChatHit(index, null, ResetDetailMode: false)
            : null;

    private void DrawAccentIfNeeded(SkiaChatDrawContext context, SKRect rect)
    {
        if (_status == ChatSlashCommandStatus.Failed)
        {
            var bar = new SKRect(rect.Left, rect.Top + 2f, rect.Left + AccentWidth, rect.Bottom - 2f);
            using var paint = new SKPaint { Color = new SKColor(220, 90, 90, 220), IsAntialias = true, Style = SKPaintStyle.Fill };
            context.Canvas.DrawRoundRect(bar, 1.5f, 1.5f, paint);
            return;
        }

        if (_isLocalSelfOnly)
        {
            var bar = new SKRect(rect.Left, rect.Top + 2f, rect.Left + AccentWidth, rect.Bottom - 2f);
            using var paint = new SKPaint { Color = new SKColor(100, 150, 200, 160), IsAntialias = true, Style = SKPaintStyle.Fill };
            context.Canvas.DrawRoundRect(bar, 1.5f, 1.5f, paint);
        }
    }

    private void DrawMetaLine(SkiaChatDrawContext context, float textLeft, float textRight, float baselineY)
    {
        var pathPart = _slashPath;
        var meta = string.IsNullOrWhiteSpace(_metaTitle) ? pathPart : $"{_metaTitle} · {pathPart}";

        using var metaFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold), compactLayout ? 10f : 10.5f);
        using var metaPaint = new SKPaint { IsAntialias = true, Color = context.Theme.Role };
        context.Canvas.DrawText(meta, textLeft, baselineY, SKTextAlign.Left, metaFont, metaPaint);

        if (_isLocalSelfOnly)
        {
            const string badge = "только ты";
            using var badgeFont = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal), 9f);
            using var badgePaint = new SKPaint { IsAntialias = true, Color = context.Theme.MutedContent };
            context.Canvas.DrawText(badge, textRight - 2f, baselineY - 1f, SKTextAlign.Right, badgeFont, badgePaint);
        }
    }

    private static float MetaLineHeight(bool compact) => compact ? 14f : 16f;
    private static float MetaBaseline(bool compact) => compact ? 12f : 14f;
    private static float ArgsLineHeight(bool compact) => compact ? 13f : 14f;
}

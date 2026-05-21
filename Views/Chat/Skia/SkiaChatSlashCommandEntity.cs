#nullable enable

using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>
/// Результат локальной слэш-команды в ленте — flat feed (ADR 0123).
/// Геометрия — <see cref="SkiaChatFeedLayout"/>.
/// </summary>
internal sealed class SkiaChatSlashCommandEntity(
    ChatSurfaceEntry entry,
    bool forwardHost,
    int feedOrdinal = 0,
    IntercomFontsSettings? intercomFonts = null) : ISkiaChatEntity
{
    private const float GapAfter = 6f;

    private readonly int _feedOrdinal = feedOrdinal;
    private readonly SkiaChatFeedLayout _layout = SkiaChatFeedLayout.For(forwardHost, intercomFonts);
    private readonly string _slashPath = entry.SlashCommandPath ?? "";
    private readonly string? _args = entry.SlashCommandArgs;
    private readonly string? _detail = string.IsNullOrWhiteSpace(entry.Body) ? null : entry.Body;
    private readonly ChatSlashCommandStatus _status = entry.SlashCommandStatus ?? ChatSlashCommandStatus.Running;
    private readonly bool _isLocalSelfOnly = entry.Audience == IntercomMessageAudience.SelfOnly;
    private readonly string _roleLabel = entry.Title;

    public SkiaChatMeasuredLayout Measure(SkiaChatMeasureContext context)
    {
        var column = _layout.SlashTextColumn(0f, context.ContentWidth);
        SkiaRichTextKitBodyLayout? richDetail = null;
        var detailHeight = 0f;
        if (!string.IsNullOrWhiteSpace(_detail))
        {
            richDetail = SkiaRichTextKitMarkdown.TryMeasureDocument(
                _detail,
                column.Width,
                baseFontSize: _layout.ProseFontSize,
                contentColor: new SKColor(220, 225, 235),
                codeColor: new SKColor(180, 190, 210),
                maxRows: SkiaChatRenderLimits.MaxDocumentRows,
                lineHeight: _layout.ProseLineHeight,
                forwardHost,
                _layout.ProseFamily,
                _layout.MonoFamily);
            detailHeight = richDetail?.BodyHeight ?? 0f;
        }

        var height = _layout.SlashRowHeight(
            !string.IsNullOrWhiteSpace(_args),
            !string.IsNullOrWhiteSpace(_detail),
            detailHeight);

        return new SkiaChatMeasuredLayout(Math.Max(20f, height), GapAfter, RichTextDetail: richDetail);
    }

    public void Draw(SkiaChatDrawContext context, float top, in SkiaChatMeasuredLayout layout)
    {
        var rowBottom = top + layout.Height;
        var isSelected = context.IsMessageHighlighted(entry.MessageIndex);
        if (isSelected)
            DrawRowSelection(context, top, rowBottom);

        if (_feedOrdinal > 0)
            SkiaChatFeedGutter.DrawOrdinal(context, top, rowBottom, _feedOrdinal, _layout, isSelected);

        var row = _layout.RowRect(context.ContentLeft, context.ContentWidth, top, layout.Height);
        var column = _layout.SlashTextColumn(context.ContentLeft, context.ContentWidth);

        SkiaChatFeedRoleRail.Draw(
            context.Canvas,
            context.Theme,
            row.Left,
            row.Top,
            row.Bottom,
            _roleLabel,
            _layout);

        DrawAccentIfNeeded(context, row, _layout.RoleRailWidth);

        var metaBaseline = _layout.SlashMetaBaselineY(row.Top);
        DrawMetaLine(context, column, metaBaseline);

        var iconRect = SkiaSlashCommandStatusIconRenderer.ResolveIconRect(
            new SKRect(column.Right, row.Top, row.Right, row.Top + _layout.SlashMetaLineHeight + _layout.SlashAccentGap),
            ChatSlashCommandPresentation.DefaultStatusIconPlacement);
        SkiaSlashCommandStatusIconRenderer.Draw(context.Canvas, iconRect, context.Theme, _status);

        if (!string.IsNullOrWhiteSpace(_args))
        {
            var argsBaseline = _layout.SlashArgsBandTop(row.Top) + _layout.SlashArgsFontSize * SkiaChatFeedLayout.TextBaselineFactor;
            using var argsFont = SkiaChatFeedFontResolver.CreateFont(_layout.SlashArgsFamily, _layout.SlashArgsFontSize);
            using var argsPaint = new SKPaint { IsAntialias = true, Color = context.Theme.MutedContent };
            context.Canvas.DrawText(_args, column.Left, argsBaseline, SKTextAlign.Left, argsFont, argsPaint);
        }

        if (!string.IsNullOrWhiteSpace(_detail))
        {
            var bodyColor = _status == ChatSlashCommandStatus.Failed
                ? new SKColor(240, 160, 160)
                : context.Theme.Content;
            var codeColor = SkiaKitColor.Blend(context.Theme.Content, context.Theme.HoverBorder, 0.35f);
            var detailBaseline = _layout.SlashDetailBaselineY(row.Top, !string.IsNullOrWhiteSpace(_args));
            if (layout.RichTextDetail is { } rich)
            {
                var origin = _layout.RichTextPaintOrigin(column.Left, detailBaseline, _layout.ProseFontSize);
                SkiaRichTextKitMarkdown.Paint(context.Canvas, origin, rich, bodyColor, codeColor);
            }
            else
            {
                var maxChars = _layout.MaxCharsForWidth(column.Width);
                var detailRows = SkiaMarkdownDocument.Layout(_detail, maxChars);
                SkiaMarkdownPainter.Draw(
                    context,
                    column.Left,
                    column.Right,
                    detailBaseline,
                    detailRows,
                    forwardHost,
                    bodyColor,
                    _layout);
            }
        }

        if (entry.MessageIndex is { } messageIndex)
        {
            var rowRect = new SKRect(
                _feedOrdinal > 0 ? context.FeedGutterLeft : context.ContentLeft,
                top,
                context.ContentLeft + context.ContentWidth,
                rowBottom);
            context.RegisterHit(rowRect, new SkiaChatHit(messageIndex, null, ResetDetailMode: false));
        }
    }

    public SkiaChatHit? CreateHit(in SkiaChatMeasuredLayout layout) => null;

    private static void DrawRowSelection(SkiaChatDrawContext context, float top, float bottom)
    {
        var left = context.FeedGutterLeft;
        var rect = new SKRect(left, top, context.ContentLeft + context.ContentWidth, bottom);
        using var rowFill = new SKPaint
        {
            Color = SkiaKit.SkiaKitColor.Blend(context.Theme.Surface, context.Theme.SelectedBorder, 0.26f),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };
        context.Canvas.DrawRect(rect, rowFill);
    }

    private void DrawAccentIfNeeded(SkiaChatDrawContext context, SKRect row, float roleW)
    {
        var accentLeft = row.Left + roleW;
        if (_status == ChatSlashCommandStatus.Failed)
        {
            var bar = new SKRect(
                accentLeft,
                row.Top + _layout.RowEdgePad,
                accentLeft + _layout.SlashAccentWidth,
                row.Bottom - _layout.RowEdgePad);
            using var paint = new SKPaint { Color = new SKColor(220, 90, 90, 220), IsAntialias = true, Style = SKPaintStyle.Fill };
            context.Canvas.DrawRoundRect(bar, 1.5f, 1.5f, paint);
            return;
        }

        if (!_isLocalSelfOnly)
            return;

        var selfBar = new SKRect(
            accentLeft,
            row.Top + _layout.RowEdgePad,
            accentLeft + _layout.SlashAccentWidth,
            row.Bottom - _layout.RowEdgePad);
        using var selfPaint = new SKPaint { Color = new SKColor(100, 150, 200, 160), IsAntialias = true, Style = SKPaintStyle.Fill };
        context.Canvas.DrawRoundRect(selfBar, 1.5f, 1.5f, selfPaint);
    }

    private void DrawMetaLine(SkiaChatDrawContext context, in SlashTextColumn column, float baselineY)
    {
        using var metaFont = SkiaChatFeedFontResolver.CreateFont(
            _layout.SlashMetaFamily,
            _layout.SlashMetaFontSize,
            SKFontStyle.Bold);
        using var metaPaint = new SKPaint { IsAntialias = true, Color = context.Theme.Role };
        context.Canvas.DrawText(_slashPath, column.Left, baselineY, SKTextAlign.Left, metaFont, metaPaint);

        if (!_isLocalSelfOnly)
            return;

        const string badge = "только ты";
        using var badgeFont = SkiaChatFeedFontResolver.CreateFont(_layout.ProseFamily, _layout.SlashBadgeFontSize);
        using var badgePaint = new SKPaint { IsAntialias = true, Color = context.Theme.MutedContent };
        context.Canvas.DrawText(badge, column.Right - 2f, baselineY - 1f, SKTextAlign.Right, badgeFont, badgePaint);
    }
}

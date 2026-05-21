#nullable enable

using CascadeIDE.Models;
using CascadeIDE.Services;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>
/// Единые метрики flat feed (ADR 0123): role rail, колонка тела, prose/slash — без разрозненных inset/baseline в сущностях.
/// </summary>
internal readonly struct SkiaChatFeedLayout
{
    public const float TextBaselineFactor = 0.85f;
    public const float MinColumnWidth = 80f;
    public const float CharsPerEm = 7.1f;
    private const float ReferenceProsePt = 11f;

    /// <summary>Чат в лобовом Forward; плотнее метрики. Не ширина колонки (split — отдельная топология).</summary>
    public bool ForwardHost { get; }
    public IntercomFontsSettings Fonts { get; }

    public string ProseFamily { get; }
    public string MonoFamily { get; }
    public string ChipFamily { get; }
    public string ChipIdFamily { get; }
    public string SlashMetaFamily { get; }
    public string SlashArgsFamily { get; }
    public string RoleFamily { get; }
    public string GutterFamily { get; }

    private float Scale { get; }

    public float RoleRailWidth => (ForwardHost ? 48f : 56f) * Scale;
    public float RoleLabelInset => 4f;
    public float TextInset => 6f;
    public float BodyTopPad => 6f;
    public float SegmentGap => 4f;
    public float RowEdgePad => 2f;

    public float ProseFontSize { get; }
    public float ProseLineHeight => (ForwardHost ? 14f : 15f) * Scale;
    public float ProseMeasureWidthTrim => TextInset * 2f;
    public float AttachChipFontSize => 11f * Scale;

    public float SlashAccentWidth => 3f;
    public float SlashAccentGap => 4f;
    public float SlashMetaBodyGap => SegmentGap;
    public float SlashIconReserve =>
        SkiaSlashCommandStatusIconRenderer.IconSize
        + SkiaSlashCommandStatusIconRenderer.IconMargin
        + SlashAccentGap;

    public float SlashMetaFontSize => (ForwardHost ? 10f : 10.5f) * Scale;
    public float SlashArgsFontSize => ProseFontSize;
    public float SlashMetaLineHeight => (ForwardHost ? 14f : 16f) * Scale;
    public float SlashArgsLineHeight => (ForwardHost ? 13f : 14f) * Scale;
    public float SlashBadgeFontSize => 9f * Scale;

    public float RoleLabelFontSize => (ForwardHost ? 9f : 9.5f) * Scale;
    public float GutterOrdinalFontSize => 10f * Scale;

    private SkiaChatFeedLayout(bool forwardHost, IntercomFontsSettings fonts)
    {
        ForwardHost = forwardHost;
        Fonts = fonts;
        ProseFontSize = fonts.ResolveProsePt(forwardHost);
        Scale = ProseFontSize / ReferenceProsePt;
        ProseFamily = fonts.ResolveProseFamily();
        MonoFamily = fonts.ResolveMonoFamily();
        ChipFamily = fonts.ResolveChipFamily();
        ChipIdFamily = fonts.ResolveChipIdFamily();
        SlashMetaFamily = fonts.ResolveSlashMetaFamily();
        SlashArgsFamily = fonts.ResolveSlashArgsFamily();
        RoleFamily = fonts.ResolveRoleFamily();
        GutterFamily = fonts.ResolveGutterFamily();
    }

    public static SkiaChatFeedLayout For(bool forwardHost, IntercomFontsSettings? fonts = null) =>
        new(forwardHost, fonts ?? IntercomFontDefaults.Intercom);

    public bool ShouldShowRoleRail(string? roleLabel, bool suppressTitle) =>
        !suppressTitle && !string.IsNullOrWhiteSpace(roleLabel);

    public FeedBodyColumn BodyColumn(float contentLeft, float contentWidth, bool includeRoleRail)
    {
        if (!includeRoleRail)
            return new FeedBodyColumn(contentLeft, Math.Max(MinColumnWidth, contentWidth));

        var left = contentLeft + RoleRailWidth;
        var width = Math.Max(MinColumnWidth, contentWidth - RoleRailWidth);
        return new FeedBodyColumn(left, width);
    }

    public SkiaChatMeasureContext NarrowMeasureContext(SkiaChatMeasureContext context, bool includeRoleRail)
    {
        if (!includeRoleRail)
            return context;

        var column = BodyColumn(0f, context.ContentWidth, includeRoleRail: true);
        return context.WithContentWidth(column.Width);
    }

    public int MaxCharsForWidth(float width) =>
        Math.Max(12, (int)(width / CharsPerEm));

    public SKRect RowRect(float contentLeft, float contentWidth, float top, float height) =>
        new(contentLeft, top, contentLeft + contentWidth, top + height);

    public SKRect SegmentRect(in FeedBodyColumn column, float top, float height) =>
        new(column.Left, top, column.Right, top + height);

    public float FirstLineBaselineY(float rowTop, float titleHeight = 0f) =>
        rowTop + titleHeight + BodyTopPad + ProseFontSize * TextBaselineFactor;

    public float RoleLabelBaselineY(float rowTop) =>
        rowTop + BodyTopPad + RoleLabelFontSize * TextBaselineFactor;

    public float SlashMetaBaselineY(float rowTop) =>
        rowTop + BodyTopPad + SlashMetaFontSize * TextBaselineFactor;

    public SKPoint RichTextPaintOrigin(float textLeft, float firstLineBaselineY, float fontSize) =>
        new(textLeft, firstLineBaselineY - fontSize * TextBaselineFactor);

    public SlashTextColumn SlashTextColumn(float contentLeft, float contentWidth)
    {
        var body = BodyColumn(contentLeft, contentWidth, includeRoleRail: true);
        var left = body.Left + SlashAccentWidth + SlashAccentGap;
        var right = body.Right - SlashIconReserve;
        var width = Math.Max(MinColumnWidth, right - left);
        return new SlashTextColumn(left, right, width);
    }

    public float SlashRowHeight(bool hasArgs, bool hasDetail, float detailHeight)
    {
        var height = SlashMetaLineHeight + SlashMetaBodyGap;
        if (hasArgs)
            height += SlashArgsLineHeight + SlashMetaBodyGap;
        if (hasDetail)
            height += SlashMetaBodyGap + detailHeight;
        return height;
    }

    public float SlashMetaBandTop(float rowTop) => rowTop;

    public float SlashArgsBandTop(float rowTop) => rowTop + SlashMetaLineHeight + SlashMetaBodyGap;

    public float SlashDetailBaselineY(float rowTop, bool hasArgs)
    {
        var bandTop = hasArgs
            ? SlashArgsBandTop(rowTop) + SlashArgsLineHeight + SlashMetaBodyGap
            : SlashMetaBandTop(rowTop) + SlashMetaLineHeight + SlashMetaBodyGap;
        return bandTop + ProseFontSize * TextBaselineFactor;
    }

    public float RoleLabelMaxWidth => RoleRailWidth - RoleLabelInset * 2f;
}

internal readonly struct FeedBodyColumn(float left, float width)
{
    public float Left { get; } = left;
    public float Width { get; } = width;
    public float Right => Left + Width;

    public float TextLeft(SkiaChatFeedLayout layout) => Left + layout.TextInset;
}

internal readonly struct SlashTextColumn(float left, float right, float width)
{
    public float Left { get; } = left;
    public float Right { get; } = right;
    public float Width { get; } = width;
}

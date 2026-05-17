#nullable enable
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat;

/// <summary>Данные карточки темы / spine для Skia overview (секции через <see cref="SkiaSectionedCard"/>).</summary>
internal sealed record SkiaTopicCardModel(
    SkiaSectionedCardModel Card,
    bool IsFocused,
    bool IsMainThread);

/// <summary>Геометрия карточки после Measure.</summary>
internal readonly record struct SkiaTopicCardLayout(
    float Height,
    float GapAfter,
    float ContentInsetX)
{
    public const int MaxSummaryLines = 4;
    public const float GapAfterDefault = 12f;
}

/// <summary>Состояние отрисовки (hover/focus).</summary>
internal readonly record struct SkiaTopicCardDrawState(bool IsHovered, bool IsFocused);

/// <summary>Skia-отрисовка карточки темы: делегирует в SkiaKit.</summary>
internal static class SkiaTopicCard
{
    public static IReadOnlyList<string> PrepareSummaryLines(string summary, int maxChars)
    {
        var lines = SkiaTextLayout.Wrap(Trim(summary, 32_000), maxChars);
        if (lines.Count == 0)
            return ["Нет краткого описания"];
        if (lines.Count == 1 && string.Equals(lines[0], "Пока без сообщений", StringComparison.Ordinal))
            return ["Пока без сообщений — задай вопрос в теме"];
        if (lines.Count > SkiaTopicCardLayout.MaxSummaryLines)
            return lines.Take(SkiaTopicCardLayout.MaxSummaryLines).ToList();
        return lines;
    }

    public static IReadOnlyList<string> ParseTagLines(string? footer)
    {
        if (string.IsNullOrWhiteSpace(footer))
            return ["—"];

        var tags = footer.Split(" · ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tags.Length == 0 ? ["—"] : tags;
    }

    public static IReadOnlyList<string> NormalizeTagLines(IReadOnlyList<string> tags) =>
        tags.Count == 0 ? ["—"] : tags;

    public static SkiaTopicCardModel Create(
        string title,
        string summary,
        string? tagFooter,
        bool isFocused,
        int maxChars)
    {
        var topicLines = new List<string> { title.Trim() };
        var card = SkiaSectionedCard.FromThreeCompartments(
            "ТЕМА",
            topicLines,
            "ТЕГИ",
            ParseTagLines(tagFooter),
            "САММАРИ",
            PrepareSummaryLines(summary, maxChars));
        return new(
            card,
            isFocused,
            title.TrimStart().StartsWith("◆ ", StringComparison.Ordinal));
    }

    public static SkiaTopicCardModel CreateSpine(
        string lineTitle,
        string currentFocus,
        IReadOnlyList<string> milestones,
        bool includeInAgentContext,
        int maxChars)
    {
        var tagLines = new List<string>(NormalizeTagLines(milestones));
        tagLines.Add(includeInAgentContext ? "в контексте агента" : "не в контексте агента");
        var summary = string.IsNullOrWhiteSpace(currentFocus)
            ? "Задай фокус линии и вехи над чатом."
            : currentFocus.Trim();
        var card = SkiaSectionedCard.FromThreeCompartments(
            "ТЕМА",
            ["↗ " + lineTitle.Trim()],
            "ТЕГИ",
            tagLines,
            "САММАРИ",
            PrepareSummaryLines(summary, maxChars));
        return new(card, IsFocused: false, IsMainThread: true);
    }

    public static SkiaTopicCardLayout Measure(SkiaTopicCardModel model) =>
        new(
            SkiaSectionedCard.MeasureTotalHeight(model.Card),
            SkiaTopicCardLayout.GapAfterDefault,
            SkiaSectionedCard.HorizontalPadding);

    public static SKColor ResolveFillColor(SkiaChatTheme theme, SkiaTopicCardModel model) =>
        model.IsFocused
            ? SkiaKitColor.Blend(theme.BubbleAssistant, theme.SelectedBorder, 0.22f)
            : SkiaKitColor.Blend(theme.Surface, theme.BubbleAssistant, 0.82f);

    public static void Draw(
        SKCanvas canvas,
        SkiaChatTheme theme,
        SKRect bounds,
        float contentLeft,
        float contentWidth,
        SkiaTopicCardModel model,
        SkiaTopicCardLayout layout,
        SkiaTopicCardDrawState state)
    {
        var drawState = new SkiaSectionedCardDrawState(
            ResolveFillColor(theme, model),
            state.IsHovered,
            state.IsFocused);
        SkiaSectionedCard.Draw(canvas, theme, bounds, contentLeft, contentWidth, model.Card, in drawState);
    }

    private static string Trim(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..maxLen] + "...";
}

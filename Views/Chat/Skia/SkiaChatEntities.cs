#nullable enable
using CascadeIDE.Views.SkiaKit;
using SkiaSharp;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>
/// Карточка темы в картотеке (overview). Отдельный chrome: <see cref="SkiaTopicCard"/>,
/// не <see cref="SkiaChatBubbleKind.CardPanel"/> (product spine — см. README.md).
/// </summary>
internal sealed class SkiaChatTopicCard(
    SkiaTopicCardModel model,
    Guid threadId) : ISkiaChatEntity, IGridTileSkiaChatEntity
{
    public static SkiaChatTopicCard ForThread(
        string title,
        string summary,
        string? tagFooter,
        bool isFocused,
        Guid threadId) =>
        new(
            SkiaTopicCard.Create(title, summary, tagFooter, isFocused, maxChars: 80),
            threadId);

    public static SkiaChatTopicCard ForSpine(
        string lineTitle,
        string currentFocus,
        IReadOnlyList<string> milestones,
        bool includeInAgentContext) =>
        new(
            SkiaTopicCard.CreateSpine(lineTitle, currentFocus, milestones, includeInAgentContext, maxChars: 80),
            Guid.Empty);

    public bool IsGridTile => threadId != Guid.Empty;

    public SkiaChatMeasuredLayout Measure(SkiaChatMeasureContext context)
    {
        var maxChars = Math.Max(16, (int)(context.ContentWidth / 7.1f));
        var measuredModel = RemeasureModel(maxChars);
        var cardLayout = SkiaTopicCard.Measure(measuredModel);
        return new SkiaChatMeasuredLayout(
            cardLayout.Height,
            cardLayout.GapAfter,
            TopicCard: measuredModel,
            TopicCardLayout: cardLayout);
    }

    public void Draw(SkiaChatDrawContext context, float top, in SkiaChatMeasuredLayout layout)
    {
        var model = layout.TopicCard!;
        var cardLayout = layout.TopicCardLayout!.Value;
        var rect = new SKRect(context.ContentLeft, top, context.ContentLeft + context.ContentWidth, top + layout.Height);
        SkiaTopicCard.Draw(
            context.Canvas,
            context.Theme,
            rect,
            context.ContentLeft + cardLayout.ContentInsetX,
            context.ContentWidth - cardLayout.ContentInsetX * 2,
            model,
            cardLayout,
            new SkiaTopicCardDrawState(context.IsHovered, model.IsFocused));
    }

    public SkiaChatHit? CreateHit(in SkiaChatMeasuredLayout layout) =>
        threadId == Guid.Empty
            ? null
            : new(MessageIndex: null, SelectThreadId: threadId, ResetDetailMode: false);

    private SkiaTopicCardModel RemeasureModel(int maxChars)
    {
        var sections = model.Card.Sections;
        if (threadId == Guid.Empty)
        {
            var title = sections.Count > 0 && sections[0].Lines.Count > 0
                ? sections[0].Lines[0].TrimStart('↗', ' ')
                : "";
            var tagSection = sections.Count > 1 ? sections[1].Lines : [];
            var summary = sections.Count > 2 ? string.Join(' ', sections[2].Lines) : "";
            var milestones = tagSection
                .Where(t => t is not "—" and not "в контексте агента" and not "не в контексте агента")
                .ToList();
            var include = tagSection.Contains("в контексте агента");
            return SkiaTopicCard.CreateSpine(title, summary, milestones, include, maxChars);
        }

        var topicTitle = sections.Count > 0 && sections[0].Lines.Count > 0 ? sections[0].Lines[0] : "";
        var summaryText = sections.Count > 2 ? string.Join(' ', sections[2].Lines) : "";
        var tagLines = sections.Count > 1 ? sections[1].Lines : [];
        var tags = tagLines.Count == 1 && tagLines[0] == "—"
            ? null
            : string.Join(" · ", tagLines);
        return SkiaTopicCard.Create(topicTitle, summaryText, tags, model.IsFocused, maxChars);
    }
}

#nullable enable
using CascadeIDE.Features.Chat;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Creator: snapshot → список <see cref="ISkiaChatEntity"/> (ADR entity pipeline).</summary>
internal static class ChatSurfaceEntityFactory
{
    public static IReadOnlyList<ISkiaChatEntity> Build(
        ChatSurfaceSnapshot snapshot,
        bool overviewMode,
        Guid detailThreadId,
        bool compactLayout = false)
    {
        var entities = new List<ISkiaChatEntity>();
        var hideNavChromeInFeed = compactLayout;

        if (!hideNavChromeInFeed)
            AppendSpine(entities, snapshot.ProductSpine, overviewMode, compactLayout);

        if (snapshot.Layout.Overview.Count > 0)
        {
            AppendOverview(entities, snapshot, overviewMode, detailThreadId, compactLayout, hideNavChromeInFeed);
            if (overviewMode)
                return entities;
        }

        if (!overviewMode && snapshot.TopicPicker != TopicPickerPresentation.None)
        {
            var counts = ChatThreadPresentation.MessageCountsByThread(snapshot);
            entities.Add(new SkiaChatTopicPickerEntity(
                snapshot.TopicPicker,
                snapshot.State.Threads,
                counts));
        }

        AppendDetailLanes(entities, snapshot, detailThreadId, compactLayout, hideNavChromeInFeed);
        return entities;
    }

    private static void AppendSpine(List<ISkiaChatEntity> entities, ChatProductSpine spine, bool overviewMode, bool compactLayout)
    {
        if (!spine.HasContent)
            return;

        var title = ChatProductSpinePresentation.ResolveLineTitle(spine);
        if (overviewMode)
        {
            entities.Add(SkiaChatTopicCard.ForSpine(
                title,
                spine.CurrentFocus,
                spine.Milestones,
                spine.IncludeInAgentContext));
            return;
        }

        entities.Add(new SkiaChatBubbleEntity(
            D(
                new SkiaChatBubbleSpec(
                    "Spine · " + title,
                    ChatProductSpinePresentation.FormatDetailStripFocus(spine.CurrentFocus),
                    Footer: null,
                    SkiaChatBubbleKind.SpineStrip,
                    SkiaBubbleFillRole.SpineStrip,
                    SkiaChatBodyTone.Normal,
                    IsPending: false,
                    IsSelected: false,
                    StartsBranch: false,
                    MessageIndex: null,
                    MinHeight: 44,
                    MaxBodyLines: 1,
                    GapAfter: 6,
                    Padding: 8,
                    TitleHeight: 16,
                    LineHeight: 15),
                compactLayout),
            static () => new SkiaChatHit(null, null, ResetDetailMode: true)));
    }

    private static void AppendOverview(
        List<ISkiaChatEntity> entities,
        ChatSurfaceSnapshot snapshot,
        bool overviewMode,
        Guid detailThreadId,
        bool compactLayout,
        bool hideNavChromeInFeed)
    {
        if (!overviewMode && !hideNavChromeInFeed)
            entities.Add(OverviewBackLink(compactLayout));

        foreach (var item in snapshot.Layout.Overview)
        {
            if (overviewMode)
            {
                entities.Add(SkiaChatTopicCard.ForThread(
                    ChatThreadOverviewPresentation.FormatTopicTitle(item),
                    item.Summary,
                    ChatThreadOverviewPresentation.FormatTopicBadges(item),
                    detailThreadId == item.ThreadId,
                    item.ThreadId));
                continue;
            }

            if (hideNavChromeInFeed)
                continue;

            var fillRole = detailThreadId == item.ThreadId
                ? SkiaBubbleFillRole.ThreadRowActive
                : SkiaBubbleFillRole.ThreadRow;
            entities.Add(new SkiaChatBubbleEntity(
                D(
                    new SkiaChatBubbleSpec(
                        ChatThreadOverviewPresentation.FormatSideThreadRowTitle(item),
                        ChatThreadOverviewPresentation.FormatSideThreadRowMeta(item),
                        Footer: null,
                        SkiaChatBubbleKind.Feed,
                        fillRole,
                        SkiaChatBodyTone.Normal,
                        IsPending: false,
                        IsSelected: false,
                        StartsBranch: false,
                        MessageIndex: null),
                    compactLayout),
                () => new SkiaChatHit(null, item.ThreadId, ResetDetailMode: false)));
        }
    }

    private static SkiaChatBubbleEntity OverviewBackLink(bool compactLayout) =>
        new(
            D(
                new SkiaChatBubbleSpec(
                    "Темы",
                    "Назад к обзору всех веток",
                    Footer: null,
                    SkiaChatBubbleKind.Feed,
                    SkiaBubbleFillRole.OverviewNav,
                    SkiaChatBodyTone.Normal,
                    IsPending: false,
                    IsSelected: false,
                    StartsBranch: false,
                    MessageIndex: null),
                compactLayout),
            static () => new SkiaChatHit(null, null, ResetDetailMode: true));

    private static void AppendDetailLanes(
        List<ISkiaChatEntity> entities,
        ChatSurfaceSnapshot snapshot,
        Guid detailThreadId,
        bool compactLayout,
        bool hideNavChromeInFeed)
    {
        var lanes = snapshot.Layout.Lanes.OrderBy(lane => lane.Thread.Order);
        if (detailThreadId != Guid.Empty)
            lanes = lanes.Where(lane => lane.Thread.ThreadId == detailThreadId).OrderBy(lane => lane.Thread.Order);

        foreach (var lane in lanes)
        {
            if (!hideNavChromeInFeed)
            {
                var headerRole = lane.Thread.IsActive
                    ? SkiaBubbleFillRole.ThreadHeaderActive
                    : SkiaBubbleFillRole.ThreadHeader;
                entities.Add(new SkiaChatBubbleEntity(
                    D(
                        new SkiaChatBubbleSpec(
                            lane.Thread.Title,
                            ChatThreadOverviewPresentation.FormatThreadHeaderMeta(lane.Thread),
                            Footer: null,
                            SkiaChatBubbleKind.Feed,
                            headerRole,
                            SkiaChatBodyTone.Normal,
                            IsPending: false,
                            IsSelected: false,
                            StartsBranch: false,
                            MessageIndex: null),
                        compactLayout),
                    () => new SkiaChatHit(null, lane.Thread.ThreadId, ResetDetailMode: false)));
            }

            ChatMessageVisualRole? previousMessageRole = null;
            var feedOrdinal = 0;
            var showFeedGutter = detailThreadId != Guid.Empty;
            foreach (var entry in lane.Entries)
            {
                if (entry.Kind == ChatSurfaceEntryKind.Message)
                {
                    if (showFeedGutter)
                        feedOrdinal++;
                    var suppressTitle = previousMessageRole == entry.VisualRole
                                        && !entry.StartsBranch
                                        && entry.VisualRole is ChatMessageVisualRole.User
                                            or ChatMessageVisualRole.Assistant
                                            or ChatMessageVisualRole.Thinking;
                    var gapAfter = suppressTitle
                        ? compactLayout ? 2f : 3f
                        : compactLayout ? 5f : 8f;
                    entities.Add(MessageEntity(
                        entry,
                        compactLayout,
                        suppressTitle,
                        gapAfter,
                        showFeedGutter ? feedOrdinal : 0));
                    previousMessageRole = entry.VisualRole;
                    continue;
                }

                entities.Add(ConfirmationEntity(entry, compactLayout));
                previousMessageRole = null;
            }
        }
    }

    private static SkiaChatBubbleSpec D(in SkiaChatBubbleSpec spec, bool compact) =>
        SkiaChatDensity.Apply(spec, compact);

    private static ISkiaChatEntity MessageEntity(
        ChatSurfaceEntry entry,
        bool compactLayout,
        bool suppressTitle,
        float gapAfter,
        int feedOrdinal = 0)
    {
        if (entry.VisualRole == ChatMessageVisualRole.SlashCommand
            && entry.SlashCommandStatus is { } slashStatus
            && !string.IsNullOrWhiteSpace(entry.SlashCommandPath))
        {
            return new SkiaChatSlashCommandEntity(entry, compactLayout);
        }

        return new SkiaChatMessageFeedEntity(entry, compactLayout, suppressTitle, gapAfter, feedOrdinal);
    }

    private static SkiaChatBubbleEntity ConfirmationEntity(ChatSurfaceEntry entry, bool compactLayout)
    {
        var fillRole = SkiaBubbleFillRoleMapping.FromMessageRole(entry.VisualRole);
        return new SkiaChatBubbleEntity(
            D(
                new SkiaChatBubbleSpec(
                    entry.Title,
                    entry.Body,
                    Footer: null,
                    SkiaChatBubbleKind.Feed,
                    fillRole,
                    SkiaChatBodyTone.Normal,
                    IsPending: entry.IsPending,
                    entry.IsSelected,
                    StartsBranch: false,
                    MessageIndex: null,
                    GapAfter: 5,
                    Padding: 0,
                    TitleHeight: 14,
                    LineHeight: 14),
                compactLayout),
            static () => new SkiaChatHit(null, null, ResetDetailMode: false));
    }
}

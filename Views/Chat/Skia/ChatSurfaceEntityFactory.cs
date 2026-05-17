#nullable enable
using CascadeIDE.Features.Chat;

namespace CascadeIDE.Views.Chat.Skia;

/// <summary>Creator: snapshot → список <see cref="ISkiaChatEntity"/> (ADR entity pipeline).</summary>
internal static class ChatSurfaceEntityFactory
{
    public static IReadOnlyList<ISkiaChatEntity> Build(
        ChatSurfaceSnapshot snapshot,
        bool overviewMode,
        Guid detailThreadId)
    {
        var entities = new List<ISkiaChatEntity>();
        AppendSpine(entities, snapshot.ProductSpine, overviewMode);

        if (snapshot.Layout.Overview.Count > 0)
        {
            AppendOverview(entities, snapshot, overviewMode, detailThreadId);
            if (overviewMode)
                return entities;
        }

        AppendDetailLanes(entities, snapshot, detailThreadId);
        return entities;
    }

    private static void AppendSpine(List<ISkiaChatEntity> entities, ChatProductSpine spine, bool overviewMode)
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
            static () => new SkiaChatHit(null, null, ResetDetailMode: true)));
    }

    private static void AppendOverview(
        List<ISkiaChatEntity> entities,
        ChatSurfaceSnapshot snapshot,
        bool overviewMode,
        Guid detailThreadId)
    {
        if (!overviewMode)
            entities.Add(OverviewBackLink());

        if (overviewMode)
        {
            var topicCount = snapshot.Layout.Overview.Count;
            entities.Add(new SkiaChatBubbleEntity(
                new SkiaChatBubbleSpec(
                    "Картотека тем",
                    ChatThreadOverviewPresentation.FormatCatalogHint(topicCount),
                    ChatThreadOverviewPresentation.CatalogFooter,
                    SkiaChatBubbleKind.OverviewHeader,
                    SkiaBubbleFillRole.OverviewNav,
                    SkiaChatBodyTone.Normal,
                    IsPending: false,
                    IsSelected: false,
                    StartsBranch: false,
                    MessageIndex: null,
                    MinHeight: 56,
                    MaxBodyLines: 1,
                    GapAfter: 10,
                    Padding: 12,
                    TitleHeight: 20,
                    FooterHeight: 14,
                    LineHeight: 15),
                static () => null));
        }

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

            var fillRole = detailThreadId == item.ThreadId
                ? SkiaBubbleFillRole.ThreadRowActive
                : SkiaBubbleFillRole.ThreadRow;
            entities.Add(new SkiaChatBubbleEntity(
                new SkiaChatBubbleSpec(
                    ChatThreadOverviewPresentation.FormatSideThreadRowTitle(item),
                    ChatThreadOverviewPresentation.FormatSideThreadRowMeta(item),
                    Footer: null,
                    SkiaChatBubbleKind.Standard,
                    fillRole,
                    SkiaChatBodyTone.Normal,
                    IsPending: false,
                    IsSelected: false,
                    StartsBranch: false,
                    MessageIndex: null),
                () => new SkiaChatHit(null, item.ThreadId, ResetDetailMode: false)));
        }
    }

    private static SkiaChatBubbleEntity OverviewBackLink() =>
        new(
            new SkiaChatBubbleSpec(
                "Темы",
                "Назад к обзору всех веток",
                Footer: null,
                SkiaChatBubbleKind.Standard,
                SkiaBubbleFillRole.OverviewNav,
                SkiaChatBodyTone.Normal,
                IsPending: false,
                IsSelected: false,
                StartsBranch: false,
                MessageIndex: null),
            static () => new SkiaChatHit(null, null, ResetDetailMode: true));

    private static void AppendDetailLanes(
        List<ISkiaChatEntity> entities,
        ChatSurfaceSnapshot snapshot,
        Guid detailThreadId)
    {
        var lanes = snapshot.Layout.Lanes.OrderBy(lane => lane.Thread.Order);
        if (detailThreadId != Guid.Empty)
            lanes = lanes.Where(lane => lane.Thread.ThreadId == detailThreadId).OrderBy(lane => lane.Thread.Order);

        foreach (var lane in lanes)
        {
            var headerRole = lane.Thread.IsActive
                ? SkiaBubbleFillRole.ThreadHeaderActive
                : SkiaBubbleFillRole.ThreadHeader;
            entities.Add(new SkiaChatBubbleEntity(
                new SkiaChatBubbleSpec(
                    lane.Thread.Title,
                    ChatThreadOverviewPresentation.FormatThreadHeaderMeta(lane.Thread),
                    Footer: null,
                    SkiaChatBubbleKind.Standard,
                    headerRole,
                    SkiaChatBodyTone.Normal,
                    IsPending: false,
                    IsSelected: false,
                    StartsBranch: false,
                    MessageIndex: null),
                () => new SkiaChatHit(null, lane.Thread.ThreadId, ResetDetailMode: false)));

            foreach (var entry in lane.Entries)
            {
                entities.Add(entry.Kind == ChatSurfaceEntryKind.Message
                    ? MessageEntity(entry)
                    : ConfirmationEntity(entry));
            }
        }
    }

    private static SkiaChatBubbleEntity MessageEntity(ChatSurfaceEntry entry)
    {
        var fillRole = SkiaBubbleFillRoleMapping.FromMessageRole(entry.VisualRole);
        return new SkiaChatBubbleEntity(
            new SkiaChatBubbleSpec(
                entry.Title,
                entry.Body,
                Footer: null,
                SkiaChatBubbleKind.Standard,
                fillRole,
                SkiaChatBodyTone.Normal,
                IsPending: false,
                entry.IsSelected,
                entry.StartsBranch,
                entry.MessageIndex),
            () => new SkiaChatHit(entry.MessageIndex, null, ResetDetailMode: false));
    }

    private static SkiaChatBubbleEntity ConfirmationEntity(ChatSurfaceEntry entry)
    {
        var fillRole = SkiaBubbleFillRoleMapping.FromMessageRole(entry.VisualRole);
        return new SkiaChatBubbleEntity(
            new SkiaChatBubbleSpec(
                entry.Title,
                entry.Body,
                Footer: null,
                SkiaChatBubbleKind.Standard,
                fillRole,
                SkiaChatBodyTone.Normal,
                IsPending: entry.IsPending,
                entry.IsSelected,
                StartsBranch: false,
                MessageIndex: null),
            static () => new SkiaChatHit(null, null, ResetDetailMode: false));
    }
}

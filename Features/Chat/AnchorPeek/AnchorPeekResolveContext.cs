#nullable enable

using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Features.Chat.AnchorPeek;

internal sealed record AnchorPeekResolveContext(
    int SelectedMessageIndex,
    IReadOnlyList<AttachmentAnchor> SelectedMessageAnchors,
    IReadOnlyDictionary<string, AttachmentAnchor> PendingDrafts,
    IReadOnlyList<FeedMessageAnchor> AllMessageAnchors)
{
    public static AnchorPeekResolveContext Empty { get; } = new(-1, [], new Dictionary<string, AttachmentAnchor>(), []);
}

internal readonly record struct FeedMessageAnchor(int MessageIndex, AttachmentAnchor Anchor);

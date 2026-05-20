#nullable enable

using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat;

/// <summary>Детерминированная projection wire → сегменты ленты (ADR 0134). Не хранится в event log.</summary>
public static class IntercomFeedProjector
{
    public static IReadOnlyList<IntercomAttachmentFeedSegment> ProjectProse(
        string prose,
        IReadOnlyList<AttachmentAnchor> attachments) =>
        IntercomAttachmentMarkers.SplitFeedSegments(prose, attachments);
}

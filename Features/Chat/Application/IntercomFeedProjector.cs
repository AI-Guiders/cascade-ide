#nullable enable

using CascadeIDE.Contracts;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat.Application;

/// <summary>Детерминированная projection wire → сегменты ленты (ADR 0134). Не хранится в event log.</summary>
[ComputingUnit]
public static class IntercomFeedProjector
{
    public static IReadOnlyList<IntercomAttachmentFeedSegment> ProjectProse(
        string prose,
        IReadOnlyList<AttachmentAnchor> attachments) =>
        IntercomAttachmentMarkers.SplitFeedSegments(prose, attachments);
}

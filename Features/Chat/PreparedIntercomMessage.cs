#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Итог prepare: wire + статус для commit policy (ADR 0134).</summary>
public sealed record PreparedIntercomMessage(
    IntercomMessagePrepareStatus Status,
    IntercomAttachmentMessageBuilder.Outbound Outbound,
    IReadOnlyList<string> Warnings,
    string? Error)
{
    public bool IsCommittable =>
        Status is IntercomMessagePrepareStatus.Success or IntercomMessagePrepareStatus.PartialSuccess;
}

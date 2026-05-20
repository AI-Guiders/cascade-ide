#nullable enable

using CascadeIDE.Features.Chat.DataAcquisition;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat;

/// <summary>Единая точка prepare для composer, MCP и slash (ADR 0134).</summary>
public static class IntercomOutboundMessagePreparer
{
    public static Task<PreparedIntercomMessage> PrepareAsync(
        string rawInput,
        IReadOnlyDictionary<string, AttachmentAnchor> pendingByShortId,
        IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        CancellationToken cancellationToken = default) =>
        IntercomAttachmentResolveAtSendWorker.TryPrepareAsync(
            rawInput,
            pendingByShortId,
            editor,
            workspaceRoot,
            solutionPath,
            cancellationToken);

    /// <summary><c>send_chat</c> / MCP: без Roslyn по member @ send.</summary>
    public static Task<PreparedIntercomMessage> PrepareForMcpAsync(
        string rawInput,
        IReadOnlyDictionary<string, AttachmentAnchor> pendingByShortId,
        IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        CancellationToken cancellationToken = default) =>
        IntercomAttachmentResolveAtSendWorker.TryPrepareForMcpAsync(
            rawInput,
            pendingByShortId,
            editor,
            workspaceRoot,
            solutionPath,
            cancellationToken);
}

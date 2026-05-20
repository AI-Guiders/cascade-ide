#nullable enable

using CascadeIDE.Contracts;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat.DataAcquisition;

/// <summary>DAL: сборка outbound @ send с Roslyn resolve вне UI-потока (ADR 0102 / 0128).</summary>
[IoBoundary]
public static class IntercomAttachmentResolveAtSendWorker
{
    public static Task<(bool Ok, IntercomAttachmentMessageBuilder.Outbound Outbound, string Error)> TryBuildAsync(
        string rawInput,
        IReadOnlyDictionary<string, AttachmentAnchor> pendingByShortId,
        IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        CancellationToken cancellationToken = default) =>
        Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var ok = IntercomAttachmentMessageBuilder.TryBuild(
                    rawInput,
                    pendingByShortId,
                    editor,
                    workspaceRoot,
                    solutionPath,
                    out var outbound,
                    out var error);
                return (ok, outbound, error);
            },
            cancellationToken);
}

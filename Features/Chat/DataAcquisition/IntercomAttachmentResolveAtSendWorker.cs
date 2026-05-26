#nullable enable

using CascadeIDE.Contracts;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat.DataAcquisition;

/// <summary>DAL: prepare outbound @ send с Roslyn resolve вне UI-потока (ADR 0102 / 0128 / 0134).</summary>
[IoBoundary]
public static class IntercomAttachmentResolveAtSendWorker
{
    public static Task<(bool Ok, IntercomAttachmentMessageBuilder.Outbound Outbound, string Error)> TryBuildAsync(
        string rawInput,
        IReadOnlyDictionary<string, AttachmentAnchor> pendingByShortId,
        IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        CancellationToken cancellationToken = default,
        string? indexDirectoryRelative = null) =>
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
                    out var error,
                    indexDirectoryRelative);
                return (ok, outbound, error);
            },
            cancellationToken);

    public static Task<PreparedIntercomMessage> TryPrepareAsync(
        string rawInput,
        IReadOnlyDictionary<string, AttachmentAnchor> pendingByShortId,
        IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        CancellationToken cancellationToken = default,
        string? indexDirectoryRelative = null) =>
        tryPrepareOnPool(
            rawInput,
            pendingByShortId,
            editor,
            workspaceRoot,
            solutionPath,
            indexDirectoryRelative,
            forMcp: false,
            cancellationToken);

    public static Task<PreparedIntercomMessage> TryPrepareForMcpAsync(
        string rawInput,
        IReadOnlyDictionary<string, AttachmentAnchor> pendingByShortId,
        IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        CancellationToken cancellationToken = default) =>
        tryPrepareOnPool(
            rawInput,
            pendingByShortId,
            editor,
            workspaceRoot,
            solutionPath,
            indexDirectoryRelative: null,
            forMcp: true,
            cancellationToken);

    private static Task<PreparedIntercomMessage> tryPrepareOnPool(
        string rawInput,
        IReadOnlyDictionary<string, AttachmentAnchor> pendingByShortId,
        IntercomAttachmentResolveAtSend.EditorSnapshot editor,
        string? workspaceRoot,
        string? solutionPath,
        string? indexDirectoryRelative,
        bool forMcp,
        CancellationToken cancellationToken) =>
        Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (forMcp)
                {
                    _ = IntercomAttachmentMessageBuilder.TryPrepareForMcp(
                        rawInput,
                        pendingByShortId,
                        editor,
                        workspaceRoot,
                        solutionPath,
                        out var preparedMcp);
                    return preparedMcp;
                }

                _ = IntercomAttachmentMessageBuilder.TryPrepare(
                    rawInput,
                    pendingByShortId,
                    editor,
                    workspaceRoot,
                    solutionPath,
                    out var prepared);
                return prepared;
            },
            cancellationToken);
}

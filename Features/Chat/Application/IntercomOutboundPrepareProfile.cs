using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Chat.Application;

/// <summary>Политика prepare @ send: composer full vs MCP fast-path (ADR 0134, 0135).</summary>
[ComputingUnit]
public readonly record struct IntercomOutboundPrepareProfile(
    bool AllowDegradedMemberResolve,
    bool SkipMemberRoslynAtSend,
    bool CaptureSenderWorkspaceContext,
    bool AddMcpFastPathWarning)
{
    public static IntercomOutboundPrepareProfile ComposerStrictBuild { get; } =
        new(AllowDegradedMemberResolve: false, SkipMemberRoslynAtSend: false, CaptureSenderWorkspaceContext: true, AddMcpFastPathWarning: false);

    public static IntercomOutboundPrepareProfile ComposerPrepare { get; } =
        new(AllowDegradedMemberResolve: true, SkipMemberRoslynAtSend: false, CaptureSenderWorkspaceContext: true, AddMcpFastPathWarning: false);

    public static IntercomOutboundPrepareProfile McpFastPrepare { get; } =
        new(AllowDegradedMemberResolve: true, SkipMemberRoslynAtSend: true, CaptureSenderWorkspaceContext: false, AddMcpFastPathWarning: true);
}

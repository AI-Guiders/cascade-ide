#nullable enable

namespace CascadeIDE.Cockpit.Channels.WorkspaceHealth;

/// <summary>
/// Context for Workspace Health channel build.
/// </summary>
public readonly record struct IdeHealthChannelContext
{
    public static IdeHealthChannelContext Default => default;
}

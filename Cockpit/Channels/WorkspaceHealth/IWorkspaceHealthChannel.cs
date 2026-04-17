#nullable enable
using CascadeIDE.Cockpit.Channels;

namespace CascadeIDE.Cockpit.Channels.WorkspaceHealth;

/// <summary>
/// Generic channel contract for Workspace Health snapshots.
/// </summary>
public interface IWorkspaceHealthChannel : IChannel<WorkspaceHealthChannelContext, WorkspaceHealthInputSnapshot>
{
}

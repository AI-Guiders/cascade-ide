#nullable enable
using System.Collections.ObjectModel;
using CascadeIDE.Cockpit.Channels.WorkspaceHealth;
using CascadeIDE.Cockpit.Composition;

namespace CascadeIDE.Cockpit.Composition.WorkspaceHealth;

/// <summary>
/// Surface compositor contract for Workspace Health segments.
/// </summary>
public interface IWorkspaceHealthSurfaceCompositor : ISurfaceCompositor<ObservableCollection<WorkspaceHealthSegment>, WorkspaceHealthInputSnapshot, WorkspaceHealthSurfaceDecision, ObservableCollection<WorkspaceHealthSegment>>
{
}

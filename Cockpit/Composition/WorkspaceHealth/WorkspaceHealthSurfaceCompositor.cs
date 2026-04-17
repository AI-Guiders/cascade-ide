#nullable enable
using System.Collections.ObjectModel;
using CascadeIDE.Cockpit.Channels.WorkspaceHealth;

namespace CascadeIDE.Cockpit.Composition.WorkspaceHealth;

/// <summary>
/// Composes Workspace Health channel snapshot into ordered surface segments.
/// </summary>
public sealed class WorkspaceHealthSurfaceCompositor : IWorkspaceHealthSurfaceCompositor
{
    public ObservableCollection<WorkspaceHealthSegment> Compose(
        ObservableCollection<WorkspaceHealthSegment> scene,
        WorkspaceHealthInputSnapshot payload,
        in WorkspaceHealthSurfaceDecision decision)
    {
        if (!decision.Enabled)
            return scene;

        scene.Clear();
        Append(scene, WorkspaceHealthSource.Build, payload.Build);
        Append(scene, WorkspaceHealthSource.Tests, payload.Tests);
        Append(scene, WorkspaceHealthSource.Debug, payload.Debug);
        Append(scene, WorkspaceHealthSource.Git, payload.Git);
        return scene;
    }

    private static void Append(
        ObservableCollection<WorkspaceHealthSegment> target,
        WorkspaceHealthSource source,
        WorkspaceHealthSegmentInput input)
    {
        target.Add(new WorkspaceHealthSegment
        {
            Source = source,
            LineText = input.LineText,
            CockpitShort = input.CockpitShort,
            IsBuildRunning = source == WorkspaceHealthSource.Build && input.IsBuildRunning
        });
    }
}

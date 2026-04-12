using System.Collections.ObjectModel;
using CascadeIDE.Cockpit.Channels.WorkspaceHealth;

namespace CascadeIDE.Cockpit.Composition.WorkspaceHealth;

/// <summary>
/// Композитор поверхности для канала Workspace Health (ADR 0036 п.3): из <see cref="WorkspaceHealthInputSnapshot"/>
/// строит упорядоченный список <see cref="WorkspaceHealthSegment"/> (порядок Build → Tests → Debug → Git, флаги вроде
/// <see cref="WorkspaceHealthSegment.IsBuildRunning"/> на сегменте Build).
/// Не задаёт зоны PFD/MFD (это CDS / пресет); не рисует контролы (поверхность — <c>WorkspaceHealthStripView</c> и др.).
/// </summary>
public static class WorkspaceHealthSegmentBuilder
{
    /// <summary>Порядок сегментов на полосе: сборка → тесты → отладка → git.</summary>
    public static void Rebuild(ObservableCollection<WorkspaceHealthSegment> target, WorkspaceHealthInputSnapshot inputs)
    {
        target.Clear();
        Append(target, WorkspaceHealthSource.Build, inputs.Build);
        Append(target, WorkspaceHealthSource.Tests, inputs.Tests);
        Append(target, WorkspaceHealthSource.Debug, inputs.Debug);
        Append(target, WorkspaceHealthSource.Git, inputs.Git);
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
            IsBuildRunning = source == WorkspaceHealthSource.Build && input.IsBuildRunning,
        });
    }
}

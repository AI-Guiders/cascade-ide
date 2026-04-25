#nullable enable
using System.Collections.ObjectModel;
using CascadeIDE.Cockpit.Channels.WorkspaceHealth;
using CascadeIDE.Cockpit.ComputingUnits;
using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

namespace CascadeIDE.Cockpit.Composition.WorkspaceHealth;

/// <summary>
/// CCU «композиция поверхности канала» (<see cref="ICockpitComputeUnit"/>, ADR 0097): упорядочивает сегменты IDE Health из <see cref="IdeHealthInputSnapshot"/> (без Avalonia).
/// Порядок — <see cref="IdeHealthInstrumentDeck"/> (ADR 0063). Канал продуктово — IDE Health (ADR 0089), не «Workspace Health» в смысле UI-лейбла.
/// </summary>
public sealed class IdeHealthSurfaceCompositor : IIdeHealthSurfaceCompositor
{
    public ObservableCollection<IdeHealthSegment> Compose(
        ObservableCollection<IdeHealthSegment> scene,
        IdeHealthInputSnapshot payload,
        in IdeHealthSurfaceDecision decision)
    {
        if (!decision.Enabled)
            return scene;

        scene.Clear();
        Append(scene, IdeHealthSource.Build, payload.Build);
        Append(scene, IdeHealthSource.Tests, payload.Tests);
        Append(scene, IdeHealthSource.Debug, payload.Debug);
        Append(scene, IdeHealthSource.Git, payload.Git);
        return scene;
    }

    private static void Append(
        ObservableCollection<IdeHealthSegment> target,
        IdeHealthSource source,
        IdeHealthSegmentInput input)
    {
        target.Add(new IdeHealthSegment
        {
            Source = source,
            Stratum = input.Stratum,
            Scope = input.Scope,
            ProjectPath = input.ProjectPath,
            LineText = input.LineText,
            CockpitShort = input.CockpitShort,
            IsBuildRunning = source == IdeHealthSource.Build && input.IsBuildRunning
        });
    }
}

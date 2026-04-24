#nullable enable
using System.Collections.ObjectModel;
using CascadeIDE.Cockpit.Channels.WorkspaceHealth;

namespace CascadeIDE.Cockpit.Composition.WorkspaceHealth;

/// <summary>
/// Composes Workspace Health channel snapshot into ordered surface segments.
/// Порядок сегментов согласован с <see cref="IdeHealthInstrumentDeck"/> (ADR 0063, ось композиции канала WH).
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
            LineText = input.LineText,
            CockpitShort = input.CockpitShort,
            IsBuildRunning = source == IdeHealthSource.Build && input.IsBuildRunning
        });
    }
}

#nullable enable
using CascadeIDE.Models;
using CascadeIDE.Cockpit.ComputingUnits.EnvironmentReadiness;

namespace CascadeIDE.Cockpit.Channels.EnvironmentReadiness;

/// <summary>
/// Builds Environment Readiness snapshot payload from app state.
/// </summary>
public sealed class EnvironmentReadinessChannel : IEnvironmentReadinessChannel
{
    private readonly EnvironmentReadinessSnapshotUnit _snapshotUnit = EnvironmentReadinessSnapshotUnit.Default;

    public ValueTask<IReadOnlyList<AnnunciatorLampItem>> Build(in EnvironmentReadinessChannelContext context) =>
        new(_snapshotUnit.BuildAsync(context));
}

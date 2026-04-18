#nullable enable
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Cockpit.Channels.EnvironmentReadiness;

/// <summary>
/// Builds Environment Readiness snapshot payload from app state.
/// </summary>
public sealed class EnvironmentReadinessChannel : IEnvironmentReadinessChannel
{
    public ValueTask<IReadOnlyList<AnnunciatorLampItem>> Build(in EnvironmentReadinessChannelContext context) =>
        new(EnvironmentReadinessSnapshotBuilder.BuildAllRowsAsync(
            context.Settings,
            context.SolutionPath,
            context.CSharpHost,
            context.MarkdownHost,
            context.CancellationToken));
}

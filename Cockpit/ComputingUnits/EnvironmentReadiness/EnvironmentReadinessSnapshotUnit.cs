#nullable enable

using CascadeIDE.Cockpit.Channels.EnvironmentReadiness;
using CascadeIDE.Contracts;
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.ComputingUnits.EnvironmentReadiness;

/// <summary>
/// CCU «Environment Readiness snapshot»: единая точка сборки строк готовности окружения (ADR 0023/0097).
/// Оркеструет построение снимка по входному контексту канала без привязки к ViewModel.
/// </summary>
[ComputingUnit]
public sealed class EnvironmentReadinessSnapshotUnit : ICockpitComputeUnit
{
    public static EnvironmentReadinessSnapshotUnit Default { get; } = new();

    private EnvironmentReadinessSnapshotUnit()
    {
    }

    public Task<IReadOnlyList<AnnunciatorLampItem>> BuildAsync(in EnvironmentReadinessChannelContext context) =>
        EnvironmentReadinessSnapshotBuilder.BuildAllRowsAsync(
            context.Settings,
            context.SolutionPath,
            context.Lsp,
            context.IsMcpStdioHost,
            context.ActiveAiProvider,
            context.CancellationToken);
}

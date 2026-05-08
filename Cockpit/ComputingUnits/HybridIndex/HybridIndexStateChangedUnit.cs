using CascadeIDE.Cockpit.DataBus;
using HybridCodebaseIndex.Core;

namespace CascadeIDE.Cockpit.ComputingUnits.HybridIndex;

/// <summary>
/// CCU: свёртка <see cref="IndexStatus"/> ядра HCI в типизированное событие <see cref="HybridIndexStateChanged"/> для IDE DataBus
/// (ADR 0097, 0106). Единая точка маппинга для watcher, reindex/MCP и будущих потребителей (Semantic Map).
/// </summary>
public sealed class HybridIndexStateChangedUnit : ICockpitComputeUnit
{
    /// <summary>
    /// Строит событие шины из снимка ядра; <paramref name="workspaceRootNormalizedOrFallback"/> подставляется, если ядро не нормализовало корень.
    /// </summary>
    public static HybridIndexStateChanged FromCoreStatus(
        IndexStatus status,
        string workspaceRootNormalizedOrFallback,
        string? solutionPathTrimmedOrNull) =>
        new(
            WorkspaceRoot: status.WorkspaceRootNormalized ?? workspaceRootNormalizedOrFallback,
            SolutionPath: solutionPathTrimmedOrNull,
            DatabasePath: status.DatabasePath,
            DocumentCount: status.DocumentCount,
            IndexedAtIso: status.IndexedAtIso,
            LastError: status.LastReindexError,
            LastErrorAtIso: status.LastReindexErrorAtIso);
}

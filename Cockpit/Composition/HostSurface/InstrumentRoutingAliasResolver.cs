using System.Diagnostics.CodeAnalysis;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Публичные alias инструмента в TOML → канонические <c>instrument_id</c> для <see cref="CockpitStandardInstrumentIds"/>.
/// </summary>
public static class InstrumentRoutingAliasResolver
{
    /// <summary>
    /// Принимает alias или уже канонический <c>instrument_id</c> (для обратной совместимости копипаста из CDS).
    /// </summary>
    public static bool TryResolve(string? token, [NotNullWhen(true)] out string? canonicalInstrumentId)
    {
        canonicalInstrumentId = null;
        var t = token?.Trim() ?? "";
        if (t.Length == 0)
            return false;

        if (IsKnownCanonical(t))
        {
            canonicalInstrumentId = NormalizeCanonical(t);
            return true;
        }

        if (t.Equals("solution_explorer", StringComparison.OrdinalIgnoreCase))
        {
            canonicalInstrumentId = CockpitStandardInstrumentIds.SolutionExplorerTree;
            return true;
        }

        if (t.Equals("workspace_map", StringComparison.OrdinalIgnoreCase))
        {
            canonicalInstrumentId = CockpitStandardInstrumentIds.WorkspaceNavigationMap;
            return true;
        }

        if (t.Equals("workspace_health", StringComparison.OrdinalIgnoreCase))
        {
            canonicalInstrumentId = CockpitStandardInstrumentIds.WorkspaceHealthStatusV1;
            return true;
        }

        return false;
    }

    private static bool IsKnownCanonical(string t) =>
        t.Equals(CockpitStandardInstrumentIds.SolutionExplorerTree, StringComparison.OrdinalIgnoreCase)
        || t.Equals(CockpitStandardInstrumentIds.WorkspaceNavigationMap, StringComparison.OrdinalIgnoreCase)
        || t.Equals(CockpitStandardInstrumentIds.WorkspaceHealthStatusV1, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeCanonical(string t)
    {
        if (t.Equals(CockpitStandardInstrumentIds.SolutionExplorerTree, StringComparison.OrdinalIgnoreCase))
            return CockpitStandardInstrumentIds.SolutionExplorerTree;
        if (t.Equals(CockpitStandardInstrumentIds.WorkspaceNavigationMap, StringComparison.OrdinalIgnoreCase))
            return CockpitStandardInstrumentIds.WorkspaceNavigationMap;
        return CockpitStandardInstrumentIds.WorkspaceHealthStatusV1;
    }
}

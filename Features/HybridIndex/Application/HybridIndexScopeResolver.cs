using CascadeIDE.Contracts;

namespace CascadeIDE.Features.HybridIndex.Application;

/// <summary>ADR 0106: пара workspace/solution под <c>[hybrid_index] scope_mode</c> без привязки к VM.</summary>
[ComputingUnit]
public static class HybridIndexScopeResolver
{
    /// <summary>Применить режим области (например <c>workspace</c> без solution в ключе SQLite vs <c>workspace+solution</c>).</summary>
    public static (string WorkspaceRoot, string? SolutionPath) ApplyScopeMode(
        string? scopeMode,
        string workspaceRoot,
        string? solutionPath)
    {
        var ws = (workspaceRoot ?? "").Trim();
        if (string.IsNullOrWhiteSpace(ws))
            return ("", null);

        var mode = (scopeMode ?? "").Trim();
        if (string.Equals(mode, "workspace", StringComparison.OrdinalIgnoreCase))
            return (ws, null);
        return (ws, solutionPath);
    }
}

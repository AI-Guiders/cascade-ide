namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Application-level orchestrator helpers for IDE MCP navigation actions.
/// Keeps mode/level normalization and fallback rules out of MainWindowViewModel.
/// </summary>
public static class IdeMcpNavigationOrchestrator
{
    public static bool TryNormalizeRequestedMode(string mode, out string requestedMode)
    {
        requestedMode = string.IsNullOrWhiteSpace(mode) ? "related" : mode.Trim().ToLowerInvariant();
        return requestedMode is "related" or "subgraph";
    }

    public static string BuildInvalidModeJson() =>
        """{"error":"invalid_mode","message":"mode must be related or subgraph."}""";

    public static (string effectiveLevel, string effectiveMode) ResolveEffectiveLevelAndMode(
        string? requestedLevel,
        string configuredLevel,
        string requestedMode)
    {
        var effectiveLevel = Models.CodeNavigationMapLevelKind.Normalize(
            string.IsNullOrWhiteSpace(requestedLevel) ? configuredLevel : requestedLevel);
        var effectiveMode = effectiveLevel == Models.CodeNavigationMapLevelKind.ControlFlow
            ? "subgraph"
            : requestedMode;
        return (effectiveLevel, effectiveMode);
    }

    public static (int? line, int? column) ResolveLineColumnForControlFlow(
        int? requestedLine,
        int? requestedColumn,
        int derivedLine,
        int derivedColumn)
    {
        var effectiveLine = requestedLine;
        var effectiveColumn = requestedColumn;

        if (effectiveLine is null || effectiveLine <= 0)
            effectiveLine = derivedLine;
        if (effectiveColumn is null || effectiveColumn <= 0)
            effectiveColumn = derivedColumn;

        return (effectiveLine, effectiveColumn);
    }
}

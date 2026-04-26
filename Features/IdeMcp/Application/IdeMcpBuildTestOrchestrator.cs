using System.Text.Json;
using System.Globalization;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Application-level orchestrator helpers for IDE MCP build/test actions.
/// Keeps payload/filter/log shaping out of MainWindowViewModel.
/// </summary>
public static class IdeMcpBuildTestOrchestrator
{
    public static (string? filterExpression, string mode, IReadOnlyList<string>? tokens) BuildAffectedTestsRequest(
        IReadOnlyList<string>? changedPaths)
    {
        var tokens = Services.McpDotnetBuildTestService.BuildAffectedTestTokens(changedPaths);
        if (tokens.Count == 0)
            return (null, "fallback_all", null);

        var filter = string.Join('|', tokens.Select(t => $"FullyQualifiedName~{t}"));
        return (filter, "affected", tokens);
    }

    public static string SerializeMissingSolutionError(string mode) =>
        JsonSerializer.Serialize(new { success = false, error = "No solution loaded or file not found.", mode });

    public static string BuildTestResultLogBlock(string summary, string consoleOutput)
    {
        var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        return $"=== {stamp} ===\n{summary}\n\n{consoleOutput}\n\n";
    }

    public static string BuildTestErrorLogBlock(string errorMessage)
    {
        var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        return $"=== {stamp} (ошибка) ===\n{errorMessage}\n\n";
    }

    public static string AppendLogWithLimit(string existing, string block, int maxChars)
    {
        var combined = existing + block;
        return combined.Length > maxChars ? combined[^maxChars..] : combined;
    }
}

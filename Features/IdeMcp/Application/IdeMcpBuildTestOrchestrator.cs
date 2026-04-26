using System.Text.Json;
using System.Globalization;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Application-level orchestrator helpers for IDE MCP build/test actions.
/// Keeps payload/filter/log shaping out of MainWindowViewModel.
/// </summary>
public static class IdeMcpBuildTestOrchestrator
{
    public static string MissingSolutionMessage() =>
        "No solution loaded or file not found.";

    public static string BuildOperationHeader(string operation, string path) =>
        $"{operation}: {path}\r\n";

    public static string BuildErrorMessage(string exceptionMessage) =>
        "Error: " + exceptionMessage;

    public static string BuildPanelLine(string text) =>
        text + "\r\n";

    public static string BuildTestSummary(int passed, int total, int failed) =>
        $"{passed}/{total} passed, {failed} failed";

    public static string SerializeSolutionFilesPayload<TEntry, TNode>(IReadOnlyList<TEntry> fileEntries, IReadOnlyList<TNode> solutionTree) =>
        JsonSerializer.Serialize(new { file_entries = fileEntries, solution_tree = solutionTree });

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

    public static string SerializeCodeCleanupFailure(string error) =>
        JsonSerializer.Serialize(new { success = false, error });

    public static string BuildTruncatedRawOutput(string output, int maxChars) =>
        output.Length > maxChars ? output[..maxChars] + "\n... (output truncated)" : output;

    public static string SerializeCodeCleanupResult(bool success, int exitCode, string rawOutput) =>
        JsonSerializer.Serialize(new
        {
            success,
            exit_code = exitCode,
            raw_output = rawOutput
        });

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

    public static string BuildUpdatedTestResultsOutput(string existingOutput, string summary, string consoleOutput) =>
        AppendLogWithLimit(existingOutput, BuildTestResultLogBlock(summary, consoleOutput), 120_000);

    public static string BuildUpdatedTestErrorOutput(string existingOutput, string errorMessage) =>
        AppendLogWithLimit(existingOutput, BuildTestErrorLogBlock(errorMessage), 120_000);

    public static bool ShouldOpenTestsPage(bool instrumentationTabsEnabled) =>
        instrumentationTabsEnabled;

    public static (string summary, int impactedTestsBadge, string updatedOutput, bool shouldOpenTestsPage)
        BuildTestUiOutcome(
            int passed,
            int total,
            int failed,
            string existingOutput,
            string consoleOutput,
            bool instrumentationTabsEnabled)
    {
        var summary = BuildTestSummary(passed, total, failed);
        var updatedOutput = BuildUpdatedTestResultsOutput(existingOutput, summary, consoleOutput);
        return (summary, failed, updatedOutput, ShouldOpenTestsPage(instrumentationTabsEnabled));
    }

    public static (string summary, int impactedTestsBadge, string updatedOutput)
        BuildTestErrorUiOutcome(string existingOutput, string errorMessage) =>
        ("", 0, BuildUpdatedTestErrorOutput(existingOutput, errorMessage));
}

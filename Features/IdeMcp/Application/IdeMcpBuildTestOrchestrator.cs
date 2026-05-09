using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using CascadeIDE.Models;
using CascadeIDE.Features.Workspace.Application;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Application-level orchestrator helpers for IDE MCP build/test actions.
/// Keeps payload/filter/log shaping out of MainWindowViewModel.
/// </summary>
public static class IdeMcpBuildTestOrchestrator
{
    public static string MissingSolutionMessage() =>
        "No solution loaded or file not found.";

    /// <summary>Текст MCP и содержимое панели при отсутствии решения (MCP-сборка с показом панели).</summary>
    public readonly record struct IdeMcpBuildMissingSolutionPanel(string McpReplyText, string BuildOutputPanelFullText);

    public static IdeMcpBuildMissingSolutionPanel BuildMissingSolutionPanelSurface()
    {
        var msg = MissingSolutionMessage();
        return new IdeMcpBuildMissingSolutionPanel(msg, BuildPanelLine(msg));
    }

    /// <summary>Текст MCP и содержимое панели при исключении в цепочке MCP-сборки.</summary>
    public readonly record struct IdeMcpBuildFailurePanel(string McpReplyText, string BuildOutputPanelFullText);

    public static IdeMcpBuildFailurePanel FailedBuildPanelSurface(string exceptionMessage)
    {
        var msg = BuildErrorMessage(exceptionMessage);
        return new IdeMcpBuildFailurePanel(msg, BuildPanelLine(msg));
    }

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

    /// <summary>Плоские записи файлов и дерево решения для MCP <c>get_solution_files</c>; обход через <see cref="McpSolutionTree"/>.</summary>
    public static string BuildSolutionFilesJson(string? solutionPath, ObservableCollection<SolutionItem> solutionRoots)
    {
        var entries = McpSolutionTree.CollectFileEntries(solutionRoots).Select(e => new
        {
            path = e.FullPath,
            title = e.Title,
            relative_path = McpSolutionTree.GetRelativePath(solutionPath, e.FullPath)
        }).ToList();
        var tree = solutionRoots.Select(r => McpSolutionTree.BuildSolutionTreeNode(r, solutionPath)).ToList();
        return SerializeSolutionFilesPayload(entries, tree);
    }

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

    /// <summary>Дифф для UI «Инструментация · тесты» и DataBus после MCP-тестового прогона (без ссылок на VM).</summary>
    public readonly record struct IdeMcpTestRunInstrumentationMutation(
        string Summary,
        int ImpactedTestsBadge,
        string UpdatedTestResultsOutput,
        bool ShouldOpenTestsPage)
    {
        public static IdeMcpTestRunInstrumentationMutation FromSuccessfulParse(
            int passed,
            int total,
            int failed,
            string existingInstrumentationOutput,
            string consoleOutput,
            bool instrumentationTabsEnabled)
        {
            var u = BuildTestUiOutcome(
                passed,
                total,
                failed,
                existingInstrumentationOutput,
                consoleOutput,
                instrumentationTabsEnabled);
            return new IdeMcpTestRunInstrumentationMutation(
                u.summary,
                u.impactedTestsBadge,
                u.updatedOutput,
                u.shouldOpenTestsPage);
        }

        /// <remarks>Не обновляет «последний summary» успешного прогона — только журнал и сброс бейджа на шину (совместимо с прежней обработкой в VM).</remarks>
        public static IdeMcpTestRunInstrumentationMutation FromThrownException(
            string existingInstrumentationOutput,
            string exceptionMessage)
        {
            var u = BuildTestErrorUiOutcome(existingInstrumentationOutput, exceptionMessage);
            return new IdeMcpTestRunInstrumentationMutation(
                u.summary,
                u.impactedTestsBadge,
                u.updatedOutput,
                ShouldOpenTestsPage: false);
        }
    }
}

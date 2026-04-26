using System.Text.Json;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: сборка, тесты.</summary>
public partial class MainWindowViewModel
{
    /// <summary>Диагностики открытого .cs файла (ошибки и предупреждения Roslyn). JSON: массив { id, message, severity, line, column }. Для не-C# или при отсутствии файла — [].</summary>
    async Task<string> Services.IIdeMcpActions.GetCurrentFileDiagnosticsAsync()
    {
        var (path, text) = await UiScheduler.Default.InvokeAsync(() => (CurrentFilePath ?? "", EditorText ?? ""));
        return await Task.Run(() => _contextMinimizer.GetDiagnosticsJson(path, text)).ConfigureAwait(false);
    }

    /// <summary>Список файлов и дерево решения. file_entries — плоский список с path, title, relative_path. solution_tree — иерархия (solution → projects → folders → files). Выполняется в UI-потоке.</summary>
    Task<string> Services.IIdeMcpActions.GetSolutionFilesAsync() =>
        UiScheduler.Default.InvokeAsync(() =>
        {
            var solutionPath = Workspace.SolutionPath;
            var entries = McpSolutionTree.CollectFileEntries(Workspace.SolutionRoots).Select(e => new
            {
                path = e.FullPath,
                title = e.Title,
                relative_path = McpSolutionTree.GetRelativePath(solutionPath, e.FullPath)
            }).ToList();
            var tree = Workspace.SolutionRoots.Select(r => McpSolutionTree.BuildSolutionTreeNode(r, solutionPath)).ToList();
            return JsonSerializer.Serialize(new { file_entries = entries, solution_tree = tree });
        });

    async Task<string> Services.IIdeMcpActions.BuildAsync()
    {
        var path = await UiScheduler.Default.InvokeAsync(() => Workspace.SolutionPath ?? "");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            var msg = "No solution loaded or file not found.";
            UiScheduler.Default.Post(() => { BuildOutputPanel.Set(msg + "\r\n"); IsBuildOutputVisible = true; });
            return msg;
        }
        int? lastExitCode = null;
        bool? lastSucceeded = null;
        try
        {
            var pathCopy = path;
            await UiScheduler.Default.InvokeAsync(() =>
            {
                BuildOutputPanel.Set($"Сборка: {pathCopy}\r\n");
                IsBuildOutputVisible = true;
            }).ConfigureAwait(false);
            await UiScheduler.Default.InvokeAsync(() => PublishToIdeDataBusAndRebuild(new BuildStateChanged(true)))
                .ConfigureAwait(false);

            void AppendBuildChunk(string chunk) => BuildOutputPanel.Append(chunk);
            var (outStr, success, exitCode, binlogPath) = await _mcpBuildTest
                .BuildWithBinlogAsync(path, AppendBuildChunk, cancellationToken: default)
                .ConfigureAwait(false);
            lastExitCode = exitCode;
            lastSucceeded = success;

            await UiScheduler.Default.InvokeAsync(() =>
            {
                BuildOutputPanel.FlushPending();
                _lastBuildBinlogPath = binlogPath;
            }).ConfigureAwait(false);
            return outStr;
        }
        catch (Exception ex)
        {
            var msg = "Error: " + ex.Message;
            UiScheduler.Default.Post(() => { BuildOutputPanel.Set(msg + "\r\n"); IsBuildOutputVisible = true; });
            lastSucceeded = false;
            return msg;
        }
        finally
        {
            var exit = lastExitCode;
            var ok = lastSucceeded;
            await UiScheduler.Default
                .InvokeAsync(() => PublishToIdeDataBusAndRebuild(new BuildStateChanged(false, exit, ok)))
                .ConfigureAwait(false);
        }
    }

    async Task<string> Services.IIdeMcpActions.BuildStructuredAsync()
    {
        var raw = await ((Services.IIdeMcpActions)this).BuildAsync().ConfigureAwait(false);
        return Services.McpDotnetBuildTestService.SerializeStructuredBuild(raw, _lastBuildBinlogPath);
    }

    async Task<string> Services.IIdeMcpActions.RunTestsAsync()
    {
        return await RunTestsInternalAsync(filterExpression: null, mode: "all").ConfigureAwait(false);
    }

    async Task<string> Services.IIdeMcpActions.RunAffectedTestsAsync(IReadOnlyList<string>? changedPaths)
    {
        var tokens = Services.McpDotnetBuildTestService.BuildAffectedTestTokens(changedPaths);
        if (tokens.Count == 0)
            return await RunTestsInternalAsync(filterExpression: null, mode: "fallback_all").ConfigureAwait(false);

        var filter = string.Join('|', tokens.Select(t => $"FullyQualifiedName~{t}"));
        return await RunTestsInternalAsync(filter, mode: "affected", tokens).ConfigureAwait(false);
    }

    private async Task<string> RunTestsInternalAsync(string? filterExpression, string mode, IReadOnlyList<string>? tokens = null)
    {
        var path = await UiScheduler.Default.InvokeAsync(() => Workspace.SolutionPath ?? "");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return JsonSerializer.Serialize(new { success = false, error = "No solution loaded or file not found.", mode });

        try
        {
            var outcome = await _mcpBuildTest.RunTestsAsync(path, filterExpression, mode, tokens).ConfigureAwait(false);
            var parsed = outcome.Parsed;
            var outStr = outcome.ConsoleOutput;

            UiScheduler.Default.Post(() =>
            {
                LastTestSummary = $"{parsed.Passed}/{parsed.Total} passed, {parsed.Failed} failed";
                ImpactedTestsBadge = parsed.Failed;
                PublishToIdeDataBusAndRebuild(new TestsStateChanged(LastTestSummary, ImpactedTestsBadge));
                const int maxLogChars = 120_000;
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var block = $"=== {stamp} ===\n{LastTestSummary}\n\n{outStr}\n\n";
                var combined = InstrumentationPanel.TestResultsOutput + block;
                if (combined.Length > maxLogChars)
                    combined = combined[^maxLogChars..];
                InstrumentationPanel.TestResultsOutput = combined;
                if (InstrumentationTabs)
                    CurrentMfdShellPage = MfdShellPage.Tests;
            });
            return outcome.JsonPayload;
        }
        catch (Exception ex)
        {
            UiScheduler.Default.Post(() =>
            {
                PublishToIdeDataBusAndRebuild(new TestsStateChanged("", 0));
                const int maxLogChars = 120_000;
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var block = $"=== {stamp} (ошибка) ===\n{ex.Message}\n\n";
                var combined = InstrumentationPanel.TestResultsOutput + block;
                if (combined.Length > maxLogChars)
                    combined = combined[^maxLogChars..];
                InstrumentationPanel.TestResultsOutput = combined;
            });
            return Services.McpDotnetBuildTestService.SerializeTestRunFailure(ex.Message, mode, filterExpression);
        }
    }

    async Task<string> Services.IIdeMcpActions.RunCodeCleanupAsync(string? includePath)
    {
        var path = await UiScheduler.Default.InvokeAsync(() => Workspace.SolutionPath ?? "");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return JsonSerializer.Serialize(new { success = false, error = "No solution loaded or file not found." });

        try
        {
            var pathCopy = path;
            await UiScheduler.Default.InvokeAsync(() =>
            {
                BuildOutputPanel.Set($"Code cleanup: {pathCopy}\r\n");
                IsBuildOutputVisible = true;
            }).ConfigureAwait(false);

            void AppendBuildChunk(string chunk) => BuildOutputPanel.Append(chunk);
            var (success, exitCode, outStr) = await _mcpBuildTest
                .RunCodeCleanupAsync(path, includePath, AppendBuildChunk, cancellationToken: default)
                .ConfigureAwait(false);
            const int maxRawChars = 4000;
            var rawTruncated = outStr.Length > maxRawChars ? outStr[..maxRawChars] + "\n... (output truncated)" : outStr;

            await UiScheduler.Default.InvokeAsync(() => BuildOutputPanel.FlushPending()).ConfigureAwait(false);

            return JsonSerializer.Serialize(new
            {
                success,
                exit_code = exitCode,
                raw_output = rawTruncated
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }
}

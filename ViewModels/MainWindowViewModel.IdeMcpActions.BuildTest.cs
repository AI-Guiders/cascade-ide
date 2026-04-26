using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;

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
            return IdeMcpBuildTestOrchestrator.SerializeSolutionFilesPayload(entries, tree);
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
        var request = IdeMcpBuildTestOrchestrator.BuildAffectedTestsRequest(changedPaths);
        return await RunTestsInternalAsync(request.filterExpression, request.mode, request.tokens).ConfigureAwait(false);
    }

    private async Task<string> RunTestsInternalAsync(string? filterExpression, string mode, IReadOnlyList<string>? tokens = null)
    {
        var path = await UiScheduler.Default.InvokeAsync(() => Workspace.SolutionPath ?? "");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return IdeMcpBuildTestOrchestrator.SerializeMissingSolutionError(mode);

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
                var block = IdeMcpBuildTestOrchestrator.BuildTestResultLogBlock(LastTestSummary, outStr);
                InstrumentationPanel.TestResultsOutput = IdeMcpBuildTestOrchestrator.AppendLogWithLimit(
                    InstrumentationPanel.TestResultsOutput,
                    block,
                    maxLogChars);
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
                var block = IdeMcpBuildTestOrchestrator.BuildTestErrorLogBlock(ex.Message);
                InstrumentationPanel.TestResultsOutput = IdeMcpBuildTestOrchestrator.AppendLogWithLimit(
                    InstrumentationPanel.TestResultsOutput,
                    block,
                    maxLogChars);
            });
            return Services.McpDotnetBuildTestService.SerializeTestRunFailure(ex.Message, mode, filterExpression);
        }
    }

    async Task<string> Services.IIdeMcpActions.RunCodeCleanupAsync(string? includePath)
    {
        var path = await UiScheduler.Default.InvokeAsync(() => Workspace.SolutionPath ?? "");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return IdeMcpBuildTestOrchestrator.SerializeCodeCleanupFailure("No solution loaded or file not found.");

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
            var rawTruncated = IdeMcpBuildTestOrchestrator.BuildTruncatedRawOutput(outStr, 4000);

            await UiScheduler.Default.InvokeAsync(() => BuildOutputPanel.FlushPending()).ConfigureAwait(false);

            return IdeMcpBuildTestOrchestrator.SerializeCodeCleanupResult(success, exitCode, rawTruncated);
        }
        catch (Exception ex)
        {
            return IdeMcpBuildTestOrchestrator.SerializeCodeCleanupFailure(ex.Message);
        }
    }
}

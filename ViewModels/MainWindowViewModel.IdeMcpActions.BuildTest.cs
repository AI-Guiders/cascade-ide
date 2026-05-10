using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: сборка, тесты.</summary>
public partial class MainWindowViewModel
{
    /// <summary>Код выхода и успех для финального <see cref="BuildStateChanged"/> после MCP-операции на панели сборки.</summary>
    private sealed class IdeMcpMutableBuildPhaseOutcome
    {
        public int? ExitCode;
        public bool? Succeeded;
    }

    /// <summary>
    /// Пара событий «сборка идёт» / «сборка завершилась» на IDE DataBus (ADR 0099); тело задаёт <see cref="IdeMcpMutableBuildPhaseOutcome"/> до return.
    /// </summary>
    private async Task<T> WithIdeMcpPublishedBuildStateAsync<T>(Func<IdeMcpMutableBuildPhaseOutcome, Task<T>> body)
    {
        await PublishIdeBuildStateOnUiAsync(new BuildStateChanged(true)).ConfigureAwait(false);
        var outcome = new IdeMcpMutableBuildPhaseOutcome();
        try
        {
            return await body(outcome).ConfigureAwait(false);
        }
        finally
        {
            await PublishIdeBuildStateOnUiAsync(new BuildStateChanged(false, outcome.ExitCode, outcome.Succeeded))
                .ConfigureAwait(false);
        }
    }

    /// <summary>Диагностики открытого .cs файла (ошибки и предупреждения Roslyn). JSON: массив { id, message, severity, line, column }. Для не-C# или при отсутствии файла — [].</summary>
    async Task<string> Services.IIdeMcpActions.GetCurrentFileDiagnosticsAsync()
    {
        var (path, text) = await UiScheduler.Default.InvokeAsync(() => (CurrentFilePath ?? "", EditorText ?? ""));
        return await Task.Run(() => _contextMinimizer.GetDiagnosticsJson(path, text)).ConfigureAwait(false);
    }

    /// <summary>Список файлов и дерево решения. file_entries — плоский список с path, title, relative_path. solution_tree — иерархия (solution → projects → folders → files). Выполняется в UI-потоке.</summary>
    Task<string> Services.IIdeMcpActions.GetSolutionFilesAsync() =>
        UiScheduler.Default.InvokeAsync(() =>
            IdeMcpBuildTestOrchestrator.BuildSolutionFilesJson(Workspace.SolutionPath, Workspace.SolutionRoots));

    async Task<string> Services.IIdeMcpActions.BuildAsync()
    {
        var path = await UiScheduler.Default.InvokeAsync(() => Workspace.SolutionPath ?? "");
        if (!IdeMcpSolutionPathAvailability.IsRunnableSolutionFile(path))
        {
            var surf = IdeMcpBuildTestOrchestrator.BuildMissingSolutionPanelSurface();
            UiScheduler.Default.Post(() =>
            {
                BuildOutputPanel.Set(surf.BuildOutputPanelFullText);
                IsBuildOutputVisible = true;
            });
            return surf.McpReplyText;
        }
        var pathCopy = path;
        await UiScheduler.Default.InvokeAsync(() =>
        {
            BuildOutputPanel.Set(IdeMcpBuildTestOrchestrator.BuildOperationHeader("Сборка", pathCopy));
            IsBuildOutputVisible = true;
        }).ConfigureAwait(false);

        return await WithIdeMcpPublishedBuildStateAsync(async outcome =>
        {
            try
            {
                void AppendBuildChunk(string chunk) => BuildOutputPanel.Append(chunk);
                var (outStr, success, exitCode, binlogPath) = await _mcpBuildTest
                    .BuildWithBinlogAsync(path, AppendBuildChunk, cancellationToken: default)
                    .ConfigureAwait(false);
                outcome.ExitCode = exitCode;
                outcome.Succeeded = success;

                await UiScheduler.Default.InvokeAsync(() =>
                {
                    BuildOutputPanel.FlushPending();
                    _lastBuildBinlogPath = binlogPath;
                }).ConfigureAwait(false);
                return outStr;
            }
            catch (Exception ex)
            {
                outcome.Succeeded = false;
                var surf = IdeMcpBuildTestOrchestrator.FailedBuildPanelSurface(ex.Message);
                UiScheduler.Default.Post(() =>
                {
                    BuildOutputPanel.Set(surf.BuildOutputPanelFullText);
                    IsBuildOutputVisible = true;
                });
                return surf.McpReplyText;
            }
        }).ConfigureAwait(false);
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
        if (!IdeMcpSolutionPathAvailability.IsRunnableSolutionFile(path))
            return IdeMcpBuildTestOrchestrator.SerializeMissingSolutionError(mode);

        try
        {
            var outcome = await _mcpBuildTest.RunTestsAsync(path, filterExpression, mode, tokens).ConfigureAwait(false);
            var parsed = outcome.Parsed;
            var outStr = outcome.ConsoleOutput;

            UiScheduler.Default.Post(() =>
            {
                var mutation = IdeMcpBuildTestOrchestrator.IdeMcpTestRunInstrumentationMutation.FromSuccessfulParse(
                    parsed.Passed,
                    parsed.Total,
                    parsed.Failed,
                    InstrumentationPanel.TestResultsOutput,
                    outStr,
                    InstrumentationTabs);
                PublishIdeMcpTestRunMutation(mutation, openTestsPageIfRequested: true);
            });
            return outcome.JsonPayload;
        }
        catch (Exception ex)
        {
            UiScheduler.Default.Post(() =>
            {
                var mutation = IdeMcpBuildTestOrchestrator.IdeMcpTestRunInstrumentationMutation.FromThrownException(
                    InstrumentationPanel.TestResultsOutput,
                    ex.Message);
                PublishIdeMcpTestRunMutation(mutation, openTestsPageIfRequested: false);
            });
            return Services.McpDotnetBuildTestService.SerializeTestRunFailure(ex.Message, mode, filterExpression);
        }
    }

    async Task<string> Services.IIdeMcpActions.RunCodeCleanupAsync(string? includePath)
    {
        var path = await UiScheduler.Default.InvokeAsync(() => Workspace.SolutionPath ?? "");
        if (!IdeMcpSolutionPathAvailability.IsRunnableSolutionFile(path))
            return IdeMcpBuildTestOrchestrator.SerializeCodeCleanupFailure(IdeMcpBuildTestOrchestrator.MissingSolutionMessage());

        var pathCopy = path;
        await UiScheduler.Default.InvokeAsync(() =>
        {
            BuildOutputPanel.Set(IdeMcpBuildTestOrchestrator.BuildOperationHeader("Code cleanup", pathCopy));
            IsBuildOutputVisible = true;
        }).ConfigureAwait(false);

        return await WithIdeMcpPublishedBuildStateAsync(async outcome =>
        {
            try
            {
                void AppendBuildChunk(string chunk) => BuildOutputPanel.Append(chunk);
                var (success, exitCode, outStr) = await _mcpBuildTest
                    .RunCodeCleanupAsync(path, includePath, AppendBuildChunk, cancellationToken: default)
                    .ConfigureAwait(false);
                outcome.ExitCode = exitCode;
                outcome.Succeeded = success;
                var rawTruncated = IdeMcpBuildTestOrchestrator.BuildTruncatedRawOutput(outStr, 4000);

                await UiScheduler.Default.InvokeAsync(() => BuildOutputPanel.FlushPending()).ConfigureAwait(false);

                return IdeMcpBuildTestOrchestrator.SerializeCodeCleanupResult(success, exitCode, rawTruncated);
            }
            catch (Exception ex)
            {
                outcome.Succeeded = false;
                return IdeMcpBuildTestOrchestrator.SerializeCodeCleanupFailure(ex.Message);
            }
        }).ConfigureAwait(false);
    }

    /// <summary>Обновляет инструментацию, шину и навигацию MFD после MCP-тестов.</summary>
    private void PublishIdeMcpTestRunMutation(
        IdeMcpBuildTestOrchestrator.IdeMcpTestRunInstrumentationMutation mutation,
        bool openTestsPageIfRequested)
    {
        LastTestSummary = mutation.Summary;
        ImpactedTestsBadge = mutation.ImpactedTestsBadge;
        PublishToIdeDataBusAndRebuild(new TestsStateChanged(mutation.Summary, mutation.ImpactedTestsBadge));
        InstrumentationPanel.TestResultsOutput = mutation.UpdatedTestResultsOutput;
        if (openTestsPageIfRequested && mutation.ShouldOpenTestsPage)
            CurrentMfdShellPage = MfdShellPage.Tests;
    }
}

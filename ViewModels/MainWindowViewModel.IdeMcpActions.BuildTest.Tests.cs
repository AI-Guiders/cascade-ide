using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: запуск тестов (все / affected) и обновление панели инструментирования после прогона.</summary>
public partial class MainWindowViewModel
{
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

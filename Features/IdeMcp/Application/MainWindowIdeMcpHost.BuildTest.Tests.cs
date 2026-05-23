using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.ViewModels;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

internal sealed partial class MainWindowIdeMcpHost
{
    public async Task<string> RunTestsAsync()
    {
        return await RunTestsInternalAsync(filterExpression: null, mode: "all").ConfigureAwait(false);
    }
    public async Task<string> RunAffectedTestsAsync(IReadOnlyList<string>? changedPaths)
    {
        var request = IdeMcpBuildTestOrchestrator.BuildAffectedTestsRequest(changedPaths);
        return await RunTestsInternalAsync(request.filterExpression, request.mode, request.tokens).ConfigureAwait(false);
    }

    private async Task<string> RunTestsInternalAsync(string? filterExpression, string mode, IReadOnlyList<string>? tokens = null)
    {
        var path = await UiScheduler.Default.InvokeAsync(() => _host.Workspace.SolutionPath ?? "");
        if (!IdeMcpSolutionPathAvailability.IsRunnableSolutionFile(path))
            return IdeMcpBuildTestOrchestrator.SerializeMissingSolutionError(mode);

        try
        {
            var outcome = await _host.McpBuildTest.RunTestsAsync(path, filterExpression, mode, tokens).ConfigureAwait(false);
            var parsed = outcome.Parsed;
            var outStr = outcome.ConsoleOutput;

            UiScheduler.Default.Post(() =>
            {
                var mutation = IdeMcpBuildTestOrchestrator.IdeMcpTestRunInstrumentationMutation.FromSuccessfulParse(
                    parsed.Passed,
                    parsed.Total,
                    parsed.Failed,
                    _host.InstrumentationPanel.TestResultsOutput,
                    outStr,
                    _host.InstrumentationTabs);
                PublishIdeMcpTestRunMutation(mutation, openTestsPageIfRequested: true);
            });
            return outcome.JsonPayload;
        }
        catch (Exception ex)
        {
            UiScheduler.Default.Post(() =>
            {
                var mutation = IdeMcpBuildTestOrchestrator.IdeMcpTestRunInstrumentationMutation.FromThrownException(
                    _host.InstrumentationPanel.TestResultsOutput,
                    ex.Message);
                PublishIdeMcpTestRunMutation(mutation, openTestsPageIfRequested: false);
            });
            return Services.McpDotnetBuildTestService.SerializeTestRunFailure(ex.Message, mode, filterExpression);
        }
    }

    /// <summary>РћР±РЅРѕРІР»СЏРµС‚ РёРЅСЃС‚СЂСѓРјРµРЅС‚Р°С†РёСЋ, С€РёРЅСѓ Рё РЅР°РІРёРіР°С†РёСЋ MFD РїРѕСЃР»Рµ MCP-С‚РµСЃС‚РѕРІ.</summary>
    private void PublishIdeMcpTestRunMutation(
        IdeMcpBuildTestOrchestrator.IdeMcpTestRunInstrumentationMutation mutation,
        bool openTestsPageIfRequested)
    {
        _host.LastTestSummary = mutation.Summary;
        _host.ImpactedTestsBadge = mutation.ImpactedTestsBadge;
        _host.McpPublishToIdeDataBusAndRebuild(new TestsStateChanged(mutation.Summary, mutation.ImpactedTestsBadge));
        _host.InstrumentationPanel.TestResultsOutput = mutation.UpdatedTestResultsOutput;
        if (openTestsPageIfRequested && mutation.ShouldOpenTestsPage)
            _host.CurrentMfdShellPage = MfdShellPage.Tests;
    }

}

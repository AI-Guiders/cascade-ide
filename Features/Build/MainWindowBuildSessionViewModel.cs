using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.Build.Application;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Build;

/// <summary>Relay: сборка решения (композитор — <see cref="MainWindowViewModel"/>).</summary>
public sealed partial class MainWindowBuildSessionViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _host;

    public MainWindowBuildSessionViewModel(MainWindowViewModel host) => _host = host;

    [RelayCommand(CanExecute = nameof(CanBuildSolution))]
    private async Task BuildSolutionAsync()
    {
        var prep = MainWindowBuildSolutionPrepProjection.TryCreatePrep(_host.Workspace.SolutionPath);
        if (prep is null)
            return;

        _host.McpPublishToIdeDataBusAndRebuild(new BuildStateChanged(true));
        _host.IsBuilding = true;
        if (!_host.IsTerminalVisible)
            _host.IsTerminalVisible = true;
        _host.IsBuildOutputVisible = true;

        _host.CurrentMfdShellPage = prep.TargetMfdPage;
        _host.BuildOutputPanel.Set(prep.BuildOutputHeader);

        void AppendBuildChunk(string chunk) => _host.BuildOutputPanel.Append(chunk);

        var (lastExitCode, lastBuildSucceeded) =
            await DotnetSolutionChunkedBuildOrchestrator.RunSolutionBuildStreamingAsync(
                    prep.SolutionPath,
                    _host.HostDotnetRunner,
                    AppendBuildChunk,
                    CancellationToken.None)
                .ConfigureAwait(true);

        _host.BuildOutputPanel.FlushPending();
        _host.McpPublishToIdeDataBusAndRebuild(new BuildStateChanged(false, lastExitCode, lastBuildSucceeded));
        _host.IsBuilding = false;
    }

    private bool CanBuildSolution() =>
        MainWindowBuildSolutionPrepProjection.CanBuild(_host.Workspace.SolutionPath, _host.IsBuilding);

    [RelayCommand]
    private void HideBuildOutput() => _host.IsBuildOutputVisible = false;
}

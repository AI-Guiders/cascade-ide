using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SolutionLoadSessionApplyProjectionTests
{
    [Fact]
    public void ApplySuccessfulLoad_updates_workspace_and_calls_host()
    {
        var workspace = new SolutionWorkspaceViewModel();
        var root = SolutionItem.CreateSolution("App", @"C:\repo\App.sln");
        var host = new RecordingHost();

        SolutionLoadSessionApplyProjection.ApplySuccessfulLoad(
            workspace,
            root,
            @"C:\repo\App.sln",
            @"C:\repo\App.sln",
            isDockedMfdSolutionExplorerTree: false,
            host);

        Assert.Equal(@"C:\repo\App.sln", workspace.SolutionPath);
        Assert.Single(workspace.SolutionRoots);
        Assert.Equal(root, workspace.SolutionRoots[0]);
        Assert.Null(workspace.SelectedSolutionItem);
        Assert.True(host.ResetCalled);
        Assert.Equal(MfdShellPage.Terminal, host.InitialPage);
    }

    [Fact]
    public void ApplySuccessfulLoad_docked_explorer_uses_solution_explorer_page()
    {
        var workspace = new SolutionWorkspaceViewModel();
        var root = SolutionItem.CreateSolution("App", @"C:\repo\App.sln");
        var host = new RecordingHost();

        SolutionLoadSessionApplyProjection.ApplySuccessfulLoad(
            workspace,
            root,
            @"C:\repo\App.sln",
            @"C:\repo\App.sln",
            isDockedMfdSolutionExplorerTree: true,
            host);

        Assert.Equal(MfdShellPage.SolutionExplorer, host.InitialPage);
    }

    private sealed class RecordingHost : SolutionLoadSessionApplyProjection.IHost
    {
        public bool ResetCalled { get; private set; }

        public MfdShellPage InitialPage { get; private set; }

        public void ResetEditorSessionForNewSolution() => ResetCalled = true;

        public void AfterSolutionApplied(MfdShellPage initialMfdPage) => InitialPage = initialMfdPage;
    }
}

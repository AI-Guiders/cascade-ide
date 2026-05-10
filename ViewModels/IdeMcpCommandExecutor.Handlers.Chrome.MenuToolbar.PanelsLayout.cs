using static CascadeIDE.Services.IdeCommands;

namespace CascadeIDE.ViewModels;

/// <summary>MCP-хендлеры показа панелей MFD, групп редакторов и сборки из UI.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterMenuToolbarPanelsLayoutAndBuild(Action<string, Handler> add)
    {
        add(ShowPfdRegionPanel, async (_, _) =>
        {
            if (_vm.ShowPfdRegionPanelCommand.CanExecute(null))
                _vm.ShowPfdRegionPanelCommand.Execute(null);
            return "OK";
        });
        add(ShowBuildOutputPanel, async (_, _) =>
        {
            if (_vm.ShowBuildOutputPanelCommand.CanExecute(null))
                _vm.ShowBuildOutputPanelCommand.Execute(null);
            return "OK";
        });
        add(ShowChatPage, async (_, _) =>
        {
            if (_vm.ShowChatPageCommand.CanExecute(null))
                _vm.ShowChatPageCommand.Execute(null);
            return "OK";
        });
        add(ShowSolutionExplorerPage, async (_, _) =>
        {
            if (_vm.ShowSolutionExplorerPageCommand.CanExecute(null))
                _vm.ShowSolutionExplorerPageCommand.Execute(null);
            return "OK";
        });
        add(ShowRelatedFilesMfdPage, async (_, _) =>
        {
            if (_vm.ShowRelatedFilesMfdPageCommand.CanExecute(null))
                _vm.ShowRelatedFilesMfdPageCommand.Execute(null);
            return "OK";
        });
        add(ShowTerminalPanel, async (_, _) =>
        {
            if (_vm.ShowTerminalPanelCommand.CanExecute(null))
                _vm.ShowTerminalPanelCommand.Execute(null);
            return "OK";
        });
        add(HideBuildOutputPanel, async (_, _) =>
        {
            if (_vm.HideBuildOutputCommand.CanExecute(null))
                _vm.HideBuildOutputCommand.Execute(null);
            return "OK";
        });

        add(SetSingleEditorGroup, async (_, _) =>
        {
            if (_vm.SetSingleEditorGroupCommand.CanExecute(null))
                _vm.SetSingleEditorGroupCommand.Execute(null);
            return "OK";
        });
        add(SetDualEditorGroup, async (_, _) =>
        {
            if (_vm.SetDualEditorGroupCommand.CanExecute(null))
                _vm.SetDualEditorGroupCommand.Execute(null);
            return "OK";
        });
        add(SetTripleEditorGroup, async (_, _) =>
        {
            if (_vm.SetTripleEditorGroupCommand.CanExecute(null))
                _vm.SetTripleEditorGroupCommand.Execute(null);
            return "OK";
        });

        add(BuildSolutionUi, async (_, _) =>
        {
            if (_vm.BuildSolutionCommand.CanExecute(null))
                await _vm.BuildSolutionCommand.ExecuteAsync(null);
            return "OK";
        });
    }
}

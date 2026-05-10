using static CascadeIDE.Services.IdeCommands;

namespace CascadeIDE.ViewModels;

/// <summary>MCP-хендлеры меню и тулбара: открытие решения/папки/файла, темы, язык UI, группы редакторов, сборка.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterMenuAndToolbarCommands(Action<string, Handler> add)
    {
        add(OpenSolutionDialog, async (_, _) =>
        {
            if (_vm.OpenSolutionCommand.CanExecute(null))
                _vm.OpenSolutionCommand.Execute(null);
            return "OK";
        });
        add(OpenFolderDialog, async (_, _) =>
        {
            if (_vm.OpenFolderCommand.CanExecute(null))
                _vm.OpenFolderCommand.Execute(null);
            return "OK";
        });
        add(OpenFileDialog, async (_, _) =>
        {
            if (_vm.OpenFileFromDialogCommand.CanExecute(null))
                _vm.OpenFileFromDialogCommand.Execute(null);
            return "OK";
        });
        add(ExitApplication, async (_, _) =>
        {
            if (_vm.ExitCommand.CanExecute(null))
                _vm.ExitCommand.Execute(null);
            return "OK";
        });
        add(About, async (_, _) =>
        {
            if (_vm.AboutCommand.CanExecute(null))
                _vm.AboutCommand.Execute(null);
            return "OK";
        });
        add(OpenSettings, async (_, _) =>
        {
            if (_vm.OpenSettingsCommand.CanExecute(null))
                _vm.OpenSettingsCommand.Execute(null);
            return "OK";
        });
        add(OpenPreviewWindow, async (_, _) =>
        {
            if (_vm.OpenPreviewWindowCommand.CanExecute(null))
                _vm.OpenPreviewWindowCommand.Execute(null);
            return "OK";
        });
        add(ExportExpandedMarkdown, async (_, _) =>
        {
            if (_vm.ExportExpandedMarkdownCommand.CanExecute(null))
                await _vm.ExportExpandedMarkdownCommand.ExecuteAsync(null);
            return "OK";
        });
        add(ToggleMfdHostWindow, async (_, _) =>
        {
            if (!_vm.ToggleMfdHostWindowCommand.CanExecute(null))
                return "Skipped: presentation does not request Mfd host window; set presentation / zone_screen_layout in settings.toml (ADR 0017).";
            _vm.ToggleMfdHostWindowCommand.Execute(null);
            return "OK";
        });
        add(TogglePmSplitHostWindow, async (_, _) =>
        {
            if (!_vm.TogglePmSplitHostWindowCommand.CanExecute(null))
                return "Skipped: presentation does not request P+M split host; use (xP+yM)(F) or (F)(xP+yM) in settings.toml (ADR 0017).";
            _vm.TogglePmSplitHostWindowCommand.Execute(null);
            return "OK";
        });

        add(ApplyLightTheme, async (_, _) =>
        {
            if (_vm.ApplyLightThemeCommand.CanExecute(null))
                await _vm.ApplyLightThemeCommand.ExecuteAsync(null);
            return "OK";
        });
        add(ApplyDarkTheme, async (_, _) =>
        {
            if (_vm.ApplyDarkThemeCommand.CanExecute(null))
                await _vm.ApplyDarkThemeCommand.ExecuteAsync(null);
            return "OK";
        });
        add(ApplyCursorLikeTheme, async (_, _) =>
        {
            if (_vm.ApplyCursorLikeThemeCommand.CanExecute(null))
                await _vm.ApplyCursorLikeThemeCommand.ExecuteAsync(null);
            return "OK";
        });
        add(ApplyPowerClassicTheme, async (_, _) =>
        {
            if (_vm.ApplyPowerClassicThemeCommand.CanExecute(null))
                await _vm.ApplyPowerClassicThemeCommand.ExecuteAsync(null);
            return "OK";
        });
        add(OpenThemeFileDialog, async (_, _) =>
        {
            if (_vm.OpenThemeFileCommand.CanExecute(null))
                await _vm.OpenThemeFileCommand.ExecuteAsync(null);
            return "OK";
        });
        add(SetUiLanguage, async (args, _) =>
        {
            var cult = McpCommandJsonArgs.String(args, "culture") ?? McpCommandJsonArgs.String(args, "ci");
            if (string.IsNullOrWhiteSpace(cult))
                return "Missing culture (e.g. ru-RU, en-US)";
            var c = cult.Trim();
            if (_vm.SetUiLanguageCommand.CanExecute(c))
                _vm.SetUiLanguageCommand.Execute(c);
            return "OK";
        });
        add(ResetUiLanguageToSystem, async (_, _) =>
        {
            if (_vm.ResetUiLanguageToSystemCommand.CanExecute(null))
                _vm.ResetUiLanguageToSystemCommand.Execute(null);
            return "OK";
        });

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

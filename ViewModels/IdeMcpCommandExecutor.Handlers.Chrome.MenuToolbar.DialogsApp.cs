using static CascadeIDE.Services.IdeCommands;

namespace CascadeIDE.ViewModels;

/// <summary>MCP-хендлеры диалогов открытия, выхода, настроек, превью и окон-хостов презентации.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterMenuToolbarDialogsAndHosts(Action<string, Handler> add)
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
    }
}

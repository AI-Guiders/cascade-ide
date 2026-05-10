using static CascadeIDE.Services.IdeCommands;

namespace CascadeIDE.ViewModels;

/// <summary>MCP-хендлеры темы оформления и языка UI.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterMenuToolbarThemeAndLanguage(Action<string, Handler> add)
    {
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
    }
}

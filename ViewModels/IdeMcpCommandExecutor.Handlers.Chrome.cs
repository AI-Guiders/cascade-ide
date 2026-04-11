using System.Text.Json;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterOutputAndFocus(Action<string, Handler> add)
    {
        add(Services.IdeCommands.FocusEditor, async (_, _) =>
        {
            ((IIdeMcpActions)_vm).FocusEditor();
            return await Task.FromResult("OK");
        });

        Handler captureWindow = async (args, _) =>
        {
            if (_vm.CaptureWindowForMcpAsync is null)
                return "Error: главное окно не привязано к VM (внутренний снимок недоступен).";
            var ws = McpCommandJsonArgs.String(args, "workspace_path");
            var rel = McpCommandJsonArgs.String(args, "output_path");
            var scope = McpCommandJsonArgs.String(args, "scope");
            return await _vm.CaptureWindowForMcpAsync(ws, rel, scope).ConfigureAwait(false);
        };
        add(Services.IdeCommands.CaptureWindow, captureWindow);
    }

    private void RegisterUiVisibilityAndModes(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ToggleTerminal, async (_, _) =>
        {
            if (_vm.ToggleTerminalCommand.CanExecute(null))
                _vm.ToggleTerminalCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.ToggleBuildOutput, async (_, _) =>
        {
            if (_vm.ToggleBuildOutputCommand.CanExecute(null))
                _vm.ToggleBuildOutputCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.ToggleSolutionExplorer, async (_, _) =>
        {
            if (_vm.ToggleSolutionExplorerCommand.CanExecute(null))
                _vm.ToggleSolutionExplorerCommand.Execute(null);
            return "OK";
        });

        add(Services.IdeCommands.SetTerminalVisible, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var tv) || tv.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            var on = tv.GetBoolean();
            _vm.IsTerminalVisible = on;
            return "OK";
        });
        add(Services.IdeCommands.SetBuildOutputVisible, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var bv) || bv.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            var on = bv.GetBoolean();
            _vm.IsBuildOutputVisible = on;
            return "OK";
        });
        add(Services.IdeCommands.SetUiMode, async (args, _) =>
        {
            var m = McpCommandJsonArgs.String(args, "mode")?.Trim();
            if (string.IsNullOrEmpty(m))
                return "Missing mode (см. UiModes/index.toml)";
            var norm = MainWindowViewModel.NormalizeUiMode(m);
            _vm.UiMode = norm;
            return "OK";
        });

        add(Services.IdeCommands.SetSolutionExplorerVisible, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var sev) || sev.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            _vm.IsSolutionExplorerVisible = sev.GetBoolean();
            return "OK";
        });
        add(Services.IdeCommands.SetChatPanelExpanded, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var cev) || cev.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            _vm.IsChatPanelExpanded = cev.GetBoolean();
            return "OK";
        });
        add(Services.IdeCommands.SetGitPanelVisible, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var gev) || gev.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            _vm.IsGitPanelVisible = gev.GetBoolean();
            return "OK";
        });
        add(Services.IdeCommands.SetInstrumentationDockVisible, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var idv) || idv.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            _vm.IsInstrumentationDockVisible = idv.GetBoolean();
            return "OK";
        });
        add(Services.IdeCommands.ToggleGitPanel, async (_, _) =>
        {
            _vm.IsGitPanelVisible = !_vm.IsGitPanelVisible;
            return "OK";
        });
        add(Services.IdeCommands.ToggleInstrumentationDock, async (_, _) =>
        {
            if (_vm.ToggleInstrumentationDockCommand.CanExecute(null))
                _vm.ToggleInstrumentationDockCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.ToggleChatPanel, async (_, _) =>
        {
            if (_vm.ToggleChatPanelCommand.CanExecute(null))
                _vm.ToggleChatPanelCommand.Execute(null);
            return "OK";
        });

        add(Services.IdeCommands.SetFocusModeUi, async (_, _) =>
        {
            _vm.UiMode = MainWindowViewModel.NormalizeUiMode("Focus");
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.SetBalancedModeUi, async (_, _) =>
        {
            _vm.UiMode = MainWindowViewModel.NormalizeUiMode("Balanced");
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.SetPowerModeUi, async (_, _) =>
        {
            _vm.UiMode = MainWindowViewModel.NormalizeUiMode("Power");
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.CycleUiMode, async (_, _) =>
        {
            if (_vm.CycleUiModeCommand.CanExecute(null))
                _vm.CycleUiModeCommand.Execute(null);
            return "OK";
        });

        add(Services.IdeCommands.ToggleCommandPalette, async (_, _) =>
        {
            if (_vm.ToggleCommandPaletteCommand.CanExecute(null))
                _vm.ToggleCommandPaletteCommand.Execute(null);
            return await Task.FromResult("OK");
        });

        add(Services.IdeCommands.ShowEnvironmentReadinessPage, async (_, _) =>
        {
            _vm.ShowEnvironmentReadinessPage = true;
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.CloseEnvironmentReadinessPage, async (_, _) =>
        {
            if (_vm.CloseEnvironmentReadinessPageCommand.CanExecute(null))
                _vm.CloseEnvironmentReadinessPageCommand.Execute(null);
            return await Task.FromResult("OK");
        });

        add(Services.IdeCommands.SetSecondaryShellPage, async (args, _) =>
        {
            var raw = McpCommandJsonArgs.String(args, "page");
            if (string.IsNullOrWhiteSpace(raw))
                return "Missing page (string, SecondaryShellPage: Chat, Terminal, Build, …)";
            if (!Enum.TryParse<SecondaryShellPage>(raw.Trim(), ignoreCase: true, out var page))
                return $"Unknown SecondaryShellPage: {raw}";
            _vm.TryNavigateToSecondaryShellPage(page);
            return await Task.FromResult("OK");
        });
    }

    private void RegisterMenuAndToolbarCommands(Action<string, Handler> add)
    {
        add(Services.IdeCommands.OpenSolutionDialog, async (_, _) =>
        {
            if (_vm.OpenSolutionCommand.CanExecute(null))
                _vm.OpenSolutionCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.OpenFileDialog, async (_, _) =>
        {
            if (_vm.OpenFileFromDialogCommand.CanExecute(null))
                _vm.OpenFileFromDialogCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.ExitApplication, async (_, _) =>
        {
            if (_vm.ExitCommand.CanExecute(null))
                _vm.ExitCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.About, async (_, _) =>
        {
            if (_vm.AboutCommand.CanExecute(null))
                _vm.AboutCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.OpenSettings, async (_, _) =>
        {
            if (_vm.OpenSettingsCommand.CanExecute(null))
                _vm.OpenSettingsCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.OpenPreviewWindow, async (_, _) =>
        {
            if (_vm.OpenPreviewWindowCommand.CanExecute(null))
                _vm.OpenPreviewWindowCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.ExportExpandedMarkdown, async (_, _) =>
        {
            if (_vm.ExportExpandedMarkdownCommand.CanExecute(null))
                await _vm.ExportExpandedMarkdownCommand.ExecuteAsync(null);
            return "OK";
        });
        add(Services.IdeCommands.ToggleMfdHostWindow, async (_, _) =>
        {
            if (_vm.ToggleMfdHostWindowCommand.CanExecute(null))
                _vm.ToggleMfdHostWindowCommand.Execute(null);
            return "OK";
        });

        add(Services.IdeCommands.ApplyLightTheme, async (_, _) =>
        {
            if (_vm.ApplyLightThemeCommand.CanExecute(null))
                await _vm.ApplyLightThemeCommand.ExecuteAsync(null);
            return "OK";
        });
        add(Services.IdeCommands.ApplyDarkTheme, async (_, _) =>
        {
            if (_vm.ApplyDarkThemeCommand.CanExecute(null))
                await _vm.ApplyDarkThemeCommand.ExecuteAsync(null);
            return "OK";
        });
        add(Services.IdeCommands.ApplyCursorLikeTheme, async (_, _) =>
        {
            if (_vm.ApplyCursorLikeThemeCommand.CanExecute(null))
                await _vm.ApplyCursorLikeThemeCommand.ExecuteAsync(null);
            return "OK";
        });
        add(Services.IdeCommands.ApplyPowerClassicTheme, async (_, _) =>
        {
            if (_vm.ApplyPowerClassicThemeCommand.CanExecute(null))
                await _vm.ApplyPowerClassicThemeCommand.ExecuteAsync(null);
            return "OK";
        });
        add(Services.IdeCommands.OpenThemeFileDialog, async (_, _) =>
        {
            if (_vm.OpenThemeFileCommand.CanExecute(null))
                await _vm.OpenThemeFileCommand.ExecuteAsync(null);
            return "OK";
        });
        add(Services.IdeCommands.SetUiLanguage, async (args, _) =>
        {
            var cult = McpCommandJsonArgs.String(args, "culture") ?? McpCommandJsonArgs.String(args, "ci");
            if (string.IsNullOrWhiteSpace(cult))
                return "Missing culture (e.g. ru-RU, en-US)";
            var c = cult.Trim();
            if (_vm.SetUiLanguageCommand.CanExecute(c))
                _vm.SetUiLanguageCommand.Execute(c);
            return "OK";
        });
        add(Services.IdeCommands.ResetUiLanguageToSystem, async (_, _) =>
        {
            if (_vm.ResetUiLanguageToSystemCommand.CanExecute(null))
                _vm.ResetUiLanguageToSystemCommand.Execute(null);
            return "OK";
        });

        add(Services.IdeCommands.ShowSolutionExplorerPanel, async (_, _) =>
        {
            if (_vm.ShowSolutionExplorerPanelCommand.CanExecute(null))
                _vm.ShowSolutionExplorerPanelCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.ShowBuildOutputPanel, async (_, _) =>
        {
            if (_vm.ShowBuildOutputPanelCommand.CanExecute(null))
                _vm.ShowBuildOutputPanelCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.ShowChatPanel, async (_, _) =>
        {
            if (_vm.ShowChatPanelCommand.CanExecute(null))
                _vm.ShowChatPanelCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.ShowTerminalPanel, async (_, _) =>
        {
            if (_vm.ShowTerminalPanelCommand.CanExecute(null))
                _vm.ShowTerminalPanelCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.HideBuildOutputPanel, async (_, _) =>
        {
            if (_vm.HideBuildOutputCommand.CanExecute(null))
                _vm.HideBuildOutputCommand.Execute(null);
            return "OK";
        });

        add(Services.IdeCommands.SetSingleEditorGroup, async (_, _) =>
        {
            if (_vm.SetSingleEditorGroupCommand.CanExecute(null))
                _vm.SetSingleEditorGroupCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.SetDualEditorGroup, async (_, _) =>
        {
            if (_vm.SetDualEditorGroupCommand.CanExecute(null))
                _vm.SetDualEditorGroupCommand.Execute(null);
            return "OK";
        });
        add(Services.IdeCommands.SetTripleEditorGroup, async (_, _) =>
        {
            if (_vm.SetTripleEditorGroupCommand.CanExecute(null))
                _vm.SetTripleEditorGroupCommand.Execute(null);
            return "OK";
        });

        add(Services.IdeCommands.BuildSolutionUi, async (_, _) =>
        {
            if (_vm.BuildSolutionCommand.CanExecute(null))
                await _vm.BuildSolutionCommand.ExecuteAsync(null);
            return "OK";
        });
    }
}

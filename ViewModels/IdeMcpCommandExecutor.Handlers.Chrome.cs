using System.Text.Json;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using static CascadeIDE.Services.IdeCommands;

namespace CascadeIDE.ViewModels;

/// <summary>Хендлеры хрома / видимости.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterOutputAndFocus(Action<string, Handler> add)
    {
        add(IdePing, async (_, _) => await Task.FromResult(IdeMcpHostOrchestrator.PingJson()));
        add(IdeRestartMcpClients, async (_, ct) => await _vm.RestartMcpClientsForAgentAsync(ct));

        add(FocusEditor, async (_, _) =>
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
        add(CaptureWindow, captureWindow);
    }

    private void RegisterUiVisibilityAndModes(Action<string, Handler> add)
    {
        add(ToggleTerminal, async (_, _) =>
        {
            if (_vm.ToggleTerminalCommand.CanExecute(null))
                _vm.ToggleTerminalCommand.Execute(null);
            return "OK";
        });
        add(ToggleWorkspaceSplittersLock, async (_, _) =>
        {
            if (_vm.ToggleWorkspaceSplittersLockCommand.CanExecute(null))
                _vm.ToggleWorkspaceSplittersLockCommand.Execute(null);
            return "OK";
        });
        add(ToggleBuildOutput, async (_, _) =>
        {
            if (_vm.ToggleBuildOutputCommand.CanExecute(null))
                _vm.ToggleBuildOutputCommand.Execute(null);
            return "OK";
        });
        add(TogglePfdRegionExpanded, async (_, _) =>
        {
            if (_vm.TogglePfdRegionExpandedCommand.CanExecute(null))
                _vm.TogglePfdRegionExpandedCommand.Execute(null);
            return "OK";
        });
        add(CycleCodeNavigationMapPresentation, async (_, _) =>
        {
            _vm.CycleCodeNavigationMapPresentation();
            return "OK";
        });
        add(CycleCodeNavigationMapLevel, async (_, _) =>
        {
            _vm.CycleCodeNavigationMapLevel();
            return "OK";
        });
        add(CycleCodeNavigationMapDetailLevel, async (_, _) =>
        {
            _vm.CycleCodeNavigationMapDetailLevel();
            return "OK";
        });

        add(SetTerminalVisible, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var tv) || tv.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            var on = tv.GetBoolean();
            _vm.IsTerminalVisible = on;
            return "OK";
        });
        add(SetBuildOutputVisible, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var bv) || bv.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            var on = bv.GetBoolean();
            _vm.IsBuildOutputVisible = on;
            return "OK";
        });
        add(SetUiMode, async (args, _) =>
        {
            var m = McpCommandJsonArgs.String(args, "mode")?.Trim();
            if (string.IsNullOrEmpty(m))
                return "Missing mode (см. UiModes/index.toml)";
            var norm = MainWindowViewModel.NormalizeUiMode(m);
            _vm.UiMode = norm;
            return "OK";
        });

        add(SetPfdRegionExpanded, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var sev) || sev.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            _vm.ApplyPfdRegionExpanded(sev.GetBoolean());
            return "OK";
        });
        add(SetMfdRegionExpanded, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var cev) || cev.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            _vm.ApplyMfdRegionExpanded(cev.GetBoolean());
            return "OK";
        });
        add(SetGitPanelVisible, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var gev) || gev.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            _vm.IsGitPanelVisible = gev.GetBoolean();
            return "OK";
        });
        add(SetInstrumentationDockVisible, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var idv) || idv.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            _vm.IsInstrumentationDockVisible = idv.GetBoolean();
            return "OK";
        });
        add(ToggleGitPanel, async (_, _) =>
        {
            _vm.IsGitPanelVisible = !_vm.IsGitPanelVisible;
            return "OK";
        });
        add(ToggleInstrumentationDock, async (_, _) =>
        {
            if (_vm.ToggleInstrumentationDockCommand.CanExecute(null))
                _vm.ToggleInstrumentationDockCommand.Execute(null);
            return "OK";
        });
        add(ToggleMfdRegionExpanded, async (_, _) =>
        {
            if (_vm.ToggleMfdRegionExpandedCommand.CanExecute(null))
                _vm.ToggleMfdRegionExpandedCommand.Execute(null);
            return "OK";
        });

        add(CycleUiMode, async (_, _) =>
        {
            if (_vm.CycleUiModeCommand.CanExecute(null))
                _vm.CycleUiModeCommand.Execute(null);
            return "OK";
        });

        add(ToggleCommandPalette, async (_, _) =>
        {
            if (_vm.ToggleCommandPaletteCommand.CanExecute(null))
                _vm.ToggleCommandPaletteCommand.Execute(null);
            return await Task.FromResult("OK");
        });

        add(ShowEnvironmentReadinessPage, async (_, _) =>
        {
            _vm.ApplyMfdRegionExpanded(true);
            _vm.TryNavigateToMfdShellPage(MfdShellPage.EnvironmentReadiness);
            return await Task.FromResult("OK");
        });
        add(ShowHybridIndexPage, async (_, _) =>
        {
            _vm.ApplyMfdRegionExpanded(true);
            _vm.TryNavigateToMfdShellPage(MfdShellPage.HybridIndex);
            return await Task.FromResult("OK");
        });
        add(CloseEnvironmentReadinessPage, async (_, _) =>
        {
            if (_vm.CloseEnvironmentReadinessPageCommand.CanExecute(null))
                _vm.CloseEnvironmentReadinessPageCommand.Execute(null);
            return await Task.FromResult("OK");
        });
        add(ShowMarkdownPreviewPage, async (_, _) =>
        {
            if (_vm.ShowMarkdownPreviewPageCommand.CanExecute(null))
                _vm.ShowMarkdownPreviewPageCommand.Execute(null);
            return await Task.FromResult("OK");
        });

        Handler setMfdShellPageHandler = async (args, _) =>
        {
            var raw = McpCommandJsonArgs.String(args, "page");
            if (string.IsNullOrWhiteSpace(raw))
                return "Missing page (string, MfdShellPage: Chat, Terminal, Build, SolutionExplorer, …)";
            if (!Enum.TryParse<MfdShellPage>(raw.Trim(), ignoreCase: true, out var page))
                return $"Unknown MfdShellPage: {raw}";
            _vm.TryNavigateToMfdShellPage(page);
            return await Task.FromResult("OK");
        };
        add(SetMfdShellPage, setMfdShellPageHandler);
        add(SetMfdShellPageLegacy, setMfdShellPageHandler);
    }

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

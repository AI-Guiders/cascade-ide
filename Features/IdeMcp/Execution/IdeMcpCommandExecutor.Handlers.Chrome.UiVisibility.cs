using System.Text.Json;
using CascadeIDE.Models;
using CascadeIDE.ViewModels;
using static CascadeIDE.Services.IdeCommands;

namespace CascadeIDE.Features.IdeMcp.Execution;

/// <summary>MCP-хендлеры видимости панелей, режима UI, PFD/MFD, навигации по страницам MFD и палитре команд.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
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
        add(SetCodeNavigationMapLevel, async (args, _) =>
        {
            var level = McpCommandJsonArgs.String(args, "level")?.Trim();
            if (string.IsNullOrEmpty(level))
                return "Missing level (file | controlFlow)";
            _vm.SetCodeNavigationMapLevel(level);
            return $"OK: {CodeNavigationMapLevelKind.Normalize(level)}";
        });
        add(CycleCodeNavigationMapDetailLevel, async (_, _) =>
        {
            _vm.CycleCodeNavigationMapDetailLevel();
            return "OK";
        });
        add(CycleCodeNavigationMapRelatedGraphLayout, async (_, _) =>
        {
            _vm.CycleCodeNavigationMapRelatedGraphLayout();
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
        add(ShowWebAiPortalPage, async (args, _) =>
        {
            var hint = args is not null ? McpCommandJsonArgs.String(args, "url")?.Trim() : null;
            if (!string.IsNullOrEmpty(hint))
                _vm.WebAiPortalUrlText = hint;
            _vm.ApplyMfdRegionExpanded(true);
            _vm.TryNavigateToMfdShellPage(MfdShellPage.WebAiPortal);
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
}

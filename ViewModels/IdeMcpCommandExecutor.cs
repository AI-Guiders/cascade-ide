using System.Text.Json;
using Avalonia.Threading;
using CascadeIDE.Services;
using ModelContextProtocol.Protocol;

namespace CascadeIDE.ViewModels;

/// <summary>Диспетчер MCP-команд IDE: разбор args и вызов <see cref="IIdeMcpActions"/> / UI-команд главного окна.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private readonly MainWindowViewModel _vm;
    private readonly Dictionary<string, Handler> _handlers;

    private delegate Task<string> Handler(IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken);

    public IdeMcpCommandExecutor(MainWindowViewModel vm)
    {
        _vm = vm;
        _handlers = BuildHandlers();
    }

    private static string? S(IReadOnlyDictionary<string, JsonElement>? a, string key) =>
        a is not null && a.TryGetValue(key, out var e) ? e.GetString() : null;

    private static int I(IReadOnlyDictionary<string, JsonElement>? a, string key, int def = 0) =>
        a is not null && a.TryGetValue(key, out var e) && e.TryGetInt32(out var v) ? v : def;

    private static bool B(IReadOnlyDictionary<string, JsonElement>? a, string key, bool def = false) =>
        a is not null && a.TryGetValue(key, out var e) && (e.ValueKind is JsonValueKind.True or JsonValueKind.False)
            ? e.GetBoolean()
            : def;

    private static IReadOnlyList<string>? SA(IReadOnlyDictionary<string, JsonElement>? a, string key)
    {
        if (a is null || !a.TryGetValue(key, out var e) || e.ValueKind != JsonValueKind.Array)
            return null;
        var values = new List<string>();
        foreach (var item in e.EnumerateArray())
        {
            var value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                values.Add(value);
        }
        return values;
    }

    private static string ParseAndShowDebugBreakpoints(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null || !args.TryGetValue("breakpoints", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return "Missing breakpoints (array of { file_path, line })";
        var list = new List<(string, int)>();
        foreach (var item in arr.EnumerateArray())
        {
            if (!item.TryGetProperty("file_path", out var fp) || !item.TryGetProperty("line", out var ln))
                continue;
            var path = fp.GetString();
            if (string.IsNullOrEmpty(path))
                continue;
            list.Add((path, ln.GetInt32()));
        }
        actions.ShowDebugBreakpoints(list);
        return "OK";
    }

    private static string ParseAndShowDebugState(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        var stackFrames = new List<(string, string?, int)>();
        var variables = new List<(string, string)>();
        if (args is not null)
        {
            if (args.TryGetValue("stack_frames", out var sf) && sf.ValueKind == JsonValueKind.Array)
                foreach (var item in sf.EnumerateArray())
                    stackFrames.Add((
                        item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        item.TryGetProperty("file", out var f) ? f.GetString() : null,
                        item.TryGetProperty("line", out var l) ? l.GetInt32() : 0));
            if (args.TryGetValue("variables", out var v) && v.ValueKind == JsonValueKind.Array)
                foreach (var item in v.EnumerateArray())
                    if (item.TryGetProperty("name", out var vn) && item.TryGetProperty("value", out var vv))
                        variables.Add((vn.GetString() ?? "", vv.GetString() ?? ""));
        }
        actions.ShowDebugState(stackFrames, variables);
        return "OK";
    }

    public async Task<string> ExecuteAsync(string commandId, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken)
    {
        if (_handlers.TryGetValue(commandId, out var handler))
            return await handler(args, cancellationToken);

        return $"Unknown command: {commandId}";

    }

    private Dictionary<string, Handler> BuildHandlers()
    {
        var map = new Dictionary<string, Handler>(StringComparer.Ordinal);

        void Add(string id, Handler h) => map.Add(id, h);

        RegisterCore(Add);
        RegisterGenerated(Add); // generated .g.cs adds pass-through handlers
        RegisterEditorAndSolution(Add);
        RegisterDebuggerBreakpoints(Add);
        RegisterPreviewAndConfirmation(Add);
        RegisterEditorStateAndContent(Add);
        RegisterEditAndNavigation(Add);
        // NOTE: these are now generated:
        // - workspace/solution info
        // - build/tests
        // - git
        // - output/build panel
        RegisterUiVisibilityAndModes(Add);
        RegisterMenuAndToolbarCommands(Add);
        RegisterFocusPowerAndAgentActions(Add);
        RegisterDocuments(Add);
        // NOTE: UI inspection/control (pure IIdeMcpActions) is generated.
        RegisterDebugUiSurface(Add);
        RegisterAgentNotes(Add);

        return map;
    }

    // Generation hook: a source generator (or ProtocolDocGen) can emit a partial method
    // that registers 1:1 handlers for IIdeMcpActions methods.
    partial void RegisterGenerated(Action<string, Handler> add);

    private void RegisterCore(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ListTools, async (_, _) =>
        {
            bool includeDebugTools = false;
#if DEBUG
            includeDebugTools = true;
#endif
            var tools = Services.IdeMcpToolCatalog.BuildTools(includeDebugTools);
            return await Task.FromResult(JsonSerializer.Serialize(tools));
        });
    }

    private void RegisterEditorAndSolution(Action<string, Handler> add)
    {
        add(Services.IdeCommands.OpenFile, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (string.IsNullOrEmpty(S(args, "path"))) return "Missing path";
            a.OpenFile(S(args, "path")!);
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.LoadSolution, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (string.IsNullOrEmpty(S(args, "path"))) return "Missing path";
            a.LoadSolution(S(args, "path")!);
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.Select, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(S(args, "file_path"))) return "Missing file_path";
            a.SelectInEditor(S(args, "file_path"), I(args, "start_line"), I(args, "start_column"), I(args, "end_line"), I(args, "end_column"));
            return await Task.FromResult("OK");
        });
    }

    private void RegisterDebuggerBreakpoints(Action<string, Handler> add)
    {
        add(Services.IdeCommands.SetBreakpoint, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(S(args, "file_path")) || !args.TryGetValue("line", out _)) return "Missing file_path or line";
            await Dispatcher.UIThread.InvokeAsync(() => a.SetBreakpoint(S(args, "file_path")!, I(args, "line", 1), S(args, "condition")));
            return "OK";
        });
        add(Services.IdeCommands.RemoveBreakpoint, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(S(args, "file_path")) || !args.TryGetValue("line", out _)) return "Missing file_path or line";
            await Dispatcher.UIThread.InvokeAsync(() => a.RemoveBreakpoint(S(args, "file_path")!, I(args, "line", 1)));
            return "OK";
        });
    }

    private void RegisterPreviewAndConfirmation(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ShowPreview, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            a.ShowPreview(S(args, "title") ?? "", S(args, "content") ?? "");
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.ShowEditorPreview, async (_, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            a.ShowEditorPreview();
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.RequestConfirmation, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            return await a.RequestConfirmationAsync(S(args, "message") ?? "", ct);
        });
    }

    private void RegisterEditorStateAndContent(Action<string, Handler> add)
    {
        add(Services.IdeCommands.GetEditorState, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            return await a.GetEditorStateAsync(args is not null && args.TryGetValue("max_preview_chars", out var mpc) && mpc.TryGetInt32(out var maxPreview) ? maxPreview : null);
        });
        add(Services.IdeCommands.GetEditorContentRange, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            return await a.GetEditorContentRangeAsync(I(args, "start_line", 1), I(args, "end_line", 1));
        });
        add(Services.IdeCommands.GetOpenDocumentText, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            int? maxCharsOpen = null;
            if (args is not null && args.TryGetValue("max_chars", out var mco) && mco.ValueKind == JsonValueKind.Number && mco.TryGetInt32(out var mcOpen) && mcOpen > 0)
                maxCharsOpen = mcOpen;
            return await a.GetOpenDocumentTextAsync(S(args, "file_path"), maxCharsOpen);
        });
    }

    private void RegisterEditAndNavigation(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ApplyEdit, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(S(args, "file_path")) || !args.TryGetValue("new_text", out _)) return "Missing arguments";
            a.ApplyEdit(S(args, "file_path")!, I(args, "start_line"), I(args, "start_column"), I(args, "end_line"), I(args, "end_column"), S(args, "new_text") ?? "");
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.GoToPosition, async (args, ct) =>
        {
            var a = (IIdeMcpActions)_vm;
            if (args is null || string.IsNullOrEmpty(S(args, "file_path")) || !args.TryGetValue("line", out _) || !args.TryGetValue("column", out _)) return "Missing file_path, line or column";
            int? endLine = args.TryGetValue("end_line", out var el) && el.TryGetInt32(out var endL) ? endL : null;
            int? endCol = args.TryGetValue("end_column", out var ec) && ec.TryGetInt32(out var endC) ? endC : null;
            a.GoToPosition(S(args, "file_path"), I(args, "line"), I(args, "column"), endLine, endCol);
            return await Task.FromResult("OK");
        });
    }

    private void RegisterWorkspaceAndSolutionInfo(Action<string, Handler> add)
    {
        // Generated.
    }

    private void RegisterBuildAndTests(Action<string, Handler> add)
    {
        // Generated.
    }

    private void RegisterGit(Action<string, Handler> add)
    {
        // Generated.
    }

    private void RegisterOutputAndFocus(Action<string, Handler> add)
    {
        add(Services.IdeCommands.FocusEditor, async (_, _) =>
        {
            ((IIdeMcpActions)_vm).FocusEditor();
            return await Task.FromResult("OK");
        });
    }

    private void RegisterUiVisibilityAndModes(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ToggleTerminal, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ToggleTerminalCommand.CanExecute(null))
                    _vm.ToggleTerminalCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.ToggleBuildOutput, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ToggleBuildOutputCommand.CanExecute(null))
                    _vm.ToggleBuildOutputCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.ToggleSolutionExplorer, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ToggleSolutionExplorerCommand.CanExecute(null))
                    _vm.ToggleSolutionExplorerCommand.Execute(null);
            });
            return "OK";
        });

        add(Services.IdeCommands.SetTerminalVisible, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var tv) || tv.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            var on = tv.GetBoolean();
            await Dispatcher.UIThread.InvokeAsync(() => _vm.IsTerminalVisible = on);
            return "OK";
        });
        add(Services.IdeCommands.SetBuildOutputVisible, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var bv) || bv.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            var on = bv.GetBoolean();
            await Dispatcher.UIThread.InvokeAsync(() => _vm.IsBuildOutputVisible = on);
            return "OK";
        });
        add(Services.IdeCommands.SetUiMode, async (args, _) =>
        {
            var m = S(args, "mode")?.Trim();
            if (string.IsNullOrEmpty(m))
                return "Missing mode (Focus|Balanced|Power)";
            if (!string.Equals(m, "Focus", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(m, "Balanced", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(m, "Power", StringComparison.OrdinalIgnoreCase))
                return $"Unknown mode: {m}";
            var norm = MainWindowViewModel.NormalizeUiMode(m);
            await Dispatcher.UIThread.InvokeAsync(() => _vm.UiMode = norm);
            return "OK";
        });

        add(Services.IdeCommands.SetSolutionExplorerVisible, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var sev) || sev.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            await Dispatcher.UIThread.InvokeAsync(() => _vm.IsSolutionExplorerVisible = sev.GetBoolean());
            return "OK";
        });
        add(Services.IdeCommands.SetChatPanelExpanded, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var cev) || cev.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            await Dispatcher.UIThread.InvokeAsync(() => _vm.IsChatPanelExpanded = cev.GetBoolean());
            return "OK";
        });
        add(Services.IdeCommands.SetGitPanelVisible, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var gev) || gev.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            await Dispatcher.UIThread.InvokeAsync(() => _vm.IsGitPanelVisible = gev.GetBoolean());
            return "OK";
        });
        add(Services.IdeCommands.SetInstrumentationDockVisible, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("visible", out var idv) || idv.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return "Missing or invalid visible (boolean)";
            await Dispatcher.UIThread.InvokeAsync(() => _vm.IsInstrumentationDockVisible = idv.GetBoolean());
            return "OK";
        });
        add(Services.IdeCommands.ToggleGitPanel, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() => _vm.IsGitPanelVisible = !_vm.IsGitPanelVisible);
            return "OK";
        });
        add(Services.IdeCommands.ToggleInstrumentationDock, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ToggleInstrumentationDockCommand.CanExecute(null))
                    _vm.ToggleInstrumentationDockCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.ToggleChatPanel, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ToggleChatPanelCommand.CanExecute(null))
                    _vm.ToggleChatPanelCommand.Execute(null);
            });
            return "OK";
        });

        add(Services.IdeCommands.SetFocusModeUi, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.SetFocusModeCommand.CanExecute(null))
                    _vm.SetFocusModeCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.SetBalancedModeUi, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.SetBalancedModeCommand.CanExecute(null))
                    _vm.SetBalancedModeCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.SetPowerModeUi, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.SetPowerModeCommand.CanExecute(null))
                    _vm.SetPowerModeCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.CycleUiMode, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.CycleUiModeCommand.CanExecute(null))
                    _vm.CycleUiModeCommand.Execute(null);
            });
            return "OK";
        });
    }

    private void RegisterMenuAndToolbarCommands(Action<string, Handler> add)
    {
        add(Services.IdeCommands.OpenSolutionDialog, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.OpenSolutionCommand.CanExecute(null))
                    _vm.OpenSolutionCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.ExitApplication, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ExitCommand.CanExecute(null))
                    _vm.ExitCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.About, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.AboutCommand.CanExecute(null))
                    _vm.AboutCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.OpenSettings, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.OpenSettingsCommand.CanExecute(null))
                    _vm.OpenSettingsCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.OpenPreviewWindow, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.OpenPreviewWindowCommand.CanExecute(null))
                    _vm.OpenPreviewWindowCommand.Execute(null);
            });
            return "OK";
        });

        add(Services.IdeCommands.ApplyLightTheme, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (_vm.ApplyLightThemeCommand.CanExecute(null))
                    await _vm.ApplyLightThemeCommand.ExecuteAsync(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.ApplyDarkTheme, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (_vm.ApplyDarkThemeCommand.CanExecute(null))
                    await _vm.ApplyDarkThemeCommand.ExecuteAsync(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.ApplyCursorLikeTheme, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (_vm.ApplyCursorLikeThemeCommand.CanExecute(null))
                    await _vm.ApplyCursorLikeThemeCommand.ExecuteAsync(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.ApplyPowerClassicTheme, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (_vm.ApplyPowerClassicThemeCommand.CanExecute(null))
                    await _vm.ApplyPowerClassicThemeCommand.ExecuteAsync(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.OpenThemeFileDialog, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (_vm.OpenThemeFileCommand.CanExecute(null))
                    await _vm.OpenThemeFileCommand.ExecuteAsync(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.SetUiLanguage, async (args, _) =>
        {
            var cult = S(args, "culture") ?? S(args, "ci");
            if (string.IsNullOrWhiteSpace(cult))
                return "Missing culture (e.g. ru-RU, en-US)";
            var c = cult.Trim();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.SetUiLanguageCommand.CanExecute(c))
                    _vm.SetUiLanguageCommand.Execute(c);
            });
            return "OK";
        });
        add(Services.IdeCommands.ResetUiLanguageToSystem, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ResetUiLanguageToSystemCommand.CanExecute(null))
                    _vm.ResetUiLanguageToSystemCommand.Execute(null);
            });
            return "OK";
        });

        add(Services.IdeCommands.ShowSolutionExplorerPanel, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ShowSolutionExplorerPanelCommand.CanExecute(null))
                    _vm.ShowSolutionExplorerPanelCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.ShowBuildOutputPanel, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ShowBuildOutputPanelCommand.CanExecute(null))
                    _vm.ShowBuildOutputPanelCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.ShowChatPanel, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ShowChatPanelCommand.CanExecute(null))
                    _vm.ShowChatPanelCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.ShowTerminalPanel, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ShowTerminalPanelCommand.CanExecute(null))
                    _vm.ShowTerminalPanelCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.HideBuildOutputPanel, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.HideBuildOutputCommand.CanExecute(null))
                    _vm.HideBuildOutputCommand.Execute(null);
            });
            return "OK";
        });

        add(Services.IdeCommands.SetSingleEditorGroup, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.SetSingleEditorGroupCommand.CanExecute(null))
                    _vm.SetSingleEditorGroupCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.SetDualEditorGroup, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.SetDualEditorGroupCommand.CanExecute(null))
                    _vm.SetDualEditorGroupCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.SetTripleEditorGroup, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.SetTripleEditorGroupCommand.CanExecute(null))
                    _vm.SetTripleEditorGroupCommand.Execute(null);
            });
            return "OK";
        });

        add(Services.IdeCommands.BuildSolutionUi, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (_vm.BuildSolutionCommand.CanExecute(null))
                    await _vm.BuildSolutionCommand.ExecuteAsync(null);
            });
            return "OK";
        });
    }

    private void RegisterFocusPowerAndAgentActions(Action<string, Handler> add)
    {
        add(Services.IdeCommands.FocusCheckpoint, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.FocusCheckpointCommand.CanExecute(null))
                    _vm.FocusCheckpointCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.FocusRollback, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.FocusRollbackCommand.CanExecute(null))
                    _vm.FocusRollbackCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.ConfirmFocusStep, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ConfirmFocusStepCommand.CanExecute(null))
                    _vm.ConfirmFocusStepCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.CancelFocusStep, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.CancelFocusStepCommand.CanExecute(null))
                    _vm.CancelFocusStepCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.ExplainCurrentStep, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ExplainCurrentStepCommand.CanExecute(null))
                    _vm.ExplainCurrentStepCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.EmergencyStop, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.EmergencyStopCommand.CanExecute(null))
                    _vm.EmergencyStopCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.RefreshWorkspaceSnapshot, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.RefreshWorkspaceSnapshotCommand.CanExecute(null))
                    _vm.RefreshWorkspaceSnapshotCommand.Execute(null);
            });
            return "OK";
        });

        add(Services.IdeCommands.ExplainTraceStep, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("step_index", out var exIdx) || exIdx.ValueKind != JsonValueKind.Number || !exIdx.TryGetInt32(out var explainStepIndex) || explainStepIndex < 0)
                return "Missing or invalid step_index (non-negative int; 0 = oldest in AgentTraceSteps)";
            var explainErr = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var list = _vm.InstrumentationPanel.AgentTraceSteps;
                if (explainStepIndex >= list.Count)
                    return $"Invalid step_index (count={list.Count})";
                _vm.ExplainTraceStepCommand.Execute(list[explainStepIndex]);
                return (string?)null;
            });
            return explainErr ?? "OK";
        });
        add(Services.IdeCommands.RollbackTraceStep, async (args, _) =>
        {
            if (args is null || !args.TryGetValue("step_index", out var rbIdx) || rbIdx.ValueKind != JsonValueKind.Number || !rbIdx.TryGetInt32(out var rollbackStepIndex) || rollbackStepIndex < 0)
                return "Missing or invalid step_index (non-negative int; 0 = oldest in AgentTraceSteps)";
            var rollbackErr = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var list = _vm.InstrumentationPanel.AgentTraceSteps;
                if (rollbackStepIndex >= list.Count)
                    return $"Invalid step_index (count={list.Count})";
                _vm.RollbackTraceStepCommand.Execute(list[rollbackStepIndex]);
                return (string?)null;
            });
            return rollbackErr ?? "OK";
        });

        add(Services.IdeCommands.SetSafetyL1, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.SetSafetyL1Command.CanExecute(null))
                    _vm.SetSafetyL1Command.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.SetSafetyL2, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.SetSafetyL2Command.CanExecute(null))
                    _vm.SetSafetyL2Command.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.SetSafetyL3, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.SetSafetyL3Command.CanExecute(null))
                    _vm.SetSafetyL3Command.Execute(null);
            });
            return "OK";
        });

        add(Services.IdeCommands.StartAutonomous, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.StartAutonomousCommand.CanExecute(null))
                    _vm.StartAutonomousCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.PauseAutonomous, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.PauseAutonomousCommand.CanExecute(null))
                    _vm.PauseAutonomousCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.ResumeAutonomous, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ResumeAutonomousCommand.CanExecute(null))
                    _vm.ResumeAutonomousCommand.Execute(null);
            });
            return "OK";
        });

        add(Services.IdeCommands.FixFailingTests, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.FixFailingTestsCommand.CanExecute(null))
                    _vm.FixFailingTestsCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.InvestigateNullref, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.InvestigateNullrefCommand.CanExecute(null))
                    _vm.InvestigateNullrefCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.PrepareCommit, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.PrepareCommitCommand.CanExecute(null))
                    _vm.PrepareCommitCommand.Execute(null);
            });
            return "OK";
        });

        add(Services.IdeCommands.SendChat, async (args, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var msg = S(args, "message");
                if (!string.IsNullOrWhiteSpace(msg))
                    _vm.ChatPanel.ChatInput = msg!;
                if (_vm.ChatPanel.SendChatCommand.CanExecute(null))
                    await _vm.ChatPanel.SendChatCommand.ExecuteAsync(null);
            });
            return "OK";
        });

        add(Services.IdeCommands.InstallOllamaModel, async (args, _) =>
        {
            var model = S(args, "model");
            if (string.IsNullOrWhiteSpace(model))
                return "Missing model";
            var m = model.Trim();
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                _vm.ModelToInstall = m;
                if (_vm.InstallModelCommand.CanExecute(null))
                    await _vm.InstallModelCommand.ExecuteAsync(null);
            });
            return "OK";
        });
    }

    private void RegisterDocuments(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ReopenClosedDocument, async (_, _) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ReopenClosedDocumentCommand.CanExecute(null))
                    _vm.ReopenClosedDocumentCommand.Execute(null);
            });
            return "OK";
        });
        add(Services.IdeCommands.ActivateDocument, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(S(args, "file_path")))
                return "Missing file_path";
            var pathAct = S(args, "file_path")!;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.ActivateDocumentCommand.CanExecute(pathAct))
                    _vm.ActivateDocumentCommand.Execute(pathAct);
            });
            return "OK";
        });
        add(Services.IdeCommands.CloseDocument, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(S(args, "file_path")))
                return "Missing file_path";
            var pathClose = S(args, "file_path")!;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.CloseDocumentCommand.CanExecute(pathClose))
                    _vm.CloseDocumentCommand.Execute(pathClose);
            });
            return "OK";
        });
        add(Services.IdeCommands.TogglePinDocument, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(S(args, "file_path")))
                return "Missing file_path";
            var pathPin = S(args, "file_path")!;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.TogglePinDocumentCommand.CanExecute(pathPin))
                    _vm.TogglePinDocumentCommand.Execute(pathPin);
            });
            return "OK";
        });
        add(Services.IdeCommands.MoveDocumentToGroup1, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(S(args, "file_path")))
                return "Missing file_path";
            var p1 = S(args, "file_path")!;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.MoveDocumentToGroup1Command.CanExecute(p1))
                    _vm.MoveDocumentToGroup1Command.Execute(p1);
            });
            return "OK";
        });
        add(Services.IdeCommands.MoveDocumentToGroup2, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(S(args, "file_path")))
                return "Missing file_path";
            var p2 = S(args, "file_path")!;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.MoveDocumentToGroup2Command.CanExecute(p2))
                    _vm.MoveDocumentToGroup2Command.Execute(p2);
            });
            return "OK";
        });
        add(Services.IdeCommands.MoveDocumentToGroup3, async (args, _) =>
        {
            if (string.IsNullOrWhiteSpace(S(args, "file_path")))
                return "Missing file_path";
            var p3 = S(args, "file_path")!;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_vm.MoveDocumentToGroup3Command.CanExecute(p3))
                    _vm.MoveDocumentToGroup3Command.Execute(p3);
            });
            return "OK";
        });
    }

    private void RegisterUiInspectionAndControl(Action<string, Handler> add)
    {
        // Generated.
    }

    private void RegisterDebugUiSurface(Action<string, Handler> add)
    {
        add(Services.IdeCommands.ShowBreakpoints, async (args, _) => await Task.FromResult(ParseAndShowDebugBreakpoints((IIdeMcpActions)_vm, args)));
        add(Services.IdeCommands.ShowDebugPosition, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            a.ShowDebugPosition(S(args, "file_path"), I(args, "line"));
            return await Task.FromResult("OK");
        });
        add(Services.IdeCommands.ShowDebugState, async (args, _) => await Task.FromResult(ParseAndShowDebugState((IIdeMcpActions)_vm, args)));

#if DEBUG
        add(Services.IdeCommands.AddControl, async (args, _) =>
        {
            var a = (IIdeMcpActions)_vm;
            return await a.AddControlAsync(S(args, "parent_name") ?? "", S(args, "control_type") ?? "", S(args, "content"), S(args, "name"));
        });
#endif
    }

    private void RegisterAgentNotes(Action<string, Handler> add)
    {
        add(Services.IdeCommands.WriteAgentNotes, async (args, ct) => await ((IIdeMcpActions)_vm).WriteAgentNotesAsync(S(args, "content") ?? "", ct));
        add(Services.IdeCommands.ReadAgentNotes, async (_, ct) => await ((IIdeMcpActions)_vm).ReadAgentNotesAsync(ct));
    }
}

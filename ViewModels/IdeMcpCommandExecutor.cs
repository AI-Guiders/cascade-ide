using System.Text.Json;
using Avalonia.Threading;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>Диспетчер MCP-команд IDE: разбор args и вызов <see cref="IIdeMcpActions"/> / UI-команд главного окна.</summary>
internal sealed class IdeMcpCommandExecutor
{
    private readonly MainWindowViewModel _vm;

    public IdeMcpCommandExecutor(MainWindowViewModel vm) => _vm = vm;

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
        var a = (IIdeMcpActions)_vm;
        switch (commandId)
        {
            case Services.IdeCommands.OpenFile:
                if (string.IsNullOrEmpty(S(args, "path"))) return "Missing path";
                a.OpenFile(S(args, "path")!);
                return "OK";
            case Services.IdeCommands.LoadSolution:
                if (string.IsNullOrEmpty(S(args, "path"))) return "Missing path";
                a.LoadSolution(S(args, "path")!);
                return "OK";
            case Services.IdeCommands.Select:
                if (args is null || string.IsNullOrEmpty(S(args, "file_path"))) return "Missing file_path";
                a.SelectInEditor(S(args, "file_path"), I(args, "start_line"), I(args, "start_column"), I(args, "end_line"), I(args, "end_column"));
                return "OK";
            case Services.IdeCommands.SetBreakpoint:
                if (args is null || string.IsNullOrEmpty(S(args, "file_path")) || !args.TryGetValue("line", out _)) return "Missing file_path or line";
                await Dispatcher.UIThread.InvokeAsync(() => a.SetBreakpoint(S(args, "file_path")!, I(args, "line", 1), S(args, "condition")));
                return "OK";
            case Services.IdeCommands.RemoveBreakpoint:
                if (args is null || string.IsNullOrEmpty(S(args, "file_path")) || !args.TryGetValue("line", out _)) return "Missing file_path or line";
                await Dispatcher.UIThread.InvokeAsync(() => a.RemoveBreakpoint(S(args, "file_path")!, I(args, "line", 1)));
                return "OK";
            case Services.IdeCommands.ShowPreview:
                a.ShowPreview(S(args, "title") ?? "", S(args, "content") ?? "");
                return "OK";
            case Services.IdeCommands.ShowEditorPreview:
                a.ShowEditorPreview();
                return "OK";
            case Services.IdeCommands.RequestConfirmation:
                return await a.RequestConfirmationAsync(S(args, "message") ?? "", cancellationToken);
            case Services.IdeCommands.GetEditorState:
                return await a.GetEditorStateAsync(args is not null && args.TryGetValue("max_preview_chars", out var mpc) && mpc.TryGetInt32(out var maxPreview) ? maxPreview : null);
            case Services.IdeCommands.GetEditorContentRange:
                return await a.GetEditorContentRangeAsync(I(args, "start_line", 1), I(args, "end_line", 1));
            case Services.IdeCommands.GetOpenDocumentText:
                {
                    int? maxCharsOpen = null;
                    if (args is not null && args.TryGetValue("max_chars", out var mco) && mco.ValueKind == JsonValueKind.Number && mco.TryGetInt32(out var mcOpen) && mcOpen > 0)
                        maxCharsOpen = mcOpen;
                    return await a.GetOpenDocumentTextAsync(S(args, "file_path"), maxCharsOpen);
                }
            case Services.IdeCommands.ApplyEdit:
                if (args is null || string.IsNullOrEmpty(S(args, "file_path")) || !args.TryGetValue("new_text", out _)) return "Missing arguments";
                a.ApplyEdit(S(args, "file_path")!, I(args, "start_line"), I(args, "start_column"), I(args, "end_line"), I(args, "end_column"), S(args, "new_text") ?? "");
                return "OK";
            case Services.IdeCommands.GoToPosition:
                if (args is null || string.IsNullOrEmpty(S(args, "file_path")) || !args.TryGetValue("line", out _) || !args.TryGetValue("column", out _)) return "Missing file_path, line or column";
                int? endLine = args.TryGetValue("end_line", out var el) && el.TryGetInt32(out var endL) ? endL : null;
                int? endCol = args.TryGetValue("end_column", out var ec) && ec.TryGetInt32(out var endC) ? endC : null;
                a.GoToPosition(S(args, "file_path"), I(args, "line"), I(args, "column"), endLine, endCol);
                return "OK";
            case Services.IdeCommands.GetSolutionInfo:
                return a.GetSolutionInfo();
            case Services.IdeCommands.GetWorkspaceState:
                return await a.GetWorkspaceStateAsync();
            case Services.IdeCommands.GetSolutionFiles:
                return await a.GetSolutionFilesAsync();
            case Services.IdeCommands.GetCurrentFileDiagnostics:
                return await a.GetCurrentFileDiagnosticsAsync();
            case Services.IdeCommands.Build:
                return await a.BuildAsync();
            case Services.IdeCommands.BuildStructured:
                return await a.BuildStructuredAsync();
            case Services.IdeCommands.RunTests:
                return await a.RunTestsAsync();
            case Services.IdeCommands.RunAffectedTests:
                return await a.RunAffectedTestsAsync(SA(args, "changed_paths"));
            case Services.IdeCommands.RunCodeCleanup:
                return await a.RunCodeCleanupAsync(S(args, "include_path"));
            case Services.IdeCommands.GetCodeMetrics:
                return await a.GetCodeMetricsAsync(S(args, "scope"), S(args, "path"));
            case Services.IdeCommands.GitStatus:
                return await a.GitStatusAsync();
            case Services.IdeCommands.GitDiff:
                return await a.GitDiffAsync(S(args, "path"), B(args, "staged"));
            case Services.IdeCommands.GitCommit:
                if (string.IsNullOrWhiteSpace(S(args, "message"))) return "Missing message";
                return await a.GitCommitAsync(S(args, "message")!, SA(args, "paths"));
            case Services.IdeCommands.GitPush:
                return await a.GitPushAsync(S(args, "remote"), S(args, "branch"));
            case Services.IdeCommands.GetBuildOutput:
                return a.GetBuildOutput();
            case Services.IdeCommands.FocusEditor:
                a.FocusEditor();
                return "OK";
            case Services.IdeCommands.ToggleTerminal:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.ToggleTerminalCommand.CanExecute(null))
                        _vm.ToggleTerminalCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.ToggleBuildOutput:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.ToggleBuildOutputCommand.CanExecute(null))
                        _vm.ToggleBuildOutputCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.ToggleSolutionExplorer:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.ToggleSolutionExplorerCommand.CanExecute(null))
                        _vm.ToggleSolutionExplorerCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.SetTerminalVisible:
                if (args is null || !args.TryGetValue("visible", out var tv) || tv.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return "Missing or invalid visible (boolean)";
                {
                    var on = tv.GetBoolean();
                    await Dispatcher.UIThread.InvokeAsync(() => _vm.IsTerminalVisible = on);
                }
                return "OK";
            case Services.IdeCommands.SetBuildOutputVisible:
                if (args is null || !args.TryGetValue("visible", out var bv) || bv.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return "Missing or invalid visible (boolean)";
                {
                    var on = bv.GetBoolean();
                    await Dispatcher.UIThread.InvokeAsync(() => _vm.IsBuildOutputVisible = on);
                }
                return "OK";
            case Services.IdeCommands.SetUiMode:
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
                }
                return "OK";

            // ——— Паритет с меню / тулбаром / task bar / чатом (те же RelayCommand, что в XAML)
            case Services.IdeCommands.OpenSolutionDialog:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.OpenSolutionCommand.CanExecute(null))
                        _vm.OpenSolutionCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.ExitApplication:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.ExitCommand.CanExecute(null))
                        _vm.ExitCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.About:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.AboutCommand.CanExecute(null))
                        _vm.AboutCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.OpenSettings:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.OpenSettingsCommand.CanExecute(null))
                        _vm.OpenSettingsCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.OpenPreviewWindow:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.OpenPreviewWindowCommand.CanExecute(null))
                        _vm.OpenPreviewWindowCommand.Execute(null);
                });
                return "OK";

            case Services.IdeCommands.SetSolutionExplorerVisible:
                if (args is null || !args.TryGetValue("visible", out var sev) || sev.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return "Missing or invalid visible (boolean)";
                await Dispatcher.UIThread.InvokeAsync(() => _vm.IsSolutionExplorerVisible = sev.GetBoolean());
                return "OK";
            case Services.IdeCommands.SetChatPanelExpanded:
                if (args is null || !args.TryGetValue("visible", out var cev) || cev.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return "Missing or invalid visible (boolean)";
                await Dispatcher.UIThread.InvokeAsync(() => _vm.IsChatPanelExpanded = cev.GetBoolean());
                return "OK";
            case Services.IdeCommands.SetGitPanelVisible:
                if (args is null || !args.TryGetValue("visible", out var gev) || gev.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return "Missing or invalid visible (boolean)";
                await Dispatcher.UIThread.InvokeAsync(() => _vm.IsGitPanelVisible = gev.GetBoolean());
                return "OK";
            case Services.IdeCommands.SetInstrumentationDockVisible:
                if (args is null || !args.TryGetValue("visible", out var idv) || idv.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    return "Missing or invalid visible (boolean)";
                await Dispatcher.UIThread.InvokeAsync(() => _vm.IsInstrumentationDockVisible = idv.GetBoolean());
                return "OK";
            case Services.IdeCommands.ToggleGitPanel:
                await Dispatcher.UIThread.InvokeAsync(() => _vm.IsGitPanelVisible = !_vm.IsGitPanelVisible);
                return "OK";
            case Services.IdeCommands.ToggleInstrumentationDock:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.ToggleInstrumentationDockCommand.CanExecute(null))
                        _vm.ToggleInstrumentationDockCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.ToggleChatPanel:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.ToggleChatPanelCommand.CanExecute(null))
                        _vm.ToggleChatPanelCommand.Execute(null);
                });
                return "OK";

            case Services.IdeCommands.SetFocusModeUi:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.SetFocusModeCommand.CanExecute(null))
                        _vm.SetFocusModeCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.SetBalancedModeUi:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.SetBalancedModeCommand.CanExecute(null))
                        _vm.SetBalancedModeCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.SetPowerModeUi:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.SetPowerModeCommand.CanExecute(null))
                        _vm.SetPowerModeCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.CycleUiMode:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.CycleUiModeCommand.CanExecute(null))
                        _vm.CycleUiModeCommand.Execute(null);
                });
                return "OK";

            case Services.IdeCommands.ApplyLightTheme:
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (_vm.ApplyLightThemeCommand.CanExecute(null))
                        await _vm.ApplyLightThemeCommand.ExecuteAsync(null);
                });
                return "OK";
            case Services.IdeCommands.ApplyDarkTheme:
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (_vm.ApplyDarkThemeCommand.CanExecute(null))
                        await _vm.ApplyDarkThemeCommand.ExecuteAsync(null);
                });
                return "OK";
            case Services.IdeCommands.ApplyCursorLikeTheme:
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (_vm.ApplyCursorLikeThemeCommand.CanExecute(null))
                        await _vm.ApplyCursorLikeThemeCommand.ExecuteAsync(null);
                });
                return "OK";
            case Services.IdeCommands.ApplyPowerClassicTheme:
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (_vm.ApplyPowerClassicThemeCommand.CanExecute(null))
                        await _vm.ApplyPowerClassicThemeCommand.ExecuteAsync(null);
                });
                return "OK";
            case Services.IdeCommands.OpenThemeFileDialog:
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (_vm.OpenThemeFileCommand.CanExecute(null))
                        await _vm.OpenThemeFileCommand.ExecuteAsync(null);
                });
                return "OK";

            case Services.IdeCommands.SetUiLanguage:
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
                }
                return "OK";
            case Services.IdeCommands.ResetUiLanguageToSystem:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.ResetUiLanguageToSystemCommand.CanExecute(null))
                        _vm.ResetUiLanguageToSystemCommand.Execute(null);
                });
                return "OK";

            case Services.IdeCommands.ShowSolutionExplorerPanel:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.ShowSolutionExplorerPanelCommand.CanExecute(null))
                        _vm.ShowSolutionExplorerPanelCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.ShowBuildOutputPanel:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.ShowBuildOutputPanelCommand.CanExecute(null))
                        _vm.ShowBuildOutputPanelCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.ShowChatPanel:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.ShowChatPanelCommand.CanExecute(null))
                        _vm.ShowChatPanelCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.ShowTerminalPanel:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.ShowTerminalPanelCommand.CanExecute(null))
                        _vm.ShowTerminalPanelCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.HideBuildOutputPanel:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.HideBuildOutputCommand.CanExecute(null))
                        _vm.HideBuildOutputCommand.Execute(null);
                });
                return "OK";

            case Services.IdeCommands.SetSingleEditorGroup:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.SetSingleEditorGroupCommand.CanExecute(null))
                        _vm.SetSingleEditorGroupCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.SetDualEditorGroup:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.SetDualEditorGroupCommand.CanExecute(null))
                        _vm.SetDualEditorGroupCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.SetTripleEditorGroup:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.SetTripleEditorGroupCommand.CanExecute(null))
                        _vm.SetTripleEditorGroupCommand.Execute(null);
                });
                return "OK";

            case Services.IdeCommands.BuildSolutionUi:
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    if (_vm.BuildSolutionCommand.CanExecute(null))
                        await _vm.BuildSolutionCommand.ExecuteAsync(null);
                });
                return "OK";

            case Services.IdeCommands.FocusCheckpoint:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.FocusCheckpointCommand.CanExecute(null))
                        _vm.FocusCheckpointCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.FocusRollback:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.FocusRollbackCommand.CanExecute(null))
                        _vm.FocusRollbackCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.ConfirmFocusStep:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.ConfirmFocusStepCommand.CanExecute(null))
                        _vm.ConfirmFocusStepCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.CancelFocusStep:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.CancelFocusStepCommand.CanExecute(null))
                        _vm.CancelFocusStepCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.ExplainCurrentStep:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.ExplainCurrentStepCommand.CanExecute(null))
                        _vm.ExplainCurrentStepCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.EmergencyStop:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.EmergencyStopCommand.CanExecute(null))
                        _vm.EmergencyStopCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.RefreshWorkspaceSnapshot:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.RefreshWorkspaceSnapshotCommand.CanExecute(null))
                        _vm.RefreshWorkspaceSnapshotCommand.Execute(null);
                });
                return "OK";

            case Services.IdeCommands.ExplainTraceStep:
                if (args is null || !args.TryGetValue("step_index", out var exIdx) || exIdx.ValueKind != JsonValueKind.Number || !exIdx.TryGetInt32(out var explainStepIndex) || explainStepIndex < 0)
                    return "Missing or invalid step_index (non-negative int; 0 = oldest in AgentTraceSteps)";
                {
                    var explainErr = await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var list = _vm.InstrumentationPanel.AgentTraceSteps;
                        if (explainStepIndex >= list.Count)
                            return $"Invalid step_index (count={list.Count})";
                        _vm.ExplainTraceStepCommand.Execute(list[explainStepIndex]);
                        return (string?)null;
                    });
                    return explainErr ?? "OK";
                }
            case Services.IdeCommands.RollbackTraceStep:
                if (args is null || !args.TryGetValue("step_index", out var rbIdx) || rbIdx.ValueKind != JsonValueKind.Number || !rbIdx.TryGetInt32(out var rollbackStepIndex) || rollbackStepIndex < 0)
                    return "Missing or invalid step_index (non-negative int; 0 = oldest in AgentTraceSteps)";
                {
                    var rollbackErr = await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var list = _vm.InstrumentationPanel.AgentTraceSteps;
                        if (rollbackStepIndex >= list.Count)
                            return $"Invalid step_index (count={list.Count})";
                        _vm.RollbackTraceStepCommand.Execute(list[rollbackStepIndex]);
                        return (string?)null;
                    });
                    return rollbackErr ?? "OK";
                }

            case Services.IdeCommands.SetSafetyL1:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.SetSafetyL1Command.CanExecute(null))
                        _vm.SetSafetyL1Command.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.SetSafetyL2:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.SetSafetyL2Command.CanExecute(null))
                        _vm.SetSafetyL2Command.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.SetSafetyL3:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.SetSafetyL3Command.CanExecute(null))
                        _vm.SetSafetyL3Command.Execute(null);
                });
                return "OK";

            case Services.IdeCommands.StartAutonomous:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.StartAutonomousCommand.CanExecute(null))
                        _vm.StartAutonomousCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.PauseAutonomous:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.PauseAutonomousCommand.CanExecute(null))
                        _vm.PauseAutonomousCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.ResumeAutonomous:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.ResumeAutonomousCommand.CanExecute(null))
                        _vm.ResumeAutonomousCommand.Execute(null);
                });
                return "OK";

            case Services.IdeCommands.FixFailingTests:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.FixFailingTestsCommand.CanExecute(null))
                        _vm.FixFailingTestsCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.InvestigateNullref:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.InvestigateNullrefCommand.CanExecute(null))
                        _vm.InvestigateNullrefCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.PrepareCommit:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.PrepareCommitCommand.CanExecute(null))
                        _vm.PrepareCommitCommand.Execute(null);
                });
                return "OK";

            case Services.IdeCommands.SendChat:
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var msg = S(args, "message");
                    if (!string.IsNullOrWhiteSpace(msg))
                        _vm.ChatPanel.ChatInput = msg!;
                    if (_vm.ChatPanel.SendChatCommand.CanExecute(null))
                        await _vm.ChatPanel.SendChatCommand.ExecuteAsync(null);
                });
                return "OK";

            case Services.IdeCommands.InstallOllamaModel:
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
                }
                return "OK";

            case Services.IdeCommands.ReopenClosedDocument:
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_vm.ReopenClosedDocumentCommand.CanExecute(null))
                        _vm.ReopenClosedDocumentCommand.Execute(null);
                });
                return "OK";
            case Services.IdeCommands.ActivateDocument:
                if (string.IsNullOrWhiteSpace(S(args, "file_path")))
                    return "Missing file_path";
                {
                    var pathAct = S(args, "file_path")!;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_vm.ActivateDocumentCommand.CanExecute(pathAct))
                            _vm.ActivateDocumentCommand.Execute(pathAct);
                    });
                }
                return "OK";
            case Services.IdeCommands.CloseDocument:
                if (string.IsNullOrWhiteSpace(S(args, "file_path")))
                    return "Missing file_path";
                {
                    var pathClose = S(args, "file_path")!;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_vm.CloseDocumentCommand.CanExecute(pathClose))
                            _vm.CloseDocumentCommand.Execute(pathClose);
                    });
                }
                return "OK";
            case Services.IdeCommands.TogglePinDocument:
                if (string.IsNullOrWhiteSpace(S(args, "file_path")))
                    return "Missing file_path";
                {
                    var pathPin = S(args, "file_path")!;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_vm.TogglePinDocumentCommand.CanExecute(pathPin))
                            _vm.TogglePinDocumentCommand.Execute(pathPin);
                    });
                }
                return "OK";
            case Services.IdeCommands.MoveDocumentToGroup1:
                if (string.IsNullOrWhiteSpace(S(args, "file_path")))
                    return "Missing file_path";
                {
                    var p1 = S(args, "file_path")!;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_vm.MoveDocumentToGroup1Command.CanExecute(p1))
                            _vm.MoveDocumentToGroup1Command.Execute(p1);
                    });
                }
                return "OK";
            case Services.IdeCommands.MoveDocumentToGroup2:
                if (string.IsNullOrWhiteSpace(S(args, "file_path")))
                    return "Missing file_path";
                {
                    var p2 = S(args, "file_path")!;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_vm.MoveDocumentToGroup2Command.CanExecute(p2))
                            _vm.MoveDocumentToGroup2Command.Execute(p2);
                    });
                }
                return "OK";
            case Services.IdeCommands.MoveDocumentToGroup3:
                if (string.IsNullOrWhiteSpace(S(args, "file_path")))
                    return "Missing file_path";
                {
                    var p3 = S(args, "file_path")!;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_vm.MoveDocumentToGroup3Command.CanExecute(p3))
                            _vm.MoveDocumentToGroup3Command.Execute(p3);
                    });
                }
                return "OK";

            case Services.IdeCommands.GetUiTheme:
                return a.GetUiTheme();
            case Services.IdeCommands.SetUiTheme:
                return await a.SetUiThemeAsync(S(args, "theme") ?? "");
            case Services.IdeCommands.GetUiLayout:
                return await a.GetUiLayoutAsync();
            case Services.IdeCommands.GetColorsUnderCursor:
                return await a.GetColorsUnderCursorAsync();
            case Services.IdeCommands.GetControlAppearance:
                return await a.GetControlAppearanceAsync(S(args, "name"));
            case Services.IdeCommands.SetControlLayout:
                if (args is null || string.IsNullOrEmpty(S(args, "name"))) return "Missing name or layout";
                return await a.SetControlLayoutAsync(S(args, "name")!, S(args, "layout") ?? "{}");
            case Services.IdeCommands.SetControlText:
                return await a.SetControlTextAsync(S(args, "name") ?? "", S(args, "text") ?? "");
            case Services.IdeCommands.ClickControl:
                return await a.ClickControlAsync(S(args, "name"));
            case Services.IdeCommands.SendKeys:
                return await a.SendKeysAsync(S(args, "name"), S(args, "keys") ?? "");
            case Services.IdeCommands.SetFocus:
                return await a.SetFocusAsync(S(args, "name"));
            case Services.IdeCommands.HighlightControl:
                return await a.HighlightControlAsync(S(args, "name"));
            case Services.IdeCommands.SetPanelSize:
                double? w = args is not null && args.TryGetValue("width", out var pw) && pw.TryGetDouble(out var wv) ? wv : null;
                double? h = args is not null && args.TryGetValue("height", out var ph) && ph.TryGetDouble(out var hv) ? hv : null;
                return await a.SetPanelSizeAsync(S(args, "panel") ?? "", w, h);
            case Services.IdeCommands.GetSupportedEditorLanguages:
                return a.GetSupportedEditorLanguages();
            case Services.IdeCommands.ShowBreakpoints:
                return ParseAndShowDebugBreakpoints(a, args);
            case Services.IdeCommands.ShowDebugPosition:
                a.ShowDebugPosition(S(args, "file_path"), I(args, "line"));
                return "OK";
            case Services.IdeCommands.ShowDebugState:
                return ParseAndShowDebugState(a, args);
#if DEBUG
            case Services.IdeCommands.AddControl:
                return await a.AddControlAsync(S(args, "parent_name") ?? "", S(args, "control_type") ?? "", S(args, "content"), S(args, "name"));
#endif
            case Services.IdeCommands.WriteAgentNotes:
                return await a.WriteAgentNotesAsync(S(args, "content") ?? "", cancellationToken);
            case Services.IdeCommands.ReadAgentNotes:
                return await a.ReadAgentNotesAsync(cancellationToken);
            default:
                return $"Unknown command: {commandId}";
        }

    }
}

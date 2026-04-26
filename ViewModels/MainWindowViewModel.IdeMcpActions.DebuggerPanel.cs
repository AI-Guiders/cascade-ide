using System.Text.Json;
using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Панель отладки и снимок DAP (ADR 0002): один <see cref="Services.DebugSessionSnapshot"/>.</summary>
public partial class MainWindowViewModel
{
    /// <summary>Один раз на останов (breakpoint/step): показать док инструментов и Mfd «Отладка · стек» — в т.ч. при launch/attach в обход UI (например MCP <c>debug_launch</c>).</summary>
    private bool _mfdDebugPagePrimedForCurrentStop;

    private void ApplyDapDebugSnapshotToUi()
    {
        var s = DapDebug.GetSnapshot();
        if (!s.IsExecutionStopped)
            _mfdDebugPagePrimedForCurrentStop = false;
        else if (!_mfdDebugPagePrimedForCurrentStop)
        {
            _mfdDebugPagePrimedForCurrentStop = true;
            IsInstrumentationDockVisible = true;
            TryNavigateToMfdShellPage(MfdShellPage.DebugStack);
        }
        DebugPositionFile = s.StoppedFile is { } stoppedFile ? Path.GetFullPath(stoppedFile) : null;
        DebugPositionLine = s.StoppedLine;
        if (s.IsExecutionStopped && s.StoppedFile is { } sp && !string.IsNullOrEmpty(sp) && File.Exists(sp))
        {
            var normalized = Path.GetFullPath(sp);
            if (!string.Equals(CurrentFilePath, normalized, StringComparison.OrdinalIgnoreCase))
            {
                IsLoadingCurrentFile = true;
                try
                {
                    Documents.OpenOrActivateDocument(normalized);
                }
                finally
                {
                    IsLoadingCurrentFile = false;
                }
            }
        }
        InstrumentationPanel.DebugStackFrames.Clear();
        var fi = 0;
        foreach (var frame in s.StackFrames)
            InstrumentationPanel.DebugStackFrames.Add(new DebugStackFrameViewModel(fi++, frame.Name, frame.File, frame.Line));
        InstrumentationPanel.DebugVariableRoots.Clear();
        ExpandDebugVariableChildrenAsync expand =
            (vref, iv, nv, ct) => DapDebug.ExpandVariableChildrenAsync(vref, iv, nv, ct);
        foreach (var g in s.VariableRootScopes)
            InstrumentationPanel.DebugVariableRoots.Add(DebugVariableNodeViewModel.CreateScope(g.ScopeName, g.Roots, expand));
        _suppressDebugStackSelectedIndex = true;
        try
        {
            var idx = s is { IsExecutionStopped: true, StackFrames.Count: > 0 } ? s.VariablesFrameIndex : -1;
            if (DebugStackSelectedIndex != idx)
            {
                _debugStackSelectedIndex = idx;
                OnPropertyChanged(nameof(DebugStackSelectedIndex));
            }
        }
        finally
        {
            _suppressDebugStackSelectedIndex = false;
        }
        OnPropertyChanged(nameof(BreakpointLinesInCurrentFile));
        OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
        OnPropertyChanged(nameof(IdeHealthDebugText));
        OnPropertyChanged(nameof(IdeHealthDebugCockpitShort));
    }

    async Task<string> Services.IIdeMcpActions.GetDebugSnapshotAsync(CancellationToken cancellationToken) =>
        await UiScheduler.Default.InvokeAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var s = DapDebug.GetSnapshot();
            return JsonSerializer.Serialize(new
            {
                s.HasActiveSession,
                s.IsExecutionStopped,
                position = new { file = s.StoppedFile, line = s.StoppedLine },
                exception = s.ExceptionText,
                breakpoints = s.Breakpoints.Take(500).Select(b => new { file = b.File, line = b.Line, condition = b.Condition }).ToList(),
                stack_frames = s.StackFrames.Select(frame => new { frame.Name, frame.File, frame.Line }).ToList(),
                variables_frame_index = s.VariablesFrameIndex,
                variable_root_scopes = s.VariableRootScopes.Take(50).Select(g => new
                {
                    scope = g.ScopeName,
                    roots = g.Roots.Take(200).Select(v => new
                    {
                        v.Name,
                        v.Value,
                        v.Type,
                        variables_reference = v.VariablesReference,
                        v.NamedVariables,
                        v.IndexedVariables
                    }).ToList()
                }).ToList()
            });
        });
}

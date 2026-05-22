using CascadeIDE.Features.Debug;
using CascadeIDE.ViewModels;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>Панель отладки и снимок DAP (ADR 0002): один <see cref="DebugSessionSnapshot"/>.</summary>
internal sealed partial class MainWindowIdeMcpHost
{
    /// <summary>Один раз на останов (breakpoint/step): показать док инструментов и Mfd «Отладка · стек» — в т.ч. при launch/attach в обход UI (например MCP <c>debug_launch</c>).</summary>
    private bool _mfdDebugPagePrimedForCurrentStop;

    internal void ApplyDapDebugSnapshotToUi()
    {
        var s = _host.DapDebug.GetSnapshot();
        var plan = IdeMcpDebugOrchestrator.BuildDapSnapshotUiPlan(s, _mfdDebugPagePrimedForCurrentStop);
        _mfdDebugPagePrimedForCurrentStop = plan.MfdPrimedForCurrentStopNext;

        if (plan.ActivateInstrumentationDockAndDebugStack)
        {
            _host.IsInstrumentationDockVisible = true;
            _host.TryNavigateToMfdShellPage(MfdShellPage.DebugStack);
        }

        _host.DebugPositionFile = plan.DebugPositionFile;
        _host.DebugPositionLine = plan.DebugPositionLine;

        if (plan.ShouldAttemptOpenStoppedSource
            && plan.StoppedSourcePathForOpenAttempt is { } stoppedPath
            && !string.IsNullOrEmpty(stoppedPath)
            && File.Exists(stoppedPath))
        {
            var normalized = CanonicalFilePath.Normalize(stoppedPath);
            if (!CanonicalFilePath.Equals(_host.CurrentFilePath, normalized))
            {
                _host.IsLoadingCurrentFile = true;
                try
                {
                    _host.Documents.OpenOrActivateDocument(normalized);
                }
                finally
                {
                    _host.IsLoadingCurrentFile = false;
                }
            }
        }

        _host.InstrumentationPanel.DebugStackFrames.Clear();
        var fi = 0;
        foreach (var frame in s.StackFrames)
            _host.InstrumentationPanel.DebugStackFrames.Add(new DebugStackFrameViewModel(fi++, frame.Name, frame.File, frame.Line));
        _host.InstrumentationPanel.DebugVariableRoots.Clear();
        ExpandDebugVariableChildrenAsync expand =
            (vref, iv, nv, ct) => _host.DapDebug.ExpandVariableChildrenAsync(vref, iv, nv, ct);
        foreach (var g in s.VariableRootScopes)
            _host.InstrumentationPanel.DebugVariableRoots.Add(DebugVariableNodeViewModel.CreateScope(g.ScopeName, g.Roots, expand));
        _host.McpSuppressDebugStackSelectedIndex = true;
        try
        {
            var idx = plan.DebugStackSelectedIndex;
            if (_host.DebugStackSelectedIndex != idx)
            {
                _host.McpDebugStackSelectedIndex = idx;
                _host.McpNotifyPropertyChanged(nameof(MainWindowViewModel.DebugStackSelectedIndex));
            }
        }
        finally
        {
            _host.McpSuppressDebugStackSelectedIndex = false;
        }

        _host.McpNotifyPropertyChanged(nameof(MainWindowViewModel.BreakpointLinesInCurrentFile));
        _host.McpNotifyPropertyChanged(nameof(MainWindowViewModel.AllBreakpointLinesInCurrentFile));
        _host.McpNotifyPropertyChanged(nameof(MainWindowViewModel.IdeHealthDebugText));
        _host.McpNotifyPropertyChanged(nameof(MainWindowViewModel.IdeHealthDebugCockpitShort));
    }

    public Task<string> GetDebugSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var s = _host.DapDebug.GetSnapshot();
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(IdeMcpDebugOrchestrator.SerializeDebugSnapshot(s));
    }
}

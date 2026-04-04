using Avalonia.Threading;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    void Services.IIdeMcpActions.ShowDebugBreakpoints(IReadOnlyList<(string FilePath, int Line)> breakpoints)
    {
        UiScheduler.Default.Post(() =>
        {
            _debuggerBreakpoints.Clear();
            foreach (var (path, line) in breakpoints)
                _debuggerBreakpoints.Add((Path.GetFullPath(path), line));
            OnPropertyChanged(nameof(DebuggerBreakpointLinesInCurrentFile));
            OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
        });
    }

    void Services.IIdeMcpActions.ShowDebugPosition(string? filePath, int line)
    {
        UiScheduler.Default.Post(() =>
        {
            DebugPositionFile = filePath is not null ? Path.GetFullPath(filePath) : null;
            DebugPositionLine = line;
            if (filePath is not null && !string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                var normalized = Path.GetFullPath(filePath);
                if (!string.Equals(CurrentFilePath, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    IsLoadingCurrentFile = true;
                    try
                    {
                        Documents.OpenOrActivateDocument(normalized);
                    }
                    finally { IsLoadingCurrentFile = false; }
                }
            }
        });
    }

    void Services.IIdeMcpActions.ShowDebugState(IReadOnlyList<(string Name, string? File, int Line)> stackFrames, IReadOnlyList<(string Name, string Value)> variables)
    {
        UiScheduler.Default.Post(() =>
        {
            InstrumentationPanel.DebugStackFrames.Clear();
            foreach (var f in stackFrames)
                InstrumentationPanel.DebugStackFrames.Add(new DebugStackFrameViewModel(f.Name, f.File, f.Line));
            InstrumentationPanel.DebugVariables.Clear();
            foreach (var v in variables)
                InstrumentationPanel.DebugVariables.Add(new DebugVariableViewModel(v.Name, v.Value));
            OnPropertyChanged(nameof(TelemetryDebugText));
            OnPropertyChanged(nameof(TelemetryDebugCockpitShort));
        });
    }
}

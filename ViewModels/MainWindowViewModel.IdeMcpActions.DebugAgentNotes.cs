using System.IO;
using Avalonia.Threading;
using CascadeIDE.Features.Instrumentation;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    void Services.IIdeMcpActions.ShowDebugBreakpoints(IReadOnlyList<(string FilePath, int Line)> breakpoints)
    {
        Dispatcher.UIThread.Post(() =>
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
        Dispatcher.UIThread.Post(() =>
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
                        OpenOrActivateDocument(normalized);
                    }
                    finally { IsLoadingCurrentFile = false; }
                }
            }
        });
    }

    void Services.IIdeMcpActions.ShowDebugState(IReadOnlyList<(string Name, string? File, int Line)> stackFrames, IReadOnlyList<(string Name, string Value)> variables)
    {
        Dispatcher.UIThread.Post(() =>
        {
            InstrumentationPanel.DebugStackFrames.Clear();
            foreach (var f in stackFrames)
                InstrumentationPanel.DebugStackFrames.Add(new DebugStackFrameViewModel(f.Name, f.File, f.Line));
            InstrumentationPanel.DebugVariables.Clear();
            foreach (var v in variables)
                InstrumentationPanel.DebugVariables.Add(new DebugVariableViewModel(v.Name, v.Value));
        });
    }

    private const string AgentNotesFileName = "agent-notes.md";

    Task<string> Services.IIdeMcpActions.WriteAgentNotesAsync(string content, CancellationToken cancellationToken)
    {
        var solutionPath = SolutionPath;
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            return Task.FromResult("Error: solution not loaded. Open a solution first.");
        var solutionDir = Path.GetDirectoryName(solutionPath);
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("Error: invalid solution path.");
        var dir = Path.Combine(solutionDir, ".cascade-ide");
        var filePath = Path.Combine(dir, AgentNotesFileName);
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, content, System.Text.Encoding.UTF8);
            return Task.FromResult("OK");
        }
        catch (Exception ex)
        {
            return Task.FromResult("Error: " + ex.Message);
        }
    }

    Task<string> Services.IIdeMcpActions.ReadAgentNotesAsync(CancellationToken cancellationToken)
    {
        var solutionPath = SolutionPath;
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            return Task.FromResult("");
        var solutionDir = Path.GetDirectoryName(solutionPath);
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("");
        var filePath = Path.Combine(solutionDir, ".cascade-ide", AgentNotesFileName);
        if (!File.Exists(filePath))
            return Task.FromResult("");
        try
        {
            return Task.FromResult(File.ReadAllText(filePath, System.Text.Encoding.UTF8));
        }
        catch
        {
            return Task.FromResult("");
        }
    }
}

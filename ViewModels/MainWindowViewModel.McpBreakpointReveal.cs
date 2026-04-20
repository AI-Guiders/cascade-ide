#nullable enable

namespace CascadeIDE.ViewModels;

/// <summary>MCP: постановка брейкпоинта с загрузкой решения и показом строки в редакторе.</summary>
public partial class MainWindowViewModel
{
    /// <summary>Регистрация брейкпоинта в памяти IDE и в JSON для dotnet-debug-mcp (без открытия файла).</summary>
    private void RegisterIdeMcpBreakpoint(string filePath, int line, string? condition)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1)
            return;
        var path = Path.GetFullPath(filePath);
        if (_breakpoints.Any(b => string.Equals(Path.GetFullPath(b.FilePath), path, StringComparison.OrdinalIgnoreCase) && b.Line == line))
            return;
        _breakpoints.Add((path, line));
        var ws = GetWorkspacePath();
        if (!string.IsNullOrEmpty(ws))
            BreakpointsFileService.SetBreakpointForDefaultTarget(ws, path, line, condition);
        OnPropertyChanged(nameof(BreakpointLinesInCurrentFile));
        OnPropertyChanged(nameof(McpFileBreakpointLinesInCurrentFile));
        OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
    }

    /// <summary>Полный сценарий для MCP <c>set_breakpoint</c>: при необходимости загрузка решения, регистрация точки, открытие файла и переход к строке.</summary>
    internal async Task<string> CompleteMcpSetBreakpointAsync(string filePath, int line, string? condition, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(filePath) || line < 1)
            return "Missing file_path or line";
        string path;
        try
        {
            path = Path.GetFullPath(filePath);
        }
        catch
        {
            return "Invalid file_path";
        }

        if (!File.Exists(path))
            return "File not found";

        var sln = SolutionFileLocator.TryFindSolutionForSourceFile(path);
        if (SolutionFileLocator.NeedsLoadSolutionBeforeBreakpoint(sln, Workspace.SolutionPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await LoadSolutionAsync(sln!).ConfigureAwait(false);
        }

        await UiScheduler.Default.InvokeAsync(() =>
        {
            RegisterIdeMcpBreakpoint(path, line, condition);
            ((Services.IIdeMcpActions)this).GoToPosition(path, line, 1, line, 1);
            _focusEditorAction?.Invoke();
        }).ConfigureAwait(false);

        return "OK";
    }
}

#nullable enable

using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: постановка брейкпоинта с загрузкой решения и показом строки в редакторе.</summary>
public partial class MainWindowViewModel
{
    /// <summary>Полный сценарий для MCP <c>set_breakpoint</c>: при необходимости загрузка решения, регистрация точки, открытие файла и переход к строке.</summary>
    internal async Task<string> CompleteMcpSetBreakpointAsync(string filePath, int line, string? condition, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(filePath) || line < 1)
            return "Missing file_path or line";
        string path;
        try
        {
            path = CanonicalFilePath.Normalize(filePath);
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
            Editor.RegisterIdeMcpBreakpoint(path, line, condition);
            IdeMcp.GoToPosition(path, line, 1, line, 1);
            _focusEditorAction?.Invoke();
        }).ConfigureAwait(false);

        return "OK";
    }

    internal async void ResyncDapBreakpointsFireAndForget()
    {
        try
        {
            await DapDebug.ResyncBreakpointsFromStorageAsync().ConfigureAwait(false);
        }
        catch
        {
            // best-effort: нет сессии или DAP занят
        }
    }
}

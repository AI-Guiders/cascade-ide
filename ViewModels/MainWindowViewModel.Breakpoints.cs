using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Брейкпоинты (IDE + .dotnet-debug-mcp-breakpoints.json + отладчик) и подсветка строки остановки.
/// </summary>
public partial class MainWindowViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMarkdownFile))]
    [NotifyPropertyChangedFor(nameof(IsMarkdownPreviewVisible))]
    [NotifyPropertyChangedFor(nameof(BreakpointLinesInCurrentFile))]
    [NotifyPropertyChangedFor(nameof(DebuggerBreakpointLinesInCurrentFile))]
    [NotifyPropertyChangedFor(nameof(McpFileBreakpointLinesInCurrentFile))]
    [NotifyPropertyChangedFor(nameof(AllBreakpointLinesInCurrentFile))]
    [NotifyPropertyChangedFor(nameof(DebugCurrentLineInCurrentFile))]
    private string? _currentFilePath;

    private readonly List<(string FilePath, int Line)> _breakpoints = [];
    private readonly List<(string FilePath, int Line)> _debuggerBreakpoints = [];
    private FileSystemWatcher? _breakpointsFileWatcher;

    /// <summary>Номера строк с брейкпоинтами в текущем открытом файле (для отрисовки в редакторе).</summary>
    public IReadOnlyList<int> BreakpointLinesInCurrentFile
    {
        get
        {
            var current = CurrentFilePath;
            if (string.IsNullOrEmpty(current))
                return [];
            var normalized = Path.GetFullPath(current);
            return _breakpoints
                .Where(b => string.Equals(Path.GetFullPath(b.FilePath), normalized, StringComparison.OrdinalIgnoreCase))
                .Select(b => b.Line)
                .OrderBy(static l => l)
                .Distinct()
                .ToList();
        }
    }

    /// <summary>Строки с брейкпоинтами отладчика (ide_show_breakpoints) в текущем файле.</summary>
    public IReadOnlyList<int> DebuggerBreakpointLinesInCurrentFile
    {
        get
        {
            var current = CurrentFilePath;
            if (string.IsNullOrEmpty(current))
                return [];
            var normalized = Path.GetFullPath(current);
            return _debuggerBreakpoints
                .Where(b => string.Equals(Path.GetFullPath(b.FilePath), normalized, StringComparison.OrdinalIgnoreCase))
                .Select(b => b.Line)
                .OrderBy(static l => l)
                .Distinct()
                .ToList();
        }
    }

    /// <summary>Строки с брейкпоинтами из .dotnet-debug-mcp-breakpoints.json в текущем файле.</summary>
    public IReadOnlyList<int> McpFileBreakpointLinesInCurrentFile
    {
        get
        {
            var ws = GetWorkspacePath();
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(CurrentFilePath))
                return [];
            return Services.BreakpointsFileService.GetLinesForFile(ws, CurrentFilePath);
        }
    }

    /// <summary>
    /// Все брейкпоинты (IDE + отладчик + JSON) для указанного файла.
    /// Нужен для отрисовки в каждой вкладке: AllBreakpointLinesInCurrentFile привязан к активному файлу и не подходит для неактивных редакторов.
    /// </summary>
    public IReadOnlyList<int> GetAllBreakpointLinesForFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return [];
        var normalized = Path.GetFullPath(filePath);
        var fromIde = _breakpoints
            .Where(b => string.Equals(Path.GetFullPath(b.FilePath), normalized, StringComparison.OrdinalIgnoreCase))
            .Select(b => b.Line);
        var fromDbg = _debuggerBreakpoints
            .Where(b => string.Equals(Path.GetFullPath(b.FilePath), normalized, StringComparison.OrdinalIgnoreCase))
            .Select(b => b.Line);
        var ws = GetWorkspacePath();
        var fromFile = string.IsNullOrEmpty(ws)
            ? []
            : Services.BreakpointsFileService.GetLinesForFile(ws, filePath);
        return fromIde.Union(fromDbg).Union(fromFile).OrderBy(static l => l).Distinct().ToList();
    }

    /// <summary>Все брейкпоинты (IDE + отладчик + файл MCP) в текущем файле для отрисовки.</summary>
    public IReadOnlyList<int> AllBreakpointLinesInCurrentFile =>
        GetAllBreakpointLinesForFile(CurrentFilePath);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DebugCurrentLineInCurrentFile))]
    private string? _debugPositionFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DebugCurrentLineInCurrentFile))]
    private int _debugPositionLine;

    /// <summary>Строка подсветки остановки отладчика для указанного файла (0 если не тот файл или сброшено).</summary>
    public int GetDebugCurrentLineForFile(string? filePath)
    {
        if (string.IsNullOrEmpty(DebugPositionFile) || string.IsNullOrEmpty(filePath))
            return 0;
        if (!string.Equals(Path.GetFullPath(DebugPositionFile), Path.GetFullPath(filePath), StringComparison.OrdinalIgnoreCase))
            return 0;
        return DebugPositionLine;
    }

    /// <summary>Номер строки текущей позиции отладки в открытом файле (0 если другой файл или сброшено).</summary>
    public int DebugCurrentLineInCurrentFile => GetDebugCurrentLineForFile(CurrentFilePath);

    private void AttachBreakpointsFileWatcher(string? solutionPath)
    {
        _breakpointsFileWatcher?.Dispose();
        _breakpointsFileWatcher = null;
        var ws = GetWorkspacePath(solutionPath);
        if (string.IsNullOrEmpty(ws) || !Directory.Exists(ws))
            return;
        try
        {
            _breakpointsFileWatcher = new FileSystemWatcher(ws)
            {
                Filter = Services.BreakpointsFileService.FileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };
            _breakpointsFileWatcher.Changed += (_, _) => Dispatcher.UIThread.Post(() =>
            {
                OnPropertyChanged(nameof(McpFileBreakpointLinesInCurrentFile));
                OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
            });
            _breakpointsFileWatcher.Renamed += (_, _) => Dispatcher.UIThread.Post(() =>
            {
                OnPropertyChanged(nameof(McpFileBreakpointLinesInCurrentFile));
                OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
            });
            _breakpointsFileWatcher.EnableRaisingEvents = true;
        }
        catch { /* нет прав или диск недоступен */ }
    }
}

using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Брейкпоинты: <see cref="BreakpointsFileService"/> / <see cref="DotnetDebug.Core.BreakpointsStorage"/> — один источник (ADR 0002).
/// </summary>
public partial class MainWindowViewModel
{
    private static readonly string[] BreakpointGlyphBindingNames =
    [
        nameof(BreakpointLinesInCurrentFile),
        nameof(AllBreakpointLinesInCurrentFile),
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMarkdownFile))]
    [NotifyPropertyChangedFor(nameof(IsMarkdownPreviewVisible))]
    [NotifyPropertyChangedFor(nameof(BreakpointLinesInCurrentFile))]
    [NotifyPropertyChangedFor(nameof(AllBreakpointLinesInCurrentFile))]
    [NotifyPropertyChangedFor(nameof(DebugCurrentLineInCurrentFile))]
    private string? _currentFilePath;

    private FileSystemWatcher? _breakpointsFileWatcher;

    /// <summary>Номера строк с брейкпоинтами в текущем открытом файле (глифы в редакторе).</summary>
    public IReadOnlyList<int> BreakpointLinesInCurrentFile => AllBreakpointLinesInCurrentFile;

    /// <summary>Все брейкпоинты для указанного файла (все target в JSON workspace).</summary>
    public IReadOnlyList<int> GetAllBreakpointLinesForFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return [];
        var normalized = Path.GetFullPath(filePath);
        return DapDebug.GetSnapshot().Breakpoints
            .Where(b => string.Equals(Path.GetFullPath(b.File), normalized, StringComparison.OrdinalIgnoreCase))
            .Select(b => b.Line)
            .OrderBy(static line => line)
            .Distinct()
            .ToList();
    }

    /// <summary>Все брейкпоинты в текущем файле для отрисовки.</summary>
    public IReadOnlyList<int> AllBreakpointLinesInCurrentFile =>
        GetAllBreakpointLinesForFile(CurrentFilePath);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DebugCurrentLineInCurrentFile))]
    private string? _debugPositionFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DebugCurrentLineInCurrentFile))]
    private int _debugPositionLine;

    /// <summary>Строка подсветки останова отладчика для указанного файла (0 если не тот файл или сброшено).</summary>
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

    private void NotifyBreakpointGlyphBindings()
    {
        foreach (var name in BreakpointGlyphBindingNames)
            OnPropertyChanged(name);
    }

    private void RefreshBreakpointSnapshotFromWorkspace(string? solutionPath)
    {
        var ws = GetWorkspacePath(solutionPath);
        DapDebug.RefreshBreakpointSnapshotFromStorage(ws);
    }

    private void AttachBreakpointsFileWatcher(string? solutionPath)
    {
        _breakpointsFileWatcher?.Dispose();
        _breakpointsFileWatcher = null;
        var ws = GetWorkspacePath(solutionPath);
        RefreshBreakpointSnapshotFromWorkspace(solutionPath);
        if (string.IsNullOrEmpty(ws) || !Directory.Exists(ws))
            return;
        try
        {
            _breakpointsFileWatcher = new FileSystemWatcher(ws)
            {
                Filter = Services.BreakpointsFileService.FileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };
            _breakpointsFileWatcher.Changed += (_, _) => UiScheduler.Default.Post(() =>
            {
                RefreshBreakpointSnapshotFromWorkspace(solutionPath);
                NotifyBreakpointGlyphBindings();
            });
            _breakpointsFileWatcher.Renamed += (_, _) => UiScheduler.Default.Post(() =>
            {
                RefreshBreakpointSnapshotFromWorkspace(solutionPath);
                NotifyBreakpointGlyphBindings();
            });
            _breakpointsFileWatcher.EnableRaisingEvents = true;
        }
        catch { /* нет прав или диск недоступен */ }
    }
}

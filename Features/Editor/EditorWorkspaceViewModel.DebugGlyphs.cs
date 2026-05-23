using System.IO;
using Avalonia.Threading;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.Features.Editor;

/// <summary>Глифы брейкпоинтов и подсветка текущей строки DAP в редакторе.</summary>
public sealed partial class EditorWorkspaceViewModel
{
    private static readonly string[] BreakpointGlyphBindingNames =
    [
        nameof(BreakpointLinesInCurrentFile),
        nameof(AllBreakpointLinesInCurrentFile),
        nameof(DebugCurrentLineInCurrentFile),
    ];

    private FileSystemWatcher? _breakpointsFileWatcher;

    public IReadOnlyList<int> BreakpointLinesInCurrentFile => AllBreakpointLinesInCurrentFile;

    public IReadOnlyList<int> GetAllBreakpointLinesForFile(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return [];
        var normalized = CanonicalFilePath.Normalize(filePath);
        return _host.DapDebug.GetSnapshot().Breakpoints
            .Where(b => CanonicalFilePath.EqualsNormalized(normalized, b.File))
            .Select(b => b.Line)
            .OrderBy(static line => line)
            .Distinct()
            .ToList();
    }

    public IReadOnlyList<int> AllBreakpointLinesInCurrentFile =>
        GetAllBreakpointLinesForFile(CurrentFilePath);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DebugCurrentLineInCurrentFile))]
    private string? _debugPositionFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DebugCurrentLineInCurrentFile))]
    private int _debugPositionLine;

    public int GetDebugCurrentLineForFile(string? filePath)
    {
        if (string.IsNullOrEmpty(DebugPositionFile) || string.IsNullOrEmpty(filePath))
            return 0;
        if (!CanonicalFilePath.Equals(DebugPositionFile, filePath))
            return 0;
        return DebugPositionLine;
    }

    public int DebugCurrentLineInCurrentFile => GetDebugCurrentLineForFile(CurrentFilePath);

    internal void NotifyBreakpointGlyphBindings()
    {
        foreach (var name in BreakpointGlyphBindingNames)
            OnPropertyChanged(name);
    }

    internal void AttachBreakpointsFileWatcher(string? solutionPath)
    {
        _breakpointsFileWatcher?.Dispose();
        _breakpointsFileWatcher = null;
        var ws = WorkspaceDirectoryFromSolutionPath.Resolve(solutionPath);
        _host.DapDebug.RefreshBreakpointSnapshotFromStorage(ws);
        if (string.IsNullOrEmpty(ws) || !Directory.Exists(ws))
            return;
        try
        {
            _breakpointsFileWatcher = new FileSystemWatcher(ws)
            {
                Filter = BreakpointsFileService.FileName,
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
        catch
        {
        }
    }

    private void RefreshBreakpointSnapshotFromWorkspace(string? solutionPath)
    {
        var ws = WorkspaceDirectoryFromSolutionPath.Resolve(solutionPath);
        _host.DapDebug.RefreshBreakpointSnapshotFromStorage(ws);
    }

    internal void RegisterIdeMcpBreakpoint(string filePath, int line, string? condition)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1)
            return;
        var path = CanonicalFilePath.Normalize(filePath);
        var ws = _host.GetWorkspacePath();
        if (!string.IsNullOrEmpty(ws))
            BreakpointsFileService.SetBreakpointForBundledSampleTarget(ws, path, line, condition);
        NotifyBreakpointGlyphBindings();
        _host.ResyncDapBreakpointsFireAndForget();
    }
}

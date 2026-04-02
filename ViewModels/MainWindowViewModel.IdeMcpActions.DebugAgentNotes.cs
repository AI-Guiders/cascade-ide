using System.IO;
using System.Text.Json;
using AgentNotes.Core;
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

    private readonly NotesStorage _notesStorage = new();

    private string? TryGetSolutionDir()
    {
        var solutionPath = Workspace.SolutionPath;
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            return null;
        return Path.GetDirectoryName(solutionPath);
    }

    Task<string> Services.IIdeMcpActions.WriteAgentNotesAsync(string content, CancellationToken cancellationToken)
    {
        var solutionDir = TryGetSolutionDir();
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("Error: solution not loaded. Open a solution first.");
        try
        {
            var result = _notesStorage.Write(solutionDir, content);
            return Task.FromResult(result == "NO_CHANGES" ? "OK" : result);
        }
        catch (Exception ex)
        {
            return Task.FromResult("Error: " + ex.Message);
        }
    }

    Task<string> Services.IIdeMcpActions.ReadAgentNotesAsync(CancellationToken cancellationToken)
    {
        var solutionDir = TryGetSolutionDir();
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("");
        try
        {
            return Task.FromResult(_notesStorage.Read(solutionDir));
        }
        catch
        {
            return Task.FromResult("");
        }
    }

    Task<string> Services.IIdeMcpActions.AppendAgentNotesAsync(string content, CancellationToken cancellationToken)
    {
        var solutionDir = TryGetSolutionDir();
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("Error: solution not loaded. Open a solution first.");
        try
        {
            var result = _notesStorage.Append(solutionDir, content ?? "");
            return Task.FromResult(result == "NO_CHANGES" ? "OK" : result);
        }
        catch (Exception ex)
        {
            return Task.FromResult("Error: " + ex.Message);
        }
    }

    Task<string> Services.IIdeMcpActions.ListAgentNotesRevisionsAsync(int? limit, CancellationToken cancellationToken)
    {
        var solutionDir = TryGetSolutionDir();
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("[]");
        try
        {
            var resolved = limit is null or <= 0 ? 20 : Math.Min(limit.Value, 200);
            return Task.FromResult(_notesStorage.ListRevisions(solutionDir, resolved));
        }
        catch
        {
            return Task.FromResult("[]");
        }
    }

    Task<string> Services.IIdeMcpActions.RollbackAgentNotesAsync(string? revisionFile, CancellationToken cancellationToken)
    {
        var solutionDir = TryGetSolutionDir();
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("Error: solution not loaded. Open a solution first.");
        try
        {
            return Task.FromResult(_notesStorage.Rollback(solutionDir, revisionFile));
        }
        catch (Exception ex)
        {
            return Task.FromResult("Error: " + ex.Message);
        }
    }

    Task<string> Services.IIdeMcpActions.ReadHotContextAsync(string? activeScope, CancellationToken cancellationToken)
    {
        var solutionDir = TryGetSolutionDir();
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("{\"content\":\"\"}");
        try { return Task.FromResult(_notesStorage.ReadHotContext(solutionDir, activeScope)); }
        catch (Exception ex) { return Task.FromResult("Error: " + ex.Message); }
    }

    Task<string> Services.IIdeMcpActions.RouteContextAsync(string query, string? activeScope, int? maxSections, int? maxChars, CancellationToken cancellationToken)
    {
        var solutionDir = TryGetSolutionDir();
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("{\"assembled_context\":\"\"}");
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult("{\"assembled_context\":\"\"}");
        try
        {
            var ms = maxSections is null or <= 0 ? 5 : Math.Clamp(maxSections.Value, 1, 20);
            var mc = maxChars is null or <= 0 ? 12000 : Math.Clamp(maxChars.Value, 1000, 40000);
            return Task.FromResult(_notesStorage.RouteContext(solutionDir, query, activeScope, ms, mc));
        }
        catch (Exception ex) { return Task.FromResult("Error: " + ex.Message); }
    }

    Task<string> Services.IIdeMcpActions.MemoryHealthAsync(string? activeScope, CancellationToken cancellationToken)
    {
        var solutionDir = TryGetSolutionDir();
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("{\"health\":\"unknown\"}");
        try { return Task.FromResult(_notesStorage.MemoryHealth(solutionDir, activeScope)); }
        catch (Exception ex) { return Task.FromResult("Error: " + ex.Message); }
    }

    Task<string> Services.IIdeMcpActions.CompactHotContextAsync(bool apply, CancellationToken cancellationToken)
    {
        var solutionDir = TryGetSolutionDir();
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("{\"changed\":false}");
        try { return Task.FromResult(_notesStorage.CompactHotContext(solutionDir, apply)); }
        catch (Exception ex) { return Task.FromResult("Error: " + ex.Message); }
    }

    Task<string> Services.IIdeMcpActions.ExtractFromArchiveAsync(string query, string? revisionFile, int? headLimit, int? contextLines, CancellationToken cancellationToken)
    {
        var solutionDir = TryGetSolutionDir();
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("{\"matches\":[]}");
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult("{\"matches\":[]}");
        try
        {
            var hl = headLimit is null or <= 0 ? 10 : Math.Clamp(headLimit.Value, 1, 100);
            var cl = contextLines is null or < 0 ? 2 : Math.Clamp(contextLines.Value, 0, 20);
            return Task.FromResult(_notesStorage.ExtractFromArchive(solutionDir, query, revisionFile, hl, cl));
        }
        catch (Exception ex) { return Task.FromResult("Error: " + ex.Message); }
    }

    Task<string> Services.IIdeMcpActions.UpsertAgentNotesSectionAsync(string sectionId, string content, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sectionId))
            return Task.FromResult("Error: missing section_id.");

        var solutionDir = TryGetSolutionDir();
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("Error: solution not loaded. Open a solution first.");

        try
        {
            var result = _notesStorage.UpsertSection(solutionDir, sectionId.Trim(), content ?? "");
            return Task.FromResult(result == "NO_CHANGES" ? "OK" : result);
        }
        catch (Exception ex)
        {
            return Task.FromResult("Error: " + ex.Message);
        }
    }

    Task<string> Services.IIdeMcpActions.SearchAgentNotesAsync(string query, int? headLimit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult("{\"matches\":[]}");

        var solutionDir = TryGetSolutionDir();
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("{\"matches\":[]}");

        try
        {
            var limit = headLimit is null or <= 0 ? 20 : Math.Min(headLimit.Value, 200);
            return Task.FromResult(_notesStorage.Search(solutionDir, query, limit));
        }
        catch
        {
            return Task.FromResult("{\"matches\":[]}");
        }
    }

    Task<string> Services.IIdeMcpActions.ReadKnowledgeFileAsync(string filePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult("");
        try
        {
            // Canon-only: uses AGENT_NOTES_CANON_PATH unless caller passed canon_path via a future arg.
            return Task.FromResult(_notesStorage.ReadKnowledgeFile(canonPath: null, filePath));
        }
        catch (Exception ex)
        {
            return Task.FromResult("Error: " + ex.Message);
        }
    }

    Task<string> Services.IIdeMcpActions.ListKnowledgeFilesAsync(string? subdir, CancellationToken cancellationToken)
    {
        try
        {
            return Task.FromResult(_notesStorage.ListKnowledgeFiles(canonPath: null, subdir));
        }
        catch (Exception ex)
        {
            return Task.FromResult("{\"error\":\"" + ex.Message.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"}");
        }
    }
}

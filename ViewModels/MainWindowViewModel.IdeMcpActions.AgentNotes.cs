using System.IO;
using System.Text.Json;
using AgentNotes.Core;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    private readonly NotesStorage _notesStorage = new();

    private string? TryGetSolutionDir()
    {
        var solutionPath = Workspace.SolutionPath;
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            return null;
        return Path.GetDirectoryName(solutionPath);
    }

    /// <summary>
    /// Workspace path passed to <see cref="NotesStorage"/> (GetNotesPath / Read / Write).
    /// Prefer solution directory; if no solution but <c>AGENT_NOTES_FILE</c> is set, use current directory so global notes still resolve (parity with agent-notes-mcp).
    /// </summary>
    private string? TryGetNotesWorkspacePath()
    {
        var solutionDir = TryGetSolutionDir();
        if (!string.IsNullOrEmpty(solutionDir))
            return solutionDir;

        var globalNotes = Environment.GetEnvironmentVariable("AGENT_NOTES_FILE");
        if (!string.IsNullOrWhiteSpace(globalNotes))
            return Environment.CurrentDirectory;

        return null;
    }

    private static string NotesWorkspaceRequiredMessage =>
        "Error: no notes workspace. Open a solution, or set AGENT_NOTES_FILE to a global agent-notes path.";

    Task<string> Services.IIdeMcpActions.WriteAgentNotesAsync(string content, CancellationToken cancellationToken)
    {
        var workspace = TryGetNotesWorkspacePath();
        if (string.IsNullOrEmpty(workspace))
            return Task.FromResult(NotesWorkspaceRequiredMessage);
        try
        {
            var result = _notesStorage.Write(workspace, content);
            return Task.FromResult(result == "NO_CHANGES" ? "OK" : result);
        }
        catch (Exception ex)
        {
            return Task.FromResult("Error: " + ex.Message);
        }
    }

    Task<string> Services.IIdeMcpActions.ReadAgentNotesAsync(CancellationToken cancellationToken)
    {
        var workspace = TryGetNotesWorkspacePath();
        if (string.IsNullOrEmpty(workspace))
            return Task.FromResult("");
        try
        {
            return Task.FromResult(_notesStorage.Read(workspace));
        }
        catch
        {
            return Task.FromResult("");
        }
    }

    Task<string> Services.IIdeMcpActions.AppendAgentNotesAsync(string content, CancellationToken cancellationToken)
    {
        var workspace = TryGetNotesWorkspacePath();
        if (string.IsNullOrEmpty(workspace))
            return Task.FromResult(NotesWorkspaceRequiredMessage);
        try
        {
            var result = _notesStorage.Append(workspace, content ?? "");
            return Task.FromResult(result == "NO_CHANGES" ? "OK" : result);
        }
        catch (Exception ex)
        {
            return Task.FromResult("Error: " + ex.Message);
        }
    }

    Task<string> Services.IIdeMcpActions.ListAgentNotesRevisionsAsync(int? limit, CancellationToken cancellationToken)
    {
        var workspace = TryGetNotesWorkspacePath();
        if (string.IsNullOrEmpty(workspace))
            return Task.FromResult("[]");
        try
        {
            var resolved = limit is null or <= 0 ? 20 : Math.Min(limit.Value, 200);
            return Task.FromResult(_notesStorage.ListRevisions(workspace, resolved));
        }
        catch
        {
            return Task.FromResult("[]");
        }
    }

    Task<string> Services.IIdeMcpActions.RollbackAgentNotesAsync(string? revisionFile, CancellationToken cancellationToken)
    {
        var workspace = TryGetNotesWorkspacePath();
        if (string.IsNullOrEmpty(workspace))
            return Task.FromResult(NotesWorkspaceRequiredMessage);
        try
        {
            return Task.FromResult(_notesStorage.Rollback(workspace, revisionFile));
        }
        catch (Exception ex)
        {
            return Task.FromResult("Error: " + ex.Message);
        }
    }

    Task<string> Services.IIdeMcpActions.ReadHotContextAsync(string? activeScope, CancellationToken cancellationToken)
    {
        var workspace = TryGetNotesWorkspacePath();
        if (string.IsNullOrEmpty(workspace))
            return Task.FromResult("{\"content\":\"\"}");
        try { return Task.FromResult(_notesStorage.ReadHotContext(workspace, activeScope)); }
        catch (Exception ex) { return Task.FromResult("Error: " + ex.Message); }
    }

    Task<string> Services.IIdeMcpActions.RouteContextAsync(string query, string? activeScope, int? maxSections, int? maxChars, CancellationToken cancellationToken)
    {
        var workspace = TryGetNotesWorkspacePath();
        if (string.IsNullOrEmpty(workspace))
            return Task.FromResult("{\"assembled_context\":\"\"}");
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult("{\"assembled_context\":\"\"}");
        try
        {
            var ms = maxSections is null or <= 0 ? 5 : Math.Clamp(maxSections.Value, 1, 20);
            var mc = maxChars is null or <= 0 ? 12000 : Math.Clamp(maxChars.Value, 1000, 40000);
            return Task.FromResult(_notesStorage.RouteContext(workspace, query, activeScope, ms, mc));
        }
        catch (Exception ex) { return Task.FromResult("Error: " + ex.Message); }
    }

    Task<string> Services.IIdeMcpActions.MemoryHealthAsync(string? activeScope, CancellationToken cancellationToken)
    {
        var workspace = TryGetNotesWorkspacePath();
        if (string.IsNullOrEmpty(workspace))
            return Task.FromResult("{\"health\":\"unknown\"}");
        try { return Task.FromResult(_notesStorage.MemoryHealth(workspace, activeScope)); }
        catch (Exception ex) { return Task.FromResult("Error: " + ex.Message); }
    }

    Task<string> Services.IIdeMcpActions.CompactHotContextAsync(bool apply, CancellationToken cancellationToken)
    {
        var workspace = TryGetNotesWorkspacePath();
        if (string.IsNullOrEmpty(workspace))
            return Task.FromResult("{\"changed\":false}");
        try { return Task.FromResult(_notesStorage.CompactHotContext(workspace, apply)); }
        catch (Exception ex) { return Task.FromResult("Error: " + ex.Message); }
    }

    Task<string> Services.IIdeMcpActions.ExtractFromArchiveAsync(string query, string? revisionFile, int? headLimit, int? contextLines, CancellationToken cancellationToken)
    {
        var workspace = TryGetNotesWorkspacePath();
        if (string.IsNullOrEmpty(workspace))
            return Task.FromResult("{\"matches\":[]}");
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult("{\"matches\":[]}");
        try
        {
            var hl = headLimit is null or <= 0 ? 10 : Math.Clamp(headLimit.Value, 1, 100);
            var cl = contextLines is null or < 0 ? 2 : Math.Clamp(contextLines.Value, 0, 20);
            return Task.FromResult(_notesStorage.ExtractFromArchive(workspace, query, revisionFile, hl, cl));
        }
        catch (Exception ex) { return Task.FromResult("Error: " + ex.Message); }
    }

    Task<string> Services.IIdeMcpActions.UpsertAgentNotesSectionAsync(string sectionId, string content, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sectionId))
            return Task.FromResult("Error: missing section_id.");

        var workspace = TryGetNotesWorkspacePath();
        if (string.IsNullOrEmpty(workspace))
            return Task.FromResult(NotesWorkspaceRequiredMessage);

        try
        {
            var result = _notesStorage.UpsertSection(workspace, sectionId.Trim(), content ?? "");
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

        var workspace = TryGetNotesWorkspacePath();
        if (string.IsNullOrEmpty(workspace))
            return Task.FromResult("{\"matches\":[]}");

        try
        {
            var limit = headLimit is null or <= 0 ? 20 : Math.Min(headLimit.Value, 200);
            return Task.FromResult(_notesStorage.Search(workspace, query, limit));
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

    Task<string> Services.IIdeMcpActions.WriteKnowledgeFileAsync(string filePath, string content, string? canonPath, bool saveRevision, CancellationToken cancellationToken)
    {
        try
        {
            return Task.FromResult(_notesStorage.WriteKnowledgeFile(canonPath, filePath, content, saveRevision));
        }
        catch (Exception ex)
        {
            return Task.FromResult("Error: " + ex.Message);
        }
    }

    Task<string> Services.IIdeMcpActions.AppendKnowledgeFileAsync(string filePath, string content, string? canonPath, bool saveRevision, CancellationToken cancellationToken)
    {
        try
        {
            return Task.FromResult(_notesStorage.AppendKnowledgeFile(canonPath, filePath, content, saveRevision));
        }
        catch (Exception ex)
        {
            return Task.FromResult("Error: " + ex.Message);
        }
    }

    Task<string> Services.IIdeMcpActions.UpsertKnowledgeSectionAsync(string filePath, string sectionId, string content, string? canonPath, bool saveRevision, CancellationToken cancellationToken)
    {
        try
        {
            return Task.FromResult(_notesStorage.UpsertKnowledgeSection(canonPath, filePath, sectionId, content, saveRevision));
        }
        catch (Exception ex)
        {
            return Task.FromResult("Error: " + ex.Message);
        }
    }

    Task<string> Services.IIdeMcpActions.DeleteKnowledgeFileAsync(string filePath, string? canonPath, CancellationToken cancellationToken)
    {
        try
        {
            return Task.FromResult(_notesStorage.DeleteKnowledgeFile(canonPath, filePath));
        }
        catch (Exception ex)
        {
            return Task.FromResult("Error: " + ex.Message);
        }
    }

    Task<string> Services.IIdeMcpActions.DeleteKnowledgeSectionAsync(string filePath, string sectionId, string? canonPath, CancellationToken cancellationToken)
    {
        try
        {
            return Task.FromResult(_notesStorage.DeleteKnowledgeSection(canonPath, filePath, sectionId));
        }
        catch (Exception ex)
        {
            return Task.FromResult("Error: " + ex.Message);
        }
    }
}

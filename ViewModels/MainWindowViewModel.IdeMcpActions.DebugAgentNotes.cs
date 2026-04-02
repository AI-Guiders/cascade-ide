using System.IO;
using System.Text.Json;
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

    private static string SectionStart(string sectionId) => $"<!-- section:{sectionId} -->";
    private static string SectionEnd(string sectionId) => $"<!-- /section:{sectionId} -->";

    private static string UpsertSection(string original, string sectionId, string content)
    {
        original ??= "";
        var startMarker = SectionStart(sectionId);
        var endMarker = SectionEnd(sectionId);

        var start = original.IndexOf(startMarker, StringComparison.Ordinal);
        var end = start < 0 ? -1 : original.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);

        var block =
$@"{startMarker}
{content}
{endMarker}";

        if (start >= 0 && end >= 0 && end > start)
        {
            var afterEnd = end + endMarker.Length;
            // Preserve trailing newline style loosely; always ensure newline between parts.
            return original[..start].TrimEnd('\r', '\n') + "\n" + block + "\n" + original[afterEnd..].TrimStart('\r', '\n');
        }

        // Append new section at end.
        var prefix = original.TrimEnd('\r', '\n');
        return (prefix.Length == 0 ? "" : prefix + "\n\n") + block + "\n";
    }

    private static string ToIsoUtc(DateTime dt) =>
        dt.Kind == DateTimeKind.Utc ? dt.ToString("O") : dt.ToUniversalTime().ToString("O");

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
        var solutionDir = TryGetSolutionDir();
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

    Task<string> Services.IIdeMcpActions.UpsertAgentNotesSectionAsync(string sectionId, string content, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sectionId))
            return Task.FromResult("Error: missing section_id.");

        var solutionDir = TryGetSolutionDir();
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("Error: solution not loaded. Open a solution first.");

        var dir = Path.Combine(solutionDir, ".cascade-ide");
        var filePath = Path.Combine(dir, AgentNotesFileName);

        try
        {
            Directory.CreateDirectory(dir);
            var original = File.Exists(filePath) ? File.ReadAllText(filePath, System.Text.Encoding.UTF8) : "";
            var updated = UpsertSection(original, sectionId.Trim(), content ?? "");
            File.WriteAllText(filePath, updated, System.Text.Encoding.UTF8);
            return Task.FromResult("OK");
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

        var filePath = Path.Combine(solutionDir, ".cascade-ide", AgentNotesFileName);
        if (!File.Exists(filePath))
            return Task.FromResult("{\"matches\":[]}");

        try
        {
            var text = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            var lines = (text ?? "").Replace("\r\n", "\n").Split('\n');
            var limit = headLimit is null or <= 0 ? 20 : Math.Min(headLimit.Value, 200);
            var matches = new List<Dictionary<string, object?>>(Math.Min(limit, 32));
            for (var i = 0; i < lines.Length && matches.Count < limit; i++)
            {
                if (lines[i].IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matches.Add(new Dictionary<string, object?>
                    {
                        ["line"] = i + 1,
                        ["text"] = lines[i]
                    });
                }
            }
            return Task.FromResult(JsonSerializer.Serialize(new Dictionary<string, object?> { ["matches"] = matches }));
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

        var solutionDir = TryGetSolutionDir();
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("");

        // Keep it relative to knowledge/; disallow path traversal.
        var rel = filePath.Replace('\\', '/').TrimStart('/');
        if (rel.Contains("..", StringComparison.Ordinal))
            return Task.FromResult("");

        var full = Path.Combine(solutionDir, "knowledge", rel.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(full))
            return Task.FromResult("");

        try { return Task.FromResult(File.ReadAllText(full, System.Text.Encoding.UTF8)); }
        catch { return Task.FromResult(""); }
    }

    Task<string> Services.IIdeMcpActions.ListKnowledgeFilesAsync(string? subdir, CancellationToken cancellationToken)
    {
        var solutionDir = TryGetSolutionDir();
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("{\"files\":[]}");

        var rel = (subdir ?? "").Replace('\\', '/').Trim('/');
        if (rel.Contains("..", StringComparison.Ordinal))
            return Task.FromResult("{\"files\":[]}");

        var root = Path.Combine(solutionDir, "knowledge", rel.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(root))
            return Task.FromResult("{\"files\":[]}");

        try
        {
            var list = new List<Dictionary<string, object?>>(256);
            foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                var fi = new FileInfo(f);
                var relPath = Path.GetRelativePath(Path.Combine(solutionDir, "knowledge"), f).Replace('\\', '/');
                list.Add(new Dictionary<string, object?>
                {
                    ["path"] = relPath,
                    ["size_bytes"] = fi.Length,
                    ["modified_utc"] = ToIsoUtc(fi.LastWriteTimeUtc)
                });
                if (list.Count >= 2000) break;
            }

            return Task.FromResult(JsonSerializer.Serialize(new Dictionary<string, object?> { ["files"] = list }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(JsonSerializer.Serialize(new Dictionary<string, object?> { ["error"] = ex.Message, ["files"] = Array.Empty<object>() }));
        }
    }
}

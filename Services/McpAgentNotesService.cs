#nullable enable
using System.Text.Json;
using AgentNotes.Core;
using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>
/// Обёртка над <see cref="NotesStorage"/> для MCP-команд заметок и knowledge: разрешение workspace,
/// те же ответы/ошибки, что раньше собирались во ViewModel.
/// </summary>
public sealed class McpAgentNotesService
{
    private static readonly JsonDocumentOptions JsonDocOptions = new() { CommentHandling = JsonCommentHandling.Skip };

    private readonly Func<CascadeIdeSettings> _settingsProvider;
    private readonly NotesStorage _storage;

    /// <inheritdoc cref="McpAgentNotesService" />
    public McpAgentNotesService(Func<CascadeIdeSettings>? settingsProvider = null, NotesStorage? storage = null)
    {
        _settingsProvider = settingsProvider ?? (() => new CascadeIdeSettings());
        _storage = storage ?? new();
    }

    private const string EmptyKnowledgeListPayload = """{"path":"","files":[],"total":0}""";

    /// <remarks>
    /// Чтение <c>knowledge/</c> без канона окружения: встроенный KB-Base (embedded zip в сборке),
    /// с опциональным оверлеем из <see cref="AgentNotesSettings.KbBaseOverlayPath"/> (файлы оверлея имеют приоритет).
    /// Если задан <c>AGENT_NOTES_CANON_PATH</c>, используется только он (поведение agent-notes-core).
    /// </remarks>

    public const string WorkspaceRequiredMessage =
        "Error: no notes workspace. Open a solution, or set AGENT_NOTES_FILE to a global agent-notes path.";

    /// <summary>
    /// Каталог workspace для <see cref="NotesStorage"/> (read/write/list).
    /// Сначала каталог решения; если решения нет, но задан <c>AGENT_NOTES_FILE</c> — <see cref="Environment.CurrentDirectory"/>
    /// (как в agent-notes-mcp).
    /// </summary>
    public static string? ResolveNotesWorkspacePath(string? solutionPath)
    {
        if (!string.IsNullOrWhiteSpace(solutionPath) && File.Exists(solutionPath))
            return Path.GetDirectoryName(solutionPath);

        var globalNotes = Environment.GetEnvironmentVariable("AGENT_NOTES_FILE");
        if (!string.IsNullOrWhiteSpace(globalNotes))
            return Environment.CurrentDirectory;

        return null;
    }

    public string WriteAgentNotes(string? workspace, string content)
    {
        if (string.IsNullOrEmpty(workspace))
            return WorkspaceRequiredMessage;
        try
        {
            var result = _storage.Write(workspace, content);
            return result == "NO_CHANGES" ? "OK" : result;
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string ReadAgentNotes(string? workspace)
    {
        if (string.IsNullOrEmpty(workspace))
            return "";
        try
        {
            return _storage.Read(workspace);
        }
        catch
        {
            return "";
        }
    }

    public string AppendAgentNotes(string? workspace, string content)
    {
        if (string.IsNullOrEmpty(workspace))
            return WorkspaceRequiredMessage;
        try
        {
            var result = _storage.Append(workspace, content ?? "");
            return result == "NO_CHANGES" ? "OK" : result;
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string ListAgentNotesRevisions(string? workspace, int? limit)
    {
        if (string.IsNullOrEmpty(workspace))
            return "[]";
        try
        {
            var resolved = limit is null or <= 0 ? 20 : Math.Min(limit.Value, 200);
            return _storage.ListRevisions(workspace, resolved);
        }
        catch
        {
            return "[]";
        }
    }

    public string RollbackAgentNotes(string? workspace, string? revisionFile)
    {
        if (string.IsNullOrEmpty(workspace))
            return WorkspaceRequiredMessage;
        try
        {
            return _storage.Rollback(workspace, revisionFile);
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string ReadHotContext(string? workspace, string? activeScope)
    {
        if (string.IsNullOrEmpty(workspace))
            return "{\"content\":\"\"}";
        try
        {
            return _storage.ReadHotContext(workspace, activeScope);
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string RouteContext(string? workspace, string query, string? activeScope, int? maxSections, int? maxChars)
    {
        if (string.IsNullOrEmpty(workspace))
            return "{\"assembled_context\":\"\"}";
        if (string.IsNullOrWhiteSpace(query))
            return "{\"assembled_context\":\"\"}";
        try
        {
            var ms = maxSections is null or <= 0 ? 5 : Math.Clamp(maxSections.Value, 1, 20);
            var mc = maxChars is null or <= 0 ? 12000 : Math.Clamp(maxChars.Value, 1000, 40000);
            return _storage.RouteContext(workspace, query, activeScope, ms, mc);
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string MemoryHealth(string? workspace, string? activeScope)
    {
        if (string.IsNullOrEmpty(workspace))
            return "{\"health\":\"unknown\"}";
        try
        {
            return _storage.MemoryHealth(workspace, activeScope);
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string CompactHotContext(string? workspace, bool apply)
    {
        if (string.IsNullOrEmpty(workspace))
            return "{\"changed\":false}";
        try
        {
            return _storage.CompactHotContext(workspace, apply);
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string ExtractFromArchive(string? workspace, string query, string? revisionFile, int? headLimit, int? contextLines)
    {
        if (string.IsNullOrEmpty(workspace))
            return "{\"matches\":[]}";
        if (string.IsNullOrWhiteSpace(query))
            return "{\"matches\":[]}";
        try
        {
            var hl = headLimit is null or <= 0 ? 10 : Math.Clamp(headLimit.Value, 1, 100);
            var cl = contextLines is null or < 0 ? 2 : Math.Clamp(contextLines.Value, 0, 20);
            return _storage.ExtractFromArchive(workspace, query, revisionFile, hl, cl);
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string UpsertAgentNotesSection(string? workspace, string sectionId, string content)
    {
        if (string.IsNullOrWhiteSpace(sectionId))
            return "Error: missing section_id.";
        if (string.IsNullOrEmpty(workspace))
            return WorkspaceRequiredMessage;
        try
        {
            var result = _storage.UpsertSection(workspace, sectionId.Trim(), content ?? "");
            return result == "NO_CHANGES" ? "OK" : result;
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string SearchAgentNotes(string? workspace, string query, int? headLimit)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "{\"matches\":[]}";
        if (string.IsNullOrEmpty(workspace))
            return "{\"matches\":[]}";
        try
        {
            var limit = headLimit is null or <= 0 ? 20 : Math.Min(headLimit.Value, 200);
            return _storage.Search(workspace, query, limit);
        }
        catch
        {
            return "{\"matches\":[]}";
        }
    }

    public string ReadKnowledgeFile(string filePath, int? offset = null, int? limit = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return "";
        try
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AgentNotesCanonEnvVar)))
                return _storage.ReadKnowledgeFile(canonPath: null, filePath, offset, limit);

            var overlayCanon = KbBaseOverlayPathResolver.TryResolveCanonRoot(_settingsProvider());
            if (overlayCanon is not null)
            {
                var overlayFullPath = _storage.GetKnowledgeFilePath(overlayCanon, filePath);
                if (File.Exists(overlayFullPath))
                    return _storage.ReadKnowledgeFile(overlayCanon, filePath, offset, limit);
            }

            var embeddedCanon = KbBaseEmbeddedBundleProvisioner.TryGetEmbeddedCanonRoot();
            if (embeddedCanon is not null)
            {
                var embeddedFullPath = _storage.GetKnowledgeFilePath(embeddedCanon, filePath);
                if (File.Exists(embeddedFullPath))
                    return _storage.ReadKnowledgeFile(embeddedCanon, filePath, offset, limit);
            }

            return "";
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string ListKnowledgeFiles(string? subdir)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AgentNotesCanonEnvVar)))
                return _storage.ListKnowledgeFiles(canonPath: null, subdir);

            var overlayCanon = KbBaseOverlayPathResolver.TryResolveCanonRoot(_settingsProvider());
            var overlayJson =
                overlayCanon is null ? EmptyKnowledgeListPayload : _storage.ListKnowledgeFiles(overlayCanon, subdir);

            var embeddedCanon = KbBaseEmbeddedBundleProvisioner.TryGetEmbeddedCanonRoot();
            var embeddedJson =
                embeddedCanon is null ? EmptyKnowledgeListPayload : _storage.ListKnowledgeFiles(embeddedCanon, subdir);

            var hint =
                TryReadKnowledgeListSearchPathFirstNonEmpty(overlayJson)
                ?? TryReadKnowledgeListSearchPathFirstNonEmpty(embeddedJson)
                ?? "";
            var mergedHint = hint.Length > 0 ? $"{hint} (overlay + embedded merge)" : "knowledge (overlay + embedded merge)";

            return KbBaseKnowledgeListMerger.Merge(overlayJson, embeddedJson, mergedHint);
        }
        catch (Exception ex)
        {
            return "{\"error\":\"" + ex.Message.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"}";
        }
    }

    /// <remarks>Must match agent-notes-core <see cref="NotesStorage.ResolveCanonPath"/> env name.</remarks>
    private const string AgentNotesCanonEnvVar = "AGENT_NOTES_CANON_PATH";

    private static string? TryReadKnowledgeListSearchPathFirstNonEmpty(string jsonPayload)
    {
        if (string.IsNullOrWhiteSpace(jsonPayload))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(jsonPayload, JsonDocOptions);
            if (!doc.RootElement.TryGetProperty("path", out var inner) || inner.ValueKind != JsonValueKind.String)
                return null;
            var trimmed = (inner.GetString() ?? "").Trim();
            return trimmed.Length > 0 ? trimmed : null;
        }
        catch
        {
            return null;
        }
    }

    public string WriteKnowledgeFile(string filePath, string content, string? canonPath, bool saveRevision)
    {
        try
        {
            return _storage.WriteKnowledgeFile(canonPath, filePath, content, saveRevision);
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string AppendKnowledgeFile(string filePath, string content, string? canonPath, bool saveRevision)
    {
        try
        {
            return _storage.AppendKnowledgeFile(canonPath, filePath, content, saveRevision);
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string UpsertKnowledgeSection(string filePath, string sectionId, string content, string? canonPath, bool saveRevision)
    {
        try
        {
            return _storage.UpsertKnowledgeSection(canonPath, filePath, sectionId, content, saveRevision);
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string DeleteKnowledgeFile(string filePath, string? canonPath)
    {
        try
        {
            return _storage.DeleteKnowledgeFile(canonPath, filePath);
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public string DeleteKnowledgeSection(string filePath, string sectionId, string? canonPath)
    {
        try
        {
            return _storage.DeleteKnowledgeSection(canonPath, filePath, sectionId);
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }
}

#nullable enable

using System.Text.Json;
using HybridCodebaseIndex.Core;

namespace CascadeIDE.SoftOrgan;

/// <summary>In-proc <c>codebase_index_status</c> parity (HybridCodebaseIndex.Core; no MCP subprocess).</summary>
public static class GlassHybridIndexStatusProbe
{
    static readonly CodebaseIndexService Service = new();

    public static GlassHybridIndexGlance.LiveInstrumentStatus? TryFetchStatus(string? workspaceRoot, string? solutionPath = null)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return null;

        try
        {
            var st = Service.GetStatusAsync(workspaceRoot.Trim(), solutionPath).GetAwaiter().GetResult();
            return new GlassHybridIndexGlance.LiveInstrumentStatus(
                DatabaseExists: st.DatabaseExists,
                DocumentCount: st.DocumentCount,
                DocumentCountMayBeStale: st.DocumentCountMayBeStale,
                IndexedAtIso: st.IndexedAtIso,
                ReindexState: st.ReindexState,
                LastReindexError: st.LastReindexError,
                DatabasePath: st.DatabasePath,
                WorkspaceRoot: st.WorkspaceRootNormalized ?? workspaceRoot,
                ByteLength: null,
                ModifiedUtc: null);
        }
        catch
        {
            return null;
        }
    }

    public static string? TryFetchStatusJson(string? workspaceRoot, string? solutionPath = null)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return null;

        try
        {
            var st = Service.GetStatusAsync(workspaceRoot.Trim(), solutionPath).GetAwaiter().GetResult();
            var dto = new
            {
                indexFormatVersion = st.IndexFormatVersion,
                databasePath = st.DatabasePath,
                databaseExists = st.DatabaseExists,
                documentCount = st.DocumentCount,
                documentCountMayBeStale = st.DocumentCountMayBeStale,
                indexedAtIso = st.IndexedAtIso,
                workspaceRoot = st.WorkspaceRootNormalized,
                lastReindexError = st.LastReindexError,
                lastReindexErrorAtIso = st.LastReindexErrorAtIso,
                settingsSource = st.SettingsSource,
                settingsParseError = st.SettingsParseError,
                reindexState = st.ReindexState,
                reindexStartedAtIso = st.ReindexStartedAtIso,
            };
            return JsonSerializer.Serialize(dto);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = "status_probe_failed", detail = ex.Message });
        }
    }

    public sealed record SearchHitRow(
        string Path,
        int LineStart,
        string Display,
        string? Snippet);

    public sealed record SearchResult(
        string? Error,
        IReadOnlyList<SearchHitRow> Hits);

    public sealed record ReindexResult(
        bool Ok,
        string Message,
        int? DocumentsIndexed = null);

    /// <summary>In-proc codebase_index_search parity for Glass HCI hand.</summary>
    public static SearchResult TrySearch(string? workspaceRoot, string query, int topN = 20, string? solutionPath = null)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return new SearchResult("workspace root unavailable", []);
        if (string.IsNullOrWhiteSpace(query))
            return new SearchResult("empty query", []);

        try
        {
            var (response, error) = Service.SearchAsync(
                    workspaceRoot.Trim(),
                    solutionPath,
                    query.Trim(),
                    topN)
                .GetAwaiter()
                .GetResult();
            if (!string.IsNullOrWhiteSpace(error))
                return new SearchResult(error, []);

            var hits = response.Hits
                .Select(h =>
                {
                    var name = System.IO.Path.GetFileName(h.Path);
                    var line = h.LineStart > 0 ? $":{h.LineStart}" : "";
                    var snip = string.IsNullOrWhiteSpace(h.Snippet)
                        ? ""
                        : " · " + TruncateOneLine(h.Snippet!, 72);
                    return new SearchHitRow(
                        h.Path,
                        h.LineStart,
                        $"{name}{line}{snip}",
                        h.Snippet);
                })
                .ToList();
            return new SearchResult(null, hits);
        }
        catch (Exception ex)
        {
            return new SearchResult(ex.Message, []);
        }
    }

    /// <summary>In-proc reindex hand (Avalonia REINDEX parity; not status refresh).</summary>
    public static ReindexResult TryReindex(string? workspaceRoot, string? solutionPath = null)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return new ReindexResult(false, "workspace root unavailable");

        try
        {
            var summary = Service.FullReindexAsync(workspaceRoot.Trim(), solutionPath).GetAwaiter().GetResult();
            return new ReindexResult(true, $"reindexed · files {summary.FilesIndexed}", summary.FilesIndexed);
        }
        catch (Exception ex)
        {
            return new ReindexResult(false, ex.Message);
        }
    }

    static string TruncateOneLine(string text, int max)
    {
        var one = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return one.Length <= max ? one : one[..Math.Max(0, max - 1)] + "…";
    }

}

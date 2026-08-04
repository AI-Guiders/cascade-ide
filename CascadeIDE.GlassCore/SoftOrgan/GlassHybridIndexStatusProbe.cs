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
}

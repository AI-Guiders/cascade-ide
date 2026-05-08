using System.Text.Json;
using System.Text.Json.Serialization;
using HybridCodebaseIndex.Core;

namespace CascadeIDE.Features.HybridIndex.McpParity;

/// <summary>JSON-ответы ide_execute для команд <c>codebase_index_*</c> — паритет с MCP-пакетом hybrid-codebase-index (ToolHandlers).</summary>
internal static class CodebaseIndexIdeJsonResponses
{
    private static readonly JsonSerializerOptions JsonOut = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions JsonOutWithNulls = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static string SerializeSearch(SearchResponse response, string? err)
    {
        var dto = new SearchResultDto(
            Err: err,
            IndexFormatVersion: response.IndexFormatVersion,
            Query: response.Query,
            DatabasePath: response.DatabasePath,
            Hits: response.Hits.Select(static h => new HitDto(
                h.HitId,
                h.Path,
                h.Extension,
                h.HitKind,
                h.RankScore,
                h.FtsScore,
                h.VecScore,
                h.Snippet,
                h.LineStart,
                h.LineEnd,
                h.ChunkCharCount,
                h.LastWriteUtcIso)).ToList());
        return JsonSerializer.Serialize(dto, JsonOut);
    }

    public static string SerializeExplain(ExplainHitResponse resp)
    {
        var dto = new ExplainResultDto(
            Err: resp.Err,
            IndexFormatVersion: resp.IndexFormatVersion,
            DatabasePath: resp.DatabasePath,
            Hit: resp.Hit is null
                ? null
                : new HitDto(
                    resp.Hit.HitId,
                    resp.Hit.Path,
                    resp.Hit.Extension,
                    resp.Hit.HitKind,
                    resp.Hit.RankScore,
                    resp.Hit.FtsScore,
                    resp.Hit.VecScore,
                    resp.Hit.Snippet,
                    resp.Hit.LineStart,
                    resp.Hit.LineEnd,
                    resp.Hit.ChunkCharCount,
                    resp.Hit.LastWriteUtcIso));
        return JsonSerializer.Serialize(dto, JsonOut);
    }

    public static string SerializeStatus(IndexStatus st)
    {
        var dto = new StatusResultDto(
            IndexFormatVersion: st.IndexFormatVersion,
            DatabasePath: st.DatabasePath,
            DatabaseExists: st.DatabaseExists,
            DocumentCount: st.DocumentCount,
            DocumentCountMayBeStale: st.DocumentCountMayBeStale,
            IndexedAtIso: st.IndexedAtIso,
            WorkspaceRoot: st.WorkspaceRootNormalized,
            LastReindexError: st.LastReindexError,
            LastReindexErrorAtIso: st.LastReindexErrorAtIso,
            SettingsSource: st.SettingsSource,
            SettingsParseError: st.SettingsParseError,
            EffectiveSettings: st.EffectiveSettings is null
                ? null
                : new EffectiveSettingsDto(
                    st.EffectiveSettings.IncludeCsInFts,
                    st.EffectiveSettings.ExtraIncludeRoots.ToList(),
                    st.EffectiveSettings.ExcludeRoots.ToList(),
                    st.EffectiveSettings.EffectiveExtensions.ToList(),
                    st.EffectiveSettings.ExcludePathSegments.ToList(),
                    st.EffectiveSettings.IgnoreFiles.ToList(),
                    st.EffectiveSettings.MaxIndexedFileBytes,
                    st.EffectiveSettings.ChunkLines,
                    st.EffectiveSettings.ChunkOverlapLines,
                    st.EffectiveSettings.BinaryProbeBytes),
            ReindexState: st.ReindexState,
            ReindexStartedAtIso: st.ReindexStartedAtIso);
        return JsonSerializer.Serialize(dto, JsonOutWithNulls);
    }

    public static string SerializeReindex(ReindexSummary summary)
    {
        var dto = new ReindexResultDto(
            IndexFormatVersion: summary.IndexFormatVersion,
            DatabasePath: summary.DatabasePath,
            FilesIndexed: summary.FilesIndexed,
            FilesSkippedTooLarge: summary.FilesSkippedTooLarge,
            FilesSkippedBinary: summary.FilesSkippedBinary,
            FilesSkippedExcluded: summary.FilesSkippedExcluded,
            SkippedReasonCounts: summary.SkippedReasonCounts,
            SkippedTopPathPrefixes: summary.SkippedTopPathPrefixes.Select(static p => new TopPrefixDto(p.PathPrefix, p.Count)).ToList(),
            SkippedSample: summary.SkippedSample.Select(static s => new SkippedDto(s.Path, s.Reason)).ToList(),
            DurationMs: (long)summary.Duration.TotalMilliseconds);
        return JsonSerializer.Serialize(dto, JsonOut);
    }

    private sealed record SearchResultDto(
        string? Err,
        int IndexFormatVersion,
        string Query,
        string DatabasePath,
        List<HitDto> Hits);

    private sealed record HitDto(
        long HitId,
        string Path,
        string Extension,
        string HitKind,
        double RankScore,
        double? FtsScore,
        double? VecScore,
        string? Snippet,
        int LineStart,
        int LineEnd,
        int ChunkCharCount,
        string? LastWriteUtcIso);

    private sealed record ExplainResultDto(
        string? Err,
        int IndexFormatVersion,
        string DatabasePath,
        HitDto? Hit);

    private sealed record StatusResultDto(
        int IndexFormatVersion,
        string DatabasePath,
        bool DatabaseExists,
        int DocumentCount,
        bool DocumentCountMayBeStale,
        string? IndexedAtIso,
        string? WorkspaceRoot,
        string? LastReindexError,
        string? LastReindexErrorAtIso,
        string SettingsSource,
        string? SettingsParseError,
        EffectiveSettingsDto? EffectiveSettings,
        string? ReindexState,
        string? ReindexStartedAtIso);

    private sealed record EffectiveSettingsDto(
        bool IncludeCsInFts,
        List<string> ExtraIncludeRoots,
        List<string> ExcludeRoots,
        List<string> EffectiveExtensions,
        List<string> ExcludePathSegments,
        List<string> IgnoreFiles,
        long MaxIndexedFileBytes,
        int ChunkLines,
        int ChunkOverlapLines,
        int BinaryProbeBytes);

    private sealed record ReindexResultDto(
        int IndexFormatVersion,
        string DatabasePath,
        int FilesIndexed,
        int FilesSkippedTooLarge,
        int FilesSkippedBinary,
        int FilesSkippedExcluded,
        IReadOnlyDictionary<string, int> SkippedReasonCounts,
        List<TopPrefixDto> SkippedTopPathPrefixes,
        List<SkippedDto> SkippedSample,
        long DurationMs);

    private sealed record TopPrefixDto(string PathPrefix, int Count);

    private sealed record SkippedDto(string Path, string Reason);
}

#nullable enable

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.Services;

namespace CascadeIDE.Features.CasaField.Application;

/// <summary>Resolve CASA agent store from <c>.cascade/workspace.toml</c> [workspace.casa_field].</summary>
public static class CasaFieldStoreResolver
{
    public static string? ResolveStoreDirectory(string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return null;

        var toml = RepositoryWorkspaceTomlLoader.TryLoad(workspaceRoot);
        var rel = toml?.Workspace?.CasaField?.StoreDir?.Trim();
        if (string.IsNullOrEmpty(rel))
            rel = "../casa-ontology-payload/examples/agent-stores/research-agent-lab-v0";

        return ResolvePath(workspaceRoot, rel);
    }

    public static string? ResolveFieldStatePath(string? workspaceRoot)
    {
        var store = ResolveStoreDirectory(workspaceRoot);
        return store is null ? null : Path.Combine(store, "field_state.json");
    }

    public static string? ResolvePath(string workspaceRoot, string path)
    {
        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        return Path.GetFullPath(Path.Combine(workspaceRoot.Trim(), path.Replace('/', Path.DirectorySeparatorChar)));
    }
}

public sealed record CasaFieldTarget(
    string ConceptId,
    string Kind,
    string? DocPath,
    string? Section,
    int? SectionLine,
    string? CodeFile,
    int? CodeLine,
    string? TextPreview);

public sealed record CasaFieldQueryResult(
    string Query,
    double WallMs,
    string? BundleName,
    int? FieldVersion,
    bool? Stale,
    IReadOnlyList<CasaFieldTarget> Targets,
    string? Error);

/// <summary>Query agent field store: match claims → KB doc + section (no LLM).</summary>
public static class CasaFieldQueryResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static string BuildJson(string? workspaceRoot, string query)
    {
        var result = Query(workspaceRoot, query);
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    public static CasaFieldQueryResult Query(string? workspaceRoot, string query)
    {
        var sw = Stopwatch.StartNew();
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return new CasaFieldQueryResult(query, 0, null, null, null, [], "no_workspace");

        var fieldPath = CasaFieldStoreResolver.ResolveFieldStatePath(workspaceRoot);
        if (fieldPath is null || !File.Exists(fieldPath))
            return new CasaFieldQueryResult(query, sw.Elapsed.TotalMilliseconds, null, null, null, [], "field_state_missing");

        var storeDir = Path.GetDirectoryName(fieldPath)!;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(fieldPath));
            var root = doc.RootElement;
            var bundleName = root.TryGetProperty("grid", out var grid)
                && grid.TryGetProperty("source_bundle", out var sb)
                ? sb.GetString()
                : null;

            var navItems = ReadNavItems(root, storeDir, bundleName);
            var fieldVersion = root.TryGetProperty("field_version", out var fv) && fv.ValueKind == JsonValueKind.Number
                ? fv.GetInt32()
                : (int?)null;
            var stale = IsFieldStale(root, storeDir);
            var targets = MatchQuery(query, navItems, workspaceRoot);
            sw.Stop();
            return new CasaFieldQueryResult(query, Math.Round(sw.Elapsed.TotalMilliseconds, 2), bundleName, fieldVersion, stale, targets, null);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new CasaFieldQueryResult(query, sw.Elapsed.TotalMilliseconds, null, null, null, [], ex.Message);
        }
    }

    private sealed record NavItem(
        string ConceptId,
        string DocPath,
        string Section,
        string Text,
        IReadOnlyList<(string File, int? Line)> CodeAnchors);

    private static bool IsFieldStale(JsonElement root, string storeDir)
    {
        if (!root.TryGetProperty("bundles", out var bundles) || bundles.ValueKind != JsonValueKind.Object)
            return false;
        foreach (var prop in bundles.EnumerateObject())
        {
            if (!File.Exists(Path.Combine(storeDir, "bundles", prop.Name)))
                return true;
        }
        return false;
    }

    private static List<NavItem> ReadNavItems(
        JsonElement root,
        string storeDir,
        string? bundleName)
    {
        if (root.TryGetProperty("claims_nav", out var nav) && nav.ValueKind == JsonValueKind.Array)
            return ParseNavArray(nav);

        if (!string.IsNullOrEmpty(bundleName))
        {
            var bundlePath = Path.Combine(storeDir, "bundles", bundleName);
            if (File.Exists(bundlePath))
            {
                using var bdoc = JsonDocument.Parse(File.ReadAllText(bundlePath));
                if (bdoc.RootElement.TryGetProperty("claims", out var claims))
                    return ParseClaims(claims);
            }
        }

        return [];
    }

    private static List<NavItem> ParseNavArray(JsonElement nav)
    {
        var list = new List<NavItem>();
        foreach (var item in nav.EnumerateArray())
        {
            var cid = item.TryGetProperty("concept_id", out var c) ? c.GetString() : null;
            var dp = item.TryGetProperty("doc_path", out var d) ? d.GetString() : null;
            var sec = item.TryGetProperty("section", out var s) ? s.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(cid) || string.IsNullOrWhiteSpace(dp))
                continue;
            list.Add(new NavItem(cid!, dp!, sec, "", ParseCodeAnchors(item)));
        }

        return list;
    }

    private static List<NavItem> ParseClaims(JsonElement claims)
    {
        var list = new List<NavItem>();
        foreach (var c in claims.EnumerateArray())
        {
            var cid = c.TryGetProperty("concept_id", out var ci) ? ci.GetString() : null;
            var dp = c.TryGetProperty("doc_path", out var di) ? di.GetString() : null;
            var sec = c.TryGetProperty("section", out var si) ? si.GetString() ?? "" : "";
            var text = c.TryGetProperty("text", out var ti) ? ti.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(cid) || string.IsNullOrWhiteSpace(dp))
                continue;
            list.Add(new NavItem(cid!, dp!, sec, text, ParseCodeAnchors(c)));
        }

        return list;
    }

    private static IReadOnlyList<(string File, int? Line)> ParseCodeAnchors(JsonElement item)
    {
        if (!item.TryGetProperty("code_anchors", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return [];

        var list = new List<(string, int?)>();
        foreach (var a in arr.EnumerateArray())
        {
            var file = a.TryGetProperty("file", out var f) ? f.GetString() : null;
            if (string.IsNullOrWhiteSpace(file))
                continue;
            int? line = a.TryGetProperty("line", out var ln) && ln.ValueKind == JsonValueKind.Number ? ln.GetInt32() : null;
            list.Add((file!, line));
        }

        return list;
    }

    private static List<CasaFieldTarget> MatchQuery(
        string query,
        List<NavItem> items,
        string workspaceRoot)
    {
        var q = query.Trim();
        var words = q.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length > 2)
            .Select(w => w.ToLowerInvariant())
            .ToArray();

        var hits = new List<(int Score, CasaFieldTarget Target)>();

        foreach (var item in items)
        {
            var score = 0;
            var cidL = item.ConceptId.ToLowerInvariant();
            var secL = item.Section.ToLowerInvariant();
            var textL = item.Text.ToLowerInvariant();
            var qL = q.ToLowerInvariant();

            if (cidL.Contains(qL, StringComparison.Ordinal) || qL.Contains(cidL, StringComparison.Ordinal))
                score += 10;

            foreach (var w in words)
            {
                if (cidL.Contains(w, StringComparison.Ordinal)) score += 3;
                if (secL.Contains(w, StringComparison.Ordinal)) score += 2;
                if (textL.Contains(w, StringComparison.Ordinal)) score += 2;
            }

            if (score <= 0)
                continue;

            var preview = item.Text.Length > 120 ? item.Text[..117] + "…" : item.Text;
            var line = KbSectionLineResolver.TryFindSectionLine(workspaceRoot, item.DocPath, item.Section);
            hits.Add((score, new CasaFieldTarget(item.ConceptId, "kb", item.DocPath, item.Section, line, null, null, preview)));

            foreach (var (file, codeLine) in item.CodeAnchors)
            {
                hits.Add((score, new CasaFieldTarget(item.ConceptId, "code", null, null, null, file, codeLine, preview)));
            }
        }

        return hits
            .OrderByDescending(h => h.Score)
            .ThenBy(h => h.Target.Kind, StringComparer.Ordinal)
            .ThenBy(h => h.Target.ConceptId, StringComparer.OrdinalIgnoreCase)
            .Select(h => h.Target)
            .Take(8)
            .ToList();
    }
}

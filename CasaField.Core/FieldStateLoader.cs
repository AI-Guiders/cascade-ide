using System.Text.Json;

namespace CasaField.Core;

public sealed record FieldGrid(
    int Width,
    int Height,
    IReadOnlyList<IReadOnlyList<string>> Cells,
    string? SourceBundle,
    IReadOnlyList<string>? SourceBundles,
    string Codec,
    bool Union);

public sealed record FieldStateSnapshot(
    int SchemaVersion,
    int FieldVersion,
    bool Stale,
    FieldGrid? Grid,
    IReadOnlyList<ClaimNavItem> ClaimsNav,
    IReadOnlyDictionary<string, JsonElement>? BundlesMeta);

public sealed record ClaimNavItem(
    string ConceptId,
    string DocPath,
    string Section,
    string? Bundle,
    IReadOnlyList<CodeAnchorItem>? CodeAnchors);

public sealed record CodeAnchorItem(string File, int? Line, int? LineEnd);

public static class FieldStateLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static FieldStateSnapshot Load(string fieldStatePath, string storeDirectory)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(fieldStatePath));
        var root = doc.RootElement;
        var schemaVersion = root.TryGetProperty("schema_version", out var sv) ? sv.GetInt32() : 1;
        var fieldVersion = root.TryGetProperty("field_version", out var fv) ? fv.GetInt32() : 0;

        FieldGrid? grid = null;
        if (root.TryGetProperty("grid", out var gridEl) && gridEl.ValueKind == JsonValueKind.Object)
        {
            var cells = new List<IReadOnlyList<string>>();
            if (gridEl.TryGetProperty("cells", out var cellsEl) && cellsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var cell in cellsEl.EnumerateArray())
                {
                    var tokens = new List<string>();
                    if (cell.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var t in cell.EnumerateArray())
                        {
                            if (t.ValueKind == JsonValueKind.String)
                                tokens.Add(t.GetString() ?? "");
                        }
                    }
                    cells.Add(tokens);
                }
            }

            var sourceBundles = new List<string>();
            if (gridEl.TryGetProperty("source_bundles", out var sbArr) && sbArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var b in sbArr.EnumerateArray())
                {
                    if (b.ValueKind == JsonValueKind.String)
                        sourceBundles.Add(b.GetString() ?? "");
                }
            }

            grid = new FieldGrid(
                gridEl.TryGetProperty("width", out var w) ? w.GetInt32() : 64,
                gridEl.TryGetProperty("height", out var h) ? h.GetInt32() : 64,
                cells,
                gridEl.TryGetProperty("source_bundle", out var sb) ? sb.GetString() : null,
                sourceBundles.Count > 0 ? sourceBundles : null,
                gridEl.TryGetProperty("codec", out var c) ? c.GetString() ?? "v3" : "v3",
                gridEl.TryGetProperty("union", out var u) && u.ValueKind == JsonValueKind.True);
        }

        var nav = ReadClaimsNav(root);
        var stale = IsStale(root, storeDirectory);
        Dictionary<string, JsonElement>? bundlesMeta = null;
        if (root.TryGetProperty("bundles", out var bundles) && bundles.ValueKind == JsonValueKind.Object)
        {
            bundlesMeta = bundles.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
        }

        return new FieldStateSnapshot(schemaVersion, fieldVersion, stale, grid, nav, bundlesMeta);
    }

    private static List<ClaimNavItem> ReadClaimsNav(JsonElement root)
    {
        if (!root.TryGetProperty("claims_nav", out var nav) || nav.ValueKind != JsonValueKind.Array)
            return [];

        var list = new List<ClaimNavItem>();
        foreach (var item in nav.EnumerateArray())
        {
            var cid = item.TryGetProperty("concept_id", out var c) ? c.GetString() : null;
            var dp = item.TryGetProperty("doc_path", out var d) ? d.GetString() : null;
            if (string.IsNullOrWhiteSpace(cid) || string.IsNullOrWhiteSpace(dp))
                continue;

            var sec = item.TryGetProperty("section", out var s) ? s.GetString() ?? "" : "";
            var bundle = item.TryGetProperty("bundle", out var b) ? b.GetString() : null;
            IReadOnlyList<CodeAnchorItem>? anchors = null;
            if (item.TryGetProperty("code_anchors", out var ca) && ca.ValueKind == JsonValueKind.Array)
            {
                var al = new List<CodeAnchorItem>();
                foreach (var a in ca.EnumerateArray())
                {
                    var file = a.TryGetProperty("file", out var f) ? f.GetString() : null;
                    if (string.IsNullOrWhiteSpace(file))
                        continue;
                    int? line = a.TryGetProperty("line", out var ln) && ln.ValueKind == JsonValueKind.Number ? ln.GetInt32() : null;
                    int? lineEnd = a.TryGetProperty("line_end", out var le) && le.ValueKind == JsonValueKind.Number ? le.GetInt32() : null;
                    al.Add(new CodeAnchorItem(file, line, lineEnd));
                }
                if (al.Count > 0)
                    anchors = al;
            }

            list.Add(new ClaimNavItem(cid!, dp!, sec, bundle, anchors));
        }

        return list;
    }

    private static bool IsStale(JsonElement root, string storeDirectory)
    {
        if (!root.TryGetProperty("bundles", out var bundles) || bundles.ValueKind != JsonValueKind.Object)
            return false;

        foreach (var prop in bundles.EnumerateObject())
        {
            if (!File.Exists(Path.Combine(storeDirectory, "bundles", prop.Name)))
                return true;
        }

        if (!root.TryGetProperty("fingerprint", out var fp) || !fp.TryGetProperty("bundles", out var fpBundles))
            return false;

        foreach (var prop in fpBundles.EnumerateObject())
        {
            var path = Path.Combine(storeDirectory, "bundles", prop.Name);
            if (!File.Exists(path))
                return true;
            var recorded = prop.Value.TryGetProperty("content_hash", out var h) ? h.GetString() : null;
            if (string.IsNullOrEmpty(recorded))
                continue;
            if (BundleContentHash(File.ReadAllText(path)) != recorded)
                return true;
        }

        return false;
    }

    internal static string BundleContentHash(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var canonical = CanonicalJsonString(doc.RootElement);
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    private static string CanonicalJsonString(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Object)
        {
            var props = el.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
            var inner = string.Join(",", props.Select(p => $"\"{p.Name}\":{CanonicalJsonString(p.Value)}"));
            return "{" + inner + "}";
        }

        if (el.ValueKind == JsonValueKind.Array)
        {
            var items = el.EnumerateArray().Select(CanonicalJsonString);
            return "[" + string.Join(",", items) + "]";
        }

        return el.GetRawText();
    }
}

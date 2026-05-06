using System.Text.Json;

namespace CascadeIDE.Services;

internal static class KbBaseKnowledgeListMerger
{
    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };

    private sealed record ListedFile(string Path, long SizeBytes, string ModifiedUtc);

    internal static string Merge(string overlayListJson, string embeddedListJson, string mergedSearchHint)
    {
        var overlayFiles = ParseFiles(overlayListJson);
        var embeddedFiles = ParseFiles(embeddedListJson);

        var byPath = new Dictionary<string, ListedFile>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in overlayFiles)
            byPath[f.Path] = f;
        foreach (var f in embeddedFiles)
        {
            if (!byPath.ContainsKey(f.Path))
                byPath[f.Path] = f;
        }

        var ordered = byPath.Values
            .OrderBy(x => x.Path, StringComparer.Ordinal)
            .Select(x => new { path = x.Path, size_bytes = x.SizeBytes, modified_utc = x.ModifiedUtc })
            .ToArray();

        return JsonSerializer.Serialize(new { path = mergedSearchHint, files = ordered, total = ordered.Length }, IndentedJson);
    }

    private static ListedFile[] ParseFiles(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch
        {
            return [];
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (!root.TryGetProperty("files", out var filesEl) || filesEl.ValueKind != JsonValueKind.Array)
                return [];

            var list = new List<ListedFile>();
            foreach (var el in filesEl.EnumerateArray())
            {
                if (el.TryGetProperty("path", out var pathEl))
                {
                    var path = pathEl.ValueKind == JsonValueKind.String ? (pathEl.GetString() ?? "").Trim() : "";
                    if (path.Length > 0)
                    {
                        var size = 0L;
                        if (el.TryGetProperty("size_bytes", out var sb) && sb.ValueKind == JsonValueKind.Number &&
                            sb.TryGetInt64(out var sl))
                            size = sl;
                        var mod =
                            el.TryGetProperty("modified_utc", out var m) && m.ValueKind == JsonValueKind.String
                                ? (m.GetString() ?? "")
                                : "";
                        list.Add(new ListedFile(path, size, mod));
                    }
                }
            }

            return list.ToArray();
        }
    }
}

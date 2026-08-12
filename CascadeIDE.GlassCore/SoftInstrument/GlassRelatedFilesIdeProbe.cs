#nullable enable

using HybridCodebaseIndex.Core;

namespace CascadeIDE.SoftInstrument;

/// <summary>HCI/codebase_index_search peel for RelatedFiles (WorkspaceNavigation composer deferred).</summary>
public static class GlassRelatedFilesIdeProbe
{
    static readonly CodebaseIndexService Service = new();

    public static IReadOnlyList<GlassRelatedFilesFeed.Item> Collect(
        string? workspaceRoot,
        string? editorPath,
        int max = 24)
    {
        if (max < 1 || string.IsNullOrWhiteSpace(workspaceRoot) || string.IsNullOrWhiteSpace(editorPath))
            return [];

        var root = Path.GetFullPath(workspaceRoot.Trim());
        var stem = Path.GetFileNameWithoutExtension(editorPath);
        if (string.IsNullOrWhiteSpace(stem) || stem.Length < 3)
            return [];

        try
        {
            var (response, err) = Service.SearchHybridAsync(
                root,
                solutionPath: null,
                query: stem,
                topN: Math.Clamp(max, 1, 32),
                pathPrefix: null,
                excludePathPrefixes: null,
                extensions: [".cs", ".xaml", ".axaml", ".md", ".toml"],
                semantic: false,
                alpha: 0.65,
                beta: 0.35,
                vecTopK: 20).GetAwaiter().GetResult();

            if (!string.IsNullOrWhiteSpace(err) || response.Hits.Count == 0)
                return [];

            var editorFull = Path.GetFullPath(editorPath);
            var list = new List<GlassRelatedFilesFeed.Item>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var hit in response.Hits)
            {
                if (list.Count >= max)
                    break;
                if (string.IsNullOrWhiteSpace(hit.Path))
                    continue;

                var full = Path.IsPathRooted(hit.Path)
                    ? Path.GetFullPath(hit.Path)
                    : Path.GetFullPath(Path.Combine(root, hit.Path));
                if (!File.Exists(full) || !seen.Add(full))
                    continue;
                if (string.Equals(full, editorFull, StringComparison.OrdinalIgnoreCase))
                    continue;

                var rel = Path.GetRelativePath(root, full).Replace('\\', '/');
                var rationale = string.IsNullOrWhiteSpace(hit.Snippet)
                    ? $"hci:{hit.HitKind}"
                    : hit.Snippet.Trim();
                if (rationale.Length > 72)
                    rationale = rationale[..69] + "…";
                list.Add(new GlassRelatedFilesFeed.Item(full, rel, "hci", rationale));
            }

            return list;
        }
        catch
        {
            return [];
        }
    }
}

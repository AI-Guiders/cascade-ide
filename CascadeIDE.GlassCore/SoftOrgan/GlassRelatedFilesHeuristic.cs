#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>Thin RelatedFiles / SemanticMap list feed (no Avalonia navigation map).</summary>
public static class GlassRelatedFilesHeuristic
{
    public sealed record Item(string FilePath, string Reason)
    {
        public string Display => $"{Path.GetFileName(FilePath)} · {Reason}";
    }

    public static IReadOnlyList<Item> Collect(string? workspaceRoot, string? editorPath, int max = 64)
    {
        var list = new List<Item>();
        if (max < 1)
            return list;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string path, string reason)
        {
            if (list.Count >= max)
                return;
            try
            {
                var full = Path.GetFullPath(path);
                if (!File.Exists(full) || !seen.Add(full))
                    return;
                if (!string.IsNullOrWhiteSpace(editorPath)
                    && string.Equals(full, Path.GetFullPath(editorPath), StringComparison.OrdinalIgnoreCase))
                    return;
                list.Add(new Item(full, reason));
            }
            catch
            {
                /* skip */
            }
        }

        if (!string.IsNullOrWhiteSpace(editorPath) && File.Exists(editorPath))
        {
            var dir = Path.GetDirectoryName(editorPath)!;
            var stem = Path.GetFileNameWithoutExtension(editorPath);
            try
            {
                foreach (var f in Directory.EnumerateFiles(dir))
                {
                    var name = Path.GetFileNameWithoutExtension(f);
                    if (string.Equals(name, stem, StringComparison.OrdinalIgnoreCase))
                        Add(f, "same stem");
                    else if (string.Equals(Path.GetExtension(f), ".md", StringComparison.OrdinalIgnoreCase)
                             && name.Contains(stem, StringComparison.OrdinalIgnoreCase))
                        Add(f, "nearby md");
                }
            }
            catch
            {
                /* skip */
            }
        }

        if (!string.IsNullOrWhiteSpace(workspaceRoot) && Directory.Exists(workspaceRoot))
        {
            foreach (var rel in new[] { "docs", "docs/adr", "README.md" })
            {
                var p = Path.Combine(workspaceRoot, rel.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(p))
                    Add(p, "workspace");
                else if (Directory.Exists(p))
                {
                    try
                    {
                        foreach (var f in Directory.EnumerateFiles(p, "*.md").Take(12))
                            Add(f, rel);
                    }
                    catch
                    {
                        /* skip */
                    }
                }
            }
        }

        return list;
    }
}

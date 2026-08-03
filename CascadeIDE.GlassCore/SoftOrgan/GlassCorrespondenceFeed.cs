#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>Thin CRS list feed (FS) — not Avalonia WorkspaceCorrespondenceResolver.</summary>
public static class GlassCorrespondenceFeed
{
    public sealed record Item(string FilePath, string Kind)
    {
        public string Display => $"{Kind} · {Path.GetFileName(FilePath)}";
    }

    public static (IReadOnlyList<Item> Reverse, IReadOnlyList<Item> Forward) Collect(
        string? workspaceRoot,
        string? editorPath,
        int maxEach = 32)
    {
        var reverse = new List<Item>();
        var forward = new List<Item>();
        var seenR = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenF = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(List<Item> target, HashSet<string> seen, string path, string kind, int max)
        {
            if (target.Count >= max)
                return;
            try
            {
                var full = Path.GetFullPath(path);
                if (!File.Exists(full) || !seen.Add(full))
                    return;
                target.Add(new Item(full, kind));
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
                    if (string.Equals(f, editorPath, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var ext = Path.GetExtension(f);
                    if (ext is ".cs" or ".xaml" or ".axaml" or ".csproj")
                        Add(reverse, seenR, f, "code", maxEach);
                    else if (ext is ".md" && Path.GetFileNameWithoutExtension(f)
                             .Contains(stem, StringComparison.OrdinalIgnoreCase))
                        Add(forward, seenF, f, "doc", maxEach);
                }
            }
            catch
            {
                /* skip */
            }
        }

        if (!string.IsNullOrWhiteSpace(workspaceRoot) && Directory.Exists(workspaceRoot))
        {
            foreach (var sub in new[] { "docs/adr", "docs" })
            {
                var dir = Path.Combine(workspaceRoot, sub.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(dir))
                    continue;
                try
                {
                    foreach (var f in Directory.EnumerateFiles(dir, "*.md").Take(maxEach))
                        Add(forward, seenF, f, sub, maxEach);
                }
                catch
                {
                    /* skip */
                }
            }
        }

        return (reverse, forward);
    }
}

#nullable enable

namespace CascadeIDE.SoftInstrument;

/// <summary>
/// Glass RelatedFiles / SemanticMap list feed — WNM <c>RelatedRow</c> shape (kind · relative · rationale),
/// Avalonia-free FS heuristics (full IdeMcp refresh composer deferred).
/// </summary>
public static class GlassRelatedFilesFeed
{
    public sealed record Item(string FullPath, string RelativePath, string Kind, string Rationale)
    {
        public string FilePath => FullPath;
        public string Display =>
            string.IsNullOrWhiteSpace(Rationale)
                ? $"{Kind} · {RelativePath}"
                : $"{Kind} · {RelativePath} · {Rationale}";
    }

    public static IReadOnlyList<Item> Collect(string? workspaceRoot, string? editorPath, int max = 64)
    {
        var list = new List<Item>();
        if (max < 1)
            return list;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var root = string.IsNullOrWhiteSpace(workspaceRoot) || !Directory.Exists(workspaceRoot)
            ? null
            : Path.GetFullPath(workspaceRoot.Trim());

        void Add(string path, string kind, string rationale)
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

                var rel = root is null
                    ? full
                    : Path.GetRelativePath(root, full).Replace('\\', '/');
                list.Add(new Item(full, rel, kind, rationale));
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
            var ext = Path.GetExtension(editorPath);

            try
            {
                foreach (var f in Directory.EnumerateFiles(dir))
                {
                    var name = Path.GetFileNameWithoutExtension(f);
                    var fExt = Path.GetExtension(f);
                    if (string.Equals(name, stem, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(f, editorPath, StringComparison.OrdinalIgnoreCase))
                    {
                        var kind = CompanionKind(ext, fExt);
                        Add(f, kind, "same stem");
                    }
                    else if (string.Equals(fExt, ".md", StringComparison.OrdinalIgnoreCase)
                             && name.Contains(stem, StringComparison.OrdinalIgnoreCase))
                    {
                        Add(f, "doc", "nearby md");
                    }
                    else if (IsTestCompanion(stem, name, fExt))
                    {
                        Add(f, "test", "test companion");
                    }
                }
            }
            catch
            {
                /* skip */
            }

            // sibling Tests directory one level up (src/Foo.cs → tests/FooTests.cs)
            TryAddTestNear(dir, stem, Add);
        }

        foreach (var hci in GlassRelatedFilesIdeProbe.Collect(root, editorPath, max: Math.Max(1, max - list.Count)))
            Add(hci.FullPath, hci.Kind, hci.Rationale);

        if (root is not null)
        {
            foreach (var rel in new[] { "docs", "docs/adr", "README.md" })
            {
                var p = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(p))
                    Add(p, "workspace", rel);
                else if (Directory.Exists(p))
                {
                    try
                    {
                        foreach (var f in Directory.EnumerateFiles(p, "*.md").Take(12))
                            Add(f, "workspace", rel);
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

    static string CompanionKind(string editorExt, string otherExt)
    {
        if (IsXamlPair(editorExt, otherExt))
            return "xaml_pair";
        if (string.Equals(otherExt, ".md", StringComparison.OrdinalIgnoreCase))
            return "doc";
        return "sibling";
    }

    static bool IsXamlPair(string a, string b)
    {
        static bool IsXaml(string e) =>
            e.Equals(".xaml", StringComparison.OrdinalIgnoreCase)
            || e.Equals(".axaml", StringComparison.OrdinalIgnoreCase);
        static bool IsCs(string e) => e.Equals(".cs", StringComparison.OrdinalIgnoreCase);
        return (IsXaml(a) && IsCs(b)) || (IsCs(a) && IsXaml(b));
    }

    static bool IsTestCompanion(string stem, string otherStem, string otherExt)
    {
        if (!otherExt.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            return false;
        return otherStem.Equals(stem + "Tests", StringComparison.OrdinalIgnoreCase)
               || otherStem.Equals(stem + "Test", StringComparison.OrdinalIgnoreCase)
               || otherStem.Equals("Test" + stem, StringComparison.OrdinalIgnoreCase);
    }

    static void TryAddTestNear(string editorDir, string stem, Action<string, string, string> add)
    {
        try
        {
            var parent = Directory.GetParent(editorDir)?.FullName;
            if (parent is null)
                return;
            foreach (var candidate in new[]
                     {
                         Path.Combine(parent, "tests", stem + "Tests.cs"),
                         Path.Combine(parent, "Tests", stem + "Tests.cs"),
                         Path.Combine(parent, stem + ".Tests", stem + "Tests.cs"),
                     })
            {
                if (File.Exists(candidate))
                    add(candidate, "test", "near tests dir");
            }
        }
        catch
        {
            /* skip */
        }
    }
}

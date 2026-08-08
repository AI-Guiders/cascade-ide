#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>CIDE Go to File peel — file list from open <see cref="Models.SolutionItem"/> tree (same LoadSolution SSOT).</summary>
public static class GlassGoToFileIndex
{
    public const int MaxResults = 80;

    public sealed record Hit(string Title, string FullPath, string Relative);

    public static IReadOnlyList<Hit> Search(Models.SolutionItem? root, string? workspaceRoot, string? filter, int max = MaxResults)
    {
        var files = new List<string>();
        if (root is not null)
            CollectFiles(root, files);
        else if (!string.IsNullOrWhiteSpace(workspaceRoot) && Directory.Exists(workspaceRoot))
            CollectFromDisk(workspaceRoot.Trim(), files);

        var term = (filter ?? "").Trim();
        IEnumerable<string> q = files;
        if (term.Length > 0)
        {
            q = files.Where(p =>
                Path.GetFileName(p).Contains(term, StringComparison.OrdinalIgnoreCase)
                || p.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var ws = workspaceRoot?.Trim() ?? "";
        return q
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .Select(p =>
            {
                var rel = ws.Length > 0 && p.StartsWith(ws, StringComparison.OrdinalIgnoreCase)
                    ? p[ws.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    : p;
                return new Hit(Path.GetFileName(p), p, rel);
            })
            .ToList();
    }

    static void CollectFiles(Models.SolutionItem item, List<string> sink)
    {
        if (!string.IsNullOrWhiteSpace(item.FullPath)
            && File.Exists(item.FullPath)
            && !item.FullPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            && !item.FullPath.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase)
            && !item.FullPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
            && !item.FullPath.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
            sink.Add(item.FullPath);

        foreach (var c in item.Children)
            CollectFiles(c, sink);
    }

    static void CollectFromDisk(string root, List<string> sink)
    {
        try
        {
            foreach (var f in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(f);
                if (name.Equals("bin", StringComparison.OrdinalIgnoreCase)
                    || f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    || f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                    || f.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    continue;
                var ext = Path.GetExtension(f);
                if (ext is ".cs" or ".xaml" or ".axaml" or ".md" or ".json" or ".toml" or ".ps1" or ".py" or ".txt")
                    sink.Add(f);
                if (sink.Count >= 2000)
                    return;
            }
        }
        catch
        {
            // best-effort index
        }
    }
}

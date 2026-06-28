namespace CascadeIDE.Services.Roslyn;

/// <summary>MSBuildWorkspace accepts .sln/.csproj — not .slnx or folder workspace roots.</summary>
public static class RoslynEditorWorkspacePath
{
    public static string? Resolve(string? workspaceSolutionPath, string filePath, string? workspaceRoot = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        if (!string.IsNullOrWhiteSpace(workspaceSolutionPath))
        {
            var normalized = Path.GetFullPath(workspaceSolutionPath.Trim());
            var ext = Path.GetExtension(normalized);
            if (ext.Equals(".sln", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".fsproj", StringComparison.OrdinalIgnoreCase))
                return normalized;

            if (ext.Equals(".slnx", StringComparison.OrdinalIgnoreCase))
            {
                var fromSlnx = TryResolveCsprojFromSlnx(normalized, filePath);
                if (!string.IsNullOrWhiteSpace(fromSlnx))
                    return fromSlnx;
            }
        }

        return TryFindNearestCsproj(filePath, workspaceRoot);
    }

    private static string? TryResolveCsprojFromSlnx(string slnxPath, string filePath)
    {
        var slnxDir = Path.GetDirectoryName(slnxPath);
        if (string.IsNullOrWhiteSpace(slnxDir) || !File.Exists(slnxPath))
            return null;

        var fullFile = Path.GetFullPath(filePath);
        string? first = null;
        try
        {
            var xml = System.Xml.Linq.XDocument.Load(slnxPath);
            foreach (var el in xml.Descendants())
            {
                var rel = el.Attribute("Path")?.Value?.Trim();
                if (string.IsNullOrWhiteSpace(rel) || !rel.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    continue;

                var candidate = Path.GetFullPath(Path.Combine(slnxDir, rel.Replace('/', Path.DirectorySeparatorChar)));
                if (!File.Exists(candidate))
                    continue;

                first ??= candidate;
                var projDir = Path.GetDirectoryName(candidate);
                if (!string.IsNullOrWhiteSpace(projDir)
                    && fullFile.StartsWith(projDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }
        }
        catch
        {
            return TryFindNearestCsproj(filePath, slnxDir);
        }

        return first ?? TryFindNearestCsproj(filePath, slnxDir);
    }

    private static string? TryFindNearestCsproj(string filePath, string? searchRoot)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(filePath));
        var root = string.IsNullOrWhiteSpace(searchRoot) ? null : Path.GetFullPath(searchRoot.Trim());

        while (!string.IsNullOrEmpty(dir))
        {
            string[] projects;
            try
            {
                projects = Directory.GetFiles(dir, "*.csproj");
            }
            catch
            {
                break;
            }

            if (projects.Length == 1)
                return projects[0];

            if (projects.Length > 1)
            {
                var sibling = projects.FirstOrDefault(p =>
                    string.Equals(Path.GetDirectoryName(p), dir, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(sibling))
                    return sibling;
                return projects[0];
            }

            if (root is not null
                && !dir.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(dir, root, StringComparison.OrdinalIgnoreCase))
                break;

            var parent = Directory.GetParent(dir);
            if (parent is null)
                break;
            dir = parent.FullName;
        }

        return null;
    }
}

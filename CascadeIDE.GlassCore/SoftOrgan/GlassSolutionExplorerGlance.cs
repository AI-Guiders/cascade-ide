#nullable enable
using System.Text;
using System.Text.RegularExpressions;

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// Glass MFD SolutionExplorer: .sln project parse (text glance + WPF TreeView peel).
/// Full interactive tree stays CIDE Avalonia <c>SolutionExplorerView</c>.
/// </summary>
public static partial class GlassSolutionExplorerGlance
{
    public readonly record struct SlnProject(string Name, string RelativePath);

    static readonly Regex ProjectLine = ProjectLineRegex();

    /// <summary>
    /// CIDE <c>SolutionParser.Load</c> peel for Glass SE:
    /// .csproj/.fsproj → standalone one project; .sln → parse; else discover under workspace root.
    /// </summary>
    public static IReadOnlyList<SlnProject>? TryLoadProjects(string? workspaceRoot, string? solutionOrProjectPath = null)
    {
        if (!string.IsNullOrWhiteSpace(solutionOrProjectPath) && File.Exists(solutionOrProjectPath))
        {
            var path = solutionOrProjectPath.Trim();
            var ext = Path.GetExtension(path);
            if (ext is ".csproj" or ".fsproj")
            {
                // CIDE LoadStandaloneProject — virtual solution with one project node.
                return [new SlnProject(Path.GetFileNameWithoutExtension(path)!, Path.GetFileName(path)!)];
            }

            if (ext is ".sln" or ".slnx" or ".slnf")
            {
                try
                {
                    var projects = ParseProjects(File.ReadAllText(path));
                    if (projects.Count > 0)
                        return projects;
                }
                catch
                {
                    // fall through to workspace discovery
                }
            }
        }

        return TryLoadProjectsFromWorkspaceRoot(workspaceRoot);
    }

    /// <summary>Discover a .sln under workspace and load projects; else lone *.csproj/*.fsproj (null if none).</summary>
    public static IReadOnlyList<SlnProject>? TryLoadProjectsFromWorkspaceRoot(string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || !Directory.Exists(workspaceRoot))
            return null;

        var root = workspaceRoot.Trim();
        var sln = TryFindSln(root);
        if (sln is not null)
        {
            try
            {
                var text = File.ReadAllText(sln);
                var projects = ParseProjects(text);
                if (projects.Count > 0)
                    return projects;
            }
            catch
            {
                // fall through to lone projects
            }
        }

        return TryLoadLoneProjects(root);
    }

    /// <summary>CIDE-aligned format: prefer open solution/project path, else workspace discovery.</summary>
    public static string? TryFormat(string? workspaceRoot, string? solutionOrProjectPath = null)
    {
        var projects = TryLoadProjects(workspaceRoot, solutionOrProjectPath);
        if (projects is null || projects.Count == 0)
            return null;

        var sb = new StringBuilder();
        sb.Append("SolutionExplorer glance");
        if (!string.IsNullOrWhiteSpace(solutionOrProjectPath) && File.Exists(solutionOrProjectPath))
        {
            var ext = Path.GetExtension(solutionOrProjectPath);
            if (ext is ".csproj" or ".fsproj")
                sb.Append(" · standalone project (CIDE LoadStandaloneProject)");
            else
                sb.Append(" · ").Append(Path.GetFileName(solutionOrProjectPath.Trim()));
        }
        else if (TryFindSln(workspaceRoot?.Trim() ?? "") is { } sln)
            sb.Append(" · ").Append(Path.GetFileName(sln));
        else
            sb.Append(" · lone project");

        sb.Append(" · projects=").Append(projects.Count).AppendLine();
        sb.AppendLine();
        foreach (var p in projects)
            sb.Append("· ").AppendLine(p.Name);
        sb.AppendLine();
        sb.Append("(Full tree = CIDE Avalonia SolutionExplorerView; Glass WPF TreeView = flat projects.)");
        return sb.ToString().TrimEnd();
    }

    /// <summary>Discover a .sln under workspace and format project list; else lone project peel (null if none).</summary>
    public static string? TryFormatFromWorkspaceRoot(string? workspaceRoot) =>
        TryFormat(workspaceRoot, solutionOrProjectPath: null);

    /// <summary>Workspace without .sln: top-level *.csproj / *.fsproj as SE nodes.</summary>
    public static IReadOnlyList<SlnProject>? TryLoadLoneProjects(string workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || !Directory.Exists(workspaceRoot))
            return null;

        try
        {
            var list = Directory.EnumerateFiles(workspaceRoot, "*.csproj", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateFiles(workspaceRoot, "*.fsproj", SearchOption.TopDirectoryOnly))
                .OrderBy(static p => p, StringComparer.OrdinalIgnoreCase)
                .Select(static p => new SlnProject(Path.GetFileNameWithoutExtension(p)!, Path.GetFileName(p)!))
                .ToList();
            return list.Count == 0 ? null : list;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Parse Visual Studio .sln project lines (testable; no I/O).</summary>
    public static IReadOnlyList<SlnProject> ParseProjects(string slnText)
    {
        if (string.IsNullOrWhiteSpace(slnText))
            return [];

        var list = new List<SlnProject>();
        foreach (Match m in ProjectLine.Matches(slnText))
        {
            if (m.Groups[1].Value is not { Length: > 0 } name)
                continue;
            var rel = m.Groups.Count > 2 ? m.Groups[2].Value.Trim() : "";
            list.Add(new SlnProject(name, rel));
        }

        return list;
    }

    /// <summary>Parse Visual Studio .sln project lines into MFD body text (testable; no I/O).</summary>
    public static string? TryFormatFromSlnText(string slnText, string? slnPath = null)
    {
        var names = ParseProjects(slnText);
        if (names.Count == 0)
            return null;

        var sb = new StringBuilder();
        sb.Append("SolutionExplorer glance");
        if (!string.IsNullOrWhiteSpace(slnPath))
            sb.Append(" · ").Append(Path.GetFileName(slnPath.Trim()));
        sb.Append(" · projects=").Append(names.Count).AppendLine();
        sb.AppendLine();
        foreach (var p in names)
            sb.Append("· ").AppendLine(p.Name);
        sb.AppendLine();
        sb.Append("(Full tree = CIDE Avalonia SolutionExplorerView; Glass WPF TreeView = flat projects.)");
        return sb.ToString().TrimEnd();
    }

    public const int MaxCsFilesPerProject = 200;

    /// <summary>Enumerate *.cs under a .csproj directory (cap <see cref="MaxCsFilesPerProject"/>).</summary>
    public static IReadOnlyList<string> EnumerateProjectCsFiles(string? projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath) || !File.Exists(projectPath))
            return [];

        try
        {
            var dir = Path.GetDirectoryName(projectPath);
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return [];

            return Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
                .OrderBy(static p => p, StringComparer.OrdinalIgnoreCase)
                .Take(MaxCsFilesPerProject)
                .Select(Path.GetFullPath)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>Resolve absolute project path under workspace (null if missing).</summary>
    public static string? TryResolveProjectPath(string? workspaceRoot, SlnProject project)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || string.IsNullOrWhiteSpace(project.RelativePath))
            return null;

        try
        {
            var full = Path.GetFullPath(Path.Combine(
                workspaceRoot.Trim(),
                project.RelativePath.Replace('\\', Path.DirectorySeparatorChar)));
            return File.Exists(full) ? full : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Public resolve for Glass Build redirected peel (same preferred CascadeIDE.sln).</summary>
    public static string? TryResolveSlnPath(string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return null;

        try
        {
            return TryFindSln(workspaceRoot.Trim());
        }
        catch
        {
            return null;
        }
    }

    static string? TryFindSln(string workspaceRoot)
    {
        try
        {
            var preferred = Path.Combine(workspaceRoot, "CascadeIDE.sln");
            if (File.Exists(preferred))
                return preferred;

            return Directory.EnumerateFiles(workspaceRoot, "*.sln", SearchOption.TopDirectoryOnly)
                .OrderBy(static p => p, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    [GeneratedRegex(
        "^\\s*Project\\(\"[^\"]+\"\\)\\s*=\\s*\"([^\"]+)\"\\s*,\\s*\"([^\"]*)\"",
        RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex ProjectLineRegex();
}

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

    /// <summary>Discover a .sln under workspace and load projects (null if none).</summary>
    public static IReadOnlyList<SlnProject>? TryLoadProjectsFromWorkspaceRoot(string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || !Directory.Exists(workspaceRoot))
            return null;

        var sln = TryFindSln(workspaceRoot.Trim());
        if (sln is null)
            return null;

        try
        {
            var text = File.ReadAllText(sln);
            var projects = ParseProjects(text);
            return projects.Count == 0 ? null : projects;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Discover a .sln under workspace and format project list (null if none).</summary>
    public static string? TryFormatFromWorkspaceRoot(string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || !Directory.Exists(workspaceRoot))
            return null;

        var sln = TryFindSln(workspaceRoot.Trim());
        if (sln is null)
            return null;

        try
        {
            var text = File.ReadAllText(sln);
            return TryFormatFromSlnText(text, sln);
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

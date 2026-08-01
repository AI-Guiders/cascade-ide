#nullable enable
using System.Text;
using System.Text.RegularExpressions;

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// Glass MFD SolutionExplorer glance: .sln project names (text peel).
/// Full interactive tree stays CIDE Avalonia <c>SolutionExplorerView</c>.
/// </summary>
public static partial class GlassSolutionExplorerGlance
{
    static readonly Regex ProjectLine = ProjectLineRegex();

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

    /// <summary>Parse Visual Studio .sln project lines into MFD body text (testable; no I/O).</summary>
    public static string? TryFormatFromSlnText(string slnText, string? slnPath = null)
    {
        if (string.IsNullOrWhiteSpace(slnText))
            return null;

        var names = new List<string>();
        foreach (Match m in ProjectLine.Matches(slnText))
        {
            if (m.Groups[1].Value is { Length: > 0 } name)
                names.Add(name);
        }

        if (names.Count == 0)
            return null;

        var sb = new StringBuilder();
        sb.Append("SolutionExplorer glance");
        if (!string.IsNullOrWhiteSpace(slnPath))
            sb.Append(" · ").Append(Path.GetFileName(slnPath.Trim()));
        sb.Append(" · projects=").Append(names.Count).AppendLine();
        sb.AppendLine();
        foreach (var n in names)
            sb.Append("· ").AppendLine(n);
        sb.AppendLine();
        sb.Append("(Full tree = CIDE Avalonia SolutionExplorerView; Glass WPF TreeView later.)");
        return sb.ToString().TrimEnd();
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
        "^\\s*Project\\(\"[^\"]+\"\\)\\s*=\\s*\"([^\"]+)\"",
        RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex ProjectLineRegex();
}

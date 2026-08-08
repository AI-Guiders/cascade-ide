#nullable enable
using System.Text;
using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.Models;

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// Glass SE helpers on top of CIDE SSOT (<see cref="SolutionParser"/> / <see cref="FolderWorkspaceTreeBuilder"/>).
/// No parallel .sln invent — load goes through parser.
/// </summary>
public static class GlassSolutionExplorerGlance
{
    /// <summary>CIDE LoadSolution peel: file → <see cref="SolutionParser"/>; folder → <see cref="FolderWorkspaceTreeBuilder"/>.</summary>
    public static SolutionItem? TryLoad(string? path, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Путь пустой.";
            return null;
        }

        var trimmed = path.Trim();
        if (Directory.Exists(trimmed))
            return FolderWorkspaceTreeBuilder.TryBuild(trimmed, out error);

        if (!File.Exists(trimmed))
        {
            error = "Файл не найден: " + trimmed;
            return null;
        }

        return SolutionParser.Load(trimmed, out error);
    }

    public static string? TryFormat(SolutionItem? root, string? pathHint = null)
    {
        if (root is null)
            return null;

        var sb = new StringBuilder();
        sb.Append("SolutionExplorer · CIDE SolutionParser");
        if (!string.IsNullOrWhiteSpace(pathHint))
            sb.Append(" · ").Append(Path.GetFileName(pathHint.Trim()));
        else if (!string.IsNullOrWhiteSpace(root.FullPath))
            sb.Append(" · ").Append(Path.GetFileName(root.FullPath));
        sb.Append(" · nodes=").Append(CountNodes(root)).AppendLine();
        sb.AppendLine();
        AppendOutline(sb, root, depth: 0, remaining: 40);
        sb.AppendLine();
        sb.Append("(Tree paint = Glass WPF from same SolutionItem SSOT as Avalonia SE.)");
        return sb.ToString().TrimEnd();
    }

    /// <summary>WorkspaceHealth peel: prefer open solution path, else discover *.sln under root.</summary>
    public static string? TryResolveSlnPath(string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return null;

        try
        {
            var root = workspaceRoot.Trim();
            var preferred = Path.Combine(root, "CascadeIDE.sln");
            if (File.Exists(preferred))
                return preferred;

            return Directory.EnumerateFiles(root, "*.sln", SearchOption.TopDirectoryOnly)
                .OrderBy(static p => p, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    static int CountNodes(SolutionItem item)
    {
        var n = 1;
        foreach (var c in item.Children)
            n += CountNodes(c);
        return n;
    }

    static void AppendOutline(StringBuilder sb, SolutionItem item, int depth, int remaining)
    {
        if (remaining <= 0)
            return;
        if (depth > 0)
            sb.Append(' ', (depth - 1) * 2).Append("· ").AppendLine(item.Title);
        foreach (var c in item.Children)
            AppendOutline(sb, c, depth + 1, remaining - 1);
    }
}

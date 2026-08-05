#nullable enable
using System.Text;

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// Glass MFD WorkspaceHealth: filesystem presence glance (no SoftOrgan invent).
/// Live strip/page SSOT stays Avalonia IdeHealth / WorkspaceHealth CCU (ADR 0089/0095).
/// </summary>
public static class GlassWorkspaceHealthGlance
{
    public readonly record struct WorkspaceFsStatus(
        string RootPath,
        bool RootExists,
        bool HasGit,
        string? SlnPath,
        bool HasCascadeIdeDir);

    public static WorkspaceFsStatus? TryProbe(string? workspaceRoot)
    {
        try
        {
            var root = GlassWorkspaceClimb.ResolveRoot(workspaceRoot);
            if (root is null)
                return new WorkspaceFsStatus("—", false, false, null, false);

            var exists = Directory.Exists(root);
            if (!exists)
                return new WorkspaceFsStatus(root, false, false, null, false);

            var git = Directory.Exists(Path.Combine(root, ".git"))
                      || File.Exists(Path.Combine(root, ".git"));
            var sln = GlassSolutionExplorerGlance.TryResolveSlnPath(root);
            var cascade = Directory.Exists(Path.Combine(root, ".cascade-ide"));
            return new WorkspaceFsStatus(root, true, git, sln, cascade);
        }
        catch
        {
            return new WorkspaceFsStatus("—", false, false, null, false);
        }
    }

    public static string? TryFormatFromWorkspaceRoot(string? workspaceRoot)
    {
        var probe = TryProbe(workspaceRoot);
        return probe is null ? null : Format(probe.Value);
    }

    /// <summary>Testable formatter (no I/O).</summary>
    public static string Format(WorkspaceFsStatus status)
    {
        var level = !status.RootExists
            ? "MISSING"
            : status.SlnPath is not null || status.HasGit
                ? "READY"
                : "THIN";

        var sb = new StringBuilder();
        sb.Append("WorkspaceHealth glance · ").AppendLine(level);

        try
        {
            var name = Path.GetFileName(
                status.RootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!string.IsNullOrWhiteSpace(name))
                sb.Append("ws · ").AppendLine(name);
        }
        catch
        {
            // ignore
        }

        sb.Append("root · ").AppendLine(status.RootExists ? "ok" : "missing");
        sb.Append("git · ").AppendLine(status.HasGit ? "yes" : "no");
        sb.Append("sln · ").AppendLine(
            status.SlnPath is { } sln ? Path.GetFileName(sln) : "none");
        sb.Append(".cascade-ide · ").AppendLine(status.HasCascadeIdeDir ? "yes" : "no");

        if (level == "THIN")
            sb.AppendLine("· no .sln / .git yet — open a solution workspace");

        sb.AppendLine();
        sb.AppendLine("┌ host ──────────────┐");
        sb.AppendLine("│ ■ Glass FS status    │");
        sb.AppendLine("│ □ Avalonia IdeHealth │");
        sb.AppendLine("└─────────────────────┘");
        return sb.ToString().TrimEnd();
    }
}

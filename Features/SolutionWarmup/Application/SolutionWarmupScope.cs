#nullable enable

namespace CascadeIDE.Features.SolutionWarmup.Application;

/// <summary>Ключ прогона прогрева (workspace + solution).</summary>
public readonly record struct SolutionWarmupScope(string WorkspaceRoot, string? SolutionPath)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(WorkspaceRoot);

    public bool Matches(string workspaceRoot, string? solutionPath)
    {
        if (!string.Equals(WorkspaceRoot, workspaceRoot.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            return false;

        var a = string.IsNullOrWhiteSpace(SolutionPath) ? null : SolutionPath.Trim();
        var b = string.IsNullOrWhiteSpace(solutionPath) ? null : solutionPath.Trim();
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}

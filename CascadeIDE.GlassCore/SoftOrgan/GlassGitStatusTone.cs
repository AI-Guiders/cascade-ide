#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>Porcelain XY → list tint bucket for Glass Git (human scan).</summary>
public static class GlassGitStatusTone
{
    public const string Plain = "plain";
    public const string Change = "change";
    public const string Add = "add";
    public const string Delete = "delete";
    public const string Untracked = "untracked";

    public static string Name(string? xy)
    {
        if (string.IsNullOrEmpty(xy))
            return Plain;

        if (xy.Contains('?', StringComparison.Ordinal))
            return Untracked;

        // Prefer delete over modify when both present (e.g. "MD").
        if (xy.Contains('D', StringComparison.Ordinal))
            return Delete;

        if (xy.Contains('A', StringComparison.Ordinal)
            || xy.Contains('C', StringComparison.Ordinal)
            || xy.Contains('U', StringComparison.Ordinal))
            return Add;

        if (xy.Contains('M', StringComparison.Ordinal)
            || xy.Contains('R', StringComparison.Ordinal)
            || xy.Contains('T', StringComparison.Ordinal))
            return Change;

        return Plain;
    }
}

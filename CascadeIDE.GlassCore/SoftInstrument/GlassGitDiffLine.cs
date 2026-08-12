#nullable enable

namespace CascadeIDE.SoftInstrument;

/// <summary>Unified-diff line kind for Glass MFD tint (no WPF).</summary>
public enum GlassGitDiffLineKind
{
    Context,
    Add,
    Delete,
    Hunk,
    Meta,
}

public static class GlassGitDiffLine
{
    public static GlassGitDiffLineKind Classify(string? line)
    {
        if (string.IsNullOrEmpty(line))
            return GlassGitDiffLineKind.Context;

        if (line.StartsWith("@@", StringComparison.Ordinal))
            return GlassGitDiffLineKind.Hunk;

        if (line.StartsWith("diff ", StringComparison.Ordinal)
            || line.StartsWith("index ", StringComparison.Ordinal)
            || line.StartsWith("---", StringComparison.Ordinal)
            || line.StartsWith("+++", StringComparison.Ordinal)
            || line.StartsWith("new file", StringComparison.Ordinal)
            || line.StartsWith("deleted file", StringComparison.Ordinal)
            || line.StartsWith("similarity", StringComparison.Ordinal)
            || line.StartsWith("rename", StringComparison.Ordinal)
            || line.StartsWith("Binary", StringComparison.Ordinal))
            return GlassGitDiffLineKind.Meta;

        // Unified diff body: first char is marker; "+++"/"---" already Meta.
        if (line[0] == '+')
            return GlassGitDiffLineKind.Add;
        if (line[0] == '-')
            return GlassGitDiffLineKind.Delete;

        return GlassGitDiffLineKind.Context;
    }
}

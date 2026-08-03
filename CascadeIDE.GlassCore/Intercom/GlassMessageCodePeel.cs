#nullable enable

using System.Text.RegularExpressions;

namespace CascadeIDE.Intercom;

/// <summary>Thin message↔code lane: bare path:line + backtick file refs (attach chips extension).</summary>
public static partial class GlassMessageCodePeel
{
    public static IReadOnlyList<GlassAttachChip> PeelBareRefs(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return [];

        var list = new List<GlassAttachChip>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match m in BarePathLine().Matches(body))
        {
            var path = m.Groups["path"].Value;
            var line = m.Groups["line"].Value;
            var end = m.Groups["end"].Success ? m.Groups["end"].Value : null;
            var inner = end is { Length: > 0 } ? $"{path}:{line}-{end}" : $"{path}:{line}";
            if (!GlassAttachChipPeel.TryParseBracketInner(inner, out var chip))
                continue;
            if (!seen.Add(chip.Bracket))
                continue;
            list.Add(chip);
        }

        foreach (Match m in BacktickPath().Matches(body))
        {
            var inner = m.Groups["inner"].Value.Trim();
            if (!GlassAttachChipPeel.TryParseBracketInner(inner, out var chip))
                continue;
            if (!seen.Add(chip.Bracket))
                continue;
            list.Add(chip);
        }

        return list;
    }

    public static IReadOnlyList<GlassAttachChip> MergeWithAttach(
        IReadOnlyList<GlassAttachChip> attachChips,
        string? body)
    {
        var bare = PeelBareRefs(body);
        if (bare.Count == 0)
            return attachChips;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<GlassAttachChip>(attachChips.Count + bare.Count);
        foreach (var c in attachChips)
        {
            if (seen.Add(c.Bracket))
                list.Add(c);
        }

        foreach (var c in bare)
        {
            if (seen.Add(c.Bracket))
                list.Add(c);
        }

        return list;
    }

    // Foo.cs:12 · src/Foo.cs:10-20 — not inside [...]
    [GeneratedRegex(
        @"(?<![\[\w/\\])(?<path>(?:[\w.-]+/)*[\w.-]+\.(?:cs|xaml|axaml|md|json|toml|ps1)):(?<line>\d+)(?:-(?<end>\d+))?(?![\]\w])",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex BarePathLine();

    // `path.cs` or `path.cs:12`
    [GeneratedRegex(
        @"`(?<inner>(?:[\w.-]+/)*[\w.-]+\.(?:cs|xaml|axaml|md|json|toml|ps1)(?::\d+(?:-\d+)?)?)`",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex BacktickPath();
}

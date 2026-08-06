#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>
/// A4 denser thin: find feed messages by attach chip / <c>[path:line]</c>
/// (reuses <see cref="GlassAttachChipPeel"/> + message select — not Avalonia IntercomCodeRef SoftFL invent).
/// </summary>
public static class GlassIntercomMessageFind
{
    public readonly record struct Hit(int Ordinal, string Body, IReadOnlyList<GlassAttachChip>? Chips);

    public static bool TryParseNeedle(string? argsTail, out GlassAttachChip needle, out string error)
    {
        needle = new GlassAttachChip("", "");
        error = "";
        var tail = (argsTail ?? "").Trim();
        if (tail.Length == 0)
        {
            error = "usage: /intercom message find [path:line] · path.cs · path.cs:10-20";
            return false;
        }

        var fromBody = GlassAttachChipPeel.FromBody(tail.StartsWith('[') ? tail : $"[{tail}]");
        if (fromBody.Count > 0)
        {
            needle = fromBody[0];
            return true;
        }

        if (GlassAttachChipPeel.TryParseBracketInner(tail.Trim('[', ']'), out needle)
            && !string.IsNullOrWhiteSpace(needle.File))
            return true;

        error = "could not parse code needle — expect [path:line] or path[:line[-line]]";
        return false;
    }

    public static IReadOnlyList<int> MatchOrdinals(
        GlassAttachChip needle,
        IEnumerable<Hit> feed)
    {
        var needleFile = NormalizeFile(needle.File);
        if (needleFile.Length == 0)
            return [];

        var hits = new List<int>();
        foreach (var hit in feed)
        {
            var ordinal = hit.Ordinal > 0 ? hit.Ordinal : 0;
            if (ordinal <= 0)
                continue;

            var chips = hit.Chips is { Count: > 0 }
                ? hit.Chips
                : GlassAttachChipPeel.FromBody(hit.Body);

            foreach (var chip in chips)
            {
                if (!FileMatches(needleFile, NormalizeFile(chip.File)))
                    continue;
                if (!LinesOverlap(needle, chip))
                    continue;
                hits.Add(ordinal);
                break;
            }
        }

        return hits;
    }

    public static string FormatResult(GlassAttachChip needle, IReadOnlyList<int> ordinals)
    {
        var label = needle.Bracket;
        if (ordinals.Count == 0)
            return $"find {label} · 0 hits";
        if (ordinals.Count == 1)
            return $"find {label} · 1 hit → #{ordinals[0]}";
        return $"find {label} · {ordinals.Count} hits → #{string.Join(", #", ordinals)}";
    }

    static string NormalizeFile(string? file) =>
        (file ?? "").Trim().Replace('\\', '/');

    static bool FileMatches(string needle, string candidate)
    {
        if (candidate.Length == 0)
            return false;
        if (string.Equals(needle, candidate, StringComparison.OrdinalIgnoreCase))
            return true;

        // suffix match: Foo.cs ↔ src/Foo.cs
        return candidate.EndsWith('/' + needle, StringComparison.OrdinalIgnoreCase)
               || needle.EndsWith('/' + candidate, StringComparison.OrdinalIgnoreCase)
               || Path.GetFileName(candidate).Equals(Path.GetFileName(needle), StringComparison.OrdinalIgnoreCase);
    }

    static bool LinesOverlap(GlassAttachChip needle, GlassAttachChip chip)
    {
        // needle without lines → any chip on that file
        if (needle.LineStart is not int ns || ns <= 0)
            return true;
        if (chip.LineStart is not int cs || cs <= 0)
            return true; // file-only chip matches any line needle

        var ne = needle.LineEnd is int nEnd && nEnd >= ns ? nEnd : ns;
        var ce = chip.LineEnd is int cEnd && cEnd >= cs ? cEnd : cs;
        return ns <= ce && cs <= ne;
    }
}

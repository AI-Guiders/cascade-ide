namespace CascadeIDE.Services;

/// <summary>Prefix and camelCase-acronym filtering for completion (e.g. SB → StringBuilder, SByte).</summary>
public static class CSharpCompletionMatcher
{
    public enum MatchKind
    {
        None = 0,
        Acronym = 1,
        Prefix = 2,
    }

    public static MatchKind GetMatchKind(string name, string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return MatchKind.Prefix;
        if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return MatchKind.Prefix;
        return MatchesCamelCaseAcronym(name, prefix) ? MatchKind.Acronym : MatchKind.None;
    }

    public static bool Matches(string name, string prefix) =>
        GetMatchKind(name, prefix) != MatchKind.None;

    public static int CompareByRelevance(string nameA, string nameB, string prefix)
    {
        var kindA = GetMatchKind(nameA, prefix);
        var kindB = GetMatchKind(nameB, prefix);
        if (kindA != kindB)
            return kindB.CompareTo(kindA);

        if (prefix.Length > 0)
        {
            var posA = nameA.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? 0 : IndexOfAcronym(nameA, prefix);
            var posB = nameB.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? 0 : IndexOfAcronym(nameB, prefix);
            if (posA != posB)
                return posA.CompareTo(posB);
        }

        return string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
    }

    private static int IndexOfAcronym(string name, string prefix)
    {
        var pi = 0;
        for (var ni = 0; ni < name.Length && pi < prefix.Length; ni++)
        {
            if (char.ToUpperInvariant(name[ni]) == char.ToUpperInvariant(prefix[pi]))
                pi++;
        }

        return pi == prefix.Length ? 0 : int.MaxValue;
    }

    private static bool MatchesCamelCaseAcronym(string name, string prefix)
    {
        if (prefix.Length == 0)
            return true;

        var pi = 0;
        for (var ni = 0; ni < name.Length && pi < prefix.Length; ni++)
        {
            if (char.ToUpperInvariant(name[ni]) == char.ToUpperInvariant(prefix[pi]))
                pi++;
        }

        return pi == prefix.Length;
    }
}

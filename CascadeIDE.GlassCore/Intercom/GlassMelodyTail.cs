#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>Thin parse of <c>c:</c> tail: alias + optional parametric ints (<c>els:10:20</c> / <c>els;10;20</c>).</summary>
public static class GlassMelodyTail
{
    public static string AliasPrefix(string tailNormalized)
    {
        if (string.IsNullOrEmpty(tailNormalized))
            return "";

        var cut = IndexOfArgSep(tailNormalized);
        return cut < 0 ? tailNormalized : tailNormalized[..cut];
    }

    public static string ArgRemainder(string tailNormalized)
    {
        if (string.IsNullOrEmpty(tailNormalized))
            return "";

        var cut = IndexOfArgSep(tailNormalized);
        return cut < 0 || cut + 1 >= tailNormalized.Length
            ? ""
            : tailNormalized[(cut + 1)..].Trim();
    }

    /// <summary>1-based line range: <c>40</c> or <c>10:20</c> / <c>10;20</c> / <c>10 20</c>.</summary>
    public static bool TryParseLineRange(string? args, out int startLine, out int? endLine)
    {
        startLine = 0;
        endLine = null;
        if (string.IsNullOrWhiteSpace(args))
            return false;

        var parts = args.Replace(';', ':').Replace(' ', ':')
            .Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0 || !int.TryParse(parts[0], out startLine) || startLine <= 0)
            return false;

        if (parts.Length >= 2 && int.TryParse(parts[1], out var end) && end > 0)
            endLine = end;

        return true;
    }

    static int IndexOfArgSep(string s)
    {
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c is ':' or ';' or ' ')
                return i;
        }

        return -1;
    }
}

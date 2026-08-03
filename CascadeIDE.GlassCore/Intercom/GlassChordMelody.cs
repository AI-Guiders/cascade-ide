#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>Glass Ctrl+K AwaitMelodyTail — Avalonia-free peel of <c>c:</c> chord input (ADR 0060).</summary>
public static class GlassChordMelody
{
    public const int MaxSuggestions = 25;

    static readonly HashSet<string> ParametricAliases =
        new(StringComparer.Ordinal) { "els", "wai" };

    public static string NormalizeInput(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";

        Span<char> buf = stackalloc char[raw.Length];
        var n = 0;
        foreach (var c in raw)
        {
            var lower = c is >= 'A' and <= 'Z' ? (char)(c + 32) : c;
            if (lower is >= 'a' and <= 'z' or >= '0' and <= '9' or ':' or ';' or '.' or '/' or '-' or '_')
                buf[n++] = lower;
        }

        return n == 0 ? "" : new string(buf[..n]);
    }

    public static IReadOnlyList<GlassChordMelodyEntry> FilterSuggestions(string tailNormalized, int max = MaxSuggestions)
    {
        if (max < 1)
            return [];

        var aliases = GlassIntentMelodyCatalog.FilterByTailPrefix(tailNormalized);
        return aliases
            .Take(max)
            .Select(a => new GlassChordMelodyEntry(
                a.Alias,
                a.CommandId,
                a.Help,
                GlassMelodyGlassActions.TryMapCommandId(a.CommandId, out _)))
            .ToArray();
    }

    public static bool IsParametricTailPrefix(string tailNormalized) =>
        !string.IsNullOrEmpty(tailNormalized)
        && (ParametricAliases.Contains(GlassMelodyTail.AliasPrefix(tailNormalized))
            || tailNormalized.Contains(':')
            || tailNormalized.Contains(';'));

    public static bool HasStrictLongerAliasPrefix(string tailNormalized) =>
        !string.IsNullOrEmpty(tailNormalized)
        && GlassIntentMelodyCatalog.All().Any(a =>
            a.Alias.Length > tailNormalized.Length
            && a.Alias.StartsWith(tailNormalized, StringComparison.Ordinal));

    public static bool ChordDefersInstantExecute(string exactAlias) =>
        ParametricAliases.Contains(exactAlias.Trim());

    public static bool TryResolveExactCommand(string tailNormalized, out string commandId)
    {
        commandId = "";
        if (string.IsNullOrEmpty(tailNormalized))
            return false;

        var alias = GlassMelodyTail.AliasPrefix(tailNormalized);
        if (alias.Length == 0)
            return false;

        var exact = GlassIntentMelodyCatalog.All()
            .FirstOrDefault(a => string.Equals(a.Alias, alias, StringComparison.Ordinal));
        if (exact is null)
            return false;

        if (HasStrictLongerAliasPrefix(tailNormalized))
            return false;

        if (ChordDefersInstantExecute(exact.Alias))
            return false;

        commandId = exact.CommandId;
        return true;
    }

    public static bool TryResolveParametricSelect(string tailNormalized, out int startLine, out int endLine)
    {
        startLine = 0;
        endLine = 0;
        if (!string.Equals(GlassMelodyTail.AliasPrefix(tailNormalized), "els", StringComparison.Ordinal))
            return false;

        if (!GlassMelodyTail.TryParseLineRange(GlassMelodyTail.ArgRemainder(tailNormalized), out startLine, out var endOpt))
            return false;

        endLine = endOpt ?? startLine;
        return true;
    }

    public static bool TryResolveParametricWebAi(string tailNormalized, out string? urlPayload)
    {
        urlPayload = null;
        if (!string.Equals(GlassMelodyTail.AliasPrefix(tailNormalized), "wai", StringComparison.Ordinal))
            return false;

        var rem = GlassMelodyTail.ArgRemainder(tailNormalized);
        urlPayload = string.IsNullOrWhiteSpace(rem) ? "" : rem.Trim();
        return true;
    }
}

public sealed record GlassChordMelodyEntry(string Alias, string CommandId, string Help, bool GlassRunnable);

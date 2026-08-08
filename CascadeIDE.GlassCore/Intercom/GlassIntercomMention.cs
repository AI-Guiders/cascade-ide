#nullable enable

using System.Text.RegularExpressions;

namespace CascadeIDE.Intercom;

/// <summary>
/// Messenger @mentions — three axes, one wake plan.
/// Seat @PF/@PM — cabin duty; wake = f(Who.kind on that seat).
/// Kind @guest/@citizen/@operator — standing / harness class.
/// Who @Sierra/@Света/@Kir — sticky nick → same sink as kind.
/// Sinks: guest → external harness cannon; citizen/operator → Glass Face.
/// </summary>
public static partial class GlassIntercomMention
{
    public enum Seat { Pf, Pm }

    public enum WakeSink
    {
        /// <summary>External guest harness (today: Cursor AutoI via PF voice latch).</summary>
        ExternalGuest,
        /// <summary>In-habitat citizen peer — Glass Face attention.</summary>
        HabitatCitizen,
        /// <summary>Operator / PM — Glass Face attention.</summary>
        GlassOperator
    }

    public readonly record struct MentionRoster(
        string? PfName,
        string? PfKind,
        string? PmName,
        string? PmKind);

    public readonly record struct WakeHit(WakeSink Sink, string Cue);

    public static bool MentionsPf(string? body) => MentionsSeat(body, Seat.Pf);
    public static bool MentionsPm(string? body) => MentionsSeat(body, Seat.Pm);

    public static bool MentionsSeat(string? body, Seat seat)
    {
        if (!BodyOk(body))
            return false;
        return seat switch
        {
            Seat.Pf => PfMention().IsMatch(body!),
            Seat.Pm => PmMention().IsMatch(body!),
            _ => false
        };
    }

    public static bool MentionsKind(string? body, string kind)
    {
        if (!BodyOk(body))
            return false;
        return NormalizeKind(kind) switch
        {
            "guest" => GuestKindMention().IsMatch(body!),
            "citizen" => CitizenKindMention().IsMatch(body!),
            "operator" => OperatorKindMention().IsMatch(body!),
            _ => false
        };
    }

    public static IReadOnlyList<WakeHit> ResolveWakes(string? body, MentionRoster roster)
    {
        if (!BodyOk(body))
            return Array.Empty<WakeHit>();

        var hits = new Dictionary<WakeSink, string>();

        void Add(WakeSink sink, string cue)
        {
            if (!hits.ContainsKey(sink))
                hits[sink] = cue;
        }

        if (MentionsSeat(body, Seat.Pf))
        {
            var kind = NormalizeKind(roster.PfKind) ?? "guest";
            Add(SinkForKind(kind), FormatWakeNote(Seat.Pf, roster.PfName));
        }

        if (MentionsSeat(body, Seat.Pm))
        {
            var kind = NormalizeKind(roster.PmKind) ?? "operator";
            Add(SinkForKind(kind), FormatWakeNote(Seat.Pm, roster.PmName));
        }

        if (MentionsKind(body, "guest"))
            Add(WakeSink.ExternalGuest, "@guest wake");
        if (MentionsKind(body, "citizen"))
            Add(WakeSink.HabitatCitizen, "@citizen wake");
        if (MentionsKind(body, "operator"))
            Add(WakeSink.GlassOperator, "@operator wake");

        foreach (var (name, kind, seatTag) in WhoCandidates(roster))
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;
            if (!MentionsWho(body!, name))
                continue;
            var k = NormalizeKind(kind) ?? (seatTag == "@PM" ? "operator" : "guest");
            Add(SinkForKind(k), seatTag + "→" + name.Trim() + " wake");
        }

        if (MentionsWho(body!, "Кир") || MentionsWho(body!, "Kir"))
            Add(WakeSink.ExternalGuest, "@Kir→guest wake");

        return hits.Select(kv => new WakeHit(kv.Key, kv.Value)).ToArray();
    }

    public static WakeSink SinkForKind(string? kind) =>
        NormalizeKind(kind) switch
        {
            "citizen" => WakeSink.HabitatCitizen,
            "operator" => WakeSink.GlassOperator,
            _ => WakeSink.ExternalGuest
        };

    public static string? NormalizeKind(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        return raw.Trim().ToLowerInvariant() switch
        {
            "guest" or "cursor" or "external" => "guest",
            "citizen" or "fm" or "peer" => "citizen",
            "operator" or "human" or "pm" => "operator",
            _ => null
        };
    }

    public static string FormatWakeNote(Seat seat, string? whoName)
    {
        var tag = seat == Seat.Pm ? "@PM" : "@PF";
        if (string.IsNullOrWhiteSpace(whoName))
            return tag + " wake";
        return tag + "→" + whoName.Trim() + " wake";
    }

    public static bool MentionsWho(string body, string whoName)
    {
        if (string.IsNullOrWhiteSpace(whoName))
            return false;
        var escaped = Regex.Escape(whoName.Trim());
        var pat = @"(?<![\p{L}\p{N}_])@" + escaped + @"(?![\p{L}\p{N}_])";
        return Regex.IsMatch(body, pat, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    /// <summary>
    /// SoftFL densify: @ autocomplete — token under caret (prefix after @, no spaces).
    /// </summary>
    public static bool TryGetAtToken(string? text, int caret, out int start, out string prefix)
    {
        start = -1;
        prefix = "";
        if (string.IsNullOrEmpty(text))
            return false;

        caret = Math.Clamp(caret, 0, text.Length);
        var i = caret - 1;
        while (i >= 0 && IsMentionBodyChar(text[i]))
            i--;

        if (i < 0 || text[i] != '@')
            return false;
        if (i > 0 && IsMentionBodyChar(text[i - 1]))
            return false;

        start = i;
        prefix = text[(i + 1)..caret];
        if (prefix.IndexOfAny([' ', '\t', '\r', '\n']) >= 0)
            return false;
        return true;
    }

    /// <summary>
    /// SoftFL densify: roster + seats/kinds/Kir for Glass SlashPopup reuse.
    /// </summary>
    public static IReadOnlyList<(string Insert, string Title, string Help)> Suggest(
        string? prefix,
        MentionRoster roster,
        int limit = 12)
    {
        var needle = (prefix ?? "").Trim().TrimStart('@');
        var bag = new List<(string Insert, string Title, string Help)>
        {
            ("@PF ", "@PF", "seat · wake by PF Who.kind"),
            ("@PM ", "@PM", "seat · wake by PM Who.kind"),
            ("@guest ", "@guest", "kind · external harness"),
            ("@citizen ", "@citizen", "kind · Glass Face"),
            ("@operator ", "@operator", "kind · Glass Face"),
            // Canonical Cyrillic Who — Latin Kir is an alias (MentionsWho), not a second roster row.
            ("@Кир ", "@Кир", "Who · guest cannon (Cursor)"),
        };

        foreach (var (name, kind, seatTag) in WhoCandidates(roster))
        {
            if (string.IsNullOrWhiteSpace(name))
                continue;
            var n = CanonicalWhoLabel(name.Trim());
            bag.Add(("@" + n + " ", "@" + n, seatTag + " · " + (NormalizeKind(kind) ?? "?")));
        }

        return bag
            .Where(x =>
                needle.Length == 0
                || x.Title.AsSpan(1).StartsWith(needle, StringComparison.OrdinalIgnoreCase)
                || WhoAliasMatches(x.Title.AsSpan(1), needle))
            .GroupBy(x => CanonicalWhoLabel(x.Title.TrimStart('@')), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(Math.Max(1, limit))
            .ToList();
    }

    /// <summary>Latin <c>Kir</c> ≡ Cyrillic <c>Кир</c> for roster dedupe / Suggest.</summary>
    public static string CanonicalWhoLabel(string name) =>
        string.Equals(name, "Kir", StringComparison.OrdinalIgnoreCase) ? "Кир" : name;

    static bool WhoAliasMatches(ReadOnlySpan<char> titleSansAt, string needle)
    {
        if (needle.Length == 0)
            return true;
        // Typing Latin Ki… should still hit canonical @Кир.
        if (CanonicalWhoLabel(titleSansAt.ToString()).Equals("Кир", StringComparison.Ordinal)
            && "Kir".StartsWith(needle, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    static bool IsMentionBodyChar(char c) =>
        char.IsLetterOrDigit(c) || c is '_' or '-';

    static IEnumerable<(string? Name, string? Kind, string SeatTag)> WhoCandidates(MentionRoster r)
    {
        yield return (r.PfName, r.PfKind, "@PF");
        yield return (r.PmName, r.PmKind, "@PM");
        yield return ("Citizen", "citizen", "@PF");
        yield return ("Operator", "operator", "@PM");
    }

    static bool BodyOk(string? body) =>
        !string.IsNullOrWhiteSpace(body)
        && !GlassIntercomLane.IsComposerPlaceholder(body.Trim());

    [GeneratedRegex(@"(?<!\w)@PF\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PfMention();

    [GeneratedRegex(@"(?<!\w)@PM\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PmMention();

    [GeneratedRegex(@"(?<!\w)@guest\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GuestKindMention();

    [GeneratedRegex(@"(?<!\w)@citizen\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CitizenKindMention();

    [GeneratedRegex(@"(?<!\w)@operator\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OperatorKindMention();
}

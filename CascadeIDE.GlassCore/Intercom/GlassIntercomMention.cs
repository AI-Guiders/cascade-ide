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

#nullable enable

using System.Text.RegularExpressions;

namespace CascadeIDE.Intercom;

/// <summary>
/// Messenger-style @mentions — orthogonal to lane Korry.
/// <para>
/// <b>@PF / @PM are meta-roles (seats)</b>, not people.
/// Who currently occupies a seat comes from sticky identity
/// (e.g. Sierra@PF · Света@PM) — cue labels may show Who, routing is by seat.
/// Person tags (@Света / @Sierra) are a later leaf — not this helper.
/// </para>
/// </summary>
public static partial class GlassIntercomMention
{
    public enum Seat
    {
        Pf,
        Pm
    }

    /// <summary>True when body addresses @PF (seat Pilot Flying).</summary>
    public static bool MentionsPf(string? body) => MentionsSeat(body, Seat.Pf);

    /// <summary>True when body addresses @PM (seat Pilot Monitoring).</summary>
    public static bool MentionsPm(string? body) => MentionsSeat(body, Seat.Pm);

    public static bool MentionsSeat(string? body, Seat seat)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;
        if (GlassIntercomLane.IsComposerPlaceholder(body.Trim()))
            return false;
        return seat switch
        {
            Seat.Pf => PfMention().IsMatch(body),
            Seat.Pm => PmMention().IsMatch(body),
            _ => false
        };
    }

    /// <summary>Status/journal cue: <c>@PF→Sierra</c> when Who known, else <c>@PF</c>.</summary>
    public static string FormatWakeNote(Seat seat, string? whoName)
    {
        var tag = seat switch
        {
            Seat.Pf => "@PF",
            Seat.Pm => "@PM",
            _ => "@?"
        };
        if (string.IsNullOrWhiteSpace(whoName))
            return tag + " wake";
        return tag + "→" + whoName.Trim() + " wake";
    }

    [GeneratedRegex(@"(?<!\w)@PF\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PfMention();

    [GeneratedRegex(@"(?<!\w)@PM\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PmMention();
}

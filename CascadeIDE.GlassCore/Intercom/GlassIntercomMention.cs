#nullable enable

using System.Text.RegularExpressions;

namespace CascadeIDE.Intercom;

/// <summary>
/// Messenger-style @mentions in Intercom body — orthogonal to lane Korry.
/// Lane = default recipient; mention = additional notify (Slack/MM).
/// </summary>
public static partial class GlassIntercomMention
{
    /// <summary>True when body addresses @PF (word boundary; case-insensitive).</summary>
    public static bool MentionsPf(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;
        if (GlassIntercomLane.IsComposerPlaceholder(body.Trim()))
            return false;
        return PfMention().IsMatch(body);
    }

    [GeneratedRegex(@"(?<!\w)@PF\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PfMention();
}

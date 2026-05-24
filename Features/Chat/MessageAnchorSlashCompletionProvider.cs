#nullable enable

using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Features.Chat;

public sealed class MessageAnchorSlashCompletionProvider : IMessageAnchorSlashCompletionProvider
{
    public const int DefaultLimit = 20;

    private readonly Func<IReadOnlyList<AttachmentAnchor>> _getSelectedMessageAnchors;

    public MessageAnchorSlashCompletionProvider(Func<IReadOnlyList<AttachmentAnchor>> getSelectedMessageAnchors) =>
        _getSelectedMessageAnchors = getSelectedMessageAnchors;

    public IReadOnlyList<MessageAnchorSlashMatch> GetMatches(string ordinalOrIdPrefix, int limit)
    {
        if (limit <= 0)
            return [];

        var prefix = (ordinalOrIdPrefix ?? "").Trim();
        var anchors = _getSelectedMessageAnchors();
        if (anchors.Count == 0)
            return [];

        var matches = new List<MessageAnchorSlashMatch>(Math.Min(anchors.Count, limit));
        for (var i = 0; i < anchors.Count && matches.Count < limit; i++)
        {
            var ordinal = (i + 1).ToString();
            var anchor = anchors[i];
            if (!matchesPrefix(prefix, ordinal, anchor))
                continue;

            var label = anchor.DisplayLabel ?? anchor.MemberKey ?? anchor.File ?? "—";
            var status = IntercomAnchorSlash.FormatOutcomeShort(anchor.ResolveOutcome);
            var id = anchor.Id ?? "";
            var help = string.IsNullOrWhiteSpace(id) ? status : $"a:{id} · {status}";
            matches.Add(new MessageAnchorSlashMatch(ordinal, $"{ordinal} · {label}", help));
        }

        return matches;
    }

    private static bool matchesPrefix(string prefix, string ordinal, AttachmentAnchor anchor)
    {
        if (prefix.Length == 0)
            return true;

        if (ordinal.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return true;

        var id = anchor.Id ?? "";
        if (id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return true;

        return $"a:{id}".StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}

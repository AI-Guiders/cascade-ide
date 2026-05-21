#nullable enable

using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    /// <summary>Список якорей выбранного сообщения и черновика (ADR 0128 §10.1).</summary>
    public string ListAnchorsForSlashContext()
    {
        if (IsChatOverviewMode)
            return "Открой тему (detail): /intercom topic open.";

        var lines = new List<string>();

        if (_pendingAttachDrafts.Count > 0)
        {
            lines.Add("Черновик composer:");
            foreach (var pair in _pendingAttachDrafts.OrderBy(static p => p.Key, StringComparer.OrdinalIgnoreCase))
                lines.Add("  " + IntercomAnchorSlash.FormatListLine(pair.Value with { Id = pair.Key }));
        }

        if (SelectedMessageIndex < 0 || SelectedMessageIndex >= ChatMessages.Count)
        {
            if (lines.Count > 0)
                return string.Join(Environment.NewLine, lines) + Environment.NewLine + "Выбери сообщение: /intercom message select <n>.";

            return "Выбери сообщение (/intercom message select <n>) или прикрепи черновик (/attach …).";
        }

        TryGetFeedOrdinalForMessageIndex(SelectedMessageIndex, out var ordinal);
        var msg = ChatMessages[SelectedMessageIndex];
        var attachments = msg.Attachments;
        if (attachments is null || attachments.Count == 0)
        {
            if (lines.Count > 0)
                return string.Join(Environment.NewLine, lines);

            return $"Сообщение #{ordinal}: вложений нет.";
        }

        lines.Add($"Сообщение #{ordinal}:");
        foreach (var anchor in attachments)
            lines.Add("  " + IntercomAnchorSlash.FormatListLine(anchor, ordinal));

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>Reveal по short id без hit-test (ADR 0128 §10.1).</summary>
    public string PeekAnchorById(string? rawId)
    {
        if (!TryResolveAnchorByShortId(rawId, out var anchor, out var messageIndex, out var error))
            return error;

        _ = RevealAttachmentFromFeedAsync(anchor, select: false, messageIndex);
        var status = IntercomAnchorSlash.FormatOutcomeShort(anchor.ResolveOutcome);
        var id = anchor.Id ?? "?";
        return $"Peek a:{id} ({status}) — открываю в редакторе.";
    }

    private bool TryResolveAnchorByShortId(
        string? rawId,
        out AttachmentAnchor anchor,
        out int? messageIndex,
        out string error)
    {
        anchor = new AttachmentAnchor();
        messageIndex = null;
        error = "";

        if (!IntercomAnchorSlash.TryNormalizeAnchorId(rawId, out var shortId, out error))
            return false;

        if (_pendingAttachDrafts.TryGetValue(shortId, out var draft))
        {
            anchor = draft;
            return true;
        }

        if (SelectedMessageIndex >= 0 && SelectedMessageIndex < ChatMessages.Count)
        {
            foreach (var candidate in ChatMessages[SelectedMessageIndex].Attachments ?? [])
            {
                if (string.IsNullOrWhiteSpace(candidate.Id)
                    || !string.Equals(candidate.Id, shortId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                anchor = candidate;
                messageIndex = SelectedMessageIndex;
                return true;
            }
        }

        for (var i = 0; i < ChatMessages.Count; i++)
        {
            foreach (var candidate in ChatMessages[i].Attachments ?? [])
            {
                if (string.IsNullOrWhiteSpace(candidate.Id)
                    || !string.Equals(candidate.Id, shortId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                anchor = candidate;
                messageIndex = i;
                return true;
            }
        }

        error = $"Якорь a:{shortId} не найден. /intercom message anchors list";
        return false;
    }
}

#nullable enable

using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Features.Chat.AnchorPeek;

internal static class AnchorPeekResolver
{
    public static bool TryResolve(
        string? raw,
        in AnchorPeekResolveContext context,
        out AttachmentAnchor anchor,
        out int? messageIndex,
        out int ordinal,
        out string error)
    {
        anchor = new AttachmentAnchor();
        messageIndex = null;
        ordinal = 0;
        error = "";

        if (!AnchorPeekTargetParser.TryParse(raw, out var target, out error))
            return false;

        return target.Kind switch
        {
            AnchorPeekTargetKind.Ordinal => tryResolveOrdinal(target.Ordinal, context, out anchor, out messageIndex, out ordinal, out error),
            AnchorPeekTargetKind.HexId => tryResolveHex(target.HexId, context, out anchor, out messageIndex, out error),
            _ => false,
        };
    }

    private static bool tryResolveOrdinal(
        int requestedOrdinal,
        in AnchorPeekResolveContext context,
        out AttachmentAnchor anchor,
        out int? messageIndex,
        out int ordinal,
        out string error)
    {
        anchor = new AttachmentAnchor();
        messageIndex = null;
        ordinal = requestedOrdinal;
        error = "";

        var selected = context.SelectedMessageAnchors;
        if (selected.Count == 0)
        {
            error = "Выбери сообщение с вложениями: /intercom message select <n>.";
            return false;
        }

        if (requestedOrdinal < 1)
        {
            error = "№ якоря — целое число от 1.";
            return false;
        }

        if (requestedOrdinal > selected.Count)
        {
            error = $"Якоря #{requestedOrdinal} нет (в сообщении {selected.Count}). /intercom message anchors list";
            return false;
        }

        anchor = selected[requestedOrdinal - 1];
        messageIndex = context.SelectedMessageIndex >= 0 ? context.SelectedMessageIndex : null;
        return true;
    }

    private static bool tryResolveHex(
        string shortId,
        in AnchorPeekResolveContext context,
        out AttachmentAnchor anchor,
        out int? messageIndex,
        out string error)
    {
        anchor = new AttachmentAnchor();
        messageIndex = null;
        error = "";

        if (context.PendingDrafts.TryGetValue(shortId, out var draft))
        {
            anchor = draft;
            return true;
        }

        foreach (var entry in context.AllMessageAnchors)
        {
            if (string.IsNullOrWhiteSpace(entry.Anchor.Id)
                || !string.Equals(entry.Anchor.Id, shortId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            anchor = entry.Anchor;
            messageIndex = entry.MessageIndex;
            return true;
        }

        error = $"Якорь a:{shortId} не найден. /intercom message anchors list";
        return false;
    }
}

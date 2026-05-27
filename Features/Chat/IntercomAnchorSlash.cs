#nullable enable

using CascadeIDE.Features.Chat.AnchorPeek;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat;

/// <summary>Фасад форматирования и anchor peek для slash/CCL (ADR 0128 §10.1).</summary>
internal static class IntercomAnchorSlash
{
    public static bool IsAnchorPeekPath(string canonicalPath) =>
        SlashPathAliases.IsAnchorPeekPath(canonicalPath);

    public static string? ExtractPeekIdTail(string canonicalPath, string? argTail) =>
        SlashPathAliases.ExtractPeekArgs(canonicalPath, argTail);

    public static bool IsAnchorPeekHexIdEntryBody(string body) =>
        isAnchorPeekHexIdEntryBody(SlashPathAliases.NormalizeCompletionBody(body));

    internal static bool IsPartialHexAnchorId(string raw) =>
        AnchorPeekTargetParser.IsPartialHex(raw);

    public static bool TryResolvePeekOrdinal(
        string? raw,
        IReadOnlyList<AttachmentAnchor> selectedMessageAnchors,
        out AttachmentAnchor anchor,
        out int ordinal) =>
        tryResolveOrdinalOnly(raw, selectedMessageAnchors, out anchor, out ordinal);

    public static bool TryFormatPeekOrdinalError(string? raw, int attachmentCount, out string error)
    {
        error = "";
        if (!AnchorPeekTargetParser.TryParse(raw ?? "", out var target, out _))
            return false;

        if (target.Kind != AnchorPeekTargetKind.Ordinal)
            return false;

        if (attachmentCount > 0 && target.Ordinal >= 1 && target.Ordinal <= attachmentCount)
            return false;

        error = target.Ordinal < 1
            ? "№ якоря — целое число от 1."
            : attachmentCount <= 0
                ? "Выбери сообщение с вложениями: /intercom message select <n>."
                : $"Якоря #{target.Ordinal} нет (в сообщении {attachmentCount}). /intercom message anchors list";
        return true;
    }

    public static bool TryNormalizeAnchorId(string? raw, out string shortId, out string error)
    {
        shortId = "";
        error = "";
        if (!AnchorPeekTargetParser.TryParse(raw, out var target, out error))
            return false;

        if (target.Kind != AnchorPeekTargetKind.HexId)
        {
            error = AnchorPeekTargetParser.InvalidHexError;
            return false;
        }

        shortId = target.HexId;
        return true;
    }

    public static string FormatListLine(AttachmentAnchor anchor, int? messageOrdinal = null)
    {
        var id = string.IsNullOrWhiteSpace(anchor.Id) ? "?" : $"a:{anchor.Id}";
        var label = anchor.DisplayLabel ?? anchor.MemberKey ?? anchor.File ?? "—";
        var outcome = FormatOutcomeShort(anchor.ResolveOutcome);
        var loc = FormatLocationShort(anchor);
        var ord = messageOrdinal is { } o ? $"#{o} " : "";
        return $"{ord}{id}  {label}  {outcome}  {loc}".TrimEnd();
    }

    public static string FormatOutcomeShort(string? resolveOutcome)
    {
        var o = resolveOutcome?.Trim();
        if (string.Equals(o, IntercomAttachmentRevealPlan.OutcomeResolved, StringComparison.OrdinalIgnoreCase))
            return "resolved";
        if (string.Equals(o, IntercomAttachmentRevealPlan.OutcomeFileMissing, StringComparison.OrdinalIgnoreCase))
            return "file_missing";
        if (string.Equals(o, IntercomAttachmentRevealPlan.OutcomeMemberNotFound, StringComparison.OrdinalIgnoreCase))
            return "member_not_found";
        if (string.Equals(o, IntercomAttachmentRevealPlan.OutcomeExcerptOnly, StringComparison.OrdinalIgnoreCase))
            return "excerpt_only";
        return string.IsNullOrWhiteSpace(o) ? "—" : o;
    }

    private static bool tryResolveOrdinalOnly(
        string? raw,
        IReadOnlyList<AttachmentAnchor> selectedMessageAnchors,
        out AttachmentAnchor anchor,
        out int ordinal)
    {
        anchor = new AttachmentAnchor();
        ordinal = 0;
        var context = new AnchorPeekResolveContext(
            SelectedMessageIndex: -1,
            selectedMessageAnchors,
            new Dictionary<string, AttachmentAnchor>(),
            []);

        return AnchorPeekResolver.TryResolve(raw, context, out anchor, out _, out ordinal, out _);
    }

    private static bool isAnchorPeekHexIdEntryBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;

        var tokens = body.TrimEnd().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            return false;

        if (tokens[0].Equals("anchor", StringComparison.OrdinalIgnoreCase))
            return isHexArgAfterPeek(tokens, startIndex: 1);

        if (tokens.Length >= 2
            && tokens[0].Equals("intercom", StringComparison.OrdinalIgnoreCase)
            && tokens[1].Equals("anchor", StringComparison.OrdinalIgnoreCase))
        {
            return isHexArgAfterPeek(tokens, startIndex: 2);
        }

        return false;
    }

    private static bool isHexArgAfterPeek(string[] tokens, int startIndex)
    {
        if (tokens.Length <= startIndex)
            return false;

        var peekToken = tokens[startIndex];
        if (!peekToken.StartsWith("peek", StringComparison.OrdinalIgnoreCase))
            return false;

        if (peekToken.Equals("peek", StringComparison.OrdinalIgnoreCase))
        {
            return tokens.Length > startIndex + 1
                   && AnchorPeekTargetParser.LooksLikeHexEntry(tokens[startIndex + 1]);
        }

        return peekToken.Length > 4 && AnchorPeekTargetParser.LooksLikeHexEntry(peekToken[4..]);
    }

    private static string FormatLocationShort(AttachmentAnchor anchor)
    {
        if (string.IsNullOrWhiteSpace(anchor.File))
            return "—";

        var file = anchor.File.Replace('\\', '/');
        if (anchor.LineStart is { } ls && anchor.LineEnd is { } le)
            return le == ls ? $"{file} L{ls}" : $"{file} L{ls}–{le}";
        return file;
    }
}

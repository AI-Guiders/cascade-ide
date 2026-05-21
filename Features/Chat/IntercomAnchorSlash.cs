#nullable enable

using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat;

/// <summary>Форматирование и разбор anchor id для slash/CCL (ADR 0128 §10.1).</summary>
internal static class IntercomAnchorSlash
{
    public static bool TryNormalizeAnchorId(string? raw, out string shortId, out string error)
    {
        shortId = "";
        error = "";
        var t = (raw ?? "").Trim();
        if (t.Length == 0)
        {
            error = "Укажи id: /anchor peek abcd1234 (или a:abcd1234).";
            return false;
        }

        if (t.StartsWith("a:", StringComparison.OrdinalIgnoreCase))
            t = t[2..].Trim();

        var wirePrefix = $"{IntercomAttachmentMarkers.MarkerOpen}a:";
        if (t.StartsWith(wirePrefix, StringComparison.Ordinal))
        {
            var end = t.IndexOf(IntercomAttachmentMarkers.MarkerClose);
            t = end >= wirePrefix.Length ? t[wirePrefix.Length..end] : t[wirePrefix.Length..];
        }

        if (t.Length != 8 || !t.All(static c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F'))
        {
            error = "Id якоря — 8 hex-символов (как в маркере ⟦a:abcd1234⟧).";
            return false;
        }

        shortId = t.ToLowerInvariant();
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

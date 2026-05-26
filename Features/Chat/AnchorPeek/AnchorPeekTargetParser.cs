#nullable enable

using System.Globalization;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Chat.AnchorPeek;

internal static class AnchorPeekTargetParser
{
    public const string EmptyTargetError =
        "Укажи № якоря (1…) или 8 hex: /anchor peek 1 или /anchor peek abcd1234.";

    public const string InvalidHexError =
        "Id якоря — 8 hex-символов (как в маркере ⟦a:abcd1234⟧).";

    public static bool TryParse(string? raw, out AnchorPeekTarget target, out string error)
    {
        target = default;
        error = "";
        var t = (raw ?? "").Trim();
        if (t.Length == 0)
        {
            error = EmptyTargetError;
            return false;
        }

        if (TryParseOrdinalToken(t, out var ordinal))
        {
            target = AnchorPeekTarget.FromOrdinal(ordinal);
            return true;
        }

        if (TryParseHexToken(t, out var hexId, out error))
        {
            target = AnchorPeekTarget.FromHexId(hexId);
            return true;
        }

        return false;
    }

    public static bool IsPartialHex(string raw)
    {
        var t = raw;
        if (t.StartsWith("a:", StringComparison.OrdinalIgnoreCase))
            t = t[2..].Trim();

        return t.Length > 0
               && t.Length < 8
               && t.All(static c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');
    }

    public static bool LooksLikeHexEntry(string token)
    {
        if (token.StartsWith("a:", StringComparison.OrdinalIgnoreCase))
            return true;

        if (token.Any(static c => c is >= 'a' and <= 'f' or >= 'A' and <= 'F'))
            return true;

        return token.Length >= 8
               && token.All(static c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');
    }

    private static bool TryParseOrdinalToken(string t, out int ordinal)
    {
        ordinal = 0;
        if (t.StartsWith("a:", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!t.All(static c => c is >= '0' and <= '9'))
            return false;

        return int.TryParse(t, NumberStyles.None, CultureInfo.InvariantCulture, out ordinal) && ordinal >= 1;
    }

    private static bool TryParseHexToken(string t, out string hexId, out string error)
    {
        hexId = "";
        error = "";
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
            error = InvalidHexError;
            return false;
        }

        hexId = t.ToLowerInvariant();
        return true;
    }
}

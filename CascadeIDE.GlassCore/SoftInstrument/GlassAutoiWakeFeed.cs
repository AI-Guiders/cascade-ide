#nullable enable

namespace CascadeIDE.SoftInstrument;

/// <summary>Autoi wake → SoftInstrument tip / StatusText, not Intercom chat bubbles.</summary>
public static class GlassAutoiWakeFeed
{
    public static bool IsNoise(
        string? body,
        string? name = null,
        string? kind = null,
        string? roleLabel = null)
    {
        // @Kir Face Radio tip under Composer Stop — must stay on Radio feed (not tip-only StatusText).
        if (IsKirVoiceCannonFaceTip(body))
            return false;

        if (LooksLikeAutoiName(name) || LooksLikeAutoiName(roleLabel))
            return true;
        if (string.Equals(kind, "wake", StringComparison.OrdinalIgnoreCase))
            return true;
        // Lived: remount painted as kind=citizen name=Citizen with Autoi Radio body —
        // still SoftInstrument tip, not chat (Who ≠ body).
        return LooksLikeCharge(body) || LooksLikeRadioPointer(body);
    }

    /// <summary>Voice-cannon Face tip under Composer Stop — Radio feed, not Autoi wake tip-only.</summary>
    public static bool IsKirVoiceCannonFaceTip(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;
        return body.Contains("@Kir wake pending", StringComparison.Ordinal)
               || body.Contains("@Kir wake fail", StringComparison.Ordinal)
               || body.Contains("пушка ждёт Voice", StringComparison.Ordinal);
    }

    public static bool LooksLikeCharge(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;

        return body.Contains("Resume the current authorized local development task", StringComparison.Ordinal)
               || body.Contains("If you feel completely lost / thread amnesia", StringComparison.Ordinal)
               || (body.Contains("Habitat=CDP", StringComparison.Ordinal)
                   && body.Contains("cdp_pressure", StringComparison.OrdinalIgnoreCase)
                   && body.Contains("op=recall", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Collapsed Autoi Radio face (I6) — even when mis-attributed as Citizen.</summary>
    public static bool LooksLikeRadioPointer(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;
        var t = body.TrimStart();
        if (t.StartsWith("Autoi ", StringComparison.OrdinalIgnoreCase) && t.Contains('\u00B7'))
            return true;
        return t.Contains("PFD.NEXT", StringComparison.Ordinal);
    }

    static bool LooksLikeAutoiName(string? s) =>
        !string.IsNullOrWhiteSpace(s)
        && (s.Contains("Autoi", StringComparison.OrdinalIgnoreCase)
            || s.Contains("AutoI", StringComparison.OrdinalIgnoreCase));
}

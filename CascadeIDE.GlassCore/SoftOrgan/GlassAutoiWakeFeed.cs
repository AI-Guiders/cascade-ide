#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>Autoi wake → SoftOrgan tip / StatusText, not Intercom chat bubbles.</summary>
public static class GlassAutoiWakeFeed
{
    public static bool IsNoise(
        string? body,
        string? name = null,
        string? kind = null,
        string? roleLabel = null)
    {
        if (LooksLikeAutoiName(name) || LooksLikeAutoiName(roleLabel))
            return true;
        if (string.Equals(kind, "wake", StringComparison.OrdinalIgnoreCase))
            return true;
        return LooksLikeCharge(body);
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

    static bool LooksLikeAutoiName(string? s) =>
        !string.IsNullOrWhiteSpace(s)
        && (s.Contains("Autoi", StringComparison.OrdinalIgnoreCase)
            || s.Contains("AutoI", StringComparison.OrdinalIgnoreCase));
}

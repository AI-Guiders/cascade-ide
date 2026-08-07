#nullable enable

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Shared Face projection across LatchPaint peels (Plan / SoftOrgan / Seats / Intercom / wake).
/// Agent SSOT may keep SoftFL refuse; Face never paints tip-mill / operator-eyes theatre.
/// </summary>
internal static partial class LatchPaint
{
    /// <summary>Face WHY — drop SoftFL / tip-mill / operator-eyes refuse theatre.</summary>
    internal static string HumanizePlanWhy(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw == "—")
            return "—";

        var s = StripPlanTheatre(raw);
        if (LooksLikePlanJargon(s))
        {
            if (s.Contains("Glass Done", StringComparison.OrdinalIgnoreCase))
                return "Glass Done — instruments people can fly";
            if (s.Contains("Citizen", StringComparison.OrdinalIgnoreCase))
                return "Citizen stable toward 15.08";
            return "Glass Done + Citizen toward 15.08";
        }

        return TruncatePlan(s, 120);
    }

    /// <summary>SoftOrgan / seats chrome tip — never SoftFL ShowFace mill.</summary>
    internal static string? HumanizeChromeHint(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var s = StripPlanTheatre(raw.Trim());
        s = StripShowFaceSoftFl(s);
        if (string.IsNullOrWhiteSpace(s) || LooksLikePlanJargon(s))
            return null;
        return TruncatePlan(s, 72);
    }

    /// <summary>Plan board / TM tree line for Face list.</summary>
    internal static string HumanizeBoardLine(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;
        var s = StripPlanTheatre(raw.Trim());
        s = StripShowFaceSoftFl(s);
        var (glance, _) = FormatGlanceNext(s);
        return glance;
    }

    static string StripShowFaceSoftFl(string s)
    {
        foreach (var needle in new[]
                 {
                     "ShowFace Place+attention SoftFL",
                     "ShowFace Place+attention So",
                     "ShowFace Place+attention"
                 })
        {
            var i = s.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            if (i < 0)
                continue;
            var before = s[..i].Trim().TrimEnd('·', '-', '—', ' ', '/');
            var after = s[(i + needle.Length)..];
            after = after.TrimStart('.', '…', ' ', 'F', 'L');
            var cut = after.IndexOf(" · ", StringComparison.Ordinal);
            if (cut >= 0)
                after = after[cut..].TrimStart('·', ' ');
            else
                after = "";
            s = string.IsNullOrWhiteSpace(before)
                ? after
                : string.IsNullOrWhiteSpace(after)
                    ? before
                    : before + " · " + after;
        }

        return s.Replace("  ", " ", StringComparison.Ordinal).Trim('·', ' ', '-');
    }

    internal static string StripPlanTheatre(string s)
    {
        foreach (var needle in new[]
                 {
                     "SoftFL invent REJECT",
                     "SoftFL REJECT",
                     "SoftFL",
                     "nested[axb]",
                     "agent refuse Face Done claim",
                     "agent refuse Face Done",
                     "agent refuse #CIDE Done",
                     "agent refuse",
                     "YOUR Glass eyes",
                     "Glass eyes",
                     "Face axis4 operator",
                     "tip mill ≠ Done",
                     "tip mill != Done",
                     "tip mill",
                     "Face SoftOrgan/#CIDE Done needs operator eyes",
                     "needs operator eyes",
                     "operator eyes",
                     "DIG REJECT SoftFL",
                     "DIG REJECT",
                     "refuse board hygiene",
                     "board-hygiene",
                     "board hygiene"
                 })
        {
            var i = s.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            if (i < 0)
                continue;
            var before = s[..i].Trim().TrimEnd('·', '-', '—', ' ', '/', ';');
            var after = s[(i + needle.Length)..].Trim().TrimStart('·', '-', '—', ' ', '/', ';', '.');
            s = string.IsNullOrWhiteSpace(before)
                ? after
                : string.IsNullOrWhiteSpace(after)
                    ? before
                    : before + " · " + after;
        }

        return s.Replace("  ", " ", StringComparison.Ordinal).Trim('·', ' ', '-');
    }

    static bool LooksLikePlanJargon(string line) =>
        line.Contains("SoftFL", StringComparison.OrdinalIgnoreCase)
        || line.Contains("tip mill", StringComparison.OrdinalIgnoreCase)
        || line.Contains("operator eyes", StringComparison.OrdinalIgnoreCase)
        || line.Contains("DIG REJECT", StringComparison.OrdinalIgnoreCase)
        || line.Contains("nested[axb]", StringComparison.OrdinalIgnoreCase)
        || line.Contains("agent refuse", StringComparison.OrdinalIgnoreCase)
        || line.Contains("Glass eyes", StringComparison.OrdinalIgnoreCase)
        || line.Contains("board hygiene", StringComparison.OrdinalIgnoreCase)
        || line.Contains("board-hygiene", StringComparison.OrdinalIgnoreCase)
        || line.Contains("ShowFace Place", StringComparison.OrdinalIgnoreCase);
}

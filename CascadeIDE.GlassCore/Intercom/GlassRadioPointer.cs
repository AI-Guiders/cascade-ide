#nullable enable

using System.Text;
using CascadeIDE.SoftInstrument;

namespace CascadeIDE.Intercom;

/// <summary>
/// Intercom = Radio: peel instrument pointers from feed prose (I6 — not SA wall).
/// Canon: glass-ux-dual-projector · <c>delta → Zone:Target · hint</c>.
/// </summary>
public static class GlassRadioPointer
{
    public const int MaxValueChars = 56;

    public readonly record struct Peel(string Body, IReadOnlyList<GlassGlanceChip> Pointers);

    public static Peel FromBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return new Peel("", []);

        var pointers = new List<GlassGlanceChip>();
        var prose = new StringBuilder();
        foreach (var raw in body.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (TryPeelLine(line.Trim(), out var chip))
            {
                pointers.Add(chip!);
                continue;
            }

            if (prose.Length > 0)
                prose.Append('\n');
            prose.Append(line);
        }

        return new Peel(prose.ToString().Trim(), pointers);
    }

    public static bool TryPeelLine(string? trimmed, out GlassGlanceChip? chip)
    {
        chip = null;
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        var t = trimmed.Trim();
        if (t.StartsWith("delta", StringComparison.OrdinalIgnoreCase))
        {
            var rest = StripArrowTail(t.AsSpan("delta".Length));
            if (rest.Length == 0)
                return false;
            chip = new GlassGlanceChip("DELTA", Trunc(rest), "ok");
            return true;
        }

        if (t.StartsWith("look", StringComparison.OrdinalIgnoreCase)
            && (t.Length == 4 || !char.IsLetterOrDigit(t[4])))
        {
            var rest = StripArrowTail(t.AsSpan("look".Length));
            if (rest.Length == 0)
                return false;
            chip = new GlassGlanceChip("LOOK", Trunc(rest), "ok");
            return true;
        }

        if (StartsWithArrow(t, out var afterArrow))
        {
            var zone = ClassifyZone(afterArrow);
            // Bare → without a known instrument zone stays prose (SA bullets ≠ Radio).
            if (zone == "LOOK")
                return false;
            chip = new GlassGlanceChip(zone, Trunc(afterArrow), "ok");
            return true;
        }

        return false;
    }

    static bool StartsWithArrow(string t, out string after)
    {
        after = "";
        if (t.StartsWith("→", StringComparison.Ordinal))
        {
            after = t["→".Length..].TrimStart();
            return after.Length > 0;
        }

        if (t.StartsWith("->", StringComparison.Ordinal))
        {
            after = t[2..].TrimStart();
            return after.Length > 0;
        }

        return false;
    }

    static string StripArrowTail(ReadOnlySpan<char> afterKeyword)
    {
        var s = afterKeyword.TrimStart().ToString();
        if (s.StartsWith("→", StringComparison.Ordinal))
            return s["→".Length..].TrimStart();
        if (s.StartsWith("->", StringComparison.Ordinal))
            return s[2..].TrimStart();
        if (s.Length > 0 && (s[0] is ':' or '·'))
            return s[1..].TrimStart();
        return s.TrimStart();
    }

    static string ClassifyZone(string target)
    {
        var head = target;
        var cut = head.IndexOfAny([':', '·', ' ', '.', '/']);
        if (cut > 0)
            head = head[..cut];
        return head.Trim().ToUpperInvariant() switch
        {
            "PFD" => "PFD",
            "MFD" => "MFD",
            "RIGHT" or "EDITOR" => "RIGHT",
            "EICAS" or "ECL" or "QRH" => "EICAS",
            "APPLIES" => "APPLIES",
            "F" or "FORWARD" or "INTERCOM" => "FWD",
            "P" or "PLAN" => "PLAN",
            "M" => "MFD",
            _ => "LOOK",
        };
    }

    static string Trunc(string s)
    {
        s = s.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return s.Length <= MaxValueChars ? s : s[..(MaxValueChars - 1)].TrimEnd() + "…";
    }
}

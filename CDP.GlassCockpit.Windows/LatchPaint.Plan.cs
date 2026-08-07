#nullable enable
using System.Text;
using System.Text.Json;

namespace CDP.GlassCockpit.Windows;

/// <summary>plan-LATEST.json → P Plan instrument cards (WHY + NEXT — not one text dump).</summary>
internal static partial class LatchPaint
{
    /// <param name="Headline">Compat = NEXT leaf (legacy PlanTitle / situ).</param>
    /// <param name="Detail">Compat dump for situ extract; prefer <see cref="Why"/> / <see cref="Next"/>.</param>
    public sealed record PlanView(
        string Headline,
        string Detail,
        string StatusLine,
        bool Active,
        string Why = "",
        string Next = "",
        string Course = "",
        string? Wall = null,
        string? NextSub = null,
        IReadOnlyList<string>? Board = null,
        IReadOnlyList<PlanBoardLeaf>? Leaves = null);

    public static PlanView PaintPlan(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var active = root.TryGetProperty("active", out var a) && a.ValueKind is JsonValueKind.True;
            var feature = Prop(root, "feature");
            var task = Prop(root, "task");
            var whyRaw = Prop(root, "why");
            var pulse = Prop(root, "pulse") ?? Prop(root, "chrome_hint");
            var boardRaw = ReadBoardRaw(root);
            var leaves = PlanBoardLeaf.ParseAll(boardRaw);
            var board = leaves.Select(static l => $"{l.Mark} · {l.Title}").ToList();

            if (!active && string.IsNullOrWhiteSpace(feature) && string.IsNullOrWhiteSpace(task)
                && string.IsNullOrWhiteSpace(whyRaw) && board.Count == 0)
            {
                return new PlanView(
                    "Plan quiet",
                    "No active Task Manager leaf.",
                    "plan · quiet",
                    false,
                    Why: "No sealed course.",
                    Next: "No active leaf.",
                    Course: "Plan quiet",
                    Board: [],
                    Leaves: []);
            }

            // Shared-SSOT Q1: WHY + NEXT as separate instrument faces (not one ECAM string).
            // Gap 3.3: NEXT glance = 1 human move; full TM title stays on Sub when collapsed.
            var leafRaw = !string.IsNullOrWhiteSpace(task) ? task!.Trim()
                : !string.IsNullOrWhiteSpace(feature) ? feature!.Trim()
                : TruncatePlan(pulse ?? "Plan", 56);
            var (next, nextSub) = FormatGlanceNext(leafRaw);

            // Face WHY: never paint SoftFL / operator-eyes refuse mills.
            var why = HumanizePlanWhy(!string.IsNullOrWhiteSpace(whyRaw) ? whyRaw!.Trim() : "—");
            var course = !string.IsNullOrWhiteSpace(feature)
                ? HumanizeBoardLine(feature!.Trim())
                : "";
            if (string.IsNullOrWhiteSpace(course))
                course = TruncatePlan(StripPlanTheatre(feature!.Trim()), 72);
            var wall = ExtractWall(pulse);

            var detail = new StringBuilder();
            detail.Append("WHY · ").Append(why);
            if (!string.IsNullOrWhiteSpace(course) && !string.Equals(course, next, StringComparison.Ordinal))
            {
                detail.AppendLine();
                detail.Append(course);
            }

            if (!string.IsNullOrWhiteSpace(wall))
            {
                detail.AppendLine();
                detail.Append(wall);
            }

            return new PlanView(
                leafRaw,
                detail.ToString().TrimEnd(),
                active
                    ? $"plan · board {board.Count} · {TruncatePlan(next, 20)}"
                    : "plan · quiet",
                active,
                Why: why,
                Next: next,
                Course: course,
                Wall: wall,
                NextSub: nextSub,
                Board: board,
                Leaves: leaves);
        }
        catch (Exception ex)
        {
            return new PlanView(
                "Plan",
                ex.Message,
                $"plan · parse fail · {ex.Message}",
                false,
                Why: ex.Message,
                Next: "Plan",
                Course: "",
                Board: [],
                Leaves: []);
        }
    }

    static IReadOnlyList<string> ReadBoardRaw(JsonElement root)
    {
        if (!root.TryGetProperty("board", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return [];

        var list = new List<string>();
        foreach (var el in arr.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.String)
                continue;
            var s = el.GetString();
            if (string.IsNullOrWhiteSpace(s))
                continue;
            list.Add(s.Trim());
            if (list.Count >= 48)
                break;
        }

        return list;
    }

    /// <summary>Autoi wake belongs on SoftOrgan tip / StatusText — not Intercom chat.</summary>
    public static bool IsAutoiWakeFeedNoise(
        string? body,
        string? name = null,
        string? kind = null,
        string? roleLabel = null) =>
        CascadeIDE.SoftOrgan.GlassAutoiWakeFeed.IsNoise(body, name, kind, roleLabel);

    /// <summary>Normalize newlines for Intercom display (Autoi filtered out before paint).</summary>
    /// <summary>Human Intercom: drop @intent/@event/@frame and peer-wire tips; keep prose.</summary>
    public static string CompactIntercomBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return body;

        // Citizen/@frame desk SA walls already in journal — collapse to Radio face on rebuild.
        if (LooksLikeSaInstrumentWall(body))
            return "Citizen · SA collapsed\n→ PFD.NEXT\ndelta → Plan · see PFD.NEXT";

        var sb = new System.Text.StringBuilder();
        var blankRun = 0;
        foreach (var raw in body.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            var t = line.TrimStart();
            if (t.Length == 0)
            {
                if (sb.Length == 0 || blankRun > 0)
                    continue;
                blankRun++;
                sb.Append('\n');
                continue;
            }

            blankRun = 0;
            if (IsIntercomWireLine(t))
                continue;

            if (sb.Length > 0)
                sb.Append('\n');
            sb.Append(line);
        }

        return StripPlanTheatre(sb.ToString().Trim());
    }

    public static bool LooksLikeSaInstrumentWall(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;

        var t = body;
        if (t.Contains("truncated habitat wake", StringComparison.OrdinalIgnoreCase))
            return true;
        if (t.Contains("`tm |", StringComparison.OrdinalIgnoreCase)
            && t.Contains("`board |", StringComparison.OrdinalIgnoreCase))
            return true;
        if (t.Contains("board | P:", StringComparison.OrdinalIgnoreCase)
            && t.Contains("`peer |", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    static bool IsIntercomWireLine(string t)
    {
        if (t.StartsWith("@intent", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("@event", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("@frame", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("@pulse", StringComparison.OrdinalIgnoreCase))
            return true;

        if (t.StartsWith("ok · gen=", StringComparison.OrdinalIgnoreCase))
            return true;
        if (t.Contains(" · mcp=live · ", StringComparison.Ordinal)
            && t.Contains("ack=", StringComparison.Ordinal))
            return true;

        // @frame desk SA instrument bullets (Citizen wall residual on Glass).
        if (IsSaInstrumentLine(t))
            return true;

        // @event table rows
        if (t.StartsWith("kind", StringComparison.OrdinalIgnoreCase) && t.Contains('|'))
            return true;
        if (t.StartsWith("id", StringComparison.OrdinalIgnoreCase) && t.Contains('|'))
            return true;
        if (t.StartsWith("ack", StringComparison.OrdinalIgnoreCase) && t.Contains('|'))
            return true;
        if (t.StartsWith("pulse", StringComparison.OrdinalIgnoreCase) && t.Contains('|'))
            return true;

        return false;
    }

    static bool IsSaInstrumentLine(string t)
    {
        var s = t.TrimStart('-', '*', ' ', '`');
        if (s.StartsWith("tm |", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("board |", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("peer |", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("dialog |", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("sticky |", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("presence |", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("cost |", StringComparison.OrdinalIgnoreCase)
            || s.StartsWith("sa |", StringComparison.OrdinalIgnoreCase))
            return true;

        return t.Contains("`tm |", StringComparison.OrdinalIgnoreCase)
            || t.Contains("`board |", StringComparison.OrdinalIgnoreCase)
            || t.Contains("`peer |", StringComparison.OrdinalIgnoreCase)
            || t.Contains("`dialog |", StringComparison.OrdinalIgnoreCase)
            || t.Contains("`presence |", StringComparison.OrdinalIgnoreCase)
            || t.Contains("`sticky |", StringComparison.OrdinalIgnoreCase)
            || t.Contains("`cost |", StringComparison.OrdinalIgnoreCase)
            || t.Contains("`sa |", StringComparison.OrdinalIgnoreCase);
    }

    public static bool LooksLikeAutoiWake(string? body) =>
        CascadeIDE.SoftOrgan.GlassAutoiWakeFeed.LooksLikeCharge(body);

    static string? ExtractWall(string? pulse)
    {
        if (string.IsNullOrWhiteSpace(pulse))
            return null;

        var idx = pulse.IndexOf("wall", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;

        var slice = pulse[idx..].Trim();
        // keep "wall ·~XmYs" — cut at next mid-dot cluster when pulse continues
        if (slice.Length > 28)
        {
            var cut = slice.IndexOf(" · ", 8, StringComparison.Ordinal);
            if (cut > 0)
                slice = slice[..cut];
            else
                slice = TruncatePlan(slice, 28);
        }

        return slice;
    }

/// <summary>PFD NEXT glance — 1 human move; Face Sub = cleaned leaf (no dig=/domain= verify args).</summary>
    internal static (string Glance, string? Sub) FormatGlanceNext(string? raw)
    {
        var full = CleanLeafTitle(raw ?? "");
        if (full.Length == 0)
            return ("No active leaf.", null);

        var cleaned = StripActTags(full);
        cleaned = StripPlanTheatre(cleaned);
        cleaned = StripAgentVerifyArgs(cleaned);

        // "Dig densest after … CLOSED — residual" → residual is the move.
        var em = cleaned.IndexOf(" — ", StringComparison.Ordinal);
        if (em > 0
            && (cleaned.StartsWith("Dig densest", StringComparison.OrdinalIgnoreCase)
                || cleaned.Contains("invent-only", StringComparison.OrdinalIgnoreCase)
                || cleaned.Contains("SoftFL", StringComparison.OrdinalIgnoreCase))
            && em + 3 < cleaned.Length)
        {
            var after = StripAgentVerifyArgs(StripPlanTheatre(cleaned[(em + 3)..].Trim()));
            if (after.Length is >= 8 and <= 96)
                return (after, string.Equals(after, cleaned, StringComparison.Ordinal) ? null : cleaned);
        }

        if (cleaned.Length <= 72)
        {
            if (LooksLikePlanJargon(cleaned))
                return ("Peer residual toward FullReady", cleaned);
            return (cleaned, null);
        }

        if (LooksLikePlanJargon(cleaned))
            return ("Peer residual toward FullReady", cleaned);

        var glance = TruncatePlan(cleaned, 72);
        return (glance, string.Equals(glance, cleaned, StringComparison.Ordinal) ? null : cleaned);
    }

    static string CleanLeafTitle(string s)
    {
        s = s.Replace('\r', ' ').Replace('\n', ' ').Trim();
        // Accidental REPL verb glued into title ("… @act #CIDE start").
        if (s.EndsWith(" start", StringComparison.Ordinal))
            s = s[..^6].TrimEnd();
        return s;
    }

    static string StripActTags(string s)
    {
        while (true)
        {
            var i = s.IndexOf("@act", StringComparison.OrdinalIgnoreCase);
            if (i < 0)
                break;
            var j = i + 4;
            while (j < s.Length && char.IsWhiteSpace(s[j]))
                j++;
            if (j >= s.Length || s[j] != '#')
                break;
            j++;
            while (j < s.Length && (char.IsLetterOrDigit(s[j]) || s[j] == '_'))
                j++;
            s = (s[..i] + s[j..]).Trim();
        }

        return s.Trim();
    }

    static string TruncatePlan(string s, int max)
    {
        s = s.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (s.Length <= max)
            return s;
        return s[..(max - 1)].TrimEnd() + "…";
    }
}

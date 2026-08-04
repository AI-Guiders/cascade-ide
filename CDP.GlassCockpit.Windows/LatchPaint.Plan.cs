#nullable enable
using System.Text;
using System.Text.Json;

namespace CDP.GlassCockpit.Windows;

/// <summary>plan-LATEST.json → P Plan readout (TM feature/task/wall — not presentation stub).</summary>
internal static partial class LatchPaint
{
    public sealed record PlanView(
        string Headline,
        string Detail,
        string StatusLine,
        bool Active);

    public static PlanView PaintPlan(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var active = root.TryGetProperty("active", out var a) && a.ValueKind is JsonValueKind.True;
            var feature = Prop(root, "feature");
            var task = Prop(root, "task");
            var why = Prop(root, "why");
            var pulse = Prop(root, "pulse") ?? Prop(root, "chrome_hint");

            if (!active && string.IsNullOrWhiteSpace(feature) && string.IsNullOrWhiteSpace(task)
                && string.IsNullOrWhiteSpace(why))
            {
                return new PlanView(
                    "Plan quiet",
                    "No active Task Manager leaf.",
                    "plan · quiet",
                    false);
            }

            // Shared-SSOT Plan face: NEXT = leaf, WHY = sealed course goal (not Intercom dump).
            var next = !string.IsNullOrWhiteSpace(task) ? task!.Trim()
                : !string.IsNullOrWhiteSpace(feature) ? feature!.Trim()
                : TruncatePlan(pulse ?? "Plan", 56);

            var detail = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(why))
                detail.Append("WHY · ").Append(why.Trim());

            if (!string.IsNullOrWhiteSpace(task) && !string.IsNullOrWhiteSpace(feature))
            {
                if (detail.Length > 0)
                    detail.AppendLine();
                detail.Append(feature.Trim());
            }

            var wall = ExtractWall(pulse);
            if (!string.IsNullOrWhiteSpace(wall))
            {
                if (detail.Length > 0)
                    detail.AppendLine();
                detail.Append(wall);
            }
            else if (detail.Length == 0 && !string.IsNullOrWhiteSpace(pulse) && string.IsNullOrWhiteSpace(task))
                detail.Append(TruncatePlan(pulse!, 96));

            if (detail.Length == 0)
                detail.Append(active ? "TM active" : "TM quiet");

            return new PlanView(
                next,
                detail.ToString().TrimEnd(),
                active ? $"plan · NEXT · {TruncatePlan(next, 24)}" : "plan · quiet",
                active);
        }
        catch (Exception ex)
        {
            return new PlanView(
                "Plan",
                ex.Message,
                $"plan · parse fail · {ex.Message}",
                false);
        }
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

        return sb.ToString().Trim();
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

    static string TruncatePlan(string s, int max)
    {
        s = s.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (s.Length <= max)
            return s;
        return s[..(max - 1)].TrimEnd() + "…";
    }
}

#nullable enable

using System.Globalization;
using System.Linq;
using System.Text.Json;
using CascadeIDE.Features.Cdp;

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// SoftOrgan domain ownership board — workspace <c>.cdp/domain/*.md</c> cards + habitat latch.
/// Human-faced instrument (not SoftOrganMfdGlance text dump; not inventory mill).
/// </summary>
public static class GlassDomainBoardGlance
{
    public sealed record DomainCard(string Id, string Title, string? LastShip)
    {
        public string Display =>
            string.IsNullOrWhiteSpace(LastShip)
                ? $"{Id} · {Title}"
                : $"{Id} · {Title} · {LastShip}";
    }

    public sealed record Snapshot(
        int CardCount,
        bool LatchActive,
        string? LatchPulse,
        int LatchCardCount,
        bool LearnPresent,
        string? LearnPulse,
        IReadOnlyList<DomainCard> Cards,
        string StatusLine);

    public static Snapshot? TryProbe(string? workspaceRoot)
    {
        try
        {
            var cards = new List<DomainCard>();
            if (!string.IsNullOrWhiteSpace(workspaceRoot))
            {
                var root = Path.GetFullPath(workspaceRoot.Trim());
                LoadDomainCards(Path.Combine(root, ".cdp", "domain"), cards);
            }

            // Glass cold start may have empty session root — climb from exe toward repo .cdp/domain.
            if (cards.Count == 0)
            {
                foreach (var climb in ClimbCandidates())
                    LoadDomainCards(Path.Combine(climb, ".cdp", "domain"), cards);
            }

            var (latchActive, latchPulse, latchCount) = ProbeDomainLatch();
            var (learnPresent, learnPulse) = ProbeLearnLatch();

            // Always emit instrument (latch/learn alone are human-faced) — null only on hard failure.
            var status =
                $"domain · cards={cards.Count}"
                + (latchActive ? $" · latch live · {latchCount}" : " · latch quiet")
                + (learnPresent ? " · learn on" : "");

            return new Snapshot(
                cards.Count,
                latchActive,
                latchPulse,
                latchCount,
                learnPresent,
                learnPulse,
                cards,
                status);
        }
        catch
        {
            return null;
        }
    }

    public static IReadOnlyList<GlassGlanceChip> BuildInstrument(Snapshot snap)
    {
        var live = snap.CardCount > 0 || snap.LatchActive;
        var chips = new List<GlassGlanceChip>
        {
            new("DOM", live ? "LIVE" : "IDLE", live ? "ok" : "idle"),
            new("CARDS", snap.CardCount.ToString(CultureInfo.InvariantCulture),
                snap.CardCount > 0 ? "ok" : "warn"),
            new("LATCH", snap.LatchActive ? Trunc(snap.LatchPulse ?? "on", 22) : "quiet",
                snap.LatchActive ? "ok" : "idle"),
            new("LEARN", snap.LearnPresent ? Trunc(snap.LearnPulse ?? "on", 22) : "—",
                snap.LearnPresent ? "ok" : "idle"),
        };

        foreach (var c in snap.Cards.Take(6))
        {
            chips.Add(new(
                Trunc(c.Id.ToUpperInvariant(), 12),
                Trunc(string.IsNullOrWhiteSpace(c.LastShip) ? c.Title : c.LastShip!, 28),
                "meta"));
        }

        return chips;
    }

    static void LoadDomainCards(string dir, List<DomainCard> cards)
    {
        if (!Directory.Exists(dir) || cards.Count > 0)
            return;
        foreach (var path in Directory.EnumerateFiles(dir, "*.md")
                     .OrderBy(p => Path.GetFileNameWithoutExtension(p), StringComparer.OrdinalIgnoreCase))
        {
            var id = Path.GetFileNameWithoutExtension(path);
            var (title, lastShip) = ParseCard(File.ReadAllText(path));
            cards.Add(new DomainCard(id, title, lastShip));
        }
    }

    static IEnumerable<string> ClimbCandidates()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var start in new[]
                 {
                     AppContext.BaseDirectory,
                     Environment.CurrentDirectory,
                 })
        {
            string? cur;
            try { cur = Path.GetFullPath(start); }
            catch { continue; }

            for (var i = 0; i < 8 && !string.IsNullOrEmpty(cur); i++)
            {
                if (seen.Add(cur))
                    yield return cur;
                cur = Path.GetDirectoryName(cur);
            }
        }
    }

    static (string Title, string? LastShip) ParseCard(string raw)
    {
        var title = "domain";
        string? lastShip = null;
        var lines = raw.Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            var t = line.Trim();
            if (t.StartsWith("# ", StringComparison.Ordinal) && title == "domain")
                title = Trunc(t[2..].Trim(), 40);
            if (t.StartsWith("- id:", StringComparison.OrdinalIgnoreCase))
            {
                var id = t["- id:".Length..].Trim().Trim('`');
                if (id.Length > 0)
                    title = id;
            }
        }

        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Trim().StartsWith("## last_ship", StringComparison.OrdinalIgnoreCase))
                continue;
            for (var j = i + 1; j < lines.Length && j < i + 6; j++)
            {
                var body = lines[j].Trim();
                if (body.StartsWith("## ", StringComparison.Ordinal))
                    break;
                if (body.StartsWith('-') || body.Length > 0)
                {
                    lastShip = Trunc(body.TrimStart('-', ' '), 36);
                    break;
                }
            }
            break;
        }

        return (title, lastShip);
    }

    static (bool Active, string? Pulse, int CardCount) ProbeDomainLatch()
    {
        try
        {
            var path = Path.Combine(CdpHabitatPaths.StateRoot, "domain-LATEST.json");
            if (!File.Exists(path))
                return (false, null, 0);
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var active = root.TryGetProperty("active", out var a) && a.ValueKind is JsonValueKind.True;
            var pulse = Prop(root, "pulse") ?? Prop(root, "chrome_hint");
            var count = root.TryGetProperty("card_count", out var c) && c.TryGetInt32(out var n) ? n : 0;
            return (active, pulse, count);
        }
        catch
        {
            return (false, null, 0);
        }
    }

    static (bool Present, string? Pulse) ProbeLearnLatch()
    {
        try
        {
            var path = Path.Combine(CdpHabitatPaths.StateRoot, "learn-LATEST.json");
            if (!File.Exists(path))
                return (false, null);
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var pulse = Prop(root, "pulse") ?? Prop(root, "chrome_hint");
            var active = root.TryGetProperty("active", out var a) && a.ValueKind is JsonValueKind.True;
            return (active || pulse is { Length: > 0 }, pulse);
        }
        catch
        {
            return (false, null);
        }
    }

    static string? Prop(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString()
            : null;

    static string Trunc(string s, int max)
    {
        s = s.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return s.Length <= max ? s : s[..(max - 1)].TrimEnd() + "…";
    }
}

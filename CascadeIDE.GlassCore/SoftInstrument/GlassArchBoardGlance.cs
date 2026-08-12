#nullable enable

using System.Globalization;
using System.Text.Json;

namespace CascadeIDE.SoftInstrument;

/// <summary>
/// Glass MFD SemanticMap · arch board (ADR 0196) — roles from
/// <c>.cdp/arch-board/AS_BUILT.json</c> (prefer) or <c>LATEST.json</c>.
/// </summary>
public static class GlassArchBoardGlance
{
    public sealed record RoleLine(string Role, string Status, string? ElectedLabel, string Id)
    {
        public string Display
        {
            get
            {
                var mark = Status switch
                {
                    "promoted" => "★",
                    "elected" => "●",
                    "open" => "○",
                    _ => "·",
                };
                var elect = string.IsNullOrWhiteSpace(ElectedLabel) ? "—" : ElectedLabel;
                return $"{mark} {Role} · {Status} · {elect}";
            }
        }
    }

    public sealed record Snapshot(
        string Mode,
        string? Profile,
        int RoleCount,
        int OpenCount,
        int ElectedCount,
        int PromotedCount,
        int EdgeCount,
        string? FocusRoleId,
        IReadOnlyList<RoleLine> Roles,
        string StatusLine);

    public static Snapshot? TryProbe(string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return null;

        try
        {
            var root = Path.GetFullPath(workspaceRoot.Trim());
            var dir = Path.Combine(root, ".cdp", "arch-board");
            var asBuilt = Path.Combine(dir, "AS_BUILT.json");
            var latest = Path.Combine(dir, "LATEST.json");
            var path = File.Exists(asBuilt) ? asBuilt : latest;
            if (!File.Exists(path))
                return null;

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var rootEl = doc.RootElement;
            var mode = Prop(rootEl, "mode") ?? "plan";
            var profile = Prop(rootEl, "profile");
            var focus = Prop(rootEl, "focus_role_id");
            var roles = new List<RoleLine>();
            var open = 0;
            var elect = 0;
            var promo = 0;

            if (rootEl.TryGetProperty("roles", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in arr.EnumerateArray())
                {
                    var id = Prop(r, "id") ?? "";
                    var role = Prop(r, "role") ?? id;
                    var status = Prop(r, "status") ?? "open";
                    var electedLabel = TryElectedLabel(r);
                    roles.Add(new RoleLine(role, status, electedLabel, id));
                    switch (status)
                    {
                        case "open":
                            open++;
                            break;
                        case "elected":
                            elect++;
                            break;
                        case "promoted":
                            promo++;
                            break;
                    }
                }
            }

            var edges = 0;
            if (rootEl.TryGetProperty("edges", out var eArr) && eArr.ValueKind == JsonValueKind.Array)
                edges = eArr.GetArrayLength();

            var statusLine =
                $"arch · {mode}"
                + (string.IsNullOrWhiteSpace(profile) ? "" : $" · {profile}")
                + $" · {roles.Count}r/{edges}e · open={open} elect={elect} promo={promo}";

            return new Snapshot(
                mode,
                profile,
                roles.Count,
                open,
                elect,
                promo,
                edges,
                focus,
                roles,
                statusLine);
        }
        catch
        {
            return null;
        }
    }

    public static IReadOnlyList<GlassGlanceChip> BuildInstrument(Snapshot snap)
    {
        var live = snap.RoleCount > 0;
        return
        [
            new("ARCH", live ? "LIVE" : "IDLE", live ? "ok" : "idle"),
            new("MODE", Trunc(snap.Mode, 16), snap.Mode == "as_built" ? "ok" : "warn"),
            new("PROFILE", string.IsNullOrWhiteSpace(snap.Profile) ? "—" : Trunc(snap.Profile!, 16),
                string.IsNullOrWhiteSpace(snap.Profile) ? "idle" : "ok"),
            new("ROLES", snap.RoleCount.ToString(CultureInfo.InvariantCulture), live ? "ok" : "idle"),
            new("OPEN", snap.OpenCount.ToString(CultureInfo.InvariantCulture),
                snap.OpenCount > 0 ? "warn" : "idle"),
            new("PROMO", snap.PromotedCount.ToString(CultureInfo.InvariantCulture),
                snap.PromotedCount > 0 ? "ok" : "idle"),
        ];
    }

    static string? TryElectedLabel(JsonElement role)
    {
        var electedId = Prop(role, "elected") ?? Prop(role, "elected_candidate_id");
        if (!role.TryGetProperty("candidates", out var cands) || cands.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var c in cands.EnumerateArray())
        {
            var id = Prop(c, "id");
            if (electedId is { Length: > 0 } && !string.Equals(id, electedId, StringComparison.Ordinal))
                continue;
            if (string.Equals(Prop(c, "status"), "elected", StringComparison.OrdinalIgnoreCase)
                || (electedId is { Length: > 0 } && string.Equals(id, electedId, StringComparison.Ordinal)))
            {
                return Prop(c, "label") ?? Prop(c, "symbol") ?? Prop(c, "path");
            }
        }

        return null;
    }

    static string? Prop(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString()
            : null;

    static string Trunc(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";
}

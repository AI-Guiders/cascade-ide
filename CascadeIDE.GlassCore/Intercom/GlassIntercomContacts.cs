#nullable enable

using System.Text.Json;

namespace CascadeIDE.Intercom;

/// <summary>
/// DM address book (NorthStar Open) — humans + agents equal standing.
/// Pure — no WPF. Design: glass-intercom-northstar-messenger-v0.
/// </summary>
public static class GlassIntercomContacts
{
    public const string Schema = "glass_intercom_contacts/v0";

    public enum Standing
    {
        Human,
        Agent
    }

    public readonly record struct Contact(string Id, string Display, Standing Standing)
    {
        public string Line => $"{Display} · {StandingLabel(Standing)}";
    }

    public readonly record struct Snapshot(string? SelectedId, IReadOnlyList<Contact> Roster);

    public static string StandingLabel(Standing standing) => standing switch
    {
        Standing.Human => "human",
        Standing.Agent => "agent",
        _ => "agent"
    };

    public static Standing ParseStanding(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Standing.Agent;

        return raw.Trim().ToLowerInvariant() switch
        {
            "human" or "operator" or "pm" => Standing.Human,
            "agent" or "guest" or "citizen" or "pf" => Standing.Agent,
            _ => Standing.Agent
        };
    }

    /// <summary>
    /// Day-1 DM book for operator Glass: you + Face Who (equal standing).
    /// Partner/Кир = PF lane tip (Cursor) — not a second DM row (lived confuse: two «Citizen»).
    /// <paramref name="partnerDisplay"/> kept for call-site compat; ignored in rows.
    /// </summary>
    public static IReadOnlyList<Contact> DefaultRoster(string? operatorDisplay = null, string? partnerDisplay = null, string? citizenDisplay = null)
    {
        _ = partnerDisplay;
        var op = string.IsNullOrWhiteSpace(operatorDisplay) ? "Operator" : operatorDisplay.Trim();
        var face = string.IsNullOrWhiteSpace(citizenDisplay) ? "Citizen" : citizenDisplay.Trim();
        if (string.Equals(face, op, StringComparison.OrdinalIgnoreCase))
            face = "Citizen";

        return
        [
            new("operator", op, Standing.Human),
            new("citizen", face, Standing.Agent)
        ];
    }

    public static Contact? Find(IReadOnlyList<Contact> roster, string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || roster.Count == 0)
            return null;

        var needle = id.Trim();
        foreach (var c in roster)
        {
            if (string.Equals(c.Id, needle, StringComparison.OrdinalIgnoreCase))
                return c;
        }

        return null;
    }

    public static string? ResolveSelectedId(IReadOnlyList<Contact> roster, string? preferred)
    {
        if (Find(roster, preferred) is not null)
            return preferred!.Trim();

        // Pre-2-row latch: partner row removed — DM to Face.
        if (string.Equals(preferred, "partner", StringComparison.OrdinalIgnoreCase)
            && Find(roster, "citizen") is not null)
            return "citizen";

        return roster.Count > 0 ? roster[0].Id : null;
    }

    public static Snapshot ParseLatchJson(string? raw, IReadOnlyList<Contact>? roster = null)
    {
        var book = roster ?? DefaultRoster();
        if (string.IsNullOrWhiteSpace(raw))
            return new Snapshot(ResolveSelectedId(book, null), book);

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return new Snapshot(ResolveSelectedId(book, null), book);

            string? selected = null;
            if (root.TryGetProperty("selected_id", out var sel) && sel.ValueKind == JsonValueKind.String)
                selected = sel.GetString();
            else if (root.TryGetProperty("peer_id", out var peer) && peer.ValueKind == JsonValueKind.String)
                selected = peer.GetString();

            // When caller passes live SSOT roster — selected_id only (latch contacts may be poisoned).
            if (roster is null
                && root.TryGetProperty("contacts", out var arr)
                && arr.ValueKind == JsonValueKind.Array)
            {
                var custom = new List<Contact>();
                foreach (var el in arr.EnumerateArray())
                {
                    if (el.ValueKind != JsonValueKind.Object)
                        continue;
                    if (!el.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.String)
                        continue;
                    var id = idEl.GetString();
                    if (string.IsNullOrWhiteSpace(id))
                        continue;
                    var display = el.TryGetProperty("display", out var d) && d.ValueKind == JsonValueKind.String
                        ? d.GetString()
                        : id;
                    var standing = el.TryGetProperty("standing", out var st) && st.ValueKind == JsonValueKind.String
                        ? ParseStanding(st.GetString())
                        : Standing.Agent;
                    custom.Add(new Contact(id.Trim(), string.IsNullOrWhiteSpace(display) ? id.Trim() : display!.Trim(), standing));
                }

                if (custom.Count > 0)
                    book = custom;
            }

            return new Snapshot(ResolveSelectedId(book, selected), book);
        }
        catch
        {
            return new Snapshot(ResolveSelectedId(book, null), book);
        }
    }

    public static string FormatLatchJson(string? selectedId, IReadOnlyList<Contact>? roster = null, DateTimeOffset? stampedUtc = null)
    {
        var book = roster ?? DefaultRoster();
        var sel = ResolveSelectedId(book, selectedId);
        var stamp = (stampedUtc ?? DateTimeOffset.UtcNow).ToString("o");
        var contacts = book.Select(c => new Dictionary<string, object?>
        {
            ["id"] = c.Id,
            ["display"] = c.Display,
            ["standing"] = StandingLabel(c.Standing)
        }).ToList();

        var doc = new Dictionary<string, object?>
        {
            ["schema"] = Schema,
            ["selected_id"] = sel,
            ["contacts"] = contacts,
            ["stamped_utc"] = stamp
        };
        return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
    }
}

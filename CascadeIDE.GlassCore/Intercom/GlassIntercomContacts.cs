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

    /// <summary>Day-1 roster: operator + habitat partner + Citizen — equal standing lines.</summary>
    public static IReadOnlyList<Contact> DefaultRoster(string? operatorDisplay = null, string? partnerDisplay = null, string? citizenDisplay = null)
    {
        var op = string.IsNullOrWhiteSpace(operatorDisplay) ? "Operator" : operatorDisplay.Trim();
        var partner = string.IsNullOrWhiteSpace(partnerDisplay) ? "Кир" : partnerDisplay.Trim();
        // Collision with Citizen seat id — guest bootstrap (LatchPaint) when sticky nick is "Citizen".
        if (string.Equals(partner, "Citizen", StringComparison.OrdinalIgnoreCase))
            partner = "Кир";

        var citizen = string.IsNullOrWhiteSpace(citizenDisplay) ? "Citizen" : citizenDisplay.Trim();

        return
        [
            new("operator", op, Standing.Human),
            new("partner", partner, Standing.Agent),
            new("citizen", citizen, Standing.Agent)
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

            if (root.TryGetProperty("contacts", out var arr) && arr.ValueKind == JsonValueKind.Array)
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

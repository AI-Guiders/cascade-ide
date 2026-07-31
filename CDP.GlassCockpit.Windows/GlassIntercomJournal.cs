#nullable enable
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Virtual History cold store — same wire as cdp-mcp <c>intercom-journal.jsonl</c>.
/// Human scroll survives restart; PF uses <c>cdp_intercom op=history</c>.
/// </summary>
internal static class GlassIntercomJournal
{
    public const string FileName = "intercom-journal.jsonl";

    static readonly object Gate = new();

    static readonly JsonSerializerOptions WriteOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static string JournalPath => Path.Combine(CdpHabitatPaths.StateRoot, FileName);

    public sealed record Entry(
        string Id,
        string FromSeat,
        string ToSeat,
        string Body,
        string Origin,
        DateTimeOffset StampedUtc,
        string RoleLabel,
        string WhenLabel);

    public static void Append(string id, string fromSeat, string toSeat, string body, string origin, DateTimeOffset stampedUtc)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(body))
            return;

        lock (Gate)
        {
            try
            {
                CdpHabitatPaths.EnsureStateRoot();
                if (File.Exists(JournalPath))
                {
                    foreach (var line in File.ReadLines(JournalPath))
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;
                        try
                        {
                            using var doc = JsonDocument.Parse(line);
                            if (doc.RootElement.TryGetProperty("id", out var idEl)
                                && string.Equals(idEl.GetString(), id, StringComparison.OrdinalIgnoreCase))
                                return;
                        }
                        catch
                        {
                            /* skip */
                        }
                    }
                }

                var payload = new
                {
                    schema = GlassIntercomSend.Schema,
                    id,
                    from_seat = fromSeat,
                    to_seat = toSeat,
                    body,
                    origin,
                    stamped_utc = stampedUtc,
                    acked = false
                };
                var json = JsonSerializer.Serialize(payload, WriteOpts);
                File.AppendAllText(JournalPath, json + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                /* best-effort */
            }
        }
    }

    public static IReadOnlyList<Entry> LoadTail(int limit = 40)
    {
        if (limit < 1) limit = 1;
        if (limit > 200) limit = 200;

        lock (Gate)
        {
            if (!File.Exists(JournalPath))
                return [];

            var all = new List<Entry>();
            foreach (var line in File.ReadLines(JournalPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;
                    var id = Prop(root, "id") ?? "";
                    var from = Prop(root, "from_seat") ?? "?";
                    var to = Prop(root, "to_seat") ?? "?";
                    var origin = Prop(root, "origin") ?? "?";
                    var body = Prop(root, "body") ?? "";
                    if (body.Length == 0)
                        continue;
                    var stamped = Prop(root, "stamped_utc") ?? "";
                    DateTimeOffset.TryParse(stamped, out var dto);
                    if (dto == default)
                        dto = DateTimeOffset.UtcNow;
                    var when = dto.ToLocalTime().ToString("HH:mm");
                    var role = $"@{from.ToUpperInvariant()} → @{to.ToUpperInvariant()} · {origin}";
                    all.Add(new Entry(id, from, to, body.Replace("\r\n", "\n"), origin, dto, role, when));
                }
                catch
                {
                    /* skip */
                }
            }

            if (all.Count <= limit)
                return all;
            return all.GetRange(all.Count - limit, limit);
        }
    }

    static string? Prop(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;
}

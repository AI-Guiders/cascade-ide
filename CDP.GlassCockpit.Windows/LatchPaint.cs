#nullable enable
using System.Text;
using System.Text.Json;

namespace CDP.GlassCockpit.Windows;

/// <summary>Latch JSON → human glass fields (never dump wire into seats).</summary>
internal static partial class LatchPaint
{
    public sealed record IntercomView(
        string Header,
        string Body,
        string RoleLabel,
        string WhenLabel,
        string StatusLine,
        string? MessageId,
        string FromSeat = "?",
        string ToSeat = "?",
        string Origin = "?",
        string? Name = null,
        string? Kind = null);

    public sealed record PresentationView(
        string Headline,
        string Detail,
        string? Topology,
        string? Tier,
        string? MfdPage,
        string StatusLine);

    public static IntercomView PaintIntercom(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var from = Prop(root, "from_seat") ?? "?";
            var to = Prop(root, "to_seat") ?? "?";
            var origin = Prop(root, "origin") ?? "?";
            var id = Prop(root, "id");
            var stamped = Prop(root, "stamped_utc") ?? "";
            var acked = root.TryGetProperty("acked", out var a) && a.ValueKind is JsonValueKind.True;
            var rawBody = Prop(root, "body") ?? "(empty)";
            var wake = LooksLikeAutoiWake(rawBody);
            var body = CompactIntercomBody(rawBody);
            var name = Prop(root, "name") ?? Prop(root, "display_name");
            var kind = Prop(root, "kind");
            var (resolvedName, resolvedKind) = ResolveIntercomIdentity(from, origin, name, kind);
            if (wake)
            {
                resolvedName = "Autoi";
                resolvedKind = "wake";
            }

            var whenLabel = TryLocalTime(stamped) ?? DateTime.Now.ToString("HH:mm");
            var role = FormatIntercomRole(from, to, resolvedName, resolvedKind);
            var header = acked ? $"{role} · acked" : $"{role} · unread";
            if (!string.IsNullOrEmpty(id))
                header += $" · {id}";

            return new IntercomView(
                header,
                body,
                role,
                whenLabel,
                $"intercom · {resolvedName} · {resolvedKind} · {(acked ? "acked" : "unread")}",
                string.IsNullOrWhiteSpace(id) ? null : id,
                from,
                to,
                origin,
                resolvedName,
                resolvedKind);
        }
        catch (Exception ex)
        {
            return new IntercomView(
                "Intercom (parse fail)",
                json,
                "system",
                DateTime.Now.ToString("HH:mm"),
                $"intercom · parse fail · {ex.Message}",
                null);
        }
    }

    public static PresentationView PaintPresentation(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var topology = Prop(root, "topology");
            var tier = Prop(root, "tier");
            var mfd = Prop(root, "mfd_page");
            var origin = Prop(root, "origin") ?? "—";

            var headline = string.Join(" · ", new[]
            {
                string.IsNullOrWhiteSpace(topology) ? null : topology,
                string.IsNullOrWhiteSpace(tier) ? null : tier
            }.Where(s => s is not null)!);

            if (string.IsNullOrWhiteSpace(headline))
                headline = "Cabin presentation";

            // Topology/MFD only — Plan/TM paints from plan-LATEST (PaintPlan), not this latch.
            var detail = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(mfd))
                detail.AppendLine($"MFD focus: {mfd}");
            detail.Append($"Origin: {origin}");

            return new PresentationView(
                headline,
                detail.ToString().TrimEnd(),
                topology,
                tier,
                mfd,
                $"presentation · {tier ?? "—"} · {mfd ?? "—"}");
        }
        catch (Exception ex)
        {
            return new PresentationView(
                "Presentation",
                ex.Message,
                null,
                null,
                null,
                $"presentation · parse fail · {ex.Message}");
        }
    }

    static string? Prop(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    static string? TryLocalTime(string stampedUtc)
    {
        if (DateTimeOffset.TryParse(stampedUtc, out var dto))
            return dto.ToLocalTime().ToString("HH:mm");
        return null;
    }

    /// <summary>Parity with cdp-mcp <c>CideIntercomVoiceLatch.ResolveIdentity</c>.</summary>
    internal static (string Name, string Kind) ResolveIntercomIdentity(
        string fromSeat,
        string origin,
        string? name,
        string? kind)
    {
        var k = NormalizeIntercomKind(kind);
        if (k is null)
        {
            if (string.Equals(origin, "human", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fromSeat, "pm", StringComparison.OrdinalIgnoreCase))
                k = "operator";
            else
                k = "guest";
        }

        var n = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        n ??= k switch
        {
            "operator" => "Света",
            "citizen" => "Citizen",
            _ => "Кир"
        };
        return (n, k);
    }

    static string? NormalizeIntercomKind(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        return raw.Trim().ToLowerInvariant() switch
        {
            "guest" or "cursor" or "external" => "guest",
            "citizen" or "fm" or "peer" => "citizen",
            // "who" is Agent Who (agent identity) — never an operator alias
            "operator" or "human" or "pm" => "operator",
            _ => null
        };
    }

    internal static string FormatIntercomRole(string fromSeat, string toSeat, string name, string kind) =>
        $"{name} · {kind} @{fromSeat.Trim().ToUpperInvariant()} → @{toSeat.Trim().ToUpperInvariant()}";
}

#nullable enable
using System.Text;
using System.Text.Json;
using CascadeIDE.Features.Cdp;

namespace CDP.GlassCockpit.Windows;

/// <summary>Latch JSON → human glass fields (never dump wire into seats).</summary>
internal static class LatchPaint
{
    public sealed record IntercomView(
        string Header,
        string Body,
        string RoleLabel,
        string WhenLabel,
        string StatusLine);

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
            var id = Prop(root, "id") ?? "";
            var stamped = Prop(root, "stamped_utc") ?? "";
            var acked = root.TryGetProperty("acked", out var a) && a.ValueKind is JsonValueKind.True;
            var body = Prop(root, "body") ?? "(empty)";

            var whenLabel = TryLocalTime(stamped) ?? DateTime.Now.ToString("HH:mm");
            var role = $"@{from.ToUpperInvariant()} → @{to.ToUpperInvariant()} · {origin}";
            var header = acked ? $"{role} · acked" : $"{role} · unread";
            if (!string.IsNullOrEmpty(id))
                header += $" · {id}";

            return new IntercomView(
                header,
                body.Replace("\r\n", "\n"),
                role,
                whenLabel,
                $"intercom · {from}→{to} · {(acked ? "acked" : "unread")}");
        }
        catch (Exception ex)
        {
            return new IntercomView(
                "Intercom (parse fail)",
                json,
                "system",
                DateTime.Now.ToString("HH:mm"),
                $"intercom · parse fail · {ex.Message}");
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

            var detail = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(mfd))
                detail.AppendLine($"MFD focus: {mfd}");
            detail.AppendLine($"Origin: {origin}");
            detail.AppendLine();
            detail.Append("PFD instruments / TM later.");

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

    /// <summary>Read SoftOrgan latch chrome_hint (null if idle / missing / parse fail).</summary>
    public static string? TryReadChromeHint(string path)
    {
        try
        {
            var raw = CdpLatchIo.TryReadAllTextIfExists(path);
            if (raw is null)
                return null;
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.TryGetProperty("chrome_hint", out var hintEl)
                && hintEl.ValueKind == JsonValueKind.String)
            {
                var hint = hintEl.GetString();
                return string.IsNullOrWhiteSpace(hint) ? null : hint.Trim();
            }

            // Dark Cockpit: active=false or missing hint → silent
            if (root.TryGetProperty("active", out var activeEl)
                && activeEl.ValueKind is JsonValueKind.False)
                return null;

            if (root.TryGetProperty("pulse", out var pulseEl)
                && pulseEl.ValueKind == JsonValueKind.String)
            {
                var pulse = pulseEl.GetString();
                return string.IsNullOrWhiteSpace(pulse) ? null : pulse.Trim();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}

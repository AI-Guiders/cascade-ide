#nullable enable
using System.Text;
using System.Text.Json;

namespace CDP.GlassCockpit.Windows;

/// <summary>Turn latch JSON into human glass — not dump-the-wire.</summary>
internal static class LatchPaint
{
    public sealed record IntercomView(
        string Header,
        string Body,
        string StatusLine);

    public sealed record PresentationView(
        string PlanText,
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

            var header =
                $"@{from.ToUpperInvariant()} → @{to.ToUpperInvariant()}  ·  {origin}" +
                (string.IsNullOrEmpty(id) ? "" : $"  ·  {id}") +
                (string.IsNullOrEmpty(stamped) ? "" : $"  ·  {stamped}") +
                (acked ? "  ·  acked" : "  ·  unread");

            return new IntercomView(
                header,
                body.Replace("\r\n", "\n"),
                $"intercom · {from}→{to} · {(acked ? "acked" : "unread")}");
        }
        catch (Exception ex)
        {
            return new IntercomView(
                "Intercom (parse fail)",
                json,
                $"intercom · parse fail · {ex.Message}");
        }
    }

    public static PresentationView PaintPresentation(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var topology = Prop(root, "topology") ?? "—";
            var tier = Prop(root, "tier") ?? "—";
            var mfd = Prop(root, "mfd_page");
            var origin = Prop(root, "origin") ?? "—";
            var stamped = Prop(root, "stamped_utc") ?? "—";

            var sb = new StringBuilder();
            sb.AppendLine("Plan / presentation");
            sb.AppendLine();
            sb.AppendLine($"Topology   {topology}");
            sb.AppendLine($"Tier       {tier}");
            sb.AppendLine($"MFD page   {mfd ?? "—"}");
            sb.AppendLine($"Origin     {origin}");
            sb.AppendLine($"Stamped    {stamped}");
            sb.AppendLine();
            sb.AppendLine("(P seat — TM / SA later peels)");

            return new PresentationView(
                sb.ToString(),
                mfd,
                $"presentation · {tier} · {topology}");
        }
        catch (Exception ex)
        {
            return new PresentationView(
                "presentation-LATEST\n\n" + json,
                null,
                $"presentation · parse fail · {ex.Message}");
        }
    }

    static string? Prop(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;
}

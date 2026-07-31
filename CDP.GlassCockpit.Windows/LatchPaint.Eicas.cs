#nullable enable
using System.Text.Json;

namespace CDP.GlassCockpit.Windows;

/// <summary>alert/qrh latch → EICAS status lines.</summary>
internal static partial class LatchPaint
{
    public sealed record EicasView(string StatusLine);

    /// <summary>alert-LATEST.json → EICAS status line; null when clear/empty.</summary>
    public static EicasView? PaintAlert(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var schema = Prop(root, "schema");
            if (!string.Equals(schema, "cide_alert_latch/v1", StringComparison.OrdinalIgnoreCase))
                return null;
            var origin = Prop(root, "origin");
            if (!string.Equals(origin, "agent", StringComparison.OrdinalIgnoreCase))
                return null;

            var level = (Prop(root, "level") ?? "clear").Trim().ToLowerInvariant();
            if (level is "clear" or "")
                return null;

            var tag = level switch
            {
                "fail" => "WARN",
                "warn" => "CAUT",
                _ => "ADV"
            };

            var pulse = Prop(root, "pulse");
            string? firstLine = null;
            if (root.TryGetProperty("lines", out var linesEl) && linesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in linesEl.EnumerateArray())
                {
                    if (el.ValueKind != JsonValueKind.String)
                        continue;
                    var s = el.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        firstLine = s.Trim();
                        break;
                    }
                }
            }

            var text = firstLine ?? pulse;
            if (string.IsNullOrWhiteSpace(text))
                return null;

            return new EicasView($"EICAS · {tag} · {text.Trim()}");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>qrh-LATEST.json → advisory line; null when no hot_id.</summary>
    public static EicasView? PaintQrh(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var schema = Prop(root, "schema");
            if (!string.Equals(schema, "cide_qrh_latch/v1", StringComparison.OrdinalIgnoreCase))
                return null;
            var origin = Prop(root, "origin");
            if (!string.Equals(origin, "agent", StringComparison.OrdinalIgnoreCase))
                return null;

            var hotId = Prop(root, "hot_id");
            if (string.IsNullOrWhiteSpace(hotId))
                return null;

            var pulse = Prop(root, "pulse");
            var hotTitle = Prop(root, "hot_title");
            var head = !string.IsNullOrWhiteSpace(pulse)
                ? pulse!.Trim()
                : (!string.IsNullOrWhiteSpace(hotTitle) ? hotTitle!.Trim() : hotId!.Trim());

            return new EicasView($"EICAS · ADV · {head}");
        }
        catch
        {
            return null;
        }
    }
}

#nullable enable
using System.Text.Json;

namespace CDP.GlassCockpit.Windows;

/// <summary>seats-LATEST.json → SoftOrgan chrome + optional ShowFace attention.</summary>
internal static partial class LatchPaint
{
    public const string SeatsSchema = "cide_seats_latch/v1";
    public const string SeatsOriginAgent = "agent";

    public sealed record SeatsView(
        string? MfdPage,
        string? ChromeHint,
        string? MOrgan,
        bool ShowFace,
        string? FaceSeat,
        string? WebAiUrl,
        string StatusLine);

    /// <summary>Null when schema/origin gate fails or JSON is unreadable.</summary>
    public static SeatsView? PaintSeats(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var schema = Prop(root, "schema");
            if (!string.Equals(schema, SeatsSchema, StringComparison.OrdinalIgnoreCase))
                return null;
            var origin = Prop(root, "origin");
            if (!string.Equals(origin, SeatsOriginAgent, StringComparison.OrdinalIgnoreCase))
                return null;

            var mfd = Prop(root, "mfd_page");
            var chrome = HumanizeChromeHint(Prop(root, "chrome_hint"));
            var mOrgan = TrySeatPin(root, "m");
            var showFace = PropBool(root, "show_face");
            var faceSeat = Prop(root, "face_seat");
            if (string.IsNullOrWhiteSpace(faceSeat))
                faceSeat = null;
            var webAi = Prop(root, "web_ai_url");
            if (string.IsNullOrWhiteSpace(webAi))
                webAi = null;

            var faceBit = showFace ? " · show_face" : "";
            var navBit = webAi is null ? "" : " · webai_nav";
            return new SeatsView(
                string.IsNullOrWhiteSpace(mfd) ? null : mfd.Trim(),
                chrome,
                mOrgan,
                showFace,
                faceSeat,
                webAi?.Trim(),
                $"seats · {mfd ?? "—"} · {(chrome ?? "—")}{faceBit}{navBit}");
        }
        catch
        {
            return null;
        }
    }

    static string? TrySeatPin(JsonElement root, string seat)
    {
        if (!root.TryGetProperty("seats", out var seats)
            || seats.ValueKind != JsonValueKind.Object)
            return null;
        if (!seats.TryGetProperty(seat, out var pin)
            || pin.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        var s = pin.GetString();
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    static bool PropBool(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el))
            return false;
        return el.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(el.GetString(), out var b) && b,
            JsonValueKind.Number => el.TryGetInt32(out var n) && n != 0,
            _ => false
        };
    }
}

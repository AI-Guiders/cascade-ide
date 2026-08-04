#nullable enable
using System.Text.Json;

namespace CDP.GlassCockpit.Windows;

/// <summary>seats-LATEST.json → MFD select + cabin SoftOrgan chrome (Avalonia CdpSeatsProjector parity).</summary>
internal static partial class LatchPaint
{
    public const string SeatsSchema = "cide_seats_latch/v1";
    public const string SeatsOriginAgent = "agent";

    public sealed record SeatsView(
        string? MfdPage,
        string? ChromeHint,
        string? MOrgan,
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
            var chrome = Prop(root, "chrome_hint");
            if (string.IsNullOrWhiteSpace(chrome))
                chrome = null;
            var mOrgan = TrySeatPin(root, "m");

            return new SeatsView(
                string.IsNullOrWhiteSpace(mfd) ? null : mfd.Trim(),
                chrome,
                mOrgan,
                $"seats · {mfd ?? "—"} · {(chrome ?? "—")}");
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
}

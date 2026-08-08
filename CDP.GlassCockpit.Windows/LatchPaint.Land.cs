#nullable enable
using System.Text.Json;

namespace CDP.GlassCockpit.Windows;

/// <summary>land-LATEST.json → AvalonEdit open/goto (Avalonia CdpLandProjector parity).</summary>
internal static partial class LatchPaint
{
    public const string LandSchema = "navigation_land_latch/v1";

    /// <param name="ShowFace">True = invite Human Face (AvalonEdit + PreferSurface). False = quiet tip.</param>
    public sealed record LandView(string Path, int? Line, string? Member, bool ShowFace, string StatusLine);

    /// <summary>Null when schema/path gate fails or JSON is unreadable.</summary>
    public static LandView? PaintLand(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var schema = Prop(root, "schema");
            if (!string.Equals(schema, LandSchema, StringComparison.OrdinalIgnoreCase))
                return null;

            var path = Prop(root, "path");
            if (string.IsNullOrWhiteSpace(path))
                return null;

            int? line = null;
            if (root.TryGetProperty("line", out var lineEl)
                && lineEl.ValueKind == JsonValueKind.Number
                && lineEl.TryGetInt32(out var n)
                && n > 0)
                line = n;

            var member = Prop(root, "member");
            var cmd = Prop(root, "command") ?? "open";
            var showFace = PropBool(root, "show_face");
            var where = line is { } L ? $"L{L}" : "open";
            var faceBit = showFace ? " · show_face" : " · quiet";
            return new LandView(
                path.Trim(),
                line,
                string.IsNullOrWhiteSpace(member) ? null : member.Trim(),
                showFace,
                $"land · {cmd} · {where}{faceBit} · {System.IO.Path.GetFileName(path)}");
        }
        catch
        {
            return null;
        }
    }
}

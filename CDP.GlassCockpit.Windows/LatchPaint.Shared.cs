#nullable enable
using System.Text.Json;

namespace CDP.GlassCockpit.Windows;

/// <summary>shared-LATEST.json → co-presence chrome (Avalonia CdpSharedFileProjector parity).</summary>
internal static partial class LatchPaint
{
    public const string SharedSchema = "shared_file_latch/v1";
    public const string SharedSuffix = " · shared";

    public sealed record SharedView(string? Path, bool Shared, string StatusLine);

    /// <summary>Null when schema gate fails or JSON is unreadable.</summary>
    public static SharedView? PaintShared(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var schema = Prop(root, "schema");
            if (!string.Equals(schema, SharedSchema, StringComparison.OrdinalIgnoreCase))
                return null;

            var path = Prop(root, "path");
            var shared = root.TryGetProperty("shared", out var s) && s.ValueKind is JsonValueKind.True;
            var file = string.IsNullOrWhiteSpace(path) ? "—" : System.IO.Path.GetFileName(path);
            return new SharedView(
                string.IsNullOrWhiteSpace(path) ? null : path.Trim(),
                shared,
                $"shared · {(shared ? "on" : "off")} · {file}");
        }
        catch
        {
            return null;
        }
    }
}

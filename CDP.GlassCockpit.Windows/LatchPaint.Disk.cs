#nullable enable
using System.Text.Json;

namespace CDP.GlassCockpit.Windows;

/// <summary>disk-LATEST.json → AvalonEdit reload (Avalonia CdpDiskSyncProjector parity).</summary>
internal static partial class LatchPaint
{
    public const string DiskSchema = "document_disk_sync_latch/v1";
    public const string DiskOriginAgent = "agent";
    public const string DiskOriginHuman = "human";

    public sealed record DiskView(string Path, string Origin, string StatusLine);

    /// <summary>Null when schema/path/origin gate fails or JSON is unreadable.</summary>
    public static DiskView? PaintDisk(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var schema = Prop(root, "schema");
            if (!string.Equals(schema, DiskSchema, StringComparison.OrdinalIgnoreCase))
                return null;

            var path = Prop(root, "path");
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var origin = Prop(root, "origin") ?? "";
            if (!string.Equals(origin, DiskOriginAgent, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(origin, DiskOriginHuman, StringComparison.OrdinalIgnoreCase))
                return null;

            return new DiskView(
                path.Trim(),
                origin.ToLowerInvariant(),
                $"disk · {origin.ToLowerInvariant()} · {System.IO.Path.GetFileName(path)}");
        }
        catch
        {
            return null;
        }
    }
}

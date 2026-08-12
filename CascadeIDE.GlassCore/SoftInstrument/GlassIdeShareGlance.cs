#nullable enable

using System.IO;
using System.Text.Json;
using CascadeIDE.Features.Cdp;

namespace CascadeIDE.SoftInstrument;

/// <summary>
/// IdeShare operator delivery face — <c>share/v1</c> LATEST.json under habitat + project .cdp/share.
/// Used by Glass FDS SHARE (not co-presence <c>shared_file_latch/v1</c>).
/// </summary>
public static class GlassIdeShareGlance
{
    public const string Schema = "share/v1";

    public sealed record Hit(string FileName, string? Path, string Status);

    /// <summary>
    /// Null when no operator share/v1 LATEST is present.
    /// With <paramref name="workspaceRoot"/>: project <c>.cdp/share</c> wins over habitat (workspace FDS face).
    /// </summary>
    public static Hit? TryReadOperatorLatest(string? workspaceRoot)
    {
        IEnumerable<string> dirs = GlassOperatorShareShelf.ResolveInboxes(workspaceRoot);
        // ResolveInboxes is habitat-first; Glass FDS on a project wants the project inbox first.
        if (!string.IsNullOrWhiteSpace(workspaceRoot))
            dirs = dirs.Reverse();

        foreach (var dir in dirs)
        {
            var metaPath = Path.Combine(dir, "LATEST.json");
            if (!File.Exists(metaPath))
                continue;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(metaPath));
                var root = doc.RootElement;
                var schema = Prop(root, "schema");
                if (!string.Equals(schema, Schema, StringComparison.OrdinalIgnoreCase))
                    continue;
                var with = Prop(root, "with");
                if (with is { Length: > 0 }
                    && !string.Equals(with, "operator", StringComparison.OrdinalIgnoreCase))
                    continue;
                var status = Prop(root, "status") ?? "shared";
                if (string.Equals(status, "shelved", StringComparison.OrdinalIgnoreCase))
                    continue;
                var path = Prop(root, "path");
                var file = !string.IsNullOrWhiteSpace(path)
                    ? Path.GetFileName(path)
                    : Prop(root, "title") ?? Prop(root, "what") ?? "shared";
                return new Hit(file!, string.IsNullOrWhiteSpace(path) ? null : path, status);
            }
            catch
            {
                /* try next inbox */
            }
        }

        return null;
    }

    static string? Prop(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;
}

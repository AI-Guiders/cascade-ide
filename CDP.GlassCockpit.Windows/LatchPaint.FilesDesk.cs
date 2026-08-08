#nullable enable
using System.Text.Json;

namespace CDP.GlassCockpit.Windows;

/// <summary>files_desk-LATEST.json → Glass FilesDesk MFD Face (not SolutionExplorer).</summary>
internal static partial class LatchPaint
{
    public const string FilesDeskSchema = "cide_files_desk_latch/v1";
    public const string FilesDeskOriginAgent = "agent";

    public sealed record FilesDeskEntryView(string Kind, string Name, string? Path, string Line);

    public sealed record FilesDeskView(
        bool Active,
        string? Pulse,
        string? Op,
        string? Where,
        string? Cwd,
        int EntryCount,
        IReadOnlyList<FilesDeskEntryView> Entries,
        string StatusLine);

    public static FilesDeskView? PaintFilesDesk(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var schema = Prop(root, "schema");
            if (!string.Equals(schema, FilesDeskSchema, StringComparison.OrdinalIgnoreCase))
                return null;
            var origin = Prop(root, "origin");
            if (!string.Equals(origin, FilesDeskOriginAgent, StringComparison.OrdinalIgnoreCase))
                return null;

            var active = PropBool(root, "active");
            var pulse = Prop(root, "pulse");
            var op = Prop(root, "op");
            var where = Prop(root, "where");
            var cwd = Prop(root, "cwd");
            var count = 0;
            if (root.TryGetProperty("entry_count", out var ec) && ec.TryGetInt32(out var n))
                count = n;

            var entries = new List<FilesDeskEntryView>();
            if (root.TryGetProperty("entries", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in arr.EnumerateArray())
                {
                    var kind = Prop(el, "kind") ?? "file";
                    var name = Prop(el, "name") ?? "";
                    var path = Prop(el, "path");
                    if (name.Length == 0 && string.IsNullOrWhiteSpace(path))
                        continue;
                    var mark = kind.Equals("dir", StringComparison.OrdinalIgnoreCase) ? "[dir]" : "[file]";
                    var label = name.Length > 0 ? name : System.IO.Path.GetFileName(path);
                    entries.Add(new FilesDeskEntryView(kind, label ?? "", path, $"{mark} {label}"));
                }
            }

            if (count <= 0)
                count = entries.Count;

            var status = active
                ? $"files · {where ?? "—"} · {cwd ?? "—"} · {count}"
                : "files · idle";
            return new FilesDeskView(active, pulse, op, where, cwd, count, entries, status);
        }
        catch
        {
            return null;
        }
    }
}

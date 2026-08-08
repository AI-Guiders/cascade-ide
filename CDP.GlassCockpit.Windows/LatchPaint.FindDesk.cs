#nullable enable
using System.Text.Json;

namespace CDP.GlassCockpit.Windows;

/// <summary>find_desk-LATEST.json → Glass FindDesk MFD Face (not RelatedFiles).</summary>
internal static partial class LatchPaint
{
    public const string FindDeskSchema = "cide_find_desk_latch/v1";
    public const string FindDeskOriginAgent = "agent";

    public sealed record FindDeskHitView(string Path, int? LineNumber, string Preview, string Display);

    public sealed record FindDeskView(
        bool Active,
        string? Pulse,
        string? Op,
        string? Where,
        string? Query,
        int HitCount,
        IReadOnlyList<FindDeskHitView> Hits,
        string StatusLine);

    public static FindDeskView? PaintFindDesk(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var schema = Prop(root, "schema");
            if (!string.Equals(schema, FindDeskSchema, StringComparison.OrdinalIgnoreCase))
                return null;
            var origin = Prop(root, "origin");
            if (!string.Equals(origin, FindDeskOriginAgent, StringComparison.OrdinalIgnoreCase))
                return null;

            var active = PropBool(root, "active");
            var pulse = Prop(root, "pulse");
            var op = Prop(root, "op");
            var where = Prop(root, "where");
            var query = Prop(root, "query");
            var hitCount = 0;
            if (root.TryGetProperty("hit_count", out var hc) && hc.TryGetInt32(out var n))
                hitCount = n;

            var hits = new List<FindDeskHitView>();
            if (root.TryGetProperty("hits", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in arr.EnumerateArray())
                {
                    var path = Prop(el, "path") ?? Prop(el, "file") ?? "";
                    if (path.Length == 0)
                        continue;
                    int? line = null;
                    if (el.TryGetProperty("line", out var ln) && ln.TryGetInt32(out var l) && l > 0)
                        line = l;
                    else if (el.TryGetProperty("line_number", out var ln2) && ln2.TryGetInt32(out var l2) && l2 > 0)
                        line = l2;
                    var preview = Prop(el, "preview") ?? Prop(el, "line_text") ?? Prop(el, "text") ?? "";
                    var label = line is int L
                        ? $"{System.IO.Path.GetFileName(path)}:{L}  {preview}".Trim()
                        : $"{System.IO.Path.GetFileName(path)}  {preview}".Trim();
                    hits.Add(new FindDeskHitView(path, line, preview, label));
                }
            }

            if (hitCount <= 0)
                hitCount = hits.Count;

            var status = active
                ? $"find · {query ?? "—"} · {hitCount} · {where ?? "—"}"
                : "find · idle";
            return new FindDeskView(active, pulse, op, where, query, hitCount, hits, status);
        }
        catch
        {
            return null;
        }
    }
}

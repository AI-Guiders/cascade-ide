#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>
/// Glass <c>/status</c> IOP glance body (workspace + editor + MFD + layout + latch).
/// Pure formatter — WPF collects <see cref="Snapshot"/>; tests assert shape without cabin.
/// </summary>
public static class GlassIopStatusGlance
{
    public sealed record Snapshot(
        string? WorkspaceRoot,
        bool IntercomForward,
        string? StatusLine,
        string? Subtitle,
        string? EditorPath,
        int? CaretLine,
        bool? EditorDirty,
        string? MfdPage,
        string? Topology,
        string? ColumnDefinitions,
        string? LatchStateRoot);

    public static string Format(Snapshot s)
    {
        var editor = string.IsNullOrWhiteSpace(s.EditorPath) ? "(none)" : s.EditorPath.Trim();
        var caret = s.CaretLine is int line ? line.ToString() : "-";
        var dirty = s.EditorDirty switch
        {
            true => "yes",
            false => "no",
            null => "-",
        };
        var mfd = string.IsNullOrWhiteSpace(s.MfdPage) ? "(none)" : s.MfdPage.Trim();
        var topo = string.IsNullOrWhiteSpace(s.Topology) ? "-" : s.Topology.Trim();
        var cols = string.IsNullOrWhiteSpace(s.ColumnDefinitions) ? "-" : s.ColumnDefinitions.Trim();
        var latch = string.IsNullOrWhiteSpace(s.LatchStateRoot) ? "-" : s.LatchStateRoot.Trim();

        return
            $"workspace: {s.WorkspaceRoot}\n"
            + $"intercom forward: {s.IntercomForward}\n"
            + $"editor: {editor}\n"
            + $"caret: {caret}\n"
            + $"dirty: {dirty}\n"
            + $"mfd: {mfd}\n"
            + $"topology: {topo} · cols={cols}\n"
            + $"latch: {latch}\n"
            + $"status: {s.StatusLine}\n"
            + $"subtitle: {s.Subtitle}";
    }
}

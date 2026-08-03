#nullable enable

using System.Text.Json;

namespace CascadeIDE.SoftOrgan;

/// <summary>Parse debug_desk-LATEST.json stack/locals for Glass MFD DebugStack.</summary>
public static class GlassDebugDeskLatchReader
{
    public sealed record StackFrame(int Index, string Name, string? File, int Line)
    {
        public string Display => File is null ? Name : $"{Name} · {File}:{Line}";
    }

    public sealed record LocalVar(string Name, string Value);

    public sealed record Snapshot(
        bool HasLatch,
        bool Stopped,
        bool ActiveDap,
        int BpCount,
        string? Pulse,
        string? Verdict,
        IReadOnlyList<StackFrame> Stack,
        IReadOnlyList<LocalVar> Locals,
        int LocalsFrameIndex);

    public static Snapshot Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Empty();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var stack = ParseStack(root);
            var localsFrame = root.TryGetProperty("locals_frame_index", out var lfiEl) && lfiEl.TryGetInt32(out var lfi)
                ? lfi
                : 0;
            var locals = ParseLocals(root);
            return new Snapshot(
                HasLatch: true,
                Stopped: root.TryGetProperty("stopped", out var st) && st.ValueKind == JsonValueKind.True,
                ActiveDap: root.TryGetProperty("active_dap", out var ad) && ad.ValueKind == JsonValueKind.True,
                BpCount: root.TryGetProperty("bp_count", out var bp) && bp.TryGetInt32(out var bpi) ? bpi : 0,
                Pulse: root.TryGetProperty("pulse", out var p) ? p.GetString() : null,
                Verdict: root.TryGetProperty("verdict", out var v) ? v.GetString() : null,
                Stack: stack,
                Locals: locals,
                LocalsFrameIndex: localsFrame);
        }
        catch
        {
            return Empty();
        }
    }

    static Snapshot Empty() =>
        new(false, false, false, 0, null, null, [], [], 0);

    static IReadOnlyList<StackFrame> ParseStack(JsonElement root)
    {
        var list = new List<StackFrame>();
        if (!root.TryGetProperty("stack", out var stack) || stack.ValueKind != JsonValueKind.Array)
            return list;

        var i = 0;
        foreach (var f in stack.EnumerateArray())
        {
            var name = f.TryGetProperty("name", out var n) ? n.GetString() ?? "?" : "?";
            var file = f.TryGetProperty("file", out var fl) ? fl.GetString() : null;
            var line = f.TryGetProperty("line", out var ln) && ln.TryGetInt32(out var li) ? li : 0;
            list.Add(new StackFrame(i++, name, file, line));
        }

        return list;
    }

    static IReadOnlyList<LocalVar> ParseLocals(JsonElement root)
    {
        var list = new List<LocalVar>();
        if (!root.TryGetProperty("locals", out var locals) || locals.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var v in locals.EnumerateArray())
        {
            var name = v.TryGetProperty("name", out var n) ? n.GetString() ?? "?" : "?";
            var val = v.TryGetProperty("value", out var vv) ? vv.GetString() ?? "" : "";
            list.Add(new LocalVar(name, val));
        }

        return list;
    }
}

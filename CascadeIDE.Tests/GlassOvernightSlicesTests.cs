#nullable enable

using CascadeIDE.Intercom;
using CascadeIDE.SoftInstrument;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassDapCommandBridgeTests : IDisposable
{
    readonly string _root;

    public GlassDapCommandBridgeTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "glass-dap-cmd-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        GlassDapCommandBridge.RootOverrideForTests = _root;
    }

    public void Dispose()
    {
        GlassDapCommandBridge.RootOverrideForTests = null;
        try { Directory.Delete(_root, recursive: true); } catch { /* ignore */ }
    }

    [Fact]
    public void TryPublish_writes_continue_latch()
    {
        Assert.True(GlassDapCommandBridge.TryPublish(GlassDapCommandBridge.Continue));
        var raw = File.ReadAllText(GlassDapCommandBridge.LatchPath);
        Assert.Contains(GlassDapCommandBridge.Schema, raw);
        Assert.Contains("\"command\": \"continue\"", raw);
    }

    [Fact]
    public void TryPublish_variables_includes_frame_index()
    {
        Assert.True(GlassDapCommandBridge.TryPublishVariables(3));
        var raw = File.ReadAllText(GlassDapCommandBridge.LatchPath);
        Assert.Contains("\"command\": \"variables\"", raw);
        Assert.Contains("\"frame_index\": 3", raw);
    }
}

public sealed class GlassDebugDeskLatchReaderTests
{
    [Fact]
    public void Read_parses_stack_and_locals()
    {
        const string json = """
            {
              "stopped": true,
              "active_dap": true,
              "bp_count": 2,
              "locals_frame_index": 1,
              "stack": [
                {"name": "Main", "file": "C:/p/Program.cs", "line": 12},
                {"name": "Run", "file": "C:/p/Program.cs", "line": 8}
              ],
              "locals": [
                {"name": "x", "value": "42"}
              ]
            }
            """;

        var snap = GlassDebugDeskLatchReader.Read(json);
        Assert.True(snap.HasLatch);
        Assert.Equal(2, snap.Stack.Count);
        Assert.Equal("Main", snap.Stack[0].Name);
        Assert.Equal(1, snap.LocalsFrameIndex);
        Assert.Single(snap.Locals);
    }
}

public sealed class GlassSemanticMapGraphTests
{
    [Fact]
    public void Collect_builds_multi_hop_edges()
    {
        var root = Path.Combine(Path.GetTempPath(), "glass-smg-" + Guid.NewGuid().ToString("N"));
        var src = Path.Combine(root, "src");
        Directory.CreateDirectory(src);
        var editor = Path.Combine(src, "Foo.cs");
        File.WriteAllText(editor, "//");
        File.WriteAllText(Path.Combine(src, "Foo.xaml"), "<Grid/>");
        File.WriteAllText(Path.Combine(src, "Bar.cs"), "//");

        try
        {
            var graph = GlassSemanticMapGraph.Collect(root, editor, maxNodes: 32, maxHop: 2);
            Assert.NotNull(graph.FocusPath);
            Assert.Contains(graph.Nodes, n => n.Hop == 1);
            Assert.Contains(graph.Edges, e => e.Reason.Length > 0);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
    }
}

public sealed class GlassMessageCodePeelTests
{
    [Fact]
    public void PeelBareRefs_finds_path_line()
    {
        var chips = GlassMessageCodePeel.PeelBareRefs("see Foo.cs:12 and Bar.cs:3-5");
        Assert.Equal(2, chips.Count);
        Assert.Equal(12, chips[0].LineStart);
        Assert.Equal(3, chips[1].LineStart);
        Assert.Equal(5, chips[1].LineEnd);
    }

    [Fact]
    public void PeelBareRefs_finds_backtick_path()
    {
        var chips = GlassMessageCodePeel.PeelBareRefs("open `LatchPaint.cs:40` now");
        Assert.Single(chips);
        Assert.Equal(40, chips[0].LineStart);
    }

    [Fact]
    public void MergeWithAttach_dedupes_brackets()
    {
        var attach = GlassAttachChipPeel.FromBody("[Foo.cs:1]");
        var merged = GlassMessageCodePeel.MergeWithAttach(attach, "also Foo.cs:1");
        Assert.Single(merged);
    }
}

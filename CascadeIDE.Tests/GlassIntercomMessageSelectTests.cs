#nullable enable
using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassIntercomMessageSelectTests
{
    [Theory]
    [InlineData("3", 3, 3)]
    [InlineData("3 5", 3, 5)]
    [InlineData("3:5", 3, 5)]
    public void TryParseRange_contiguous(string tail, int start, int end)
    {
        Assert.True(GlassIntercomMessageSelect.TryParseRange(tail, out var s, out var e, out _));
        Assert.Equal(start, s);
        Assert.Equal(end, e);
    }

    [Fact]
    public void Apply_and_clear()
    {
        Assert.Equal("OK", GlassIntercomMessageSelect.Apply(10, 2, 4, out var sel));
        Assert.Equal(4, sel.ActiveOrdinal);
        Assert.Equal(3, sel.Highlighted.Count);
        Assert.True(GlassIntercomMessageSelect.IsClear("clear"));
    }

    [Fact]
    public void Slash_resolves_cide_path_and_short_id()
    {
        Assert.True(GlassSlashCatalog.TryResolve("/intercom message select 5", out var cmd, out var args));
        Assert.Equal("select", cmd.Id);
        Assert.Equal("/intercom message select", cmd.Path);
        Assert.Equal(GlassSlashCatalog.ArgTailKind.Required, cmd.ArgTail);
        Assert.Equal("5", args);

        Assert.True(GlassSlashCatalog.TryResolve("/select 3", out cmd, out args));
        Assert.Equal("select", cmd.Id);
        Assert.Equal("3", args);

        Assert.True(GlassSlashCatalog.TryResolve("/intercom message select clear", out cmd, out args));
        Assert.Equal("clear", args);

        Assert.True(GlassSlashCatalog.TryResolve("/intercom message next", out cmd, out _));
        Assert.Equal("message_next", cmd.Id);
        Assert.True(GlassSlashCatalog.TryResolve("/intercom message prev", out cmd, out _));
        Assert.Equal("message_prev", cmd.Id);
    }

    [Fact]
    public void ArgTail_ADR0150_auto_run_matrix()
    {
        // required: autocomplete Enter must NOT run bare (insert + wait for N).
        Assert.False(GlassSlashCatalog.ShouldAutoRunOnCommit("/intercom message select"));
        Assert.False(GlassSlashCatalog.ShouldAutoRunOnCommit("/open"));
        Assert.False(GlassSlashCatalog.ShouldAutoRunOnCommit("/citizen"));
        Assert.True(GlassSlashCatalog.ShouldAutoRunOnCommit("/select 3"));

        // none / optional: bare auto-run OK (attach may still emit honest usage if no selection).
        Assert.True(GlassSlashCatalog.ShouldAutoRunOnCommit("/help"));
        Assert.True(GlassSlashCatalog.ShouldAutoRunOnCommit("/attach"));
        Assert.True(GlassSlashCatalog.ShouldAutoRunOnCommit("/topics"));

        var hits = GlassSlashCatalog.Suggest("/sel");
        Assert.Contains(hits, h => h.InsertText == "/select ");
    }

    [Fact]
    public void Multi_bracket_and_offset()
    {
        Assert.True(GlassIntercomMessageSelect.TryParseSegments("[3;5] [8;15] [20]", out var segs, out _));
        Assert.Equal(3, segs.Count);
        Assert.Equal("OK", GlassIntercomMessageSelect.ApplySegments(25, segs, out var sel));
        Assert.Equal(20, sel.ActiveOrdinal);
        Assert.Equal(5 - 3 + 1 + 15 - 8 + 1 + 1, sel.Highlighted.Count);

        Assert.Equal("OK", GlassIntercomMessageSelect.ApplyOffset(25, sel, -1, out var prev));
        Assert.Equal(19, prev.ActiveOrdinal);
    }

    [Fact]
    public void Empty_select_args_is_usage_not_silent()
    {
        Assert.False(GlassIntercomMessageSelect.TryParseSegments("", out _, out var err));
        Assert.StartsWith("usage:", err);
    }
}

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
        Assert.Equal("5", args);

        Assert.True(GlassSlashCatalog.TryResolve("/select 3", out cmd, out args));
        Assert.Equal("select", cmd.Id);
        Assert.Equal("3", args);

        Assert.True(GlassSlashCatalog.TryResolve("/intercom message select clear", out cmd, out args));
        Assert.Equal("clear", args);
    }
}

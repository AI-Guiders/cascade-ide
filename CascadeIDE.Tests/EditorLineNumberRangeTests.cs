using CascadeIDE.Models.Editor;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class EditorLineNumberRangeTests
{
    [Fact]
    public void LineNumber_TryCreate_Rejects_ZeroAndNegative()
    {
        Assert.False(LineNumber.TryCreate(0, out _));
        Assert.False(LineNumber.TryCreate(-1, out _));
        Assert.True(LineNumber.TryCreate(1, out var one));
        Assert.Equal(1, one.Value);
    }

    [Fact]
    public void LineRange_TryCreate_Rejects_InvertedOrder()
    {
        Assert.True(LineNumber.TryCreate(7, out var a));
        Assert.True(LineNumber.TryCreate(3, out var b));
        Assert.False(LineRange.TryCreate(a, b, out _));
    }

    [Fact]
    public void LineRange_TryCreate_Accepts_SingleLine()
    {
        Assert.True(LineNumber.TryCreate(5, out var a));
        Assert.True(LineRange.TryCreate(a, a, out var r));
        Assert.Equal(5, r.Start.Value);
        Assert.Equal(5, r.End.Value);
    }

    [Fact]
    public void ColumnNumber_TryCreate_Rejects_ZeroAndNegative()
    {
        Assert.False(ColumnNumber.TryCreate(0, out _));
        Assert.False(ColumnNumber.TryCreate(-3, out _));
        Assert.True(ColumnNumber.TryCreate(1, out var c));
        Assert.Equal(1, c.Value);
    }
}

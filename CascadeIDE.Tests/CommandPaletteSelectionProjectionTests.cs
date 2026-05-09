using CascadeIDE.Features.Shell.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CommandPaletteSelectionProjectionTests
{
    [Theory]
    [InlineData(1, 5, 3, 4)]
    [InlineData(4, 5, 5, 0)]
    [InlineData(0, 5, -1, 4)]
    public void Circular_move_wraps(int current, int count, int delta, int expect)
    {
        Assert.True(CommandPaletteSelectionProjection.TryMoveCircular(current, delta, count, out var next));
        Assert.Equal(expect, next);
    }

    [Fact]
    public void Circular_empty_list_noop()
    {
        Assert.False(CommandPaletteSelectionProjection.TryMoveCircular(0, 1, 0, out var next));
        Assert.Equal(0, next);
    }

    [Fact]
    public void Page_move_clamped()
    {
        Assert.True(CommandPaletteSelectionProjection.TryPageMove(1, 1, 8, 10, out var n));
        Assert.Equal(9, n);
        Assert.True(CommandPaletteSelectionProjection.TryPageMove(9, 1, 8, 10, out n));
        Assert.Equal(9, n);
    }

    [Fact]
    public void Page_move_zero_step_fails()
    {
        Assert.False(CommandPaletteSelectionProjection.TryPageMove(3, 0, 8, 10, out var n));
        Assert.Equal(3, n);
    }
}

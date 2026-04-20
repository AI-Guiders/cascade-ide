using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeCommandDocDisplayTests
{
    [Fact]
    public void ShortTitleForCommandId_Truncates_before_returns_clause()
    {
        var t = IdeCommandDocDisplay.ShortTitleForCommandId("debug_continue");
        Assert.Contains("Продолжить", t, StringComparison.Ordinal);
        Assert.DoesNotContain("returns:", t, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShortTitleForCommandId_UnknownId_returns_id()
    {
        Assert.Equal("__no_such_command_xyz__", IdeCommandDocDisplay.ShortTitleForCommandId("__no_such_command_xyz__"));
    }
}

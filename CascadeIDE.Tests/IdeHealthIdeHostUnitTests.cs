using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using CascadeIDE.Cockpit.DataBus;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeHealthIdeHostUnitTests
{
    [Theory]
    [InlineData(false, false, null)]
    [InlineData(true, false, "LSP · C#")]
    [InlineData(false, true, "LSP · MD")]
    [InlineData(true, true, "LSP · C# · MD")]
    public void Compose_sets_LspStatusHint(bool cs, bool md, string? expected)
    {
        var u = IdeHealthIdeHostUnit.Default.Compose(new IdeHostStateChanged(cs, md, cs, md));
        Assert.Equal(expected, u.LspStatusHint);
    }
}

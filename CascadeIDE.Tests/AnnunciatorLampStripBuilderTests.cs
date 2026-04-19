using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AnnunciatorLampStripBuilderTests
{
    [Fact]
    public void Build_returns_items_in_order_with_defaults()
    {
        var list = AnnunciatorLampStripBuilder.Create()
            .AddLamp("a", "MD", AnnunciatorLampLevel.Ok)
            .AddLamp("b", "C#", AnnunciatorLampLevel.Caution, "C# LSP", "x")
            .Build();

        Assert.Equal(2, list.Count);
        Assert.Equal("a", list[0].Id);
        Assert.Equal("MD", list[0].LampShortLabel);
        Assert.Equal("MD", list[0].Title);
        Assert.Equal("", list[0].Detail);
        Assert.Equal(AnnunciatorLampLevel.Ok, list[0].Level);

        Assert.Equal("b", list[1].Id);
        Assert.Equal("C# LSP", list[1].Title);
        Assert.Equal("x", list[1].Detail);
    }
}

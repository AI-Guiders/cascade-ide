using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public class PresentationSurfaceWireTests
{
    [Fact]
    public void Intercom_Plus_Channel_OneOf_Is_F_And_PmOneOf()
    {
        var pack = PresentationSurfaceWire.Parse("(intercom)(sit/world/alert)");
        Assert.True(pack.IsSuccess, pack.Error);
        Assert.Equal(2, pack.Slots.Count);

        Assert.Equal(PresentationScanRole.F, pack.Slots[0].Role);
        Assert.Equal(new[] { "intercom" }, pack.Slots[0].Stack);
        Assert.Equal("intercom", pack.Slots[0].Active);

        Assert.Equal(PresentationScanRole.PmOneOf, pack.Slots[1].Role);
        Assert.Equal(PresentationZoneCompose.OneOf, pack.Slots[1].Compose);
        Assert.Equal(new[] { "sit", "world", "alert" }, pack.Slots[1].Stack);
        Assert.Equal("sit", pack.Slots[1].Active);
    }

    [Fact]
    public void Editor_Dedicated_Same_Pattern()
    {
        var pack = PresentationSurfaceWire.Parse("(editor)(sit/world)");
        Assert.True(pack.IsSuccess, pack.Error);
        Assert.Equal(PresentationScanRole.F, pack.Slots[0].Role);
        Assert.Equal("editor", pack.Slots[0].Active);
        Assert.Equal(PresentationScanRole.PmOneOf, pack.Slots[1].Role);
    }

    [Fact]
    public void Three_Groups_Full_Scan()
    {
        var pack = PresentationSurfaceWire.Parse("(intercom)(sit)(world)");
        Assert.True(pack.IsSuccess, pack.Error);
        Assert.Equal(3, pack.Slots.Count);
        Assert.Contains(pack.Slots, s => s.Role == PresentationScanRole.F && s.Active == "intercom");
        Assert.Contains(pack.Slots, s => s.Role == PresentationScanRole.P && s.Active == "sit");
        Assert.Contains(pack.Slots, s => s.Role == PresentationScanRole.M && s.Active == "world");
    }

    [Fact]
    public void Legacy_Meta_Wire_Via_Parser()
    {
        var legacy = PresentationParser.Parse("(F)(P/M)", PresentationGrammarTokens.Default);
        var pack = PresentationSurfaceWire.FromLegacyMetaWire(legacy);
        Assert.True(pack.IsSuccess, pack.Error);
        Assert.Equal(PresentationScanRole.F, pack.Slots[0].Role);
        Assert.Equal(PresentationScanRole.PmOneOf, pack.Slots[1].Role);
        Assert.Equal(new[] { "p", "m" }, pack.Slots[1].Stack);
    }

    [Fact]
    public void Single_TopLevel_Scan_OneOf_P_F_M()
    {
        var pack = PresentationSurfaceWire.Parse("(P/F/M)");
        Assert.True(pack.IsSuccess, pack.Error);
        Assert.Single(pack.Slots);
        Assert.Equal(PresentationScanRole.PmOneOf, pack.Slots[0].Role);
        Assert.Equal(PresentationZoneCompose.OneOf, pack.Slots[0].Compose);
        Assert.Equal(new[] { "p", "f", "m" }, pack.Slots[0].Stack);
        Assert.Equal("p", pack.Slots[0].Active);
    }

    [Fact]
    public void Single_Group_Spatial_Split_Fails_Surface_Wire()
    {
        var pack = PresentationSurfaceWire.Parse("(P+F+M)");
        Assert.False(pack.IsSuccess);
        Assert.Contains("OneOf", pack.Error, StringComparison.OrdinalIgnoreCase);
    }
}

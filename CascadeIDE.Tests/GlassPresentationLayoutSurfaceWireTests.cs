using CascadeIDE.GlassCore.Presentation;
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public class GlassPresentationLayoutSurfaceWireTests
{
    [Fact]
    public void Resolve_Surface_Wire_Sets_PmOneOf_Flags()
    {
        var settings = new CascadeIdeSettings();
        settings.Display.Screens.Topology = "(intercom)(sit/world/alert)";
        var snap = GlassPresentationLayout.Resolve(settings);
        Assert.True(snap.ParseOk, snap.ParseError);
        Assert.True(snap.Flags.PmOneOfHostTopology);
        Assert.True(snap.Flags.OneOfHostTopology);
        Assert.NotNull(snap.SurfacePack);
        Assert.Contains(snap.SurfacePack!.Slots, s => s.Role == PresentationScanRole.F && s.Active == "intercom");
        Assert.Contains(snap.SurfacePack.Slots, s => s.Role == PresentationScanRole.PmOneOf);
    }

    [Theory]
    [InlineData("sit", PresentationAnchorKind.Pfd)]
    [InlineData("world", PresentationAnchorKind.Mfd)]
    [InlineData("intercom", PresentationAnchorKind.Forward)]
    public void ZoneForSurface_Maps_Paint(string surface, PresentationAnchorKind want) =>
        Assert.Equal(want, GlassPresentationLayout.ZoneForSurface(surface));

    [Fact]
    public void Resolve_Single_TopLevel_P_F_M_OneOf_No_Hosts()
    {
        var settings = new CascadeIdeSettings();
        settings.Display.Screens.Topology = "(P/F/M)";
        var snap = GlassPresentationLayout.Resolve(settings);
        Assert.True(snap.ParseOk, snap.ParseError);
        Assert.False(snap.Flags.PmOneOfHostTopology);
        Assert.False(snap.Flags.OneOfHostTopology);
        Assert.False(snap.Flags.TripleOneAnchorPerZone);
        Assert.Equal("*,4,0,4,0", snap.ColumnDefinitions);
        Assert.NotNull(snap.SurfacePack);
        Assert.Single(snap.SurfacePack!.Slots);
        Assert.Equal(PresentationScanRole.PmOneOf, snap.SurfacePack.Slots[0].Role);
        Assert.Equal(new[] { "p", "f", "m" }, snap.SurfacePack.Slots[0].Stack);
    }

    [Theory]
    [InlineData("p", "*,4,0,4,0")]
    [InlineData("f", "0,4,*,4,0")]
    [InlineData("m", "0,4,0,4,*")]
    public void ColumnDefsForScanOneOfActive_Xor(string surface, string cols) =>
        Assert.Equal(cols, GlassPresentationLayout.ColumnDefsForScanOneOfActive(surface));
}

using CascadeIDE.GlassCore.Presentation;
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public class GlassPresentationLayoutSurfaceWireTests
{
    [Fact]
    public void OperatorReviewFlight_Is_Single_TopLevel_No_Satellite_Host()
    {
        var settings = new CascadeIdeSettings();
        settings.Display.Screens.Topology = GlassPresentationLayout.OperatorReviewFlightTopology;
        var snap = GlassPresentationLayout.Resolve(settings);
        Assert.True(snap.ParseOk, snap.ParseError);
        Assert.Equal("(F/P/M)", snap.Topology);
        Assert.True(GlassPresentationLayout.IsSingleTopLevelOneOf(snap.SurfacePack));
        Assert.False(GlassPresentationLayout.SpawnsSatelliteOneOfHost(snap.Flags));
        Assert.False(snap.Flags.PmOneOfHostTopology);
        Assert.False(snap.Flags.OneOfHostTopology);
    }

    [Fact]
    public void TwoGroup_Intercom_Plus_ChannelOneOf_Spawns_Satellite_Host()
    {
        // Regression: agent published this as "cabin tour" — 2 windows, Intercom stuck on F.
        // Legal wire, but NOT OperatorReviewFlightTopology.
        var settings = new CascadeIdeSettings();
        settings.Display.Screens.Topology = "(intercom)(sit/world/alert)";
        var snap = GlassPresentationLayout.Resolve(settings);
        Assert.True(snap.ParseOk, snap.ParseError);
        Assert.False(GlassPresentationLayout.IsSingleTopLevelOneOf(snap.SurfacePack));
        Assert.True(GlassPresentationLayout.SpawnsSatelliteOneOfHost(snap.Flags));
        Assert.NotEqual(GlassPresentationLayout.OperatorReviewFlightTopology, snap.Topology);
    }

    [Fact]
    public void ChannelStack_AllInOneWindow_No_Satellite_Host()
    {
        var settings = new CascadeIdeSettings();
        settings.Display.Screens.Topology = "(intercom/sit/world/alert)";
        var snap = GlassPresentationLayout.Resolve(settings);
        Assert.True(snap.ParseOk, snap.ParseError);
        Assert.True(GlassPresentationLayout.IsSingleTopLevelOneOf(snap.SurfacePack));
        Assert.False(GlassPresentationLayout.SpawnsSatelliteOneOfHost(snap.Flags));
        Assert.Equal(new[] { "intercom", "sit", "world", "alert" }, snap.SurfacePack!.Slots[0].Stack);
    }

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

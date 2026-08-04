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
}

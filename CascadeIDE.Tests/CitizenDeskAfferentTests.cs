using CascadeIDE.Cockpit.Channels.Eicas;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CitizenDeskAfferentTests
{
    [Fact]
    public void TryPack_EmptyBoard_ReturnsNull()
    {
        Assert.Null(CitizenDeskAfferent.TryPack([]));
        Assert.Null(CitizenDeskAfferent.TryPack(null));
    }

    [Fact]
    public void TryPack_EmitsWireGrammar()
    {
        var packed = CitizenDeskAfferent.TryPack(
            ["P:plan · #12", "F:editor · open"],
            sa: "clear",
            tm: "#12 afferent",
            peer: "build ok");
        Assert.NotNull(packed);
        Assert.StartsWith("@frame desk v0", packed, StringComparison.Ordinal);
        Assert.Contains("board | P:plan · #12 | F:editor · open", packed, StringComparison.Ordinal);
        Assert.Contains("sa | clear", packed, StringComparison.Ordinal);
        Assert.Contains("tm | #12 afferent", packed, StringComparison.Ordinal);
        Assert.Contains("peer | build ok", packed, StringComparison.Ordinal);
        Assert.Contains("cost | A", packed, StringComparison.Ordinal);
    }

    [Fact]
    public void TryPackFromHabitat_UsesEicasSeverity()
    {
        var packed = CitizenDeskAfferent.TryPackFromHabitat(
            chromeVisibleLines: null,
            eicasMessages: [new EicasMessage(EicasSeverity.Caution, "disk drift")],
            saDeskHint: null,
            planHint: null,
            ideHealthPeer: null);
        Assert.NotNull(packed);
        Assert.Contains("Caution: disk drift", packed, StringComparison.Ordinal);
        Assert.Contains("eicas · Caution · disk drift", packed, StringComparison.Ordinal);
    }

    [Fact]
    public void MergeIntoMinimized_PutsDeskFirst()
    {
        var merged = CitizenDeskAfferent.MergeIntoMinimized("@frame desk\n", "hot block");
        Assert.StartsWith("@frame desk", merged, StringComparison.Ordinal);
        Assert.Contains("hot block", merged, StringComparison.Ordinal);
        Assert.Contains("---", merged, StringComparison.Ordinal);
    }
}

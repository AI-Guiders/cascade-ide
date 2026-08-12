using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Peel #14 dogfood: closed citizen wire loop composes without a second host.
/// Afferent desk pulse + efferent @intent map (attention lives in MAF prompts).
/// </summary>
public sealed class CitizenWireLoopDogfoodTests
{
    [Fact]
    public void ClosedLoop_AfferentThenEfferent_Compose()
    {
        // Afferent: SoftInstrument board → @frame desk → minimized first.
        var desk = CitizenDeskAfferent.TryPack(
            ["P:plan · #14 dogfood", "F:editor · Services/CitizenIntentEfferent.cs"],
            sa: "clear",
            tm: "#14 dogfood closed wire");
        Assert.NotNull(desk);

        var minimized = CitizenDeskAfferent.MergeIntoMinimized(desk, "hot | last edit IntentEfferent");
        Assert.NotNull(minimized);
        Assert.StartsWith("@frame desk", minimized, StringComparison.Ordinal);
        Assert.Contains("tm | #14 dogfood closed wire", minimized, StringComparison.Ordinal);
        Assert.Contains("hot | last edit IntentEfferent", minimized, StringComparison.Ordinal);

        // Efferent: crew callouts → IDE organs; W-spray refused.
        var intents = CitizenIntentEfferent.ExtractIntentTexts("""
            saw desk pulse
            @intent open path=Services/CitizenIntentEfferent.cs
            @intent seats_detail=full
            @intent drill editor
            """);
        Assert.Equal(3, intents.Count);

        var open = CitizenIntentEfferent.MapToIde(intents[0]);
        Assert.True(open.Ok);
        Assert.Equal("open_file", open.CommandId);
        Assert.Equal("Services/CitizenIntentEfferent.cs", open.Args!["path"].GetString());

        var spray = CitizenIntentEfferent.MapToIde(intents[1]);
        Assert.False(spray.Ok);
        Assert.Contains("refuse_w_spray", spray.Reason, StringComparison.Ordinal);

        var drill = CitizenIntentEfferent.MapToIde(intents[2]);
        Assert.True(drill.Ok);
        Assert.Equal("get_editor_state", drill.CommandId);
    }
}

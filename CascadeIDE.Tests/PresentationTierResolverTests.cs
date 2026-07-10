using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class PresentationTierResolverTests
{
    private static PresentationGrammarTokens Grammar() =>
        PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");

    private static DisplayPresentationSettings DefaultSettings() => new();

    [Fact]
    public void Auto_two_monitors_returns_compact()
    {
        var parse = PresentationParser.Parse("(P+F) (M)", Grammar());
        var monitors = new PresentationMonitorSnapshot(2, 1920, 1080, 3840);
        Assert.Equal(PresentationTierKind.Compact, PresentationTierResolver.Resolve(DefaultSettings(), parse, monitors));
    }

    [Fact]
    public void Auto_triple_topology_three_monitors_returns_cockpit()
    {
        var parse = PresentationParser.Parse("(P) (F) (M)", Grammar());
        var monitors = new PresentationMonitorSnapshot(3, 1920, 1080, 5760);
        Assert.Equal(PresentationTierKind.Cockpit, PresentationTierResolver.Resolve(DefaultSettings(), parse, monitors));
    }

    [Fact]
    public void Auto_ultrawide_single_screen_can_be_cockpit()
    {
        var settings = new DisplayPresentationSettings { UltrawideCockpitEnabled = true };
        var parse = PresentationParser.Parse("(F)", Grammar());
        var monitors = new PresentationMonitorSnapshot(1, 5120, 1440, 5120);
        Assert.Equal(PresentationTierKind.Cockpit, PresentationTierResolver.Resolve(settings, parse, monitors));
    }

    [Fact]
    public void Explicit_compact_overrides_three_monitors()
    {
        var settings = new DisplayPresentationSettings { Tier = "compact" };
        var parse = PresentationParser.Parse("(P) (F) (M)", Grammar());
        var monitors = new PresentationMonitorSnapshot(3, 1920, 1080, 5760);
        Assert.Equal(PresentationTierKind.Compact, PresentationTierResolver.Resolve(settings, parse, monitors));
    }
}

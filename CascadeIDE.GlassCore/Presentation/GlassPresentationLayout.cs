#nullable enable
using CascadeIDE.GlassCore.Settings;
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.GlassCore.Presentation;

/// <summary>
/// Resolves CIDE presentation line → main-grid column frame (same builders as Avalonia CIDE).
/// Toolkit-agnostic: WPF applies widths; Avalonia used string ColumnDefinitions.Parse.
/// </summary>
public static class GlassPresentationLayout
{
    public sealed record Snapshot(
        string Topology,
        string ColumnDefinitions,
        PresentationTopologyFlags Flags,
        bool ParseOk,
        string? ParseError);

    public static Snapshot Resolve(CascadeIdeSettings settings, string? topologyOverride = null)
    {
        var screens = settings.Display.Screens;
        var topology = string.IsNullOrWhiteSpace(topologyOverride)
            ? screens.Topology
            : topologyOverride.Trim();

        return ResolveTopology(
            topology,
            screens.Grammar.Brackets,
            screens.Grammar.BetweenScreens,
            screens.Grammar.BetweenZones,
            screens.Grammar.Pfd,
            screens.Grammar.Forward,
            screens.Grammar.Mfd);
    }

    /// <summary>Thin peel overload — prefer <see cref="Resolve(CascadeIdeSettings,string?)"/>.</summary>
    public static Snapshot Resolve(IdeGlassSettings settings, string? topologyOverride = null)
    {
        var topology = string.IsNullOrWhiteSpace(topologyOverride)
            ? settings.Topology
            : topologyOverride.Trim();

        return ResolveTopology(
            topology,
            settings.Grammar.Brackets,
            settings.Grammar.BetweenScreens,
            settings.Grammar.BetweenZones,
            settings.Grammar.Pfd,
            settings.Grammar.Forward,
            settings.Grammar.Mfd);
    }

    static Snapshot ResolveTopology(
        string topology,
        string brackets,
        string betweenScreens,
        string betweenZones,
        string pfd,
        string forward,
        string mfd)
    {
        var grammar = PresentationGrammarTokens.FromSettings(
            brackets,
            betweenScreens,
            betweenZones,
            pfd,
            forward,
            mfd);

        var parse = PresentationParser.Parse(topology, grammar);
        if (!parse.IsSuccess)
        {
            return new Snapshot(
                topology,
                PresentationMainGridColumnDefinitions.Default,
                default,
                false,
                parse.Error);
        }

        var flags = PresentationTopologyResolver.ResolveFlags(parse);
        var frame = PresentationTopologyResolver.BuildMainWindowGridAtStartup(parse, flags);
        return new Snapshot(topology, frame.ColumnDefinitions, flags, true, null);
    }
}

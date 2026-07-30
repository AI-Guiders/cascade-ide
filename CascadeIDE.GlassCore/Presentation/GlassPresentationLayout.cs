#nullable enable
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

        var grammar = PresentationGrammarTokens.FromSettings(
            screens.Grammar.Brackets,
            screens.Grammar.BetweenScreens,
            screens.Grammar.BetweenZones,
            screens.Grammar.Pfd,
            screens.Grammar.Forward,
            screens.Grammar.Mfd);

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

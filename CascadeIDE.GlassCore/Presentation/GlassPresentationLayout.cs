#nullable enable
using CascadeIDE.GlassCore.Settings;
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

    public static Snapshot Resolve(IdeGlassSettings settings, string? topologyOverride = null)
    {
        var topology = string.IsNullOrWhiteSpace(topologyOverride)
            ? settings.Topology
            : topologyOverride.Trim();

        var grammar = PresentationGrammarTokens.FromSettings(
            settings.Grammar.Brackets,
            settings.Grammar.BetweenScreens,
            settings.Grammar.BetweenZones,
            settings.Grammar.Pfd,
            settings.Grammar.Forward,
            settings.Grammar.Mfd);

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

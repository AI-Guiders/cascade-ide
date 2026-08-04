#nullable enable
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.GlassCore.Presentation;

/// <summary>
/// Resolves presentation line → main-grid + topology flags.
/// Prefers surface wire <c>(intercom)(sit/world/alert)</c> (Scan + channel stacks);
/// falls back to legacy P/F/M parser.
/// </summary>
public static class GlassPresentationLayout
{
    public sealed record Snapshot(
        string Topology,
        string ColumnDefinitions,
        PresentationTopologyFlags Flags,
        bool ParseOk,
        string? ParseError,
        PresentationSurfacePack? SurfacePack = null);

    public static Snapshot Resolve(CascadeIdeSettings settings, string? topologyOverride = null)
    {
        var screens = settings.Display.Screens;
        var topology = string.IsNullOrWhiteSpace(topologyOverride)
            ? screens.Topology
            : topologyOverride.Trim();

        var surface = PresentationSurfaceWire.Parse(topology);
        if (surface.IsSuccess && surface.Slots.Count > 0)
        {
            var flags = FlagsFromSurfacePack(surface);
            var cols = ColumnDefsForSurfacePack(surface);
            return new Snapshot(topology, cols, flags, true, null, surface);
        }

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
                surface.Error ?? parse.Error);
        }

        var legacyFlags = PresentationTopologyResolver.ResolveFlags(parse);
        var frame = PresentationTopologyResolver.BuildMainWindowGridAtStartup(parse, legacyFlags);
        var legacyPack = PresentationSurfaceWire.FromLegacyMetaWire(parse);
        return new Snapshot(
            topology,
            frame.ColumnDefinitions,
            legacyFlags,
            true,
            null,
            legacyPack.IsSuccess ? legacyPack : null);
    }

    static PresentationTopologyFlags FlagsFromSurfacePack(PresentationSurfacePack pack)
    {
        var hasF = pack.Slots.Any(s => s.Role == PresentationScanRole.F);
        var hasPmOneOf = pack.Slots.Any(s => s.Role == PresentationScanRole.PmOneOf);
        var hasP = pack.Slots.Any(s => s.Role == PresentationScanRole.P);
        var hasM = pack.Slots.Any(s => s.Role == PresentationScanRole.M);
        var triple = hasF && hasP && hasM && pack.Slots.Count == 3 && !hasPmOneOf;

        return new PresentationTopologyFlags(
            DedicatedMfdSecondScreen: false,
            TripleOneAnchorPerZone: triple,
            ForwardMfdTwoScreen: false,
            PmForwardTwoScreen: false,
            PmOneOfForwardTwoScreen: hasF && hasPmOneOf,
            OneOfPlusDedicatedTwoScreen: hasF && hasPmOneOf);
    }

    static string ColumnDefsForSurfacePack(PresentationSurfacePack pack)
    {
        // Same main-grid suppress pattern as (F)(P/M): Forward owns main; P/M live on OneOf host.
        if (pack.Slots.Any(s => s.Role == PresentationScanRole.PmOneOf)
            && pack.Slots.Any(s => s.Role == PresentationScanRole.F))
            return "0,4,*,4,0";

        if (pack.Slots.Count == 3)
            return "0,4,*,4,0";

        return PresentationMainGridColumnDefinitions.Default;
    }

    /// <summary>Map surface/channel token → Glass zone for OneOf remount (scan paint, not identity).</summary>
    public static PresentationAnchorKind? ZoneForSurface(string? surface)
    {
        if (string.IsNullOrWhiteSpace(surface))
            return null;
        var s = surface.Trim().ToLowerInvariant();
        return s switch
        {
            "intercom" or "editor" or "work" or "f" or "forward" or "fwd" => PresentationAnchorKind.Forward,
            "sit" or "report" or "plan" or "p" or "pfd" or "alert" or "ecl" or "eicas" => PresentationAnchorKind.Pfd,
            "world" or "probe" or "shell" or "git" or "browser" or "mcp" or "m" or "mfd" => PresentationAnchorKind.Mfd,
            _ => null,
        };
    }
}

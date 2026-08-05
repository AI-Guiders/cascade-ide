#nullable enable
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.GlassCore.Presentation;

/// <summary>
/// Resolves presentation line → main-grid + topology flags.
/// Prefers surface wire; falls back to legacy P/F/M parser.
/// Operator review cabin = <see cref="OperatorReviewFlightTopology"/> — single TopLevel OneOf (no satellite host).
/// </summary>
public static class GlassPresentationLayout
{
    /// <summary>
    /// Sealed review / cabin-tour wire: all scan channels XOR in <b>one</b> TopLevel.
    /// Not <c>(intercom)(sit/world/alert)</c> (that is 2 windows: F dedicated + satellite OneOf host).
    /// </summary>
    public const string OperatorReviewFlightTopology = "(F/P/M)";

    public sealed record Snapshot(
        string Topology,
        string ColumnDefinitions,
        PresentationTopologyFlags Flags,
        bool ParseOk,
        string? ParseError,
        PresentationSurfacePack? SurfacePack = null);

    /// <summary>True when pack is a single TopLevel OneOf (no dedicated F + satellite host).</summary>
    public static bool IsSingleTopLevelOneOf(PresentationSurfacePack? pack) =>
        pack?.Slots is [{ Role: PresentationScanRole.PmOneOf }];

    /// <summary>True when flags open a satellite OneOf host (2+ physical windows).</summary>
    public static bool SpawnsSatelliteOneOfHost(PresentationTopologyFlags flags) =>
        flags.PmOneOfHostTopology || flags.OneOfHostTopology;

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
        // Single TopLevel OneOf — no satellite hosts; GlassHostWindows XOR-paints main columns.
        if (pack.Slots is [{ Role: PresentationScanRole.PmOneOf }])
            return default;

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
        // (P/F/M) / (sit/world/…) — one TopLevel: only the active scan column is wide.
        if (pack.Slots is [{ Role: PresentationScanRole.PmOneOf, Active: var active }])
            return ColumnDefsForScanOneOfActive(active);

        // Same main-grid suppress pattern as (F)(P/M): Forward owns main; P/M live on OneOf host.
        if (pack.Slots.Any(s => s.Role == PresentationScanRole.PmOneOf)
            && pack.Slots.Any(s => s.Role == PresentationScanRole.F))
            return "0,4,*,4,0";

        if (pack.Slots.Count == 3)
            return "0,4,*,4,0";

        return PresentationMainGridColumnDefinitions.Default;
    }

    /// <summary>Main-grid XOR widths for single-TopLevel scan OneOf (P|F|M active).</summary>
    public static string ColumnDefsForScanOneOfActive(string? activeSurface)
    {
        var zone = ZoneForSurface(activeSurface);
        return zone switch
        {
            PresentationAnchorKind.Pfd => "*,4,0,4,0",
            PresentationAnchorKind.Mfd => "0,4,0,4,*",
            _ => "0,4,*,4,0",
        };
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

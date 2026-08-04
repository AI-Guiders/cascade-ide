#nullable enable
namespace CascadeIDE.Services.Presentation;

/// <summary>
/// ADR 0193 attention channel — first-class packing identity for topology OneOf (v1).
/// P|F|M are <see cref="PresentationZoneMeta"/> only (paint/remount hints).
/// </summary>
public enum PresentationChannelId
{
    Sit,
    Work,
    Probe,
    Report,
    World,
    Alert,
}

/// <summary>
/// Legacy zone tag on a channel face — meta only, not window/OneOf identity.
/// </summary>
public enum PresentationZoneMeta
{
    P,
    F,
    M,
}

/// <summary>
/// Channel ↔ P|F|M meta map + OneOf prefer (topology-oneof-slash-v1).
/// </summary>
public static class PresentationChannelTopology
{
    public const string Sit = "sit";
    public const string Work = "work";
    public const string Probe = "probe";
    public const string Report = "report";
    public const string World = "world";
    public const string Alert = "alert";

    /// <summary>Meta token → default channel face (report→sit face, probe→world face).</summary>
    public static PresentationChannelId ChannelForMeta(PresentationZoneMeta meta) =>
        meta switch
        {
            PresentationZoneMeta.P => PresentationChannelId.Sit,
            PresentationZoneMeta.F => PresentationChannelId.Work,
            PresentationZoneMeta.M => PresentationChannelId.World,
            _ => throw new ArgumentOutOfRangeException(nameof(meta), meta, null),
        };

    /// <summary>Optional meta glyph for a channel face (alert has none).</summary>
    public static PresentationZoneMeta? MetaForChannel(PresentationChannelId channel) =>
        channel switch
        {
            PresentationChannelId.Sit or PresentationChannelId.Report => PresentationZoneMeta.P,
            PresentationChannelId.Work => PresentationZoneMeta.F,
            PresentationChannelId.Probe or PresentationChannelId.World => PresentationZoneMeta.M,
            PresentationChannelId.Alert => null,
            _ => null,
        };

    public static PresentationChannelId? TryParseChannel(string? id) =>
        id?.Trim().ToLowerInvariant() switch
        {
            Sit => PresentationChannelId.Sit,
            Work => PresentationChannelId.Work,
            Probe => PresentationChannelId.Probe,
            Report => PresentationChannelId.Report,
            World => PresentationChannelId.World,
            Alert => PresentationChannelId.Alert,
            _ => null,
        };

    /// <summary>Compat: legacy <see cref="PresentationAnchorKind"/> as meta tag.</summary>
    public static PresentationZoneMeta? MetaFromAnchor(PresentationAnchorKind kind) =>
        kind switch
        {
            PresentationAnchorKind.Pfd => PresentationZoneMeta.P,
            PresentationAnchorKind.Forward => PresentationZoneMeta.F,
            PresentationAnchorKind.Mfd => PresentationZoneMeta.M,
            _ => null,
        };

    public static PresentationAnchorKind? AnchorFromMeta(PresentationZoneMeta meta) =>
        meta switch
        {
            PresentationZoneMeta.P => PresentationAnchorKind.Pfd,
            PresentationZoneMeta.F => PresentationAnchorKind.Forward,
            PresentationZoneMeta.M => PresentationAnchorKind.Mfd,
            _ => null,
        };

    /// <summary>
    /// Prefer active channel when its face meta is in the OneOf meta set (compat wire still speaks P/F/M).
    /// Returns the meta to show — callers remount by meta; identity of the switch is the channel.
    /// </summary>
    public static PresentationZoneMeta? PreferMetaForChannel(
        PresentationChannelId channel,
        PresentationZoneMeta oneOfA,
        PresentationZoneMeta oneOfB)
    {
        if (MetaForChannel(channel) is not { } want)
            return null;
        if (want == oneOfA || want == oneOfB)
            return want;
        return null;
    }

    /// <summary>
    /// Describe 2-window packing in channel language from compat meta wire.
    /// Dedicated meta → dedicated channel; OneOf metas → OneOf channel faces.
    /// </summary>
    public static bool TryDescribeChannelPackFromMetaWire(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        IReadOnlyList<PresentationZoneCompose>? composes,
        out PresentationChannelId dedicatedChannel,
        out PresentationChannelId oneOfChannelA,
        out PresentationChannelId oneOfChannelB,
        out PresentationZoneMeta dedicatedMeta,
        out PresentationZoneMeta oneOfMetaA,
        out PresentationZoneMeta oneOfMetaB,
        out int dedicatedScreen,
        out int oneOfScreen)
    {
        dedicatedChannel = default;
        oneOfChannelA = default;
        oneOfChannelB = default;
        dedicatedMeta = default;
        oneOfMetaA = default;
        oneOfMetaB = default;
        dedicatedScreen = -1;
        oneOfScreen = -1;

        if (!PresentationLayoutAnalyzer.TryDescribeOneOfPlusDedicatedTwoScreen(
                screens,
                composes,
                out var dedAnchor,
                out var aAnchor,
                out var bAnchor,
                out dedicatedScreen,
                out oneOfScreen))
            return false;

        if (MetaFromAnchor(dedAnchor) is not { } dMeta
            || MetaFromAnchor(aAnchor) is not { } aMeta
            || MetaFromAnchor(bAnchor) is not { } bMeta)
            return false;

        dedicatedMeta = dMeta;
        oneOfMetaA = aMeta;
        oneOfMetaB = bMeta;
        dedicatedChannel = ChannelForMeta(dMeta);
        oneOfChannelA = ChannelForMeta(aMeta);
        oneOfChannelB = ChannelForMeta(bMeta);
        return true;
    }
}

/// <summary>Obsolete name — use <see cref="PresentationChannelTopology"/>.</summary>
[Obsolete("Use PresentationChannelTopology — channels first, P/F/M are meta.")]
public static class PresentationOneOfChannelPolicy
{
    public const string Sit = PresentationChannelTopology.Sit;
    public const string Work = PresentationChannelTopology.Work;
    public const string Probe = PresentationChannelTopology.Probe;
    public const string Report = PresentationChannelTopology.Report;
    public const string World = PresentationChannelTopology.World;
    public const string Alert = PresentationChannelTopology.Alert;

    public static PresentationAnchorKind? AnchorForChannel(string? channel) =>
        PresentationChannelTopology.TryParseChannel(channel) is { } id
        && PresentationChannelTopology.MetaForChannel(id) is { } meta
            ? PresentationChannelTopology.AnchorFromMeta(meta)
            : null;

    public static PresentationAnchorKind? PreferOneOfForChannel(
        string? channel,
        PresentationAnchorKind oneOfA,
        PresentationAnchorKind oneOfB)
    {
        if (PresentationChannelTopology.TryParseChannel(channel) is not { } id)
            return null;
        if (PresentationChannelTopology.MetaFromAnchor(oneOfA) is not { } a
            || PresentationChannelTopology.MetaFromAnchor(oneOfB) is not { } b)
            return null;
        return PresentationChannelTopology.PreferMetaForChannel(id, a, b) is { } meta
            ? PresentationChannelTopology.AnchorFromMeta(meta)
            : null;
    }
}

#nullable enable
namespace CascadeIDE.Services.Presentation;

/// <summary>
/// ADR 0193 attention channel → presentation anchor (projection, not identity).
/// Used by OneOf auto-switch when topology packs channels onto 2|3 windows (topology-oneof-slash-v1).
/// </summary>
public static class PresentationOneOfChannelPolicy
{
    public const string Sit = "sit";
    public const string Work = "work";
    public const string Probe = "probe";
    public const string Report = "report";
    public const string World = "world";
    public const string Alert = "alert";

    /// <summary>
    /// Default face anchor for a channel. Alert stays chrome (null = do not PreferOneOf).
    /// </summary>
    public static PresentationAnchorKind? AnchorForChannel(string? channel) =>
        channel?.Trim().ToLowerInvariant() switch
        {
            Sit or Report => PresentationAnchorKind.Pfd,
            Work => PresentationAnchorKind.Forward,
            Probe or World => PresentationAnchorKind.Mfd,
            Alert => null,
            _ => null,
        };

    /// <summary>
    /// Prefer OneOf member when the channel's face is in the OneOf set; else null (dedicated already visible or chrome).
    /// </summary>
    public static PresentationAnchorKind? PreferOneOfForChannel(
        string? channel,
        PresentationAnchorKind oneOfA,
        PresentationAnchorKind oneOfB)
    {
        if (AnchorForChannel(channel) is not { } want)
            return null;
        if (want == oneOfA || want == oneOfB)
            return want;
        return null;
    }
}

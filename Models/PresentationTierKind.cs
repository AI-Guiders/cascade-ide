namespace CascadeIDE.Models;

/// <summary>Presentation tier (ADR 0171): compact = standard IDE; cockpit = P/F/M spatial layout.</summary>
public enum PresentationTierKind
{
    Compact,
    Cockpit,
}

public static class PresentationTierKindExtensions
{
    public const string AutoValue = "auto";
    public const string CompactValue = "compact";
    public const string CockpitValue = "cockpit";

    public static string ToTomlValue(this PresentationTierKind kind) =>
        kind == PresentationTierKind.Cockpit ? CockpitValue : CompactValue;

    public static PresentationTierKind ParseTomlValue(string? raw) =>
        string.Equals(raw, CockpitValue, StringComparison.OrdinalIgnoreCase)
            ? PresentationTierKind.Cockpit
            : PresentationTierKind.Compact;
}

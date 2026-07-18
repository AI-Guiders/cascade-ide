namespace CascadeIDE.Models;

/// <summary>Presentation tier policy (ADR 0171). TOML: <c>[display.presentation]</c>.</summary>
public sealed class DisplayPresentationSettings
{
    /// <summary><c>auto</c> | <c>compact</c> | <c>cockpit</c>.</summary>
    public string Tier { get; set; } = PresentationTierKindExtensions.AutoValue;

    /// <summary>Auto → cockpit on single ultrawide when total width ≥ this (px).</summary>
    public int CockpitMinTotalWidthPx { get; set; } = 4800;

    /// <summary>Minimum width per logical anchor on ultrawide (px).</summary>
    public int CockpitMinAnchorWidthPx { get; set; } = 1280;

    /// <summary>Compact: Intercom in right auxiliary column (<c>side</c>) or bottom dock (<c>bottom</c>).</summary>
    public string CompactIntercomPlacement { get; set; } = "side";

    /// <summary>Allow cockpit tier on single ultrawide when auto/heuristic matches.</summary>
    public bool UltrawideCockpitEnabled { get; set; } = true;

    /// <summary>First-run tier wizard completed.</summary>
    public bool TierFirstRunCompleted { get; set; }

    /// <summary>Compact auxiliary panel width (px).</summary>
    public int CompactAuxiliaryPanelWidthPx { get; set; } = 380;

    /// <summary>Compact IDE-scan: MFD bottom dock height when terminal/build/git visible (px).</summary>
    public int CompactMfdBottomDockHeightPx { get; set; } = 220;

    /// <summary>Compact <c>bottom</c> placement: Intercom bottom dock height when chat visible (px).</summary>
    public int CompactIntercomBottomDockHeightPx { get; set; } = 280;
}

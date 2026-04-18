namespace CascadeIDE.Models;

/// <summary>Декларативный mount-style и правила (Wave 3). TOML: <c>[display.mount]</c>, <c>[[display.mount.rules]]</c>.</summary>
public sealed class DisplayMountSettings
{
    public string DefaultStyle { get; set; } = InstrumentMountPolicyIds.V1;

    public List<InstrumentMountPolicyRuleSettings> Rules { get; set; } = [];

    public bool EnforceEligibility { get; set; }

    public double MinSa { get; set; } = 0.6;

    public double MinPerformance { get; set; } = 0.6;

    public double MaxWorkload { get; set; } = 0.5;

    public bool RequireScores { get; set; }
}

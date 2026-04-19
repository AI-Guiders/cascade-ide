namespace CascadeIDE.Models;

/// <summary>
/// Правило резолва mount-style для mount-слоя инструмента.
/// TOML: <c>[[display.mount.rules]]</c>.
/// </summary>
public sealed class InstrumentMountPolicyRuleSettings
{
    public string Surface { get; set; } = "*";

    public string Slot { get; set; } = "*";

    public string Instrument { get; set; } = "*";

    public string Style { get; set; } = InstrumentMountPolicyIds.V1;

    public double? SaScore { get; set; }

    public double? PerformanceScore { get; set; }

    public double? WorkloadScore { get; set; }
}

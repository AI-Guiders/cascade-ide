namespace CascadeIDE.Models;

/// <summary>
/// Правило резолва slot-policy для mount-слоя инструмента.
/// В <c>settings.toml</c>: <c>[[display.instrument_mount_policy_rules]]</c>.
/// </summary>
public sealed class InstrumentMountPolicyRuleSettings
{
    /// <summary>Целевой slot-id (например: <c>pfd</c>, <c>forward</c>, <c>mfd</c> или <c>*</c>).</summary>
    public string SlotId { get; set; } = "*";

    /// <summary>Целевой instrument-id (например: <c>workspace_health_status_v1</c> или <c>*</c>).</summary>
    public string InstrumentId { get; set; } = "*";

    /// <summary>Policy-id, который должен быть применён для пары <c>slot + instrument</c>.</summary>
    public string SlotPolicy { get; set; } = "wave3_preview_v1";
}

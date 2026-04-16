using CascadeIDE.Models;

namespace CascadeIDE.Services;

internal interface ISettingsValidationSpecification
{
    IEnumerable<string> Validate(CascadeIdeSettings settings);
}

internal sealed class DisplaySettingsValidationSpecification : ISettingsValidationSpecification
{
    public IEnumerable<string> Validate(CascadeIdeSettings settings)
    {
        var display = settings.Display;

        if (!IsScore(display.InstrumentMountPolicyMinSaScore))
            yield return "[display] instrument_mount_policy_min_sa_score must be in [0..1].";
        if (!IsScore(display.InstrumentMountPolicyMinPerformanceScore))
            yield return "[display] instrument_mount_policy_min_performance_score must be in [0..1].";
        if (!IsScore(display.InstrumentMountPolicyMaxWorkloadScore))
            yield return "[display] instrument_mount_policy_max_workload_score must be in [0..1].";

        for (var i = 0; i < display.InstrumentMountPolicyRules.Count; i++)
        {
            var rule = display.InstrumentMountPolicyRules[i];
            if (string.IsNullOrWhiteSpace(rule.MountStyle))
                yield return $"[display.instrument_mount_policy_rules][{i}] mount_style is required.";
            if (rule.SaScore is { } sa && !IsScore(sa))
                yield return $"[display.instrument_mount_policy_rules][{i}] sa_score must be in [0..1].";
            if (rule.PerformanceScore is { } perf && !IsScore(perf))
                yield return $"[display.instrument_mount_policy_rules][{i}] performance_score must be in [0..1].";
            if (rule.WorkloadScore is { } workload && !IsScore(workload))
                yield return $"[display.instrument_mount_policy_rules][{i}] workload_score must be in [0..1].";
        }

        for (var i = 0; i < display.InstrumentPlacementRules.Count; i++)
        {
            var rule = display.InstrumentPlacementRules[i];
            if (string.IsNullOrWhiteSpace(rule.SurfaceId))
                yield return $"[display.instrument_placement_rules][{i}] surface_id is required.";
            if (string.IsNullOrWhiteSpace(rule.SlotId))
                yield return $"[display.instrument_placement_rules][{i}] slot_id is required.";
            if (string.IsNullOrWhiteSpace(rule.InstrumentId))
                yield return $"[display.instrument_placement_rules][{i}] instrument_id is required.";
        }
    }

    private static bool IsScore(double value) => value is >= 0 and <= 1;
}

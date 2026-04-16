using CascadeIDE.Cockpit.Composition.HostSurface;
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

        if (display.InstrumentRouting is { Count: > 0 } routing)
        {
            foreach (var kv in routing)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                {
                    yield return "[display.instrument_routing] key must not be empty.";
                    continue;
                }

                var k = kv.Key.Trim();
                if (!k.Equals(InstrumentRoutingSlotKeys.PfdPrimary, StringComparison.OrdinalIgnoreCase)
                    && !k.Equals(InstrumentRoutingSlotKeys.MfdPrimary, StringComparison.OrdinalIgnoreCase))
                {
                    yield return $"[display.instrument_routing] unknown key '{k}' (expected {InstrumentRoutingSlotKeys.PfdPrimary} or {InstrumentRoutingSlotKeys.MfdPrimary}).";
                }

                if (string.IsNullOrWhiteSpace(kv.Value))
                    yield return $"[display.instrument_routing] value for '{k}' is required.";
                else if (!InstrumentRoutingAliasResolver.TryResolve(kv.Value, out _))
                    yield return $"[display.instrument_routing] unknown instrument alias or id for '{k}'.";
            }
        }
    }

    private static bool IsScore(double value) => value is >= 0 and <= 1;
}

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

        var im = display.Mount;
        if (!IsScore(im.MinSa))
            yield return "[display.mount] min_sa must be in [0..1].";
        if (!IsScore(im.MinPerformance))
            yield return "[display.mount] min_performance must be in [0..1].";
        if (!IsScore(im.MaxWorkload))
            yield return "[display.mount] max_workload must be in [0..1].";

        for (var i = 0; i < im.Rules.Count; i++)
        {
            var rule = im.Rules[i];
            if (string.IsNullOrWhiteSpace(rule.Style))
                yield return $"[display.mount.rules][{i}] style is required.";
            if (rule.SaScore is { } sa && !IsScore(sa))
                yield return $"[display.mount.rules][{i}] sa_score must be in [0..1].";
            if (rule.PerformanceScore is { } perf && !IsScore(perf))
                yield return $"[display.mount.rules][{i}] performance_score must be in [0..1].";
            if (rule.WorkloadScore is { } workload && !IsScore(workload))
                yield return $"[display.mount.rules][{i}] workload_score must be in [0..1].";
        }

        if (display.Instruments is { Count: > 0 } routing)
        {
            foreach (var kv in routing)
            {
                if (string.IsNullOrWhiteSpace(kv.Key))
                {
                    yield return "[display.instruments] key must not be empty.";
                    continue;
                }

                var k = kv.Key.Trim();
                var isPrimary = k.Equals(InstrumentRoutingSlotKeys.PfdPrimary, StringComparison.OrdinalIgnoreCase)
                    || k.Equals(InstrumentRoutingSlotKeys.MfdPrimary, StringComparison.OrdinalIgnoreCase);
                var isStrip = InstrumentRoutingSlotKeys.IsStatusStripKey(k);

                if (!isPrimary && !isStrip)
                {
                    yield return $"[display.instruments] unknown key '{k}' (expected {InstrumentRoutingSlotKeys.PfdPrimary}, {InstrumentRoutingSlotKeys.MfdPrimary}, {InstrumentRoutingSlotKeys.PfdStatusStrip}, or {InstrumentRoutingSlotKeys.ForwardStatusStrip}).";
                }

                if (string.IsNullOrWhiteSpace(kv.Value))
                    yield return $"[display.instruments] value for '{k}' is required.";
                else if (isStrip)
                {
                    if (!InstrumentStatusStripRouting.TryParse(kv.Value, out var showStrip, out _))
                        yield return $"[display.instruments] unknown value for '{k}' (expected {InstrumentStatusStripRouting.None} or background_status).";
                }
                else if (!InstrumentRoutingAliasResolver.TryResolve(kv.Value, out _))
                    yield return $"[display.instruments] unknown instrument alias or id for '{k}'.";
            }
        }
    }

    private static bool IsScore(double value) => value is >= 0 and <= 1;
}

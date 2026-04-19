using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

internal interface IInstrumentMountPolicyEligibilitySpecification
{
    bool IsSatisfiedBy(InstrumentMountPolicyRuleSettings rule, DisplaySettings displaySettings);
}

internal sealed class RolloutMetricsEligibilitySpecification : IInstrumentMountPolicyEligibilitySpecification
{
    public bool IsSatisfiedBy(InstrumentMountPolicyRuleSettings rule, DisplaySettings displaySettings)
    {
        var m = displaySettings.Mount;
        if (!m.EnforceEligibility)
            return true;

        if (m.RequireScores
            && (rule.SaScore is null || rule.PerformanceScore is null || rule.WorkloadScore is null))
            return false;

        if (rule.SaScore is { } sa && sa < m.MinSa)
            return false;
        if (rule.PerformanceScore is { } perf && perf < m.MinPerformance)
            return false;
        if (rule.WorkloadScore is { } workload && workload > m.MaxWorkload)
            return false;

        return true;
    }
}

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
        if (!displaySettings.EnforceInstrumentMountPolicyEligibility)
            return true;

        if (displaySettings.RequireInstrumentMountPolicyScores
            && (rule.SaScore is null || rule.PerformanceScore is null || rule.WorkloadScore is null))
            return false;

        if (rule.SaScore is { } sa && sa < displaySettings.InstrumentMountPolicyMinSaScore)
            return false;
        if (rule.PerformanceScore is { } perf && perf < displaySettings.InstrumentMountPolicyMinPerformanceScore)
            return false;
        if (rule.WorkloadScore is { } workload && workload > displaySettings.InstrumentMountPolicyMaxWorkloadScore)
            return false;

        return true;
    }
}

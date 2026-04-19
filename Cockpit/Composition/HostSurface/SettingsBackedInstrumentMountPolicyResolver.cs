using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Settings-backed strategy for mount mount-style resolution.
/// Priority order:
/// 1) exact current surface (slot+instrument -> slot/* -> */instrument -> */*)
/// 2) global surface wildcard with the same specificity ladder.
/// </summary>
public sealed class SettingsBackedInstrumentMountPolicyResolver : IInstrumentMountPolicyResolver
{
    private static readonly IInstrumentMountPolicyRuleSpecification RuleMatches = new InstrumentMountPolicyRuleMatchesSpecification();
    private static readonly IInstrumentMountPolicyEligibilitySpecification RuleEligibility = new RolloutMetricsEligibilitySpecification();

    public string Resolve(
        DisplaySettings displaySettings,
        string surfaceId,
        string slotId,
        string instrumentId)
    {
        static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();

        var mount = displaySettings.Mount;
        var normalizedDefault = string.IsNullOrWhiteSpace(mount.DefaultStyle)
            ? InstrumentMountPolicyIds.V1
            : mount.DefaultStyle.Trim();
        var rules = mount.Rules;
        if (rules is null || rules.Count == 0)
            return normalizedDefault;

        var normalizedSurface = Normalize(surfaceId);
        var normalizedSlot = Normalize(slotId);
        var normalizedInstrument = Normalize(instrumentId);

        string Pick(bool surfaceExact, bool slotExact, bool instrumentExact)
        {
            var context = new InstrumentMountPolicyMatchContext(
                normalizedSurface,
                normalizedSlot,
                normalizedInstrument,
                surfaceExact,
                slotExact,
                instrumentExact);
            var match = rules.FirstOrDefault(rule =>
                RuleMatches.IsSatisfiedBy(rule, in context)
                && RuleEligibility.IsSatisfiedBy(rule, displaySettings));
            return string.IsNullOrWhiteSpace(match?.Style) ? string.Empty : match.Style.Trim();
        }

        if (TryResolveForSurface(surfaceExact: true, out var surfaceSpecific))
            return surfaceSpecific;

        if (TryResolveForSurface(surfaceExact: false, out var globalFallback))
            return globalFallback;

        return normalizedDefault;

        bool TryResolveForSurface(bool surfaceExact, out string resolvedPolicy)
        {
            resolvedPolicy = Pick(surfaceExact, slotExact: true, instrumentExact: true);
            if (!string.IsNullOrWhiteSpace(resolvedPolicy))
                return true;

            resolvedPolicy = Pick(surfaceExact, slotExact: true, instrumentExact: false);
            if (!string.IsNullOrWhiteSpace(resolvedPolicy))
                return true;

            resolvedPolicy = Pick(surfaceExact, slotExact: false, instrumentExact: true);
            if (!string.IsNullOrWhiteSpace(resolvedPolicy))
                return true;

            resolvedPolicy = Pick(surfaceExact, slotExact: false, instrumentExact: false);
            return !string.IsNullOrWhiteSpace(resolvedPolicy);
        }
    }
}

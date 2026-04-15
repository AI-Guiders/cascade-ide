using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

internal readonly record struct InstrumentMountPolicyMatchContext(
    string SurfaceId,
    string SlotId,
    string InstrumentId,
    bool SurfaceExact,
    bool SlotExact,
    bool InstrumentExact);

internal interface IInstrumentMountPolicyRuleSpecification
{
    bool IsSatisfiedBy(InstrumentMountPolicyRuleSettings rule, in InstrumentMountPolicyMatchContext context);
}

internal sealed class InstrumentMountPolicyRuleMatchesSpecification : IInstrumentMountPolicyRuleSpecification
{
    public bool IsSatisfiedBy(InstrumentMountPolicyRuleSettings rule, in InstrumentMountPolicyMatchContext context)
    {
        var ruleSurface = Normalize(rule.SurfaceId);
        var ruleSlot = Normalize(rule.SlotId);
        var ruleInstrument = Normalize(rule.InstrumentId);

        var surfaceMatches = context.SurfaceExact
            ? ruleSurface == context.SurfaceId
            : IsWildcard(ruleSurface);
        var slotMatches = context.SlotExact
            ? ruleSlot == context.SlotId
            : IsWildcard(ruleSlot);
        var instrumentMatches = context.InstrumentExact
            ? ruleInstrument == context.InstrumentId
            : IsWildcard(ruleInstrument);

        return surfaceMatches && slotMatches && instrumentMatches;
    }

    private static bool IsWildcard(string value) => value is "*" or "";

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();
}

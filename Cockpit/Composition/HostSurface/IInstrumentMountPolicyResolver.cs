using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Strategy for resolving mount slot-policy by runtime surface context and instrument placement.
/// </summary>
public interface IInstrumentMountPolicyResolver
{
    string Resolve(
        IReadOnlyList<InstrumentMountPolicyRuleSettings>? rules,
        string defaultSlotPolicy,
        string surfaceId,
        string slotId,
        string instrumentId);
}

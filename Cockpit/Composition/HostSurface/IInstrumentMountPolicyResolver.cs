using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Strategy for resolving mount mount-style by runtime surface context and instrument placement.
/// </summary>
public interface IInstrumentMountPolicyResolver
{
    string Resolve(
        DisplaySettings displaySettings,
        string surfaceId,
        string slotId,
        string instrumentId);
}

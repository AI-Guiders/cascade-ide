using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Сборка <see cref="IdeHealthStatusMountContext"/> с тем же <paramref name="surfaceId"/>, что и кадр хоста / CDS.
/// </summary>
public static class IdeHealthMountContextFactory
{
    public static IdeHealthStatusMountContext Create(
        IInstrumentMountPolicyResolver resolver,
        DisplaySettings displaySettings,
        string surfaceId,
        string slotId,
        IdeHealthStatusMountPayload payload)
    {
        var instrumentId = CockpitStandardInstrumentIds.IdeHealthStatusV1;
        var style = resolver.Resolve(displaySettings, surfaceId, slotId, instrumentId);
        return new IdeHealthStatusMountContext(instrumentId, slotId, style, payload);
    }
}

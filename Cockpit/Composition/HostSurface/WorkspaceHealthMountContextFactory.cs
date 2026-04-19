using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Сборка <see cref="WorkspaceHealthStatusMountContext"/> с тем же <paramref name="surfaceId"/>, что и кадр хоста / CDS.
/// </summary>
public static class WorkspaceHealthMountContextFactory
{
    public static WorkspaceHealthStatusMountContext Create(
        IInstrumentMountPolicyResolver resolver,
        DisplaySettings displaySettings,
        string surfaceId,
        string slotId,
        WorkspaceHealthStatusMountPayload payload)
    {
        var instrumentId = CockpitStandardInstrumentIds.WorkspaceHealthStatusV1;
        var style = resolver.Resolve(displaySettings, surfaceId, slotId, instrumentId);
        return new WorkspaceHealthStatusMountContext(instrumentId, slotId, style, payload);
    }
}

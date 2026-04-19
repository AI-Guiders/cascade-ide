namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Единый контракт для mount-layer Wave 3: стабильные id слота/инструмента, резолв style и типизированный payload.
/// Передаётся в <see cref="CascadeIDE.Views.ZoneInstrumentMountView"/> одним свойством вместо разрозненных привязок.
/// </summary>
public sealed record WorkspaceHealthStatusMountContext(
    string InstrumentId,
    string SlotId,
    string MountStyle,
    WorkspaceHealthStatusMountPayload Payload);

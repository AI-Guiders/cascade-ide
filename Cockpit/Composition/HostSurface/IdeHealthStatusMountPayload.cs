namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Типизированный снимок для mount-инструмента <see cref="CockpitStandardInstrumentIds.IdeHealthStatusV1"/>:
/// короткие строки канала Workspace Health + уровень безопасности (без привязки к конкретным свойствам VM).
/// </summary>
public sealed record IdeHealthStatusMountPayload(
    string BuildCockpitShort,
    string TestsCockpitShort,
    string DebugCockpitShort,
    string SafetyLevel);

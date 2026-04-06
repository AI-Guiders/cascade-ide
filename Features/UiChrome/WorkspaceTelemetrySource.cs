namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Источник строки в единой полосе телеметрии воркспейса (ADR 0021 / ARINC 661-идея).
/// </summary>
public enum WorkspaceTelemetrySource
{
    Build,
    Tests,
    Debug,
    Git,
}

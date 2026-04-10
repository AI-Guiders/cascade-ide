namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Источник строки в единой полосе Workspace Health (ADR 0021 / ARINC 661-идея).
/// </summary>
public enum WorkspaceHealthSource
{
    Build,
    Tests,
    Debug,
    Git,
}

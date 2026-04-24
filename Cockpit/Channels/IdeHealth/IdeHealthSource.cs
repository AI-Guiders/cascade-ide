namespace CascadeIDE.Cockpit.Channels.WorkspaceHealth;

/// <summary>
/// Источник строки в единой полосе Workspace Health (ADR 0021 / ARINC 661-идея).
/// </summary>
public enum IdeHealthSource
{
    Build,
    Tests,
    Debug,
    Git,
}

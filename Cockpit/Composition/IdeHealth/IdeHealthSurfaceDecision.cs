#nullable enable

namespace CascadeIDE.Cockpit.Composition.WorkspaceHealth;

/// <summary>
/// Routing decision for Workspace Health surface composition.
/// </summary>
public readonly record struct IdeHealthSurfaceDecision(bool Enabled = true);

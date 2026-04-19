#nullable enable

namespace CascadeIDE.Cockpit.Composition.EnvironmentReadiness;

/// <summary>
/// Routing decision for Environment Readiness surface composition.
/// </summary>
public readonly record struct EnvironmentReadinessSurfaceDecision(bool Enabled = true);

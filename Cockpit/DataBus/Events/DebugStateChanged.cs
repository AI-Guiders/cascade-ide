using CascadeIDE.Services;

namespace CascadeIDE.Cockpit.DataBus;

/// <summary>Canonical debug session snapshot for IdeHealth and other projections (ADR 0002, ADR 0099).</summary>
public readonly record struct DebugStateChanged(DebugSessionSnapshot Snapshot);

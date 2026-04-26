namespace CascadeIDE.Cockpit.DataBus;

/// <summary>Tests pipeline state for UI/CCU projections.</summary>
public readonly record struct TestsStateChanged(string Summary, int ImpactedBadge);

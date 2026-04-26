namespace CascadeIDE.Cockpit.DataBus;

/// <summary>Build pipeline state for UI/CCU projections. Конец прогона: <see cref="IsBuilding"/> = false и при необходимости <see cref="LastExitCode"/> / <see cref="LastBuildSucceeded"/>.</summary>
public readonly record struct BuildStateChanged(
    bool IsBuilding,
    int? LastExitCode = null,
    bool? LastBuildSucceeded = null);

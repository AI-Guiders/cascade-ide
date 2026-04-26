namespace CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

/// <summary>
/// Каноничный снимок состояния сборки для CCU/каналов (ADR 0097): свёртка <see cref="CascadeIDE.Cockpit.DataBus.BuildStateChanged"/>,
/// богаче, чем один bool — по мере сигналов из домена.
/// </summary>
public readonly record struct BuildStateSnapshot(
    bool IsBuilding,
    int? LastExitCode = null,
    bool? LastBuildSucceeded = null)
{
    public static BuildStateSnapshot Empty { get; } = new(false, null, null);
}

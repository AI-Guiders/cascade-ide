using CascadeIDE.Cockpit.Cds;

namespace CascadeIDE.ViewModels;

/// <summary>Сборка <see cref="CockpitSurfaceState"/> главного окна (<see cref="CockpitSurfaceSnapshotBuilder.Build"/>).</summary>
public partial class MainWindowViewModel
{
    /// <summary>CDS-снимок кабины (ADR 0036 п.2; см. <c>docs/design/cds-contract-v0.md</c>).</summary>
    public CockpitSurfaceState BuildCockpitSurfaceSnapshot() => CockpitSurfaceSnapshotBuilder.Build(this);
}

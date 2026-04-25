#nullable enable
using System.Collections.ObjectModel;
using CascadeIDE.Cockpit.Channels.WorkspaceHealth;
using CascadeIDE.Cockpit.ComputingUnits;
using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

namespace CascadeIDE.Cockpit.Composition.WorkspaceHealth;

/// <summary>
/// Контракт композитора полосы IDE Health (ADR 0089): снимок → сегменты для канала. CCU (ADR 0097) — <see cref="IdeHealthSurfaceCompositor"/>.
/// </summary>
public interface IIdeHealthSurfaceCompositor
    : ISurfaceCompositor<ObservableCollection<IdeHealthSegment>, IdeHealthInputSnapshot, IdeHealthSurfaceDecision, ObservableCollection<IdeHealthSegment>>,
        ICockpitComputeUnit
{
}

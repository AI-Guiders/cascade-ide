#nullable enable
using CascadeIDE.Cockpit.ComputingUnits;
using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

namespace CascadeIDE.Cockpit.Channels.WorkspaceHealth;

/// <summary>
/// Контракт канала IDE Health (ADR 0089): снимок <see cref="IdeHealthInputSnapshot"/> на тик.
/// Канал объявлен как <see cref="ICockpitComputeUnit"/> (ADR 0097) — реализация <see cref="IdeHealthSnapshotUnit"/>.
/// </summary>
public interface IIdeHealthChannel : IChannel<IdeHealthChannelContext, IdeHealthInputSnapshot>, ICockpitComputeUnit
{
}

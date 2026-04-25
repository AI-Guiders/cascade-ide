using CascadeIDE.Cockpit.Channels.Eicas;
using CascadeIDE.Cockpit.Channels.WorkspaceHealth;

namespace CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

/// <summary>
/// Входные строки одного сегмента до маппинга в <see cref="IdeHealthSegment"/>.
/// <see cref="IsBuildRunning"/> учитывается только для источника <see cref="IdeHealthSource.Build"/>.
/// </summary>
public readonly record struct IdeHealthSegmentInput(
    string LineText,
    string CockpitShort,
    bool IsBuildRunning = false,
    IdeHealthStratum Stratum = IdeHealthStratum.Solution,
    IdeHealthScope Scope = IdeHealthScope.Solution,
    string? ProjectPath = null);

/// <summary>
/// Снимок четырёх источников <strong>IDE Health</strong> (ADR 0089; build → tests → debug → git).
/// Реализует <see cref="ICockpitComputeUnitPayload"/>: нормализованная полезная нагрузка на границе CCU (ADR 0097) — до композитора/полос; без DAP; git как скаляры в <see cref="IdeHealthSegmentInput"/> (строки подставляет <see cref="IdeHealthSnapshotUnit"/>).
/// Слой канала (ADR 0036 п.1) → <see cref="IdeHealthFormattingUnit"/> (текст), затем
/// <see cref="Composition.WorkspaceHealth.IdeHealthSurfaceCompositor"/> (порядок сегментов, ADR 0036 п.3). EICAS — иной контур (<see cref="IEicasFeed"/>).
/// </summary>
public readonly record struct IdeHealthInputSnapshot(
    IdeHealthSegmentInput Build,
    IdeHealthSegmentInput Tests,
    IdeHealthSegmentInput Debug,
    IdeHealthSegmentInput Git) : ICockpitComputeUnitPayload;

using CascadeIDE.Cockpit.Channels.Eicas;

namespace CascadeIDE.Cockpit.Channels.WorkspaceHealth;

/// <summary>
/// Входные строки одного сегмента до маппинга в <see cref="WorkspaceHealthSegment"/>.
/// <see cref="IsBuildRunning"/> учитывается только для источника <see cref="WorkspaceHealthSource.Build"/>.
/// </summary>
public readonly record struct WorkspaceHealthSegmentInput(
    string LineText,
    string CockpitShort,
    bool IsBuildRunning = false);

/// <summary>
/// Снимок четырёх источников Workspace Health (build → tests → debug → git).
/// Слой <strong>канала</strong> (ADR 0036 п.1): нормализованные входы до композитора; без привязки к контролам.
/// Подаётся в <see cref="WorkspaceHealthSurfaceCompositor"/> (композитор полосы, ADR 0036 п.3). Канал EICAS — отдельный контур (<see cref="IEicasFeed"/>).
/// </summary>
public readonly record struct WorkspaceHealthInputSnapshot(
    WorkspaceHealthSegmentInput Build,
    WorkspaceHealthSegmentInput Tests,
    WorkspaceHealthSegmentInput Debug,
    WorkspaceHealthSegmentInput Git);

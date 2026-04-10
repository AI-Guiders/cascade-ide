namespace CascadeIDE.Features.UiChrome;

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
/// Единая структура для <see cref="WorkspaceHealthCompositor"/> и тестов. Канал EICAS — отдельный контур (<see cref="IEicasFeed"/>), не снимок здесь и не тот же контур, что сегменты build/tests/debug/git.
/// </summary>
public readonly record struct WorkspaceHealthInputSnapshot(
    WorkspaceHealthSegmentInput Build,
    WorkspaceHealthSegmentInput Tests,
    WorkspaceHealthSegmentInput Debug,
    WorkspaceHealthSegmentInput Git);

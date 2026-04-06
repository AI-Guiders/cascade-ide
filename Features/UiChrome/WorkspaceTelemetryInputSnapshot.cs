namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Входные строки одного сегмента до маппинга в <see cref="WorkspaceTelemetrySegment"/>.
/// <see cref="IsBuildRunning"/> учитывается только для источника <see cref="WorkspaceTelemetrySource.Build"/>.
/// </summary>
public readonly record struct WorkspaceTelemetrySegmentInput(
    string LineText,
    string CockpitShort,
    bool IsBuildRunning = false);

/// <summary>
/// Снимок четырёх источников телеметрии воркспейса (build → tests → debug → git).
/// Единая структура для <see cref="WorkspaceTelemetryCompositor"/> и тестов. Канал EICAS — отдельный контур (<see cref="IEicasFeed"/>), не снимок здесь и не тот же контур, что сегменты build/tests/debug/git.
/// </summary>
public readonly record struct WorkspaceTelemetryInputSnapshot(
    WorkspaceTelemetrySegmentInput Build,
    WorkspaceTelemetrySegmentInput Tests,
    WorkspaceTelemetrySegmentInput Debug,
    WorkspaceTelemetrySegmentInput Git);

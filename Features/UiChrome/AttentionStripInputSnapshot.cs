namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Входные строки одного сегмента до маппинга в <see cref="AttentionStripSegment"/>.
/// <see cref="IsBuildRunning"/> учитывается только для источника <see cref="AttentionStripSource.Build"/>.
/// </summary>
public readonly record struct AttentionStripSegmentInput(
    string LineText,
    string CockpitShort,
    bool IsBuildRunning = false);

/// <summary>
/// Снимок четырёх источников полосы телеметрии (build → tests → debug → git).
/// Единая структура для <see cref="AttentionStripCompositor"/> и тестов; дальше сюда же лягут новые каналы (EICAS, агент) без раздувания сигнатур.
/// </summary>
public readonly record struct AttentionStripInputSnapshot(
    AttentionStripSegmentInput Build,
    AttentionStripSegmentInput Tests,
    AttentionStripSegmentInput Debug,
    AttentionStripSegmentInput Git);

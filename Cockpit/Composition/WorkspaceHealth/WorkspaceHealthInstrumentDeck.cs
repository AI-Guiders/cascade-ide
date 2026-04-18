namespace CascadeIDE.Cockpit.Composition.WorkspaceHealth;

/// <summary>
/// Именованная колода канала Workspace Health на оси композиции (ADR 0063): порядок логических сегментов build → tests → debug → git,
/// совпадает с <see cref="WorkspaceHealthSurfaceCompositor"/> и не зависит от <see cref="ContentRepresentation"/> (Strip/Page).
/// </summary>
public static class WorkspaceHealthInstrumentDeck
{
    /// <summary>Стабильный id колоды для тестов и трассировки (не ключ пользовательского TOML).</summary>
    public const string DeckId = "workspace_health_channel_v1";

    /// <summary>Семантический контекст якоря для канала WH (нижний хром / mount), не слот Pfd/Mfd целиком.</summary>
    public const string SemanticAnchorId = "workspace_health";

    /// <summary>Логические ячейки сегментов в порядке композитора (согласованы с <see cref="WorkspaceHealthSurfaceCompositor.Compose"/>).</summary>
    public static readonly IReadOnlyList<string> OrderedSegmentIds = new[]
    {
        WorkspaceHealthSegmentIds.Build,
        WorkspaceHealthSegmentIds.Tests,
        WorkspaceHealthSegmentIds.Debug,
        WorkspaceHealthSegmentIds.Git,
    };

    /// <summary>Каноническое описание колоды WH: горизонтальная композиция сегментов (полоса или страница — отдельная ось).</summary>
    public static InstrumentDeckDescriptor Default { get; } = new(
        DeckId,
        SemanticAnchorId,
        InstrumentDeckLayoutPattern.Grid,
        OrderedSegmentIds);
}

/// <summary>Стабильные идентификаторы ячеек deck канала Workspace Health (внутренний контракт, не TOML).</summary>
public static class WorkspaceHealthSegmentIds
{
    public const string Build = "workspace_health_segment_build";
    public const string Tests = "workspace_health_segment_tests";
    public const string Debug = "workspace_health_segment_debug";
    public const string Git = "workspace_health_segment_git";
}

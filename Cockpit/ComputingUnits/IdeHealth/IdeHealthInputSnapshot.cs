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
/// Страт A (workspace, ADR 0095): рабочая копия — сейчас сегмент <see cref="Git"/>; дальше без ломки контракта можно добавить поля рядом с git.
/// </summary>
public readonly record struct IdeHealthWorkspaceInput(IdeHealthSegmentInput Git);

/// <summary>
/// Страт B (solution, ADR 0095): решение/проект — сборка, тесты, отладка в одной полосе IDE Health.
/// </summary>
public readonly record struct IdeHealthSolutionInput(
    IdeHealthSegmentInput Build,
    IdeHealthSegmentInput Tests,
    IdeHealthSegmentInput Debug);

/// <summary>
/// Страт C (IDE host, ADR 0095): процесс IDE — LSP/MCP/env. Поля снимка IDE Health; отдельные сегменты в полосе не обязаны
/// совпадать 1:1 (см. <see cref="Composition.WorkspaceHealth.IdeHealthSurfaceCompositor"/>).
/// </summary>
/// <param name="LspStatusHint">Краткая строка для LSP, когда появится провайдер; иначе <see langword="null"/>.</param>
public readonly record struct IdeHealthIdeHostInput(string? LspStatusHint = null);

/// <summary>
/// Снимок входов <strong>IDE Health</strong> (ADR 0089): три страта (ADR 0095). Сегменты полосы (build → tests → debug) — в <see cref="Solution"/>, git — в <see cref="Workspace"/>.
/// Реализует <see cref="ICockpitComputeUnitPayload"/>: нормализованная полезная нагрузка на границе CCU (ADR 0097) — до композитора/полос; без DAP.
/// Слой канала (ADR 0036 п.1) → <see cref="IdeHealthFormattingUnit"/> (текст), затем
/// <see cref="Composition.WorkspaceHealth.IdeHealthSurfaceCompositor"/> (порядок сегментов, ADR 0036 п.3). EICAS — иной контур (<see cref="IEicasFeed"/>).
/// </summary>
public readonly record struct IdeHealthInputSnapshot(
    IdeHealthWorkspaceInput Workspace,
    IdeHealthSolutionInput Solution,
    IdeHealthIdeHostInput IdeHost) : ICockpitComputeUnitPayload
{
    /// <summary>Тесты и фиктивные снимки: сборка сегментов без ручного конструктора вложенных структур.</summary>
    public static IdeHealthInputSnapshot FromFlat(
        IdeHealthSegmentInput build,
        IdeHealthSegmentInput tests,
        IdeHealthSegmentInput debug,
        IdeHealthSegmentInput git) =>
        IdeHealthStrataComposer.Compose(
            new IdeHealthWorkspaceInput(git),
            new IdeHealthSolutionInput(build, tests, debug),
            default);
}

namespace CascadeIDE.Models;

/// <summary>
/// Раскладка по физическим дисплеям и окно-хост зоны Mfd (ADR 0017).
/// В <c>settings.toml</c> — секция <c>[display]</c>.
/// </summary>
public sealed class DisplaySettings
{
    /// <summary>Строка раскладки по физическим дисплеям (ADR 0017). Пусто — не задано.</summary>
    public string Presentation { get; set; } = "";

    /// <summary>Синоним <see cref="Presentation"/>; задаётся одно из двух, не оба (приоритет у <see cref="Presentation"/>).</summary>
    public string ZoneScreenLayout { get; set; } = "";

    /// <summary>
    /// При пресете «Mfd на втором экране» и >=2 мониторах — открыть окно-хост зоны Mfd при старте (ADR 0017 v1).
    /// </summary>
    public bool OpenMfdHostWindowOnStartup { get; set; } = true;

    /// <summary>Последняя сохранённая позиция окна <c>MfdHostWindow</c> (пиксели); вместе с шириной/высотой — или все заданы, или сброс.</summary>
    public int? MfdHostWindowPixelX { get; set; }

    /// <summary>См. <see cref="MfdHostWindowPixelX"/>.</summary>
    public int? MfdHostWindowPixelY { get; set; }

    /// <summary>См. <see cref="MfdHostWindowPixelX"/>.</summary>
    public double? MfdHostWindowWidth { get; set; }

    /// <summary>См. <see cref="MfdHostWindowPixelX"/>.</summary>
    public double? MfdHostWindowHeight { get; set; }

    /// <summary>
    /// Показывать отладочный preview контуров зон P/F/M поверх основного layout.
    /// Используется для визуальной валидации геометрии без изменения контента зон.
    /// </summary>
    public bool UseSkiaZoneGeometryPreview { get; set; }

    /// <summary>
    /// Wave 3: включить отрисовку инструмента в Skia mount-слое зон P/F/M.
    /// </summary>
    public bool UseSkiaInstrumentMount { get; set; }

    /// <summary>
    /// Идентификатор mount-style для mount-инструмента (декларативный контракт Wave 3).
    /// Пример: <see cref="InstrumentMountPolicyIds.V1"/>.
    /// </summary>
    public string InstrumentMountStyle { get; set; } = InstrumentMountPolicyIds.V1;

    /// <summary>
    /// Реестр правил резолва style по тройке <c>surface_id + slot_id + instrument_id</c>.
    /// Если пусто — используется <see cref="InstrumentMountStyle"/> как fallback.
    /// </summary>
    public List<InstrumentMountPolicyRuleSettings> InstrumentMountPolicyRules { get; set; } = [];

    /// <summary>
    /// Включить eligibility-gate для rollout style: правило применяется только если проходит SA/perf/workload проверки.
    /// </summary>
    public bool EnforceInstrumentMountPolicyEligibility { get; set; }

    /// <summary>Минимальный SA score (0..1) для допуска style-rule при <see cref="EnforceInstrumentMountPolicyEligibility"/>.</summary>
    public double InstrumentMountPolicyMinSaScore { get; set; } = 0.6;

    /// <summary>Минимальный performance score (0..1) для допуска style-rule при <see cref="EnforceInstrumentMountPolicyEligibility"/>.</summary>
    public double InstrumentMountPolicyMinPerformanceScore { get; set; } = 0.6;

    /// <summary>Максимальный workload score (0..1, меньше лучше) для допуска style-rule при <see cref="EnforceInstrumentMountPolicyEligibility"/>.</summary>
    public double InstrumentMountPolicyMaxWorkloadScore { get; set; } = 0.5;

    /// <summary>
    /// Если <see langword="true"/>, rule без SA/perf/workload scores отклоняется eligibility-gate.
    /// Если <see langword="false"/>, отсутствие score допускается (gate не блокирует rule).
    /// </summary>
    public bool RequireInstrumentMountPolicyScores { get; set; }

    /// <summary>
    /// Если <see langword="true"/>, репозиторная карта инструментов (workspace.toml) имеет приоритет над пользовательской
    /// для одинакового ключа <c>surface_id + slot_id</c>.
    /// </summary>
    public bool PreferRepoInstrumentsPlacement { get; set; }

    /// <summary>
    /// Пользовательский слой карты размещения инструментов по слотам.
    /// TOML: <c>[[display.instrument_placement_rules]]</c>.
    /// </summary>
    public List<InstrumentPlacementRuleSettings> InstrumentPlacementRules { get; set; } = [];
}

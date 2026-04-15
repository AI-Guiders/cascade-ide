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
    /// Wave 3: включить лёгкий preview контента инструмента в mount-слое P/F/M.
    /// </summary>
    public bool UseSkiaInstrumentWave3Preview { get; set; }

    /// <summary>
    /// Идентификатор slot-policy для mount preview-инструмента (декларативный контракт Wave 3).
    /// Пример: <c>wave3_preview_v1</c>.
    /// </summary>
    public string InstrumentMountSlotPolicy { get; set; } = "wave3_preview_v1";

    /// <summary>
    /// Реестр правил резолва policy по паре <c>slot_id + instrument_id</c>.
    /// Если пусто — используется <see cref="InstrumentMountSlotPolicy"/> как fallback.
    /// </summary>
    public List<InstrumentMountPolicyRuleSettings> InstrumentMountPolicyRules { get; set; } = [];
}

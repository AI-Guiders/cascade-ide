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
}

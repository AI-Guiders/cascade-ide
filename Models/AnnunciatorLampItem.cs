namespace CascadeIDE.Models;

/// <summary>Состояние подсветки лампы на полосе (annunciator / Korry): <see cref="Ok"/> — «выкл.» / нет тревоги.</summary>
/// <remarks>
/// Цвета из <c>CockpitPrimitivesPalette</c> сопоставлены с EICAS W/C/A (ADR 0021 §5): в бытовой речи «Error» ≈ <see cref="Unavailable"/>,
/// «Warning» (внимание без катастрофы) ≈ <see cref="Warning"/>, «Information» ≈ <see cref="Info"/>.
/// </remarks>
public enum AnnunciatorLampLevel
{
    /// <summary>Лампа не горит (норма).</summary>
    Ok,

    /// <summary>Внимание без критики (янтарь) — в терминах EICAS: Caution (C); не путать с красным EICAS Warning.</summary>
    Warning,

    /// <summary>Информация (синий) — в терминах EICAS: Advisory (A).</summary>
    Info,

    /// <summary>Недоступно / критичная ошибка (красный) — в терминах EICAS: Warning (W).</summary>
    Unavailable
}

/// <summary>
/// Одна ячейка полосы ламп: стабильный id, тултип и короткая подпись на линзе (примитив <c>Lamp</c>, ADR 0063).
/// Используется экраном готовности окружения и любыми другими полосами с тем же примитивом.
/// </summary>
/// <param name="Id">Стабильный id ячейки для deck и тестов (напр. <see cref="EnvironmentReadinessCellIds"/>).</param>
/// <param name="LampShortLabel">Короткая подпись у лампы.</param>
public sealed record AnnunciatorLampItem(
    string Id,
    string Title,
    string Detail,
    AnnunciatorLampLevel Level,
    string LampShortLabel);

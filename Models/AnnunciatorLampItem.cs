namespace CascadeIDE.Models;

/// <summary>Состояние подсветки лампы на полосе (annunciator / Korry): <see cref="Ok"/> — «выкл.» / нет тревоги.</summary>
/// <remarks>Соответствие EICAS W/C/A и бытовым словам — таблица в ADR 0021 §5.</remarks>
public enum AnnunciatorLampLevel
{
    /// <summary>Лампа не горит (норма).</summary>
    Ok,

    /// <summary>EICAS Caution (C), янтарь.</summary>
    Caution,

    /// <summary>EICAS Advisory (A), синий.</summary>
    Advisory,

    /// <summary>EICAS Warning (W), красный.</summary>
    Critical,
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

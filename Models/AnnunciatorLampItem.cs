namespace CascadeIDE.Models;

/// <summary>Состояние подсветки лампы на полосе (annunciator / Korry): <see cref="Ok"/> — «выкл.» / нет тревоги.</summary>
/// <remarks>
/// Общая визуальная шкала W/C/A для кокпита: разные каналы подают свои снимки, но используют одну грамматику ламп.
/// Экран готовности окружения строит строки <see cref="AnnunciatorLampItem"/>; полоса EICAS — сообщения
/// <see cref="CascadeIDE.Cockpit.Channels.Eicas.EicasMessage"/> с <see cref="CascadeIDE.Cockpit.Channels.Eicas.EicasSeverity"/> (те же уровни внимания, другая модель и коллекция).
/// Соответствие EICAS W/C/A и бытовым словам — таблица в ADR 0021 §5.
/// </remarks>
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
/// Канал «готовность окружения» (ADR 0023) собирает список таких ячеек; это не поток EICAS, но та же шкала <see cref="AnnunciatorLampLevel"/>.
/// </summary>
/// <param name="Id">Стабильный id ячейки для deck и тестов (напр. <see cref="EnvironmentReadinessCellIds"/>).</param>
/// <param name="LampShortLabel">Короткая подпись у лампы.</param>
public sealed record AnnunciatorLampItem(
    string Id,
    string Title,
    string Detail,
    AnnunciatorLampLevel Level,
    string LampShortLabel);

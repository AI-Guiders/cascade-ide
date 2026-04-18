namespace CascadeIDE.Cockpit;

/// <summary>
/// Ось формы представления контента в регионе внимания: каким шаблоном/контейнером показать поток данных (полоса vs блок страницы региона и т.д.).
/// Канон ADR 0063; ортогонально композиции инструментов (<see cref="InstrumentDeckDescriptor"/>) и маршрутизации слотов.
/// </summary>
public enum ContentRepresentation
{
    /// <summary>Узкая полоса региона (напр. нижняя полоса Workspace Health).</summary>
    Strip = 0,

    /// <summary>Блок «страницы» региона; не путать с именованной колодой инструментов (instrument deck) на оси композиции.</summary>
    Page = 1,
}

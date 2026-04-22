namespace CascadeIDE.Models;

/// <summary>
/// Детализация композиции карты кода (ADR 0055): прежде всего политика Declutter; Layout подбирает метрики.
/// Настройка: <c>[code_navigation_map].detail_level</c>.
/// </summary>
public enum CodeNavigationMapDetailLevel
{
    Glance,
    Normal,
    Inspect
}

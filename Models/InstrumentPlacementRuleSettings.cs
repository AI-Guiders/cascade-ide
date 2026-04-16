namespace CascadeIDE.Models;

/// <summary>
/// Декларативная запись выбора инструмента для пары <c>surface_id + slot_id</c>.
/// Используется в слоях bundle/repo/user для карты instrument → slot → surface (ADR 0050).
/// </summary>
public sealed class InstrumentPlacementRuleSettings
{
    public string SurfaceId { get; set; } = "";

    public string SlotId { get; set; } = "";

    public string InstrumentId { get; set; } = "";
}

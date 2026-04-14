namespace CascadeIDE.Cockpit.Composition;

/// <summary>
/// Дескриптор инструмента кабины (ADR 0047): единица выбора композитора для слота внимания.
/// Не Avalonia <c>Control</c>; поверхность сопоставляет <see cref="InstrumentId"/> конкретной разметке.
/// </summary>
/// <param name="InstrumentId">Стабильный идентификатор вида представления (например <c>solution_explorer_tree</c>, <c>workspace_navigation_map</c>).</param>
/// <param name="SlotId">Слот внимания: <c>pfd</c>, <c>mfd</c>, <c>forward</c> и т.д. (согласовать с картой зон ADR 0021).</param>
/// <param name="SchemaVersion">Версия схемы этого дескриптора (не версия payload канала).</param>
public readonly record struct CockpitInstrumentDescriptor(string InstrumentId, string SlotId, string SchemaVersion = "0.2");

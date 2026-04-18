namespace CascadeIDE.Cockpit;

/// <summary>
/// Именованная композиция инструментов в одном <b>семантическом якоре внимания</b> (ADR 0063 instrument deck).
/// Ось B (порядок и раскладка); ортогонально <see cref="ContentRepresentation"/> и v1 маршрутизации «один инструмент на слот» ([instrument_routing]).
/// </summary>
/// <param name="DeckId">Стабильное имя колоды для логов, тестов и будущей сериализации (не пользовательский TOML на текущем этапе).</param>
/// <param name="SemanticAnchorId">Якорь внимания: например константы <c>pfd</c>/<c>mfd</c> из <see cref="Composition.HostSurface.CockpitSlotIds"/> или идентификатор канала (см. <see cref="Composition.WorkspaceHealth.WorkspaceHealthInstrumentDeck.SemanticAnchorId"/>).</param>
/// <param name="LayoutPattern">Сетка, вкладки, стек, split.</param>
/// <param name="OrderedInstrumentIds">
/// Упорядоченные <c>instrument_id</c> / логические ячейки композиции в этом якоре (alias из маршрутизации при необходимости).
/// </param>
public readonly record struct InstrumentDeckDescriptor(
    string DeckId,
    string SemanticAnchorId,
    InstrumentDeckLayoutPattern LayoutPattern,
    IReadOnlyList<string> OrderedInstrumentIds);

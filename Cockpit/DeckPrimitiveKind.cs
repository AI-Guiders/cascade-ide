namespace CascadeIDE.Cockpit;

/// <summary>
/// Визуальный примитив ячейки deck или фрагмента Skia-инструмента: геометрия «атомарного glance» (ADR 0063 § типы индикаторов).
/// Не ось <see cref="ContentRepresentation"/>; не семантика канала EICAS/WCA — только форма сигнала.
/// Семантика ролей <b>Presence</b> / <b>Activity</b> обычно выражается через <see cref="Lamp"/> / <see cref="Sign"/> / <see cref="Bar"/> / <see cref="Trend"/>, отдельные значения enum не обязательны.
/// </summary>
public enum DeckPrimitiveKind
{
    /// <summary>Дискретное состояние (annunciator, latch внимания).</summary>
    Lamp = 0,

    /// <summary>Величина или отклонение на оси (полоса, маркер).</summary>
    Bar = 1,

    /// <summary>Иконка, бейдж, краткая категория (не табло-число).</summary>
    Sign = 2,

    /// <summary>Крупная цифра или моноширинная строка-значение.</summary>
    Readout = 3,

    /// <summary>Микро-график по времени (sparkline).</summary>
    Trend = 4,

    /// <summary>Скаляр на дуге/кольце.</summary>
    Gauge = 5,

    /// <summary>Несколько долей в одной полосе (разбиение 100 %); в ADR таблице названо «Stack», в коде — иное имя, чтобы не путать с <see cref="InstrumentDeckLayoutPattern.Stack"/>.</summary>
    SegmentedBar = 6,

    /// <summary>Одна строка с жёстким truncate (ветка, файл, команда).</summary>
    Caption = 7,
}

namespace CascadeIDE.Cockpit;

/// <summary>
/// Структура раскладки именованной колоды инструментов в одном якоре (ADR 0063): сетка, вкладки, стек, split — перечень задаётся реализацией.
/// </summary>
public enum InstrumentDeckLayoutPattern
{
    /// <summary>Несколько ячеек одновременно (образ PFD).</summary>
    Grid = 0,

    /// <summary>Переключение видимого инструмента по вкладкам.</summary>
    Tabs = 1,

    /// <summary>Один видимый слой, навигация по стеку.</summary>
    Stack = 2,

    /// <summary>Деление области (например две колонки/строки).</summary>
    Split = 3,
}

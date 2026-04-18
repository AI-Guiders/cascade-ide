using CascadeIDE.Contracts.Experimental;

namespace CascadeIDE.Cockpit.Cds;

/// <summary>
/// Топология презентации зон внимания: <strong>где физически</strong> размещены регионы (одно окно, несколько окон, мониторы).
/// Не путать с <see cref="AttentionZoneCanonicalIds"/> — это семантика зоны («pfd» / «mfd» / «forward»), а не форма раскладки.
/// </summary>
/// <remarks>
/// Сейчас реализована только <see cref="MainWindowDockedGrid"/>; иные сценарии — ADR 0021 §13, ADR 0017.
/// Свойства вроде <c>IsPfdColumnVisible</c> на главном VM относятся исключительно к этой топологии.
/// </remarks>
public enum AttentionLayoutSurfaceKind
{
    /// <summary>Одно главное окно: колонки <c>MainGrid</c> (якоря PFD / forward / MFD в одной сетке).</summary>
    MainWindowDockedGrid = 0,

    /// <summary>Главное окно без колонки Mfd + отдельный <c>TopLevel</c> с <c>SecondaryShellView</c> (ADR 0017).</summary>
    MainWindowPlusMfdHostTopLevel = 1,

    /// <summary>Главное окно без колонки Pfd + отдельный <c>TopLevel</c> с зоной PFD (ADR 0017).</summary>
    MainWindowPlusPfdHostTopLevel = 2,

    /// <summary>Оба хоста PFD и MFD вынесены в отдельные <c>TopLevel</c>; в main остаётся лобовое (ADR 0017).</summary>
    MainWindowPlusPfdMfdHostTopLevel = 3,
}

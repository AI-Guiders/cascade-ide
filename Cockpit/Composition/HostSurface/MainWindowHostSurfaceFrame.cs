using CascadeIDE.Cockpit.Composition.Shell;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Единый кадр выхода композитора для <b>хоста поверхности</b> (Avalonia как фюзеляж, отрисовка слотов — отдельно, в т.ч. Skia):
/// метрики колонок shell + логические инструменты по слотам (ADR 0036, 0047). Без деревьев контролов и без команд отрисовки.
/// </summary>
/// <param name="Shell">Геометрия/видимость колонок PFD/MFD в main grid.</param>
/// <param name="Instruments">Что монтировать в слотах; v1 — PFD при выделенной колонке (дерево решения). Расширение — Semantic Map, MFD-состав.</param>
public readonly record struct MainWindowHostSurfaceFrame(
    MainWindowShellSurfaceComposition Shell,
    IReadOnlyList<CockpitInstrumentDescriptor> Instruments);

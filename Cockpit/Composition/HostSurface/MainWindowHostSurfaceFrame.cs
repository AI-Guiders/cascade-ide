using CascadeIDE.Cockpit.Composition.Shell;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Единый кадр выхода композитора для <b>хоста поверхности</b> (Avalonia как фюзеляж, отрисовка слотов — отдельно, в т.ч. Skia):
/// метрики колонок shell + логические инструменты по слотам (ADR 0036, 0047). Без деревьев контролов и без команд отрисовки.
/// </summary>
/// <param name="Shell">Геометрия/видимость колонок PFD/MFD в main grid.</param>
/// <param name="Instruments">Привязки слот → инструмент; задаётся <see cref="ISurfaceSlotInstrumentBindingProvider"/> (карта кода в Pfd, дерево решения в Mfd при видимых колонках — см. дефолтную реализацию).</param>
public readonly record struct MainWindowHostSurfaceFrame(
    MainWindowShellSurfaceComposition Shell,
    IReadOnlyList<CockpitInstrumentDescriptor> Instruments);

using CascadeIDE.Cockpit.Composition.Shell;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Привязки слот → <see cref="CockpitInstrumentDescriptor"/> для кадра поверхности (CDS/Skia): не «резолв DI»,
/// а манифест того, какие канонические <c>instrument_id</c> считаются смонтированными в каких слотах при текущей геометрии shell.
/// </summary>
public interface ISurfaceSlotInstrumentBindingProvider
{
    IReadOnlyList<CockpitInstrumentDescriptor> GetBindings(in SurfaceSlotInstrumentBindingContext context);
}

/// <param name="Shell">Уже посчитанная оболочка (видимость колонок, ширина Mfd).</param>
/// <param name="SurfaceId"><c>main_window_docked_grid</c> или <c>main_window_plus_mfd_host_top_level</c>.</param>
/// <param name="SafetyLevel">Уровень безопасности канала (safety.observe … safety.autonomous).</param>
public readonly record struct SurfaceSlotInstrumentBindingContext(
    MainWindowShellSurfaceComposition Shell,
    string SurfaceId,
    string SafetyLevel);

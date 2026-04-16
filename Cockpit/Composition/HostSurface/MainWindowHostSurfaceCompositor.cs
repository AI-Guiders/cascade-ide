using CascadeIDE.Cockpit.Composition.Shell;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Композитор кадра хоста главного окна: shell + <see cref="CockpitInstrumentDescriptor"/> (ADR 0047).
/// Держит слабую связность с Avalonia — только DTO для привязок и будущего Skia-backend слотов.
/// Состав инструментов по слотам — <see cref="ISurfaceSlotInstrumentBindingProvider"/> (дефолт: <see cref="DefaultSurfaceSlotInstrumentBindingProvider"/>).
/// </summary>
public static class MainWindowHostSurfaceCompositor
{
    /// <summary>Собирает <see cref="MainWindowHostSurfaceFrame"/> за один проход поверх <see cref="MainWindowShellSurfaceCompositor"/>.</summary>
    /// <param name="slotInstrumentBindingProvider">Если null — <see cref="DefaultSurfaceSlotInstrumentBindingProvider.Instance"/>.</param>
    public static MainWindowHostSurfaceFrame ComposeFrame(
        in MainWindowShellSurfaceCompositionInput input,
        ISurfaceSlotInstrumentBindingProvider? slotInstrumentBindingProvider = null)
    {
        var shell = MainWindowShellSurfaceCompositor.Compose(input);
        var surfaceId = input.SuppressMfdColumnForMfdHostWindow
            ? MainWindowHostSurfaceIds.PlusMfdHostTopLevel
            : MainWindowHostSurfaceIds.DockedGrid;

        var provider = slotInstrumentBindingProvider ?? DefaultSurfaceSlotInstrumentBindingProvider.Instance;
        var instruments = provider.GetBindings(
            new SurfaceSlotInstrumentBindingContext(shell, surfaceId, input.SafetyLevel));

        return new MainWindowHostSurfaceFrame(shell, instruments);
    }
}

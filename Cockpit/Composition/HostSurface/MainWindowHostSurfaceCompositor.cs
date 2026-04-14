using CascadeIDE.Cockpit.Composition.Shell;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Композитор кадра хоста главного окна: shell + <see cref="CockpitInstrumentDescriptor"/> (ADR 0047).
/// Держит слабую связность с Avalonia — только DTO для привязок и будущего Skia-backend слотов.
/// </summary>
public static class MainWindowHostSurfaceCompositor
{
    /// <summary>Собирает <see cref="MainWindowHostSurfaceFrame"/> за один проход поверх <see cref="MainWindowShellSurfaceCompositor"/>.</summary>
    public static MainWindowHostSurfaceFrame ComposeFrame(in MainWindowShellSurfaceCompositionInput input)
    {
        var shell = MainWindowShellSurfaceCompositor.Compose(input);
        IReadOnlyList<CockpitInstrumentDescriptor> instruments = shell.PfdSurfaceVisible
            ? [new CockpitInstrumentDescriptor(CockpitStandardInstrumentIds.SolutionExplorerTree, CockpitSlotIds.Pfd)]
            : [];
        return new MainWindowHostSurfaceFrame(shell, instruments);
    }
}

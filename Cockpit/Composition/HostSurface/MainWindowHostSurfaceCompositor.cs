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
        var surfaceId = ResolveSurfaceId(input);
        var instruments = BuildInstruments(shell, surfaceId, input);
        return new MainWindowHostSurfaceFrame(shell, instruments);
    }

    private static string ResolveSurfaceId(in MainWindowShellSurfaceCompositionInput input)
    {
        if (input.SuppressPfdColumnForPfdHostWindow && input.SuppressMfdColumnForMfdHostWindow)
            return MainWindowHostSurfaceIds.PlusPfdMfdHostTopLevel;
        if (input.SuppressPfdColumnForPfdHostWindow)
            return MainWindowHostSurfaceIds.PlusPfdHostTopLevel;
        if (input.SuppressMfdColumnForMfdHostWindow)
            return MainWindowHostSurfaceIds.PlusMfdHostTopLevel;
        return MainWindowHostSurfaceIds.DockedGrid;
    }

    private static IReadOnlyList<CockpitInstrumentDescriptor> BuildInstruments(
        in MainWindowShellSurfaceComposition shell,
        string surfaceId,
        in MainWindowShellSurfaceCompositionInput input)
    {
        var list = new List<CockpitInstrumentDescriptor>(2);
        if (shell.PfdSurfaceVisible
            && InstrumentPlacementRuntime.TryResolveInstrument(surfaceId, CockpitSlotIds.Pfd, input.DisplaySettings, out var pfdInstrumentId))
        {
            list.Add(new CockpitInstrumentDescriptor(pfdInstrumentId, CockpitSlotIds.Pfd));
        }

        if (shell.MfdColumnVisibleInMainGrid
            && InstrumentPlacementRuntime.TryResolveInstrument(surfaceId, CockpitSlotIds.Mfd, input.DisplaySettings, out var mfdInstrumentId))
        {
            list.Add(new CockpitInstrumentDescriptor(mfdInstrumentId, CockpitSlotIds.Mfd));
        }

        return list;
    }
}

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
        var surfaceId = input.SuppressMfdColumnForMfdHostWindow
            ? "main_window_plus_mfd_host_top_level"
            : "main_window_docked_grid";
        var instruments = new List<CockpitInstrumentDescriptor>(capacity: 3);
        TryAddInstrument(
            instruments,
            shell.PfdSurfaceVisible,
            surfaceId,
            CockpitSlotIds.Pfd,
            CockpitStandardInstrumentIds.SolutionExplorerTree,
            input.SafetyLevel);
        TryAddInstrument(
            instruments,
            shell.PfdSurfaceVisible,
            surfaceId,
            CockpitSlotIds.Pfd,
            CockpitStandardInstrumentIds.WorkspaceHealthStatusV1,
            input.SafetyLevel);
        TryAddInstrument(
            instruments,
            shell.MfdColumnVisibleInMainGrid,
            surfaceId,
            CockpitSlotIds.Mfd,
            CockpitStandardInstrumentIds.WorkspaceHealthStatusV1,
            input.SafetyLevel);
        return new MainWindowHostSurfaceFrame(shell, instruments);
    }

    private static void TryAddInstrument(
        List<CockpitInstrumentDescriptor> instruments,
        bool slotVisible,
        string surfaceId,
        string slotId,
        string instrumentId,
        string safetyLevel)
    {
        if (!slotVisible)
            return;
        var placementContext = new InstrumentPlacementContext(
            SurfaceId: surfaceId,
            SlotId: slotId,
            InstrumentId: instrumentId,
            SafetyLevel: safetyLevel);
        if (!IsPlacementAllowed(placementContext))
            return;
        instruments.Add(new CockpitInstrumentDescriptor(instrumentId, slotId));
    }

    private static bool IsPlacementAllowed(in InstrumentPlacementContext context)
    {
        foreach (var rule in CockpitInstrumentPlacementRules.MainWindow)
        {
            if (!string.Equals(rule.InstrumentId, context.InstrumentId, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.Equals(rule.SlotId, context.SlotId, StringComparison.OrdinalIgnoreCase))
                continue;
            if (rule.Specification.IsSatisfiedBy(in context))
                return true;
        }

        return false;
    }
}

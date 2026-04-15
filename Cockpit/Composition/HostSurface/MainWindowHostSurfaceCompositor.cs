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
        var placementContext = new InstrumentPlacementContext(
            SurfaceId: surfaceId,
            SlotId: CockpitSlotIds.Pfd,
            InstrumentId: CockpitStandardInstrumentIds.SolutionExplorerTree,
            SafetyLevel: input.SafetyLevel);

        IReadOnlyList<CockpitInstrumentDescriptor> instruments =
            shell.PfdSurfaceVisible && IsPlacementAllowed(placementContext)
                ? [new CockpitInstrumentDescriptor(CockpitStandardInstrumentIds.SolutionExplorerTree, CockpitSlotIds.Pfd)]
                : [];
        return new MainWindowHostSurfaceFrame(shell, instruments);
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

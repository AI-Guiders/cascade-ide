using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Разрешение инструментов в слотах PFD/MFD главного окна (surface <see cref="MainWindowHostSurfaceIds.DockedGrid"/>):
/// одна точка для <see cref="ViewModels.MainWindowViewModel"/> и тестов (карта + <see cref="DisplaySettings"/>).
/// </summary>
public static class MainWindowDockedGridInstrumentSlots
{
    public static string ResolvePfdInstrumentId(DisplaySettings? display)
    {
        if (InstrumentPlacementRuntime.TryResolveInstrument(
                MainWindowHostSurfaceIds.DockedGrid,
                CockpitSlotIds.Pfd,
                display!,
                out var id)
            && !string.IsNullOrWhiteSpace(id))
            return id;

        return CockpitStandardInstrumentIds.SolutionExplorerTree;
    }

    public static string ResolveMfdInstrumentId(DisplaySettings? display)
    {
        if (InstrumentPlacementRuntime.TryResolveInstrument(
                MainWindowHostSurfaceIds.DockedGrid,
                CockpitSlotIds.Mfd,
                display!,
                out var id)
            && !string.IsNullOrWhiteSpace(id))
            return id;

        return "";
    }

    public static bool IsDockedPfdSolutionExplorerTree(DisplaySettings? display) =>
        string.Equals(
            ResolvePfdInstrumentId(display),
            CockpitStandardInstrumentIds.SolutionExplorerTree,
            StringComparison.OrdinalIgnoreCase);

    public static bool IsDockedPfdWorkspaceNavigationMap(DisplaySettings? display) =>
        string.Equals(
            ResolvePfdInstrumentId(display),
            CockpitStandardInstrumentIds.WorkspaceNavigationMap,
            StringComparison.OrdinalIgnoreCase);

    public static bool IsDockedMfdSolutionExplorerTree(DisplaySettings? display) =>
        string.Equals(
            ResolveMfdInstrumentId(display),
            CockpitStandardInstrumentIds.SolutionExplorerTree,
            StringComparison.OrdinalIgnoreCase)
        && !IsDockedPfdSolutionExplorerTree(display);
}

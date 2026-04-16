namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Дефолт главного окна: <see cref="CockpitStandardInstrumentIds.WorkspaceNavigationMap"/> в Pfd при видимой колонке;
/// <see cref="CockpitStandardInstrumentIds.SolutionExplorerTree"/> в Mfd при видимой колонке Mfd в main grid.
/// </summary>
public sealed class DefaultSurfaceSlotInstrumentBindingProvider : ISurfaceSlotInstrumentBindingProvider
{
    public static DefaultSurfaceSlotInstrumentBindingProvider Instance { get; } = new();

    private DefaultSurfaceSlotInstrumentBindingProvider()
    {
    }

    public IReadOnlyList<CockpitInstrumentDescriptor> GetBindings(in SurfaceSlotInstrumentBindingContext context)
    {
        var shell = context.Shell;
        var list = new List<CockpitInstrumentDescriptor>();

        if (shell.PfdSurfaceVisible)
        {
            var ctx = new InstrumentPlacementContext(
                SurfaceId: context.SurfaceId,
                SlotId: CockpitSlotIds.Pfd,
                InstrumentId: CockpitStandardInstrumentIds.WorkspaceNavigationMap,
                SafetyLevel: context.SafetyLevel);
            if (CockpitInstrumentPlacementRules.IsAllowed(in ctx))
                list.Add(new CockpitInstrumentDescriptor(CockpitStandardInstrumentIds.WorkspaceNavigationMap, CockpitSlotIds.Pfd));
        }

        if (shell.MfdColumnVisibleInMainGrid)
        {
            var ctx = new InstrumentPlacementContext(
                SurfaceId: context.SurfaceId,
                SlotId: CockpitSlotIds.Mfd,
                InstrumentId: CockpitStandardInstrumentIds.SolutionExplorerTree,
                SafetyLevel: context.SafetyLevel);
            if (CockpitInstrumentPlacementRules.IsAllowed(in ctx))
                list.Add(new CockpitInstrumentDescriptor(CockpitStandardInstrumentIds.SolutionExplorerTree, CockpitSlotIds.Mfd));
        }

        return list;
    }
}

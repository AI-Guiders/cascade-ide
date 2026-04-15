namespace CascadeIDE.Cockpit.Composition.HostSurface;

/// <summary>
/// Дефолт для главного окна: при видимой колонке Pfd — <see cref="CockpitStandardInstrumentIds.SolutionExplorerTree"/>,
/// если <see cref="CockpitInstrumentPlacementRules"/> разрешает. Дальше: SM в Pfd, SE в Mfd — другая реализация <see cref="ISurfaceSlotInstrumentBindingProvider"/>.
/// </summary>
public sealed class DefaultSurfaceSlotInstrumentBindingProvider : ISurfaceSlotInstrumentBindingProvider
{
    public static DefaultSurfaceSlotInstrumentBindingProvider Instance { get; } = new();

    private DefaultSurfaceSlotInstrumentBindingProvider()
    {
    }

    public IReadOnlyList<CockpitInstrumentDescriptor> GetBindings(in SurfaceSlotInstrumentBindingContext context)
    {
        if (!context.Shell.PfdSurfaceVisible)
            return [];

        var ctx = new InstrumentPlacementContext(
            SurfaceId: context.SurfaceId,
            SlotId: CockpitSlotIds.Pfd,
            InstrumentId: CockpitStandardInstrumentIds.SolutionExplorerTree,
            SafetyLevel: context.SafetyLevel);

        if (!CockpitInstrumentPlacementRules.IsAllowed(in ctx))
            return [];

        return [new CockpitInstrumentDescriptor(CockpitStandardInstrumentIds.SolutionExplorerTree, CockpitSlotIds.Pfd)];
    }
}

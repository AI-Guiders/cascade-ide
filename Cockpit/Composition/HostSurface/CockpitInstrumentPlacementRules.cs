namespace CascadeIDE.Cockpit.Composition.HostSurface;

internal readonly record struct InstrumentPlacementRule(
    string InstrumentId,
    string SlotId,
    IInstrumentPlacementSpecification Specification);

internal static class CockpitInstrumentPlacementRules
{
    public static readonly IReadOnlyList<InstrumentPlacementRule> MainWindow =
    [
        // v1: solution tree stays on PFD, on supported host surfaces and standard safety levels.
        new InstrumentPlacementRule(
            CockpitStandardInstrumentIds.SolutionExplorerTree,
            CockpitSlotIds.Pfd,
            new AndInstrumentPlacementSpecification(
                new AllowedSurfaceSpecification(
                    "main_window_docked_grid",
                    "main_window_plus_mfd_host_top_level"),
                new AllowedSlotSpecification(CockpitSlotIds.Pfd),
                new AllowedSafetyLevelsSpecification("L1", "L2", "L3")))
    ];
}

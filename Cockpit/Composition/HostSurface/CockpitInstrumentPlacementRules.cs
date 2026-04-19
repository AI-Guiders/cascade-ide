namespace CascadeIDE.Cockpit.Composition.HostSurface;

internal readonly record struct InstrumentPlacementRule(
    string InstrumentId,
    string SlotId,
    IInstrumentPlacementSpecification Specification);

internal static class CockpitInstrumentPlacementRules
{
    public static bool IsAllowed(in InstrumentPlacementContext context)
    {
        foreach (var rule in MainWindow)
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

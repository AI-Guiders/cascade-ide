namespace CascadeIDE.Cockpit.Composition.HostSurface;

internal readonly record struct InstrumentPlacementContext(
    string SurfaceId,
    string SlotId,
    string InstrumentId,
    string SafetyLevel);

internal interface IInstrumentPlacementSpecification
{
    bool IsSatisfiedBy(in InstrumentPlacementContext context);
}

internal sealed class AllowedSurfaceSpecification(params string[] allowedSurfaces) : IInstrumentPlacementSpecification
{
    private readonly HashSet<string> _allowed = new(allowedSurfaces, StringComparer.OrdinalIgnoreCase);

    public bool IsSatisfiedBy(in InstrumentPlacementContext context) => _allowed.Contains(context.SurfaceId);
}

internal sealed class AllowedSlotSpecification(params string[] allowedSlots) : IInstrumentPlacementSpecification
{
    private readonly HashSet<string> _allowed = new(allowedSlots, StringComparer.OrdinalIgnoreCase);

    public bool IsSatisfiedBy(in InstrumentPlacementContext context) => _allowed.Contains(context.SlotId);
}

internal sealed class AllowedSafetyLevelsSpecification(params string[] allowedSafetyLevels) : IInstrumentPlacementSpecification
{
    private readonly HashSet<string> _allowed = new(allowedSafetyLevels, StringComparer.OrdinalIgnoreCase);

    public bool IsSatisfiedBy(in InstrumentPlacementContext context) => _allowed.Contains(context.SafetyLevel);
}

internal sealed class AndInstrumentPlacementSpecification(params IInstrumentPlacementSpecification[] specifications)
    : IInstrumentPlacementSpecification
{
    public bool IsSatisfiedBy(in InstrumentPlacementContext context)
    {
        foreach (var specification in specifications)
        {
            if (!specification.IsSatisfiedBy(in context))
                return false;
        }

        return true;
    }
}

namespace CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

/// <summary>
/// CCU «решение о scope» (ADR 0097): определяет, когда IDE Health сегменты должны быть project-scoped
/// вместо solution-scoped на основе startup project и активного build/tests сигнала.
/// </summary>
public sealed class IdeHealthScopeDecisionUnit : ICockpitComputeUnit
{
    /// <summary>Единственный экземпляр юнита (без состояния).</summary>
    public static IdeHealthScopeDecisionUnit Default { get; } = new();

    private IdeHealthScopeDecisionUnit()
    {
    }

    public IdeHealthScopeDecision Decide(string? startupProjectPath, bool isBuilding, string? lastTestSummary)
    {
        var hasStartupProject = !string.IsNullOrWhiteSpace(startupProjectPath);
        var hasProjectSignal = isBuilding || !string.IsNullOrWhiteSpace(lastTestSummary);
        var hasProjectScope = hasStartupProject && hasProjectSignal;
        return hasProjectScope
            ? new IdeHealthScopeDecision(IdeHealthScope.Project, startupProjectPath!)
            : new IdeHealthScopeDecision(IdeHealthScope.Solution, null);
    }
}

public readonly record struct IdeHealthScopeDecision(IdeHealthScope Scope, string? ProjectPath);

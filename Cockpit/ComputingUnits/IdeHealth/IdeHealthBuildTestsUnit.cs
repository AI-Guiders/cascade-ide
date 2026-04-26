namespace CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

/// <summary>
/// CCU «build/tests сводка»: формирует пару сегментов из сигналов сборки/тестов и решения области (solution/project).
/// Логика форматирования остаётся в <see cref="IdeHealthFormattingUnit"/>; этот юнит только выбирает нужную ветку.
/// </summary>
public sealed class IdeHealthBuildTestsUnit : ICockpitComputeUnit
{
    public static IdeHealthBuildTestsUnit Default { get; } = new();

    private IdeHealthBuildTestsUnit()
    {
    }

    public IdeHealthBuildTestsSnapshot Compose(
        IdeHealthScopeDecision scopeDecision,
        BuildStateSnapshot buildState,
        string? testSummary,
        int impactedTestsBadge)
    {
        if (scopeDecision.Scope == IdeHealthScope.Project && !string.IsNullOrWhiteSpace(scopeDecision.ProjectPath))
        {
            var projectPath = scopeDecision.ProjectPath;
            return new IdeHealthBuildTestsSnapshot(
                IdeHealthFormattingUnit.Default.ProjectBuildSegment(projectPath, buildState),
                IdeHealthFormattingUnit.Default.ProjectTestsSegment(projectPath, testSummary, impactedTestsBadge));
        }

        return new IdeHealthBuildTestsSnapshot(
            IdeHealthFormattingUnit.Default.BuildSegment(buildState),
            IdeHealthFormattingUnit.Default.TestsSegment(testSummary, impactedTestsBadge));
    }
}

public readonly record struct IdeHealthBuildTestsSnapshot(
    IdeHealthSegmentInput Build,
    IdeHealthSegmentInput Tests);

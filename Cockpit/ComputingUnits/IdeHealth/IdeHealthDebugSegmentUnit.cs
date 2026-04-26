namespace CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

using CascadeIDE.Services;

/// <summary>
/// CCU «debug сегмент»: собирает сегмент Debug из снимка DAP и решения области (solution/project).
/// Детализация строки для project scope делегируется <see cref="IdeHealthDebugSummaryUnit"/>.
/// </summary>
public sealed class IdeHealthDebugSegmentUnit : ICockpitComputeUnit
{
    private readonly IdeHealthDebugSummaryUnit _summary = IdeHealthDebugSummaryUnit.Default;

    public static IdeHealthDebugSegmentUnit Default { get; } = new();

    private IdeHealthDebugSegmentUnit()
    {
    }

    public IdeHealthSegmentInput Compose(IdeHealthScopeDecision scopeDecision, in DebugSessionSnapshot snapshot)
    {
        if (scopeDecision.Scope == IdeHealthScope.Project && !string.IsNullOrWhiteSpace(scopeDecision.ProjectPath))
        {
            var summary = _summary.Summarize(snapshot);
            return IdeHealthFormattingUnit.Default.ProjectDebugSegment(scopeDecision.ProjectPath, summary);
        }

        var variableCount = snapshot.VariableRootScopes.Sum(scope => scope.Roots.Count);
        return IdeHealthFormattingUnit.Default.DebugSegment(
            snapshot.HasActiveSession,
            snapshot.IsExecutionStopped,
            snapshot.StackFrames.Count,
            variableCount);
    }
}

namespace CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

/// <summary>
/// CCU «сводка отладки» (ADR 0097): вычисляет человекочитаемую часть debug-сегмента для project scope.
/// </summary>
public sealed class IdeHealthDebugSummaryUnit : ICockpitComputeUnit
{
    /// <summary>Единственный экземпляр юнита (без состояния).</summary>
    public static IdeHealthDebugSummaryUnit Default { get; } = new();

    private IdeHealthDebugSummaryUnit()
    {
    }

    public string Summarize(in Services.DebugSessionSnapshot snapshot)
    {
        if (!snapshot.HasActiveSession)
            return "idle";

        if (!snapshot.IsExecutionStopped)
            return "running…";

        var variableCount = snapshot.VariableRootScopes.Sum(scope => scope.Roots.Count);
        return $"paused · frames {snapshot.StackFrames.Count}, vars {variableCount}";
    }
}

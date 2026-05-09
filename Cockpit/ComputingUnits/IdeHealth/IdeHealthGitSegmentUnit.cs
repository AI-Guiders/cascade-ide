using CascadeIDE.Contracts;

namespace CascadeIDE.Cockpit.ComputingUnits.IdeHealth;

/// <summary>
/// CCU «git-сегмент» (ADR 0097): из двух git-строк собирает payload для IDE Health.
/// </summary>
[ComputingUnit]
public sealed class IdeHealthGitSegmentUnit : ICockpitComputeUnit
{
    /// <summary>Единственный экземпляр юнита (без состояния).</summary>
    public static IdeHealthGitSegmentUnit Default { get; } = new();

    private IdeHealthGitSegmentUnit()
    {
    }

    public IdeHealthSegmentInput Compose(string gitLine, string gitCockpitShort) =>
        IdeHealthFormattingUnit.Default.GitSegment(gitLine, gitCockpitShort);
}

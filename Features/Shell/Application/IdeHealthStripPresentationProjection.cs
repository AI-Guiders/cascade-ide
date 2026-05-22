using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Shell.Application;

/// <summary>
/// Тексты полосы IDE Health из последнего <see cref="IdeHealthInputSnapshot"/> (без геттеров коллекций на главном VM).
/// </summary>
[PresentationProjection]
public static class IdeHealthStripPresentationProjection
{
    public static string SolutionBuildLineText(IdeHealthInputSnapshot? snapshot) =>
        snapshot is { } s ? s.Solution.Build.LineText : "";

    public static string SolutionBuildCockpitShort(IdeHealthInputSnapshot? snapshot) =>
        snapshot is { } s ? s.Solution.Build.CockpitShort : "";

    public static string SolutionTestsLineText(IdeHealthInputSnapshot? snapshot) =>
        snapshot is { } s ? s.Solution.Tests.LineText : "";

    public static string SolutionTestsCockpitShort(IdeHealthInputSnapshot? snapshot) =>
        snapshot is { } s ? s.Solution.Tests.CockpitShort : "";

    public static string SolutionDebugLineText(IdeHealthInputSnapshot? snapshot) =>
        snapshot is { } s ? s.Solution.Debug.LineText : "";

    public static string SolutionDebugCockpitShort(IdeHealthInputSnapshot? snapshot) =>
        snapshot is { } s ? s.Solution.Debug.CockpitShort : "";
}

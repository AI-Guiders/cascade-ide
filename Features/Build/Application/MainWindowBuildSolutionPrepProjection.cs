using CascadeIDE.Contracts;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Build.Application;

/// <summary>Подготовка UI-сессии локальной сборки решения из главного окна.</summary>
[PresentationProjection("main window build solution ui prep")]
public static class MainWindowBuildSolutionPrepProjection
{
    public sealed record Prep(string SolutionPath, string BuildOutputHeader, MfdShellPage TargetMfdPage);

    public static bool CanBuild(string? solutionPath, bool isBuilding) =>
        IdeMcpSolutionPathAvailability.IsRunnableSolutionFile(solutionPath) && !isBuilding;

    public static Prep? TryCreatePrep(string? solutionPath)
    {
        if (!IdeMcpSolutionPathAvailability.IsRunnableSolutionFile(solutionPath))
            return null;

        return new Prep(
            solutionPath!,
            $"Сборка: {solutionPath}\r\n",
            MfdShellPage.Build);
    }
}

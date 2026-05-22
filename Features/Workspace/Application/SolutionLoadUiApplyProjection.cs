using CascadeIDE.Contracts;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Workspace.Application;

/// <summary>Решения после успешной загрузки дерева решения (первая страница MFD и нормализованный путь).</summary>
[PresentationProjection("solution load ui apply")]
public static class SolutionLoadUiApplyProjection
{
    public sealed record Plan(string NormalizedSolutionPath, MfdShellPage InitialMfdPage);

    public static Plan Create(string originalPath, string? normalizedSolutionPath, bool isDockedMfdSolutionExplorerTree) =>
        new(
            normalizedSolutionPath ?? originalPath,
            isDockedMfdSolutionExplorerTree ? MfdShellPage.SolutionExplorer : MfdShellPage.Terminal);
}

using CascadeIDE.Contracts;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Workspace.Application;

/// <summary>Решения после успешной загрузки дерева решения (первая страница MFD и нормализованный путь).</summary>
[PresentationProjection("solution load ui apply")]
public static class SolutionLoadUiApplyProjection
{
    public sealed record Plan(string NormalizedSolutionPath, MfdShellPage InitialMfdPage);

    public static Plan Create(
        string originalPath,
        string? normalizedSolutionPath,
        bool isDockedMfdSolutionExplorerTree,
        PresentationTierKind presentationTier) =>
        new(
            normalizedSolutionPath ?? originalPath,
            ResolveInitialMfdPage(isDockedMfdSolutionExplorerTree, presentationTier));

    private static MfdShellPage ResolveInitialMfdPage(
        bool isDockedMfdSolutionExplorerTree,
        PresentationTierKind presentationTier)
    {
        if (presentationTier == PresentationTierKind.Compact)
            return MfdShellPage.Chat;

        return isDockedMfdSolutionExplorerTree
            ? MfdShellPage.SolutionExplorer
            : MfdShellPage.Terminal;
    }
}

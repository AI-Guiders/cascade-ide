using CascadeIDE.Contracts;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Workspace.Application;

/// <summary>
/// Применение успешной загрузки дерева решения к <see cref="SolutionWorkspaceViewModel"/> и хосту (документы, редактор, MFD).
/// </summary>
[PresentationProjection("solution load session apply")]
public static class SolutionLoadSessionApplyProjection
{
    /// <summary>Мутации главного окна, которые нельзя держать в workspace VM.</summary>
    public interface IHost
    {
        void ResetEditorSessionForNewSolution();

        void AfterSolutionApplied(MfdShellPage initialMfdPage);
    }

    public static void ApplySuccessfulLoad(
        SolutionWorkspaceViewModel workspace,
        SolutionItem root,
        string originalPath,
        string? normalizedSolutionPath,
        bool isDockedMfdSolutionExplorerTree,
        IHost host)
    {
        var plan = SolutionLoadUiApplyProjection.Create(
            originalPath,
            normalizedSolutionPath,
            isDockedMfdSolutionExplorerTree);

        host.ResetEditorSessionForNewSolution();
        workspace.SelectedSolutionItem = null;
        workspace.SolutionPath = plan.NormalizedSolutionPath;
        workspace.SolutionRoots.Clear();
        workspace.SolutionRoots.Add(root);
        host.AfterSolutionApplied(plan.InitialMfdPage);
    }
}

using CascadeIDE.Contracts;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Shell.Application;

/// <summary>Видимость страниц вторичной оболочки Mfd при текущих флагах возможностей/доков (ADR 0021).</summary>
[ComputingUnit]
public static class MfdShellPageAllowanceProjection
{
    /// <summary>Порядок обхода при выборе первой доступной страницы (как в главном окне до выноса политики).</summary>
    public static readonly MfdShellPage[] PageOrder =
    [
        MfdShellPage.WorkspaceHealth,
        MfdShellPage.SolutionExplorer,
        MfdShellPage.RelatedFiles,
        MfdShellPage.MarkdownPreview,
        MfdShellPage.HybridIndex,
        MfdShellPage.WebAiPortal,
        MfdShellPage.Editor,
        MfdShellPage.Chat,
        MfdShellPage.AiChatSettings,
        MfdShellPage.EnvironmentReadiness,
        MfdShellPage.Terminal,
        MfdShellPage.Build,
        MfdShellPage.Problems,
        MfdShellPage.Git,
        MfdShellPage.Events,
        MfdShellPage.Tests,
        MfdShellPage.Hypotheses,
        MfdShellPage.DebugStack,
    ];

    public readonly record struct Snapshot(
        bool ShowIdeHealthMfdPage,
        bool IsDockedMfdSolutionExplorerTree,
        bool IsTerminalVisible,
        bool IsBuildOutputVisible,
        bool IsProblemsPanelVisible,
        bool IsGitPanelVisible,
        bool InstrumentationTabs,
        bool HypothesesTab,
        bool IsIntercomPrimaryWorkSurface);

    public static bool IsAllowed(MfdShellPage page, Snapshot s) => page switch
    {
        MfdShellPage.WorkspaceHealth => s.ShowIdeHealthMfdPage,
        MfdShellPage.SolutionExplorer => s.IsDockedMfdSolutionExplorerTree,
        MfdShellPage.RelatedFiles => true,
        MfdShellPage.MarkdownPreview => true,
        MfdShellPage.HybridIndex => true,
        MfdShellPage.WebAiPortal => true,
        MfdShellPage.Editor => s.IsIntercomPrimaryWorkSurface,
        MfdShellPage.Chat => !s.IsIntercomPrimaryWorkSurface,
        MfdShellPage.AiChatSettings => true,
        MfdShellPage.EnvironmentReadiness => true,
        MfdShellPage.Terminal => s.IsTerminalVisible,
        MfdShellPage.Build => s.IsBuildOutputVisible,
        MfdShellPage.Problems => s.IsProblemsPanelVisible,
        MfdShellPage.Git => s.IsGitPanelVisible,
        MfdShellPage.Events or MfdShellPage.Tests or MfdShellPage.DebugStack => s.InstrumentationTabs,
        MfdShellPage.Hypotheses => s.HypothesesTab,
        _ => false,
    };

    public static MfdShellPage FirstAllowedOrChat(Snapshot s)
    {
        foreach (var p in PageOrder)
        {
            if (IsAllowed(p, s))
                return p;
        }

        return MfdShellPage.Chat;
    }
}

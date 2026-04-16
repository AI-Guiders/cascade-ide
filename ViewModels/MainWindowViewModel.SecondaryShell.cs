using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Вторичный контур оболочки: одна активная страница; навигация — команды и палитра. Якорь на экране задаётся presentation (зона Mfd в main и/или окно-хост).</summary>
public partial class MainWindowViewModel
{
    /// <summary>Дерево решения: показывать при развёрнутом регионе MFD или на странице <see cref="SecondaryShellPage.SolutionExplorer"/>.</summary>
    public bool IsSolutionExplorerTreeChromeVisible =>
        IsMfdRegionExpanded || CurrentSecondaryShellPage == SecondaryShellPage.SolutionExplorer;

    /// <summary>Детерминированный порядок обхода при выборе первой доступной страницы.</summary>
    internal static readonly SecondaryShellPage[] SecondaryShellPageOrder =
    [
        SecondaryShellPage.WorkspaceHealth,
        SecondaryShellPage.SolutionExplorer,
        SecondaryShellPage.Chat,
        SecondaryShellPage.AiChatSettings,
        SecondaryShellPage.EnvironmentReadiness,
        SecondaryShellPage.Terminal,
        SecondaryShellPage.Build,
        SecondaryShellPage.Problems,
        SecondaryShellPage.Git,
        SecondaryShellPage.Events,
        SecondaryShellPage.Tests,
        SecondaryShellPage.Hypotheses,
        SecondaryShellPage.DebugStack,
    ];

    /// <summary>MCP и палитра: перейти на страницу вторичного контура, если она разрешена пресетом.</summary>
    public void TryNavigateToSecondaryShellPage(SecondaryShellPage page)
    {
        EnsureSecondaryShellSurfaceForLayout();
        if (IsSecondaryShellPageAllowed(page))
            CurrentSecondaryShellPage = page;
        else
            CoerceSecondaryShellPageToAllowed();
    }

    /// <summary>
    /// Любая страница вторичного контура должна открываться с учетом активной раскладки:
    /// при пресете с вынесенным MFD поднимаем/фокусируем TopLevel-хост.
    /// </summary>
    private void EnsureSecondaryShellSurfaceForLayout()
    {
        if (PresentationRequestsMfdHostWindow)
            RequestToggleMfdHostWindow?.Invoke();
    }

    private bool IsSecondaryShellPageAllowed(SecondaryShellPage page) => page switch
    {
        SecondaryShellPage.WorkspaceHealth => ShowWorkspaceHealthSecondaryPage,
        SecondaryShellPage.SolutionExplorer => true,
        SecondaryShellPage.Chat => true,
        SecondaryShellPage.AiChatSettings => true,
        SecondaryShellPage.EnvironmentReadiness => true,
        SecondaryShellPage.Terminal => IsTerminalVisible,
        SecondaryShellPage.Build => IsBuildOutputVisible,
        SecondaryShellPage.Problems => IsProblemsPanelVisible,
        SecondaryShellPage.Git => IsGitPanelVisible,
        SecondaryShellPage.Events or SecondaryShellPage.Tests or SecondaryShellPage.DebugStack => InstrumentationTabs,
        SecondaryShellPage.Hypotheses => HypothesesTab,
        _ => false,
    };

    private SecondaryShellPage GetFirstAllowedSecondaryShellPage()
    {
        foreach (var p in SecondaryShellPageOrder)
        {
            if (IsSecondaryShellPageAllowed(p))
                return p;
        }

        return SecondaryShellPage.Chat;
    }

    private void CoerceSecondaryShellPageToAllowed()
    {
        if (IsSecondaryShellPageAllowed(CurrentSecondaryShellPage))
            return;
        CurrentSecondaryShellPage = GetFirstAllowedSecondaryShellPage();
    }
}

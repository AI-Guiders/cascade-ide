using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Вторичный контур оболочки: одна активная страница; навигация — команды и палитра. Якорь на экране — пресет (v1: зона Mfd).</summary>
public partial class MainWindowViewModel
{
    /// <summary>Детерминированный порядок обхода при выборе первой доступной страницы.</summary>
    internal static readonly SecondaryShellPage[] SecondaryShellPageOrder =
    [
        SecondaryShellPage.WorkspaceHealth,
        SecondaryShellPage.Chat,
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
        if (IsSecondaryShellPageAllowed(page))
            CurrentSecondaryShellPage = page;
        else
            CoerceSecondaryShellPageToAllowed();
    }

    private bool IsSecondaryShellPageAllowed(SecondaryShellPage page) => page switch
    {
        SecondaryShellPage.WorkspaceHealth => ShowWorkspaceHealthSecondaryPage,
        SecondaryShellPage.Chat => true,
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

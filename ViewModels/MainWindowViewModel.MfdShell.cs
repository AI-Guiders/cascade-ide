using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Оболочка Mfd: одна активная страница; навигация — команды и палитра. Якорь на экране задаётся presentation (зона Mfd в main и/или окно-хост).</summary>
public partial class MainWindowViewModel
{
    /// <summary>Детерминированный порядок обхода при выборе первой доступной страницы.</summary>
    internal static readonly MfdShellPage[] MfdShellPageOrder =
    [
        MfdShellPage.WorkspaceHealth,
        MfdShellPage.SolutionExplorer,
        MfdShellPage.MarkdownPreview,
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

    /// <summary>MCP и палитра: перейти на страницу оболочки Mfd, если она разрешена пресетом.</summary>
    public void TryNavigateToMfdShellPage(MfdShellPage page)
    {
        EnsureMfdShellSurfaceForLayout();
        if (IsMfdShellPageAllowed(page))
            CurrentMfdShellPage = page;
        else
            CoerceMfdShellPageToAllowed();
    }

    /// <summary>
    /// Любая страница оболочки Mfd должна открываться с учетом активной раскладки:
    /// при пресете с вынесенным MFD поднимаем/фокусируем TopLevel-хост.
    /// </summary>
    private void EnsureMfdShellSurfaceForLayout()
    {
        if (PresentationRequestsMfdHostWindow)
            RequestToggleMfdHostWindow?.Invoke();
    }

    private bool IsMfdShellPageAllowed(MfdShellPage page) => page switch
    {
        MfdShellPage.WorkspaceHealth => ShowWorkspaceHealthMfdPage,
        MfdShellPage.SolutionExplorer => IsDockedMfdSolutionExplorerTree,
        MfdShellPage.MarkdownPreview => true,
        MfdShellPage.Chat => true,
        MfdShellPage.AiChatSettings => true,
        MfdShellPage.EnvironmentReadiness => true,
        MfdShellPage.Terminal => IsTerminalVisible,
        MfdShellPage.Build => IsBuildOutputVisible,
        MfdShellPage.Problems => IsProblemsPanelVisible,
        MfdShellPage.Git => IsGitPanelVisible,
        MfdShellPage.Events or MfdShellPage.Tests or MfdShellPage.DebugStack => InstrumentationTabs,
        MfdShellPage.Hypotheses => HypothesesTab,
        _ => false,
    };

    private MfdShellPage GetFirstAllowedMfdShellPage()
    {
        foreach (var p in MfdShellPageOrder)
        {
            if (IsMfdShellPageAllowed(p))
                return p;
        }

        return MfdShellPage.Chat;
    }

    private void CoerceMfdShellPageToAllowed()
    {
        if (IsMfdShellPageAllowed(CurrentMfdShellPage))
            return;
        CurrentMfdShellPage = GetFirstAllowedMfdShellPage();
    }
}

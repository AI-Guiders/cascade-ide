using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Models;
using CascadeIDE.Models.Shell;

namespace CascadeIDE.ViewModels;

/// <summary>Оболочка Mfd: одна активная страница; навигация — команды и палитра. Якорь на экране задаётся presentation (зона Mfd в main и/или окно-хост).</summary>
public partial class MainWindowViewModel
{
    /// <summary>Та же <see cref="CurrentMfdShellPage"/>, в контракте <see cref="IShellPage"/>; Pfd — <see cref="PfdLayout"/>.</summary>
    public IMfdShellPage CurrentMfdShellPageAsShell => new MfdShellPageDescriptor(CurrentMfdShellPage);

    /// <summary>v1 — единый расклад; будущий декларативный пресет — смена id (ADR 0088).</summary>
    public IPfdLayout PfdLayout => PfdLayouts.Default;

    internal static readonly MfdShellPage[] MfdShellPageOrder =
        MfdShellPageAllowanceProjection.PageOrder;

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

    private MfdShellPageAllowanceProjection.Snapshot MfdShellAllowanceSnapshot => new(
        ShowIdeHealthMfdPage,
        IsDockedMfdSolutionExplorerTree,
        IsTerminalVisible,
        IsBuildOutputVisible,
        IsProblemsPanelVisible,
        IsGitPanelVisible,
        InstrumentationTabs,
        HypothesesTab,
        PrimaryWorkSurface == PrimaryWorkSurfaceKind.Intercom);

    private bool IsMfdShellPageAllowed(MfdShellPage page) =>
        MfdShellPageAllowanceProjection.IsAllowed(page, MfdShellAllowanceSnapshot);

    private MfdShellPage GetFirstAllowedMfdShellPage() =>
        MfdShellPageAllowanceProjection.FirstAllowedOrChat(MfdShellAllowanceSnapshot);

    private void CoerceMfdShellPageToAllowed()
    {
        if (IsMfdShellPageAllowed(CurrentMfdShellPage))
            return;
        CurrentMfdShellPage = GetFirstAllowedMfdShellPage();
    }

    /// <summary>
    /// Контент страницы «Обозреватель» в <c>MfdShellView</c> — только если карта реально монтирует дерево в слот MFD
    /// (см. <see cref="IsDockedMfdSolutionExplorerTree"/>). Иначе, даже при устаревшем <see cref="CurrentMfdShellPage"/>, дубль с колонкой PFD не показываем.
    /// </summary>
    public bool IsMfdShellSolutionExplorerPageActive =>
        CurrentMfdShellPage == MfdShellPage.SolutionExplorer && IsDockedMfdSolutionExplorerTree;
}

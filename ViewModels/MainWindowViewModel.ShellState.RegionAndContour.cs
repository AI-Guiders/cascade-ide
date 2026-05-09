using CascadeIDE.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Часть <see cref="MainWindowViewModel"/>: регионы MainGrid и видимость страниц вторичного контура MFD.</summary>
public partial class MainWindowViewModel
{
    /// <summary>
    /// Intent геометрии: регион Mfd в <c>MainGrid</c> развёрнут (ширина по режиму) или свёрнут.
    /// Страница «Чат» — <see cref="MfdShellPage.Chat"/> через <see cref="CurrentMfdShellPage"/>, отдельно.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MfdRegionToggleButtonText))]
    [NotifyPropertyChangedFor(nameof(MfdRegionPixelWidth))]
    [NotifyPropertyChangedFor(nameof(IsMfdRegionVisible))]
    [NotifyPropertyChangedFor(nameof(IsMfdColumnVisible))]
    [NotifyPropertyChangedFor(nameof(IsSkiaZoneGeometryOverlayMfdVisible))]
    [NotifyPropertyChangedFor(nameof(IsPfdIdeHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(IsMfdIdeHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(PfdIdeHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdIdeHealthMountContext))]
    [NotifyCanExecuteChangedFor(nameof(ToggleMfdRegionExpandedCommand))]
    private bool _isMfdRegionExpanded = true;

    /// <summary>
    /// Intent геометрии: регион Pfd в <c>MainGrid</c> развёрнут (ширина по workspace/режиму) или свёрнут.
    /// Содержимое колонки PFD — по карте инструментов (runtime + Display / workspace.toml); см. IsDockedPfdSolutionExplorerTree / IsDockedPfdWorkspaceNavigationMap.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPfdColumnVisible))]
    [NotifyPropertyChangedFor(nameof(IsSkiaZoneGeometryOverlayPfdVisible))]
    [NotifyPropertyChangedFor(nameof(IsPfdIdeHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(PfdIdeHealthMountContext))]
    [NotifyCanExecuteChangedFor(nameof(TogglePfdRegionExpandedCommand))]
    private bool _isPfdRegionExpanded = true;

    /// <summary>Страница «Терминал» в колонке MFD (меню Вид → Терминал); телеметрия полосы Power при необходимости.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTelemetryHiddenHint))]
    [NotifyPropertyChangedFor(nameof(TelemetryButtonText))]
    [NotifyPropertyChangedFor(nameof(IsMfdContourContentVisible))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceBottomChrome))]
    private bool _isTerminalVisible;

    /// <summary>Страница «Git» в колонке MFD (меню Вид → Git).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMfdContourContentVisible))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceBottomChrome))]
    private bool _isGitPanelVisible;

    /// <summary>Страница «Вывод сборки» в колонке MFD (меню Вид → вывод сборки).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMfdContourContentVisible))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceBottomChrome))]
    private bool _isBuildOutputVisible;

    /// <summary>Док инструментирования в колонке MFD: вкладки «События / Тесты / …» (сохраняется в настройках).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InstrumentationTabs))]
    [NotifyPropertyChangedFor(nameof(HypothesesTab))]
    [NotifyPropertyChangedFor(nameof(IsMfdContourContentVisible))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceBottomChrome))]
    private bool _isInstrumentationDockVisible = true;

    /// <summary>Какая страница показана в оболочке Mfd (без TabControl; v1 — колонка зоны Mfd).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMfdShellSolutionExplorerPageActive))]
    [NotifyPropertyChangedFor(nameof(CurrentMfdShellPageAsShell))]
    private MfdShellPage _currentMfdShellPage = MfdShellPage.Terminal;
}

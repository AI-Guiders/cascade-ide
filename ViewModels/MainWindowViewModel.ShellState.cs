using System.Collections.ObjectModel;
using CascadeIDE.Models;
using CascadeIDE.Models.Shell;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

public enum CommandPaletteHost
{
    MainWindow,
    PfdHost,
    MfdHost,
    /// <summary>Окно сплита P+M — тот же <see cref="MainWindowViewModel"/>, отдельный TopLevel (ADR 0017).</summary>
    PmSplitHost,
}

/// <summary>
/// Состояние раскладки главного окна: три зоны внимания в <c>MainGrid</c> (PFD · Forward · MFD), см. ADR 0021 и <c>docs/ux/cascade-ide-ui-layout-v1.md</c>.
/// Терминал, сборка, Git и пр. — во вторичном контуре колонки MFD (<c>MfdShellView</c> / <c>MfdShellPageStack</c>); отдельной строки «нижней панели» на всю ширину под сеткой нет.
/// Режим ИИ и облачные ключи — <c>MainWindowViewModel.ShellState.AiProviders.cs</c>; чат и MCP/ACP — <c>MainWindowViewModel.ShellState.ChatAndSessionConfig.cs</c>; полоса агента / тесты для IDE Health — <c>MainWindowViewModel.ShellState.AutonomousAgentStripe.cs</c>.
/// </summary>
public partial class MainWindowViewModel
{
    [ObservableProperty]
    private CommandPaletteHost _commandPaletteHost = CommandPaletteHost.MainWindow;

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
    [NotifyPropertyChangedFor(nameof(IsBottomPanelVisible))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceBottomChrome))]
    private bool _isTerminalVisible;

    /// <summary>Страница «Git» в колонке MFD (меню Вид → Git).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBottomPanelVisible))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceBottomChrome))]
    private bool _isGitPanelVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UiModeFamily))]
    [NotifyPropertyChangedFor(nameof(MfdRegionPixelWidth))]
    [NotifyPropertyChangedFor(nameof(IsMfdRegionVisible))]
    [NotifyPropertyChangedFor(nameof(IsMfdColumnVisible))]
    [NotifyPropertyChangedFor(nameof(IsSkiaZoneGeometryOverlayPfdVisible))]
    [NotifyPropertyChangedFor(nameof(IsSkiaZoneGeometryOverlayForwardVisible))]
    [NotifyPropertyChangedFor(nameof(IsSkiaZoneGeometryOverlayMfdVisible))]
    [NotifyPropertyChangedFor(nameof(UseSkiaInstrumentMount))]
    [NotifyPropertyChangedFor(nameof(IsPfdIdeHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(IsMfdIdeHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(IsMfdHostWindowIdeHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(PfdIdeHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdIdeHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(ShowTaskBar))]
    [NotifyPropertyChangedFor(nameof(ShowIdeHealthStrip))]
    [NotifyPropertyChangedFor(nameof(ShowIdeHealthMfdPage))]
    [NotifyPropertyChangedFor(nameof(ShowEicasAlertsBar))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(IdeHealthStripSurface))]
    [NotifyPropertyChangedFor(nameof(IdeHealthContentRepresentation))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceBottomChrome))]
    [NotifyPropertyChangedFor(nameof(QuickActions))]
    [NotifyPropertyChangedFor(nameof(ShowAgentOperations))]
    [NotifyPropertyChangedFor(nameof(AgentTrace))]
    [NotifyPropertyChangedFor(nameof(AutonomousAgentTelemetry))]
    [NotifyPropertyChangedFor(nameof(IdeHealthOnTerminalTab))]
    [NotifyPropertyChangedFor(nameof(ShowSafetyControls))]
    [NotifyPropertyChangedFor(nameof(ShowTelemetryHiddenHint))]
    [NotifyPropertyChangedFor(nameof(TelemetryButtonText))]
    [NotifyPropertyChangedFor(nameof(ShowEditorGroup2))]
    [NotifyPropertyChangedFor(nameof(ShowEditorGroup3))]
    [NotifyPropertyChangedFor(nameof(IsRiskCardVisible))]
    [NotifyPropertyChangedFor(nameof(IsResultCardVisible))]
    [NotifyPropertyChangedFor(nameof(AgentOperationsPanel))]
    [NotifyPropertyChangedFor(nameof(InstrumentationTabs))]
    [NotifyPropertyChangedFor(nameof(HypothesesTab))]
    [NotifyPropertyChangedFor(nameof(ShowInstrumentationLayoutMenu))]
    [NotifyPropertyChangedFor(nameof(IsBottomPanelVisible))]
    [NotifyPropertyChangedFor(nameof(IsProblemsPanelVisible))]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    private string _uiMode = "Balanced";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEditorGroup2))]
    [NotifyPropertyChangedFor(nameof(ShowEditorGroup3))]
    private int _editorGroupCount = 1;

    /// <summary>Снимок раскладки UI (JSON), полоса Workspace Health в Power.</summary>
    [ObservableProperty]
    private string _workspaceSnapshotJson = "";

    public ObservableCollection<FocusPlanItemViewModel> FocusPlanItems { get; } = [];

    /// <summary>Какая страница показана в оболочке Mfd (без TabControl; v1 — колонка зоны Mfd).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMfdShellSolutionExplorerPageActive))]
    [NotifyPropertyChangedFor(nameof(CurrentMfdShellPageAsShell))]
    private MfdShellPage _currentMfdShellPage = MfdShellPage.Terminal;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuildSolutionCommand))]
    [NotifyPropertyChangedFor(nameof(IdeHealthBuildText))]
    [NotifyPropertyChangedFor(nameof(IdeHealthBuildCockpitShort))]
    [NotifyPropertyChangedFor(nameof(IdeHealthMountPayload))]
    [NotifyPropertyChangedFor(nameof(PfdIdeHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdIdeHealthMountContext))]
    private bool _isBuilding;

    /// <summary>Страница «Вывод сборки» в колонке MFD (меню Вид → вывод сборки).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBottomPanelVisible))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceBottomChrome))]
    private bool _isBuildOutputVisible;

    /// <summary>Док инструментирования в колонке MFD: вкладки «События / Тесты / …» (сохраняется в настройках).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InstrumentationTabs))]
    [NotifyPropertyChangedFor(nameof(HypothesesTab))]
    [NotifyPropertyChangedFor(nameof(IsBottomPanelVisible))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceBottomChrome))]
    private bool _isInstrumentationDockVisible = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallModelCommand))]
    private string _modelToInstall = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallModelCommand))]
    private bool _isPullingModel;

    [ObservableProperty]
    private string _pullModelProgress = "";

    /// <summary>Mermaid/PlantUML в превью Markdown через Kroki (текст диаграммы отправляется на сервер).</summary>
    [ObservableProperty]
    private bool _markdownKrokiEnabled = true;

    /// <summary>Базовый URL Kroki для превью диаграмм.</summary>
    [ObservableProperty]
    private string _markdownKrokiBaseUrl = "https://kroki.io";

    public string EditorTextGroup2 => Documents.SelectedDocumentGroup2?.Content ?? "";

    public string EditorTextGroup3 => Documents.SelectedDocumentGroup3?.Content ?? "";
}

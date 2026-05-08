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
/// Раскладка панелей, нижняя зона, Workspace Health / автономный агент, ключи провайдеров и чата.
/// Режим ИИ и поля облачных ключей — partial <c>MainWindowViewModel.ShellState.AiProviders.cs</c>.
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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTelemetryHiddenHint))]
    [NotifyPropertyChangedFor(nameof(TelemetryButtonText))]
    [NotifyPropertyChangedFor(nameof(IsBottomPanelVisible))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceBottomChrome))]
    private bool _isTerminalVisible;

    /// <summary>Вкладка «Git» в нижней панели (Вид → Git).</summary>
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

    [ObservableProperty]
    private string _activeTaskTitle = "Нет активной задачи";

    [ObservableProperty]
    private string _activeTaskStatus = "Ожидание";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActiveTaskProgressVisible))]
    private int _activeTaskProgress;

    [ObservableProperty]
    private string _activeObjective = "Нет активной операции агента.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRiskSummaryVisible))]
    [NotifyPropertyChangedFor(nameof(IsRiskCardVisible))]
    private string _riskSummary = "Риски не зафиксированы.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsResultSummaryVisible))]
    [NotifyPropertyChangedFor(nameof(IsResultCardVisible))]
    private string _resultSummary = "Результатов пока нет.";

    [ObservableProperty]
    private string _nextActionSummary = "Ожидание следующего шага.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSafetyL1))]
    [NotifyPropertyChangedFor(nameof(IsSafetyL2))]
    [NotifyPropertyChangedFor(nameof(IsSafetyL3))]
    [NotifyPropertyChangedFor(nameof(SafetyLevelDescription))]
    [NotifyPropertyChangedFor(nameof(SafetyL1Opacity))]
    [NotifyPropertyChangedFor(nameof(SafetyL2Opacity))]
    [NotifyPropertyChangedFor(nameof(SafetyL3Opacity))]
    [NotifyPropertyChangedFor(nameof(IdeHealthMountPayload))]
    [NotifyPropertyChangedFor(nameof(IsPfdIdeHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(IsMfdIdeHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(PfdIdeHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdIdeHealthMountContext))]
    private string _safetyLevel = "L2";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocBadgeVisible))]
    [NotifyPropertyChangedFor(nameof(LocBadgeSummary))]
    private int _locBadge;

    /// <summary>Подпись уровня Low/Medium/High для <see cref="LocBadgeSummary"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LocBadgeSummary))]
    private string _locTierLabel = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImpactedTestsBadgeVisible))]
    [NotifyPropertyChangedFor(nameof(IdeHealthTestsText))]
    [NotifyPropertyChangedFor(nameof(IdeHealthTestsCockpitShort))]
    [NotifyPropertyChangedFor(nameof(IdeHealthMountPayload))]
    [NotifyPropertyChangedFor(nameof(PfdIdeHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdIdeHealthMountContext))]
    private int _impactedTestsBadge;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IdeHealthTestsText))]
    [NotifyPropertyChangedFor(nameof(IdeHealthTestsCockpitShort))]
    [NotifyPropertyChangedFor(nameof(IdeHealthMountPayload))]
    [NotifyPropertyChangedFor(nameof(PfdIdeHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdIdeHealthMountContext))]
    private string _lastTestSummary = "";

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBottomPanelVisible))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceBottomChrome))]
    private bool _isBuildOutputVisible;

    /// <summary>Вкладки «События / Тесты / …» (сохраняется в настройках).</summary>
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

    [ObservableProperty]
    private string _sendMessageKey = "Enter";

    /// <summary>Показывать thinking/reasoning в истории после завершения ответа.</summary>
    [ObservableProperty]
    private bool _showThinkingInHistory = true;

    /// <summary>Отправлять только диагностики и сигнатуры текущего файла (минимальный контекст).</summary>
    [ObservableProperty]
    private bool _useMinimizedContext = true;

    /// <summary>
    /// JSON-конфиг внешних MCP-серверов (stdio) для автономного режима.
    /// Формат — как в <see cref="McpSettings.ExternalServersJson"/>.
    /// </summary>
    [ObservableProperty]
    private string _externalMcpServersJson = "[]";

    /// <summary>Подмешивать stdio MCP текущей IDE (<c>cascade-ide</c>) в <c>session/new</c> для Cursor ACP (ADR 0048 §7).</summary>
    [ObservableProperty]
    private bool _acpAutoInjectIdeMcp = true;

    /// <summary>Mermaid/PlantUML в превью Markdown через Kroki (текст диаграммы отправляется на сервер).</summary>
    [ObservableProperty]
    private bool _markdownKrokiEnabled = true;

    /// <summary>Базовый URL Kroki для превью диаграмм.</summary>
    [ObservableProperty]
    private string _markdownKrokiBaseUrl = "https://kroki.io";

    public string EditorTextGroup2 => Documents.SelectedDocumentGroup2?.Content ?? "";

    public string EditorTextGroup3 => Documents.SelectedDocumentGroup3?.Content ?? "";

    public static readonly IReadOnlyList<string> SendMessageKeyOptions = ["Enter", "Ctrl+Enter", "Shift+Enter"];

    public IReadOnlyList<string> SendMessageKeyOptionsList => SendMessageKeyOptions;
}

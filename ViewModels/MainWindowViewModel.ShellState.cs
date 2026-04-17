using System.Collections.ObjectModel;
using CascadeIDE.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Раскладка панелей, нижняя зона, Workspace Health / автономный агент, ключи провайдеров и чата.</summary>
public partial class MainWindowViewModel
{
    /// <summary>
    /// Intent геометрии: регион Mfd в <c>MainGrid</c> развёрнут (ширина по режиму) или свёрнут.
    /// Страница «Чат» — <see cref="SecondaryShellPage.Chat"/> через <see cref="CurrentSecondaryShellPage"/>, отдельно.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MfdRegionToggleButtonText))]
    [NotifyPropertyChangedFor(nameof(MfdRegionPixelWidth))]
    [NotifyPropertyChangedFor(nameof(IsMfdRegionVisible))]
    [NotifyPropertyChangedFor(nameof(IsMfdColumnVisible))]
    [NotifyPropertyChangedFor(nameof(IsSkiaZonePreviewMfdVisible))]
    [NotifyPropertyChangedFor(nameof(IsPfdWorkspaceHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(IsMfdWorkspaceHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(PfdWorkspaceHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdWorkspaceHealthMountContext))]
    [NotifyCanExecuteChangedFor(nameof(ToggleMfdRegionExpandedCommand))]
    private bool _isMfdRegionExpanded = true;

    /// <summary>
    /// Intent геометрии: регион Pfd в <c>MainGrid</c> развёрнут (ширина по workspace/режиму) или свёрнут.
    /// Содержимое колонки PFD — по карте инструментов (runtime + Display / workspace.toml); см. IsDockedPfdSolutionExplorerTree / IsDockedPfdWorkspaceNavigationMap.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPfdColumnVisible))]
    [NotifyPropertyChangedFor(nameof(IsSkiaZonePreviewPfdVisible))]
    [NotifyPropertyChangedFor(nameof(IsPfdWorkspaceHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(PfdWorkspaceHealthMountContext))]
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
    [NotifyPropertyChangedFor(nameof(IsSkiaZonePreviewPfdVisible))]
    [NotifyPropertyChangedFor(nameof(IsSkiaZonePreviewForwardVisible))]
    [NotifyPropertyChangedFor(nameof(IsSkiaZonePreviewMfdVisible))]
    [NotifyPropertyChangedFor(nameof(UseSkiaInstrumentMount))]
    [NotifyPropertyChangedFor(nameof(IsPfdWorkspaceHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(IsMfdWorkspaceHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(IsMfdHostWindowWorkspaceHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(PfdWorkspaceHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdWorkspaceHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(ShowTaskBar))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceHealthStrip))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceHealthSecondaryPage))]
    [NotifyPropertyChangedFor(nameof(ShowEicasAlertsBar))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceChromeBand))]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthUiSurface))]
    [NotifyPropertyChangedFor(nameof(ShowWorkspaceBottomChrome))]
    [NotifyPropertyChangedFor(nameof(QuickActions))]
    [NotifyPropertyChangedFor(nameof(ShowAgentOperations))]
    [NotifyPropertyChangedFor(nameof(AgentTrace))]
    [NotifyPropertyChangedFor(nameof(AutonomousAgentTelemetry))]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthOnTerminalTab))]
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
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthMountPayload))]
    [NotifyPropertyChangedFor(nameof(IsPfdWorkspaceHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(IsMfdWorkspaceHealthMountVisible))]
    [NotifyPropertyChangedFor(nameof(PfdWorkspaceHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdWorkspaceHealthMountContext))]
    private string _safetyLevel = "L2";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsComplexityBadgeVisible))]
    private int _complexityBadge;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImpactedTestsBadgeVisible))]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthTestsText))]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthTestsCockpitShort))]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthMountPayload))]
    [NotifyPropertyChangedFor(nameof(PfdWorkspaceHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdWorkspaceHealthMountContext))]
    private int _impactedTestsBadge;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthTestsText))]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthTestsCockpitShort))]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthMountPayload))]
    [NotifyPropertyChangedFor(nameof(PfdWorkspaceHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdWorkspaceHealthMountContext))]
    private string _lastTestSummary = "";

    /// <summary>Снимок раскладки UI (JSON), полоса Workspace Health в Power.</summary>
    [ObservableProperty]
    private string _workspaceSnapshotJson = "";

    public ObservableCollection<FocusPlanItemViewModel> FocusPlanItems { get; } = [];

    /// <summary>Какая страница показана во вторичном контуре оболочки (без TabControl; v1 — колонка зоны Mfd).</summary>
    [ObservableProperty]
    private SecondaryShellPage _currentSecondaryShellPage = SecondaryShellPage.Terminal;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuildSolutionCommand))]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthBuildText))]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthBuildCockpitShort))]
    [NotifyPropertyChangedFor(nameof(WorkspaceHealthMountPayload))]
    [NotifyPropertyChangedFor(nameof(PfdWorkspaceHealthMountContext))]
    [NotifyPropertyChangedFor(nameof(MfdWorkspaceHealthMountContext))]
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

    /// <summary>После отправки user-сообщения не вызывать встроенный LLM; ответы добавлять через MCP (<c>send_chat</c> с <c>role=assistant</c>).</summary>
    [ObservableProperty]
    private bool _chatMcpOnly;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOllamaSelected))]
    [NotifyPropertyChangedFor(nameof(IsAnthropicSelected))]
    [NotifyPropertyChangedFor(nameof(IsOpenAiSelected))]
    [NotifyPropertyChangedFor(nameof(IsDeepSeekSelected))]
    [NotifyPropertyChangedFor(nameof(IsCursorAcpSelected))]
    [NotifyPropertyChangedFor(nameof(CurrentModelDisplay))]
    private string _activeAiProvider = "Ollama";

    public bool IsOllamaSelected => ActiveAiProvider == "Ollama";
    public bool IsAnthropicSelected => ActiveAiProvider == "Anthropic";
    public bool IsOpenAiSelected => ActiveAiProvider == "OpenAI";
    public bool IsDeepSeekSelected => ActiveAiProvider == "DeepSeek";
    public bool IsCursorAcpSelected => ActiveAiProvider == "CursorACP";

    /// <summary>Отображаемое имя модели (для облачных — из настроек).</summary>
    public string CurrentModelDisplay => ActiveAiProvider switch
    {
        "Anthropic" => _settings.Ai.AnthropicModel,
        "OpenAI" => _settings.Ai.OpenAiModel,
        "DeepSeek" => _settings.Ai.DeepSeekModel,
        "CursorACP" => "Cursor ACP",
        _ => SelectedOllamaModel ?? _settings.Ai.DefaultOllamaModel ?? ""
    };

    [ObservableProperty]
    private string _anthropicApiKey = "";

    [ObservableProperty]
    private string _openAiApiKey = "";

    [ObservableProperty]
    private string _deepSeekApiKey = "";

    public string EditorTextGroup2 => Documents.SelectedDocumentGroup2?.Content ?? "";

    public string EditorTextGroup3 => Documents.SelectedDocumentGroup3?.Content ?? "";

    public static readonly IReadOnlyList<string> SendMessageKeyOptions = ["Enter", "Ctrl+Enter", "Shift+Enter"];

    public IReadOnlyList<string> SendMessageKeyOptionsList => SendMessageKeyOptions;
}

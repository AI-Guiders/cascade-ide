using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Раскладка панелей, нижняя зона, телеметрия Power/агента, ключи провайдеров и чата.</summary>
public partial class MainWindowViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChatPanelToggleButtonText))]
    private bool _isChatPanelExpanded = true;

    [ObservableProperty]
    private bool _isSolutionExplorerVisible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowTelemetryHiddenHint))]
    [NotifyPropertyChangedFor(nameof(TelemetryButtonText))]
    [NotifyPropertyChangedFor(nameof(IsBottomPanelVisible))]
    private bool _isTerminalVisible;

    /// <summary>Вкладка «Git» в нижней панели (Вид → Git).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBottomPanelVisible))]
    private bool _isGitPanelVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFocusMode))]
    [NotifyPropertyChangedFor(nameof(IsBalancedMode))]
    [NotifyPropertyChangedFor(nameof(IsPowerMode))]
    [NotifyPropertyChangedFor(nameof(ShowTaskBar))]
    [NotifyPropertyChangedFor(nameof(ShowTelemetryStrip))]
    [NotifyPropertyChangedFor(nameof(ShowQuickActions))]
    [NotifyPropertyChangedFor(nameof(ShowAgentOperations))]
    [NotifyPropertyChangedFor(nameof(ShowAgentTrace))]
    [NotifyPropertyChangedFor(nameof(ShowPowerTelemetry))]
    [NotifyPropertyChangedFor(nameof(ShowPowerTelemetryOnTerminalTab))]
    [NotifyPropertyChangedFor(nameof(ShowSafetyControls))]
    [NotifyPropertyChangedFor(nameof(ShowTelemetryHiddenHint))]
    [NotifyPropertyChangedFor(nameof(TelemetryButtonText))]
    [NotifyPropertyChangedFor(nameof(ShowEditorGroup2))]
    [NotifyPropertyChangedFor(nameof(ShowEditorGroup3))]
    [NotifyPropertyChangedFor(nameof(IsRiskCardVisible))]
    [NotifyPropertyChangedFor(nameof(IsResultCardVisible))]
    [NotifyPropertyChangedFor(nameof(ShowAgentOperationsBlock))]
    [NotifyPropertyChangedFor(nameof(ShowInstrumentationTabs))]
    [NotifyPropertyChangedFor(nameof(ShowInstrumentationLayoutMenu))]
    [NotifyPropertyChangedFor(nameof(IsBottomPanelVisible))]
    [NotifyPropertyChangedFor(nameof(WindowTitle))]
    [NotifyPropertyChangedFor(nameof(MainWorkspaceTelemetryColumnSpan))]
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
    private string _safetyLevel = "L2";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsComplexityBadgeVisible))]
    private int _complexityBadge;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImpactedTestsBadgeVisible))]
    [NotifyPropertyChangedFor(nameof(TelemetryTestsText))]
    [NotifyPropertyChangedFor(nameof(TelemetryTestsCockpitShort))]
    private int _impactedTestsBadge;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TelemetryTestsText))]
    [NotifyPropertyChangedFor(nameof(TelemetryTestsCockpitShort))]
    private string _lastTestSummary = "";

    /// <summary>Снимок раскладки UI (JSON), полоса телеметрии в Power.</summary>
    [ObservableProperty]
    private string _workspaceSnapshotJson = "";

    public ObservableCollection<FocusPlanItemViewModel> FocusPlanItems { get; } = [];

    /// <summary>0 = Terminal, 1 = Build, 2 = Git, 3 = Events, 4 = Tests, 5 = Debug.</summary>
    [ObservableProperty]
    private int _bottomPanelTabIndex;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuildSolutionCommand))]
    [NotifyPropertyChangedFor(nameof(TelemetryBuildText))]
    [NotifyPropertyChangedFor(nameof(TelemetryBuildCockpitShort))]
    private bool _isBuilding;

    [ObservableProperty]
    private bool _isBuildOutputVisible;

    /// <summary>Вкладки «События / Тесты / Отладка» в Balanced/Power (сохраняется в настройках).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowInstrumentationTabs))]
    [NotifyPropertyChangedFor(nameof(IsBottomPanelVisible))]
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

    /// <summary>Отправлять только диагностики и сигнатуры текущего файла (минимальный контекст).</summary>
    [ObservableProperty]
    private bool _useMinimizedContext = true;

    /// <summary>Включить MCP-сервер при старте с --mcp-stdio (сохраняется в настройках, действует при следующем запуске).</summary>
    [ObservableProperty]
    private bool _ideMcpServerEnabled = true;

    /// <summary>
    /// JSON-конфиг внешних MCP-серверов (stdio) для автономного режима.
    /// Формат — как в <see cref="CascadeIdeSettings.ExternalMcpServersJson"/>.
    /// </summary>
    [ObservableProperty]
    private string _externalMcpServersJson = "[]";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOllamaSelected))]
    [NotifyPropertyChangedFor(nameof(IsAnthropicSelected))]
    [NotifyPropertyChangedFor(nameof(IsOpenAiSelected))]
    [NotifyPropertyChangedFor(nameof(IsDeepSeekSelected))]
    [NotifyPropertyChangedFor(nameof(CurrentModelDisplay))]
    private string _activeAiProvider = "Ollama";

    public bool IsOllamaSelected => ActiveAiProvider == "Ollama";
    public bool IsAnthropicSelected => ActiveAiProvider == "Anthropic";
    public bool IsOpenAiSelected => ActiveAiProvider == "OpenAI";
    public bool IsDeepSeekSelected => ActiveAiProvider == "DeepSeek";

    /// <summary>Отображаемое имя модели (для облачных — из настроек).</summary>
    public string CurrentModelDisplay => ActiveAiProvider switch
    {
        "Anthropic" => _settings.AnthropicModelId,
        "OpenAI" => _settings.OpenAiModelId,
        "DeepSeek" => _settings.DeepSeekModelId,
        _ => SelectedOllamaModel ?? _settings.PreferredOllamaModel ?? ""
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

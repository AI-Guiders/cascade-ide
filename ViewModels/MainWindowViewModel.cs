using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using Avalonia.Threading;
using CascadeIDE.Features.Build;
using CascadeIDE.Features.Chat;
using CascadeIDE.Features.AutonomousAgent;
using CascadeIDE.Features.Git;
using CascadeIDE.Features.Instrumentation;
using CascadeIDE.Features.Terminal;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;
using CascadeIDE.Services.Lsp;
using CommunityToolkit.Mvvm.ComponentModel;
namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, Services.IIdeMcpActions, IAutonomousAgentSessionHost
{
    public const string InstallNewSentinel = "— Установить модель… —";

    private readonly Services.IOllamaService _ollama = new Services.OllamaService();
    private readonly Services.AiProviderManager _aiProviderManager;
    private readonly CascadeIdeSettings _settings = Services.SettingsService.Load();
    private AiKeys _aiKeys = Services.AiKeysStorage.Load();
    private CascadeIdeSettings? _lastSavedSettings;
    private AiKeys? _lastSavedAiKeys;

    private Func<int?, Services.EditorStateDto?>? _editorStateProvider;
    private Func<int, int, string?>? _editorContentRangeProvider;
    private Action<string, int, int, int, int, string>? _applyEditAction;
    private Action? _focusEditorAction;

    public static readonly IReadOnlyList<string> AiProviderKeys = ["Ollama", "Anthropic", "OpenAI", "DeepSeek"];
    public IReadOnlyList<string> AiProviderKeysList => AiProviderKeys;

    /// <summary>Варианты C# LSP (настройки; активен не более одного процесса).</summary>
    public IReadOnlyList<string> CSharpLspProviderOptionsList => CSharpLspProviderIds.All;

    public bool IsCSharpLspProcessSelected =>
        !string.Equals(_csharpLspProvider, CSharpLspProviderIds.ParseOnly, StringComparison.OrdinalIgnoreCase);

    private readonly Services.CSharpLanguageService _csharpLanguageService;
    private readonly Services.ContextMinimizer _contextMinimizer;
    private readonly Services.WorkspaceDiagnosticsCoordinator _workspaceDiagnostics;
    private CSharpLspDiagnosticsHost? _csharpLspHost;
    private readonly IdeMcpCommandExecutor _ideMcpExecutor;

    private Services.McpClientService _mcpClientService;
    private AutonomousAgentService _autonomousAgentService;

    public MainWindowViewModel()
    {
        Workspace = new SolutionWorkspaceViewModel();
        Chrome = new UiChromeViewModel();
        Documents = new DocumentsWorkspaceViewModel(this, Workspace, () => ReopenClosedDocumentCommand.NotifyCanExecuteChanged());
        Documents.PropertyChanged += OnDocumentsPropertyChanged;
        _csharpLanguageService = new Services.CSharpLanguageService();
        _contextMinimizer = new Services.ContextMinimizer(_csharpLanguageService);
        _aiProviderManager = new Services.AiProviderManager(_contextMinimizer, ResolveProvider);
        _ideMcpServerEnabled = _settings.IdeMcpServerEnabled;
        _externalMcpServersJson = _settings.ExternalMcpServersJson;
        _activeAiProvider = _settings.ActiveAiProvider;
        _anthropicApiKey = _aiKeys.AnthropicApiKey ?? "";
        _openAiApiKey = _aiKeys.OpenAiApiKey ?? "";
        _deepSeekApiKey = _aiKeys.DeepSeekApiKey ?? "";
        _isSolutionExplorerVisible = _settings.SolutionExplorerVisible;
        _isTerminalVisible = _settings.TerminalVisible;
        _isGitPanelVisible = _settings.GitPanelVisible;
        _isInstrumentationDockVisible = _settings.InstrumentationDockVisible;
        _uiMode = NormalizeUiMode(_settings.UiMode);
        InitializeAgentUiDefaults();
        RegisterAgentFeedHandlers();
        ApplyUiModeLayout(_uiMode, persist: false);
        if (IsPowerMode)
            Dispatcher.UIThread.Post(RefreshWorkspaceSnapshotCore, DispatcherPriority.Background);

        Documents.InitializeDock();

        _lastSavedSettings = (CascadeIdeSettings)_settings.Clone();
        _lastSavedAiKeys = (AiKeys)_aiKeys.Clone();

        BuildOutputPanel = new BuildOutputPanelViewModel();
        TerminalPanel = new TerminalPanelViewModel(() => Workspace.SolutionPath);
        GitPanel = new GitPanelViewModel(_gitRunner, GetWorkspacePath, this, LoadSolution, RefreshGitSummaryAsync);
        ChatPanel = new ChatPanelViewModel(
            _aiProviderManager,
            () => ActiveAiProvider,
            () => SelectedOllamaModel,
            () => UseMinimizedContext,
            () => CurrentFilePath,
            () => EditorText);
        InstrumentationPanel = new InstrumentationPanelViewModel();
        InstrumentationPanel.PropertyChanged += OnInstrumentationPanelPropertyChanged;

        ProblemsPanel = new ProblemsPanelViewModel(NavigateToProblemFromList);
        _workspaceDiagnostics = new Services.WorkspaceDiagnosticsCoordinator(_csharpLanguageService, ProblemsPanel);
        _workspaceDiagnostics.Attach(this);

        _csharpLspProvider = string.IsNullOrEmpty(_settings.CSharpLspProvider)
            ? CSharpLspProviderIds.ParseOnly
            : _settings.CSharpLspProvider;
        _csharpLspExecutable = _settings.CSharpLspExecutable ?? "";
        _csharpLspArguments = _settings.CSharpLspArguments ?? "";

        _mcpClientService = new Services.McpClientService(_settings.ExternalMcpServersJson);
        _autonomousAgentService = CreateAutonomousAgentService(_mcpClientService);
        Autonomous = new AutonomousAgentSessionViewModel(_autonomousAgentService, this);
        _ideMcpExecutor = new IdeMcpCommandExecutor(this);

        Workspace.PropertyChanged += (_, e) => OnWorkspacePropertyChanged(e.PropertyName);
    }

    /// <summary>Solution/workspace state and background loading.</summary>
    public SolutionWorkspaceViewModel Workspace { get; }

    /// <summary>Git-телеметрия полосы задач и bloom при смене UI-режима.</summary>
    public UiChromeViewModel Chrome { get; }

    /// <summary>Открытые файлы, группы редакторов и Dock.</summary>
    public DocumentsWorkspaceViewModel Documents { get; }

    /// <summary>Автономный агент (Power): цель, шаги, start/pause/resume.</summary>
    public AutonomousAgentSessionViewModel Autonomous { get; }

    private void OnDocumentsPropertyChanged(object? _, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null)
            return;
        OnPropertyChanged(e.PropertyName);
        if (e.PropertyName == nameof(DocumentsWorkspaceViewModel.SelectedDocumentGroup2))
            OnPropertyChanged(nameof(EditorTextGroup2));
        if (e.PropertyName == nameof(DocumentsWorkspaceViewModel.SelectedDocumentGroup3))
            OnPropertyChanged(nameof(EditorTextGroup3));
    }

    private void OnWorkspacePropertyChanged(string? propertyName)
    {
        switch (propertyName)
        {
            case nameof(SolutionWorkspaceViewModel.SolutionPath):
                OnPropertyChanged(nameof(McpFileBreakpointLinesInCurrentFile));
                OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
                BuildSolutionCommand.NotifyCanExecuteChanged();
                HandleSolutionPathChanged(Workspace.SolutionPath);
                break;
            case nameof(SolutionWorkspaceViewModel.SelectedSolutionItem):
                HandleSelectedSolutionItemChanged(Workspace.SelectedSolutionItem);
                break;
        }
    }

    /// <summary>Событие: перейти в активном редакторе на строку/колонку (1-based) после открытия файла.</summary>
    public event Action<int, int>? GotoActiveEditorLineColumnRequested;

    private void NavigateToProblemFromList(ProblemListItem item)
    {
        if (string.IsNullOrWhiteSpace(item.FilePath))
            return;
        Documents.OpenOrActivateDocument(item.FilePath);
        var line = item.Line;
        var col = item.Column;
        Dispatcher.UIThread.Post(() => GotoActiveEditorLineColumnRequested?.Invoke(line, col), DispatcherPriority.Loaded);
    }

    private AutonomousAgentService CreateAutonomousAgentService(Services.McpClientService mcpClientService) =>
        new AutonomousAgentService(
            _aiProviderManager,
            this,
            mcpClientService,
            () => ActiveAiProvider,
            () => SelectedOllamaModel,
            () => UseMinimizedContext,
            () => CurrentFilePath,
            () => EditorText,
            (kind, text, status, at) => InstrumentationPanel.AppendAgentTraceStep(kind, text, status, at),
            msg => Dispatcher.UIThread.Post(() => InstrumentationPanel.EventTimeline.Insert(0, msg)));

    /// <summary>
    /// Полоса телеметрии читает счётчики отладки с главного VM; при смене MCP-стека обновляем строки.
    /// </summary>
    private void OnInstrumentationPanelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(InstrumentationPanelViewModel.IsDebugPanelVisible))
            return;
        OnPropertyChanged(nameof(TelemetryDebugText));
        OnPropertyChanged(nameof(TelemetryDebugCockpitShort));
    }

    /// <summary>Вывод сборки (нижняя вкладка Build output).</summary>
    public BuildOutputPanelViewModel BuildOutputPanel { get; }

    /// <summary>Ошибки и предупреждения по открытым .cs (Roslyn).</summary>
    public ProblemsPanelViewModel ProblemsPanel { get; }

    /// <summary>Терминал (нижняя вкладка Terminal).</summary>
    public TerminalPanelViewModel TerminalPanel { get; }

    /// <summary>Панель Git (нижняя вкладка); состояние и команды вынесены из <see cref="MainWindowViewModel"/>.</summary>
    public GitPanelViewModel GitPanel { get; }

    /// <summary>Чат с LLM (правая колонка): история, ввод, отправка.</summary>
    public ChatPanelViewModel ChatPanel { get; }

    /// <summary>Инструментирование: трасса агента, события, тесты, стек MCP-отладки. В разметке — <c>DataContext="{Binding InstrumentationPanel}"</c>.</summary>
    public InstrumentationPanelViewModel InstrumentationPanel { get; }

    /// <summary>Общий экземпляр Roslyn для редактора, контекста чата и диагностик.</summary>
    public Services.CSharpLanguageService CSharpLanguage => _csharpLanguageService;

    /// <summary>Кэш диагностик по открытым .cs и панель Problems.</summary>
    public Services.WorkspaceDiagnosticsCoordinator WorkspaceDiagnostics => _workspaceDiagnostics;

    private void SaveSettingsIfChanged()
    {
        if (_lastSavedSettings is null || !_settings.Is(_lastSavedSettings))
        {
            Services.SettingsService.Save(_settings);
            _lastSavedSettings = (CascadeIdeSettings)_settings.Clone();
        }
    }

    private void SaveAiKeysIfChanged()
    {
        if (_lastSavedAiKeys is null || !_aiKeys.Is(_lastSavedAiKeys))
        {
            Services.AiKeysStorage.Save(_aiKeys);
            _lastSavedAiKeys = (AiKeys)_aiKeys.Clone();
        }
    }

    private (Services.IAiChatProvider? Provider, string Model) ResolveProvider(string providerKey)
    {
        var key = providerKey ?? _settings.ActiveAiProvider;
        return key switch
        {
            "Anthropic" => (new Services.AnthropicProvider(_aiKeys.AnthropicApiKey ?? "", _settings.AnthropicModelId), _settings.AnthropicModelId),
            "OpenAI" => (new Services.OpenAiCompatibleProvider(_settings.OpenAiBaseUrl, _aiKeys.OpenAiApiKey ?? "", _settings.OpenAiModelId), _settings.OpenAiModelId),
            "DeepSeek" => (new Services.OpenAiCompatibleProvider(_settings.DeepSeekBaseUrl, _aiKeys.DeepSeekApiKey ?? "", _settings.DeepSeekModelId), _settings.DeepSeekModelId),
            _ => (new Services.OllamaProvider(_ollama), SelectedOllamaModel ?? _settings.PreferredOllamaModel)
        };
    }

    public void SetEditorStateProvider(Func<int?, Services.EditorStateDto?> provider) => _editorStateProvider = provider;
    public void SetEditorContentRangeProvider(Func<int, int, string?> provider) => _editorContentRangeProvider = provider;
    public void SetApplyEdit(Action<string, int, int, int, int, string> action) => _applyEditAction = action;
    public void SetFocusEditor(Action action) => _focusEditorAction = action;

    /// <summary>Вызвать, чтобы показать диалог «Открыть решение» (View подставит реализацию).</summary>
    public Action? RequestOpenSolution { get; set; }
    /// <summary>Вызвать для закрытия окна (View подставит Close).</summary>
    public Action? RequestClose { get; set; }
    /// <summary>Показать «О программе» (View подставит диалог).</summary>
    public Action? RequestShowAbout { get; set; }
    /// <summary>Показать окно настроек (View подставит создание и Show).</summary>
    public Action? RequestOpenSettings { get; set; }
    /// <summary>Показать диалог выбора файла темы (.json). Возвращает путь к файлу или null.</summary>
    public Func<Task<string?>>? RequestOpenThemeFile { get; set; }
    /// <summary>Показать превью Markdown в отдельном окне (контент от агента).</summary>
    public Action<string, string>? RequestShowMarkdownPreviewWindow { get; set; }
    /// <summary>Показать превью текущего редактора в отдельном окне (живое обновление).</summary>
    public Action? RequestShowMarkdownPreviewForEditor { get; set; }
    /// <summary>Показать подтверждение пользователю. Возвращает "ok" или "cancel".</summary>
    public Func<string, CancellationToken, Task<string>>? RequestConfirmation { get; set; }
    /// <summary>Поставщик снимка дерева UI (View подставит вызов UiLayoutSnapshot.BuildJson).</summary>
    public Func<string>? GetUiLayoutProvider { get; set; }
    public Func<string>? GetColorsUnderCursorProvider { get; set; }
    public Func<string?, string>? GetControlAppearanceProvider { get; set; }
    public Func<string, string, string>? SetControlLayoutProvider { get; set; }
    public Func<string, string, string?, string?, string>? AddControlProvider { get; set; }
    public Func<string, string, string>? SetControlTextProvider { get; set; }
    public Func<string?, string>? ClickControlProvider { get; set; }
    public Func<string?, string, string>? SendKeysProvider { get; set; }
    public Func<string?, string>? SetFocusProvider { get; set; }
    public Func<string?, string>? HighlightControlProvider { get; set; }
    public Func<string, double?, double?, string>? SetPanelSizeProvider { get; set; }

    private readonly Services.AppDataService _appData = new();
    private readonly Services.IGitCommandRunner _gitRunner = new Services.GitCommandRunner();
    private readonly Services.IDotnetCommandRunner _dotnetRunner = new Services.DotnetCommandRunner();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallModelCommand))]
    private bool _ollamaAvailable;

    [ObservableProperty]
    private string _ollamaStatus = "Проверка Ollama…";

    /// <summary>True, если IDE запущена как MCP-сервер (--mcp-stdio). Показывать подсказку «управляется агентом».</summary>
    [ObservableProperty]
    private bool _isMcpServerMode;

    public ObservableCollection<string> OllamaModels { get; } = [];

    /// <summary>Список моделей + пункт "Install New" для ComboBox.</summary>
    public ObservableCollection<string> OllamaModelChoices { get; } = [];

    [ObservableProperty]
    private string? _selectedOllamaModel;

    /// <summary>Краткое описание выбранной модели (размер, контекст, возможности) из Ollama API.</summary>
    [ObservableProperty]
    private string _selectedModelDetails = "";

    /// <summary>Последняя выбранная реальная модель (для восстановления после "Install New").</summary>
    public string? LastSelectedRealModel { get; set; }

    private CancellationTokenSource? _openFileDebounceCts;
    // Solution load version is owned by Workspace.
    private string? _lastBuildBinlogPath;

    /// <summary>True, если открыт файл .md или .markdown — показываем превью.</summary>
    public bool IsMarkdownFile =>
        !string.IsNullOrEmpty(CurrentFilePath)
        && (CurrentFilePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            || CurrentFilePath.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMarkdownPreviewVisible))]
    private bool _isLoadingCurrentFile;

    /// <summary>Показывать панель превью Markdown только когда контент уже загружен (избегаем смены лейаута до загрузки, из‑за которой сбрасывается выбор в дереве).</summary>
    public bool IsMarkdownPreviewVisible => IsMarkdownFile && !IsLoadingCurrentFile;

    [ObservableProperty]
    private string _editorText = "";

    /// <summary>Запрос выделения: начальный offset. View применит к редактору и сбросит.</summary>
    [ObservableProperty]
    private int? _editorSelectionStart;

    /// <summary>Запрос выделения: длина. View применит к редактору и сбросит.</summary>
    [ObservableProperty]
    private int? _editorSelectionLength;

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

    private string _csharpLspProvider = CSharpLspProviderIds.ParseOnly;
    private string _csharpLspExecutable = "";
    private string _csharpLspArguments = "";

    /// <summary><see cref="CSharpLspProviderIds"/>.</summary>
    public string CSharpLspProvider
    {
        get => _csharpLspProvider;
        set
        {
            var v = string.IsNullOrWhiteSpace(value) ? CSharpLspProviderIds.ParseOnly : value.Trim();
            if (!SetProperty(ref _csharpLspProvider, v))
                return;
            _settings.CSharpLspProvider = v;
            SaveSettingsIfChanged();
            OnPropertyChanged(nameof(IsCSharpLspProcessSelected));
            _ = RestartCSharpLanguageServerAsync();
        }
    }

    public string CSharpLspExecutable
    {
        get => _csharpLspExecutable;
        set
        {
            var v = value ?? "";
            if (!SetProperty(ref _csharpLspExecutable, v))
                return;
            _settings.CSharpLspExecutable = v;
            SaveSettingsIfChanged();
            _ = RestartCSharpLanguageServerAsync();
        }
    }

    public string CSharpLspArguments
    {
        get => _csharpLspArguments;
        set
        {
            var v = value ?? "";
            if (!SetProperty(ref _csharpLspArguments, v))
                return;
            _settings.CSharpLspArguments = v;
            SaveSettingsIfChanged();
            _ = RestartCSharpLanguageServerAsync();
        }
    }

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

    /// <summary>Краткий список языков с подсветкой в редакторе (для окна настроек).</summary>
    public string SupportedEditorLanguagesSummary => Services.EditorLanguageSupport.GetSummary();

    public void LoadSendMessageKeyFromStorage()
    {
        var stored = _appData.Get("SendMessageKey");
        if (!string.IsNullOrEmpty(stored) && SendMessageKeyOptions.Contains(stored))
            SendMessageKey = stored;
    }

    private void HandleSelectedSolutionItemChanged(SolutionItem? value)
    {
        _openFileDebounceCts?.Cancel();
        _openFileDebounceCts = new CancellationTokenSource();
        var cts = _openFileDebounceCts;
        _ = Documents.OpenFileAfterDebounceAsync(cts.Token);
    }

    private void HandleSolutionPathChanged(string value)
    {
        AttachBreakpointsFileWatcher(value);
        _ = RefreshGitSummaryAsync();
        _ = GitPanel.RefreshRepositoryFlagAsync();
        if (IsGitPanelVisible)
            _ = GitPanel.RefreshGitPanelAsync();
        _ = RestartCSharpLanguageServerAsync();
    }

    Task<string> Services.IIdeMcpActions.ExecuteCommandAsync(string commandId, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken) =>
        _ideMcpExecutor.ExecuteAsync(commandId, args, cancellationToken);

}

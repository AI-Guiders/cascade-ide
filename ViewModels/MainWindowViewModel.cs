using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using Avalonia.Media;
using Avalonia.Threading;
using CascadeIDE.Features.Build;
using CascadeIDE.Features.Chat;
using CascadeIDE.Features.AutonomousAgent;
using CascadeIDE.Features.Git;
using CascadeIDE.Features.Instrumentation;
using CascadeIDE.Features.Terminal;
using CascadeIDE.Models;
using CascadeIDE.Services.Lsp;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, Services.IIdeMcpActions
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
    private CancellationTokenSource? _autonomousCts;
    private Task? _autonomousTask;
    private AutonomousRunState? _autonomousRunState;

    public MainWindowViewModel()
    {
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
        OpenDocuments.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasOpenDocuments));
        };

        DockFactory = new Factory();
        DockLayout = BuildDockLayout();
        DockFactory.InitLayout(DockLayout);

        _lastSavedSettings = (CascadeIdeSettings)_settings.Clone();
        _lastSavedAiKeys = (AiKeys)_aiKeys.Clone();

        BuildOutputPanel = new BuildOutputPanelViewModel();
        TerminalPanel = new TerminalPanelViewModel(() => SolutionPath);
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
        _ideMcpExecutor = new IdeMcpCommandExecutor(this);
    }

    /// <summary>Событие: перейти в активном редакторе на строку/колонку (1-based) после открытия файла.</summary>
    public event Action<int, int>? GotoActiveEditorLineColumnRequested;

    private void NavigateToProblemFromList(ProblemListItem item)
    {
        if (string.IsNullOrWhiteSpace(item.FilePath))
            return;
        OpenOrActivateDocument(item.FilePath);
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

    partial void OnIdeMcpServerEnabledChanged(bool value)
    {
        _settings.IdeMcpServerEnabled = value;
        SaveSettingsIfChanged();
    }

    partial void OnExternalMcpServersJsonChanged(string value)
    {
        _settings.ExternalMcpServersJson = value ?? "[]";

        // External MCP connectivity affects autonomous tool list/calls.
        _autonomousCts?.Cancel();
        _mcpClientService = new Services.McpClientService(_settings.ExternalMcpServersJson);
        _autonomousAgentService = CreateAutonomousAgentService(_mcpClientService);

        SaveSettingsIfChanged();
    }

    partial void OnIsSolutionExplorerVisibleChanged(bool value)
    {
        _settings.SolutionExplorerVisible = value;
        OnPropertyChanged(nameof(IsSolutionPanelHidden));
        SaveSettingsIfChanged();
    }

    partial void OnIsTerminalVisibleChanged(bool value)
    {
        _settings.TerminalVisible = value;
        OnPropertyChanged(nameof(IsTerminalPanelHidden));
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        SaveSettingsIfChanged();
        if (value)
            BottomPanelTabIndex = 0;
        else if (BottomPanelTabIndex == 0)
            CoerceBottomPanelTabToVisible();
    }

    partial void OnIsBuildOutputVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBuildPanelHidden));
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        if (value)
            BottomPanelTabIndex = 1;
        else if (BottomPanelTabIndex == 1)
            CoerceBottomPanelTabToVisible();
    }

    partial void OnIsInstrumentationDockVisibleChanged(bool value)
    {
        _settings.InstrumentationDockVisible = value;
        SaveSettingsIfChanged();
        if (value)
        {
            BottomPanelTabIndex = 4;
            return;
        }

        if (BottomPanelTabIndex is >= 4 and <= 6)
            CoerceBottomPanelTabToVisible();
    }

    partial void OnIsChatPanelExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(IsChatPanelHidden));
    }

    partial void OnActiveAiProviderChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _settings.ActiveAiProvider = value;
            SaveSettingsIfChanged();
        }
        ChatPanel.RefreshSendChatCommandState();
    }

    partial void OnAnthropicApiKeyChanged(string value)
    {
        _aiKeys.AnthropicApiKey = string.IsNullOrEmpty(value) ? null : value;
        SaveAiKeysIfChanged();
    }

    partial void OnOpenAiApiKeyChanged(string value)
    {
        _aiKeys.OpenAiApiKey = string.IsNullOrEmpty(value) ? null : value;
        SaveAiKeysIfChanged();
    }

    partial void OnDeepSeekApiKeyChanged(string value)
    {
        _aiKeys.DeepSeekApiKey = string.IsNullOrEmpty(value) ? null : value;
        SaveAiKeysIfChanged();
    }

    private readonly Services.AppDataService _appData = new();
    private readonly Services.IGitCommandRunner _gitRunner = new Services.GitCommandRunner();

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

    [ObservableProperty]
    private ObservableCollection<SolutionItem> _solutionRoots = [];

    [ObservableProperty]
    private SolutionItem? _selectedSolutionItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMarkdownFile))]
    [NotifyPropertyChangedFor(nameof(IsMarkdownPreviewVisible))]
    [NotifyPropertyChangedFor(nameof(BreakpointLinesInCurrentFile))]
    [NotifyPropertyChangedFor(nameof(DebuggerBreakpointLinesInCurrentFile))]
    [NotifyPropertyChangedFor(nameof(McpFileBreakpointLinesInCurrentFile))]
    [NotifyPropertyChangedFor(nameof(AllBreakpointLinesInCurrentFile))]
    [NotifyPropertyChangedFor(nameof(DebugCurrentLineInCurrentFile))]
    private string? _currentFilePath;

    [ObservableProperty]
    private OpenDocumentViewModel? _selectedDocument;

    private readonly List<(string FilePath, int Line)> _breakpoints = [];
    private readonly List<(string FilePath, int Line)> _debuggerBreakpoints = [];
    private FileSystemWatcher? _breakpointsFileWatcher;
    private CancellationTokenSource? _openFileDebounceCts;
    private CancellationTokenSource? _uiModeBloomCts;
    /// <summary>Предыдущий применённый режим UI — чтобы не давать bloom при первом применении и при повторной установке того же значения.</summary>
    private string? _lastAppliedUiModeForBloomEffects;
    private long _solutionLoadVersion;
    private string? _lastBuildBinlogPath;
    private bool _isSwitchingDocument;
    private readonly Stack<string> _recentlyClosedDocumentPaths = new();
    private int _recentlyClosedDocumentCount;
    private const int OpenFileDebounceMs = 100;

    public ObservableCollection<OpenDocumentViewModel> OpenDocuments { get; } = [];
    public bool HasOpenDocuments => OpenDocuments.Count > 0;
    public int RecentlyClosedDocumentCount => _recentlyClosedDocumentCount;

    // ---- Dock MDI (inside MainWindow only; no floating) ----
    public Dock.Model.Core.IFactory DockFactory { get; }

    public Dock.Model.Core.IDock DockLayout { get; private set; }

    public ObservableCollection<Dock.Model.Core.IDockable> DockDocuments { get; } = [];

    [ObservableProperty]
    private Dock.Model.Core.IDockable? _dockActiveDocument;

    private IDisposable? _selectedDocumentContentSubscription;

    /// <summary>Номера строк с брейкпоинтами в текущем открытом файле (для отрисовки в редакторе).</summary>
    public IReadOnlyList<int> BreakpointLinesInCurrentFile
    {
        get
        {
            var current = CurrentFilePath;
            if (string.IsNullOrEmpty(current))
                return [];
            var normalized = Path.GetFullPath(current);
            return _breakpoints
                .Where(b => string.Equals(Path.GetFullPath(b.FilePath), normalized, StringComparison.OrdinalIgnoreCase))
                .Select(b => b.Line)
                .OrderBy(static l => l)
                .Distinct()
                .ToList();
        }
    }

    /// <summary>Строки с брейкпоинтами отладчика (ide_show_breakpoints) в текущем файле.</summary>
    public IReadOnlyList<int> DebuggerBreakpointLinesInCurrentFile
    {
        get
        {
            var current = CurrentFilePath;
            if (string.IsNullOrEmpty(current))
                return [];
            var normalized = Path.GetFullPath(current);
            return _debuggerBreakpoints
                .Where(b => string.Equals(Path.GetFullPath(b.FilePath), normalized, StringComparison.OrdinalIgnoreCase))
                .Select(b => b.Line)
                .OrderBy(static l => l)
                .Distinct()
                .ToList();
        }
    }

    /// <summary>Строки с брейкпоинтами из .dotnet-debug-mcp-breakpoints.json в текущем файле.</summary>
    public IReadOnlyList<int> McpFileBreakpointLinesInCurrentFile
    {
        get
        {
            var ws = GetWorkspacePath();
            if (string.IsNullOrEmpty(ws) || string.IsNullOrEmpty(CurrentFilePath))
                return [];
            return Services.BreakpointsFileService.GetLinesForFile(ws, CurrentFilePath);
        }
    }

    /// <summary>Все брейкпоинты (IDE + отладчик + файл MCP) в текущем файле для отрисовки.</summary>
    public IReadOnlyList<int> AllBreakpointLinesInCurrentFile =>
        BreakpointLinesInCurrentFile
            .Union(DebuggerBreakpointLinesInCurrentFile)
            .Union(McpFileBreakpointLinesInCurrentFile)
            .OrderBy(static l => l)
            .ToList();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DebugCurrentLineInCurrentFile))]
    private string? _debugPositionFile;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DebugCurrentLineInCurrentFile))]
    private int _debugPositionLine;

    /// <summary>Номер строки текущей позиции отладки в открытом файле (0 если другой файл или сброшено).</summary>
    public int DebugCurrentLineInCurrentFile
    {
        get
        {
            if (string.IsNullOrEmpty(DebugPositionFile) || string.IsNullOrEmpty(CurrentFilePath))
                return 0;
            if (!string.Equals(Path.GetFullPath(DebugPositionFile), Path.GetFullPath(CurrentFilePath), StringComparison.OrdinalIgnoreCase))
                return 0;
            return DebugPositionLine;
        }
    }

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
    [NotifyCanExecuteChangedFor(nameof(BuildSolutionCommand))]
    [NotifyPropertyChangedFor(nameof(McpFileBreakpointLinesInCurrentFile))]
    [NotifyPropertyChangedFor(nameof(AllBreakpointLinesInCurrentFile))]
    private string _solutionPath = "";

    [ObservableProperty]
    private string _solutionLoadError = "";

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

    partial void OnIsGitPanelVisibleChanged(bool value)
    {
        _settings.GitPanelVisible = value;
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        SaveSettingsIfChanged();
        if (value)
        {
            BottomPanelTabIndex = 3;
            _ = GitPanel.RefreshGitPanelAsync();
        }
        else if (BottomPanelTabIndex == 3)
            CoerceBottomPanelTabToVisible();
    }

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

    /// <summary>Короткая «кинематографичная» вспышка при смене режима UI (Opacity + кисть; разметка — MainWindow).</summary>
    [ObservableProperty]
    private double _uiModeBloomOpacity;

    [ObservableProperty]
    private IBrush _uiModeBloomBrush = Brushes.Transparent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowEditorGroup2))]
    [NotifyPropertyChangedFor(nameof(ShowEditorGroup3))]
    private int _editorGroupCount = 1;

    [ObservableProperty]
    private int _activeEditorGroup = 1;

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
    [NotifyCanExecuteChangedFor(nameof(StartAutonomousCommand))]
    private string _autonomousObjective = "Autonomous objective: fix issues in the current workspace.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartAutonomousCommand))]
    private int _autonomousMaxSteps = 10;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartAutonomousCommand))]
    [NotifyCanExecuteChangedFor(nameof(PauseAutonomousCommand))]
    private bool _isAutonomousRunning;

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
    [NotifyPropertyChangedFor(nameof(TelemetryGitText))]
    [NotifyPropertyChangedFor(nameof(IsFilesChangedBadgeVisible))]
    private int _filesChangedBadge;

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

    public ObservableCollection<OpenDocumentViewModel> Group1Documents { get; } = [];
    public ObservableCollection<OpenDocumentViewModel> Group2Documents { get; } = [];
    public ObservableCollection<OpenDocumentViewModel> Group3Documents { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorTextGroup2))]
    private OpenDocumentViewModel? _selectedDocumentGroup2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorTextGroup3))]
    private OpenDocumentViewModel? _selectedDocumentGroup3;

    public string EditorTextGroup2 => SelectedDocumentGroup2?.Content ?? "";
    public string EditorTextGroup3 => SelectedDocumentGroup3?.Content ?? "";

    public static readonly IReadOnlyList<string> SendMessageKeyOptions = ["Enter", "Ctrl+Enter", "Shift+Enter"];

    public IReadOnlyList<string> SendMessageKeyOptionsList => SendMessageKeyOptions;

    /// <summary>Краткий список языков с подсветкой в редакторе (для окна настроек).</summary>
    public string SupportedEditorLanguagesSummary => Services.EditorLanguageSupport.GetSummary();

    partial void OnSendMessageKeyChanged(string value)
    {
        _appData.Put("SendMessageKey", value);
    }

    public void LoadSendMessageKeyFromStorage()
    {
        var stored = _appData.Get("SendMessageKey");
        if (!string.IsNullOrEmpty(stored) && SendMessageKeyOptions.Contains(stored))
            SendMessageKey = stored;
    }

    partial void OnSelectedSolutionItemChanged(SolutionItem? value)
    {
        _openFileDebounceCts?.Cancel();
        _openFileDebounceCts = new CancellationTokenSource();
        var cts = _openFileDebounceCts;
        _ = OpenFileAfterDebounceAsync(cts.Token);
    }

    partial void OnSolutionPathChanged(string value)
    {
        AttachBreakpointsFileWatcher(value);
        _ = RefreshGitSummaryAsync();
        _ = GitPanel.RefreshRepositoryFlagAsync();
        if (IsGitPanelVisible)
            _ = GitPanel.RefreshGitPanelAsync();
        _ = RestartCSharpLanguageServerAsync();
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TelemetryGitText))]
    [NotifyPropertyChangedFor(nameof(TelemetryGitCockpitShort))]
    private string _gitBranchSummary = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TelemetryGitText))]
    [NotifyPropertyChangedFor(nameof(TelemetryGitCockpitShort))]
    private int _gitStagedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TelemetryGitText))]
    [NotifyPropertyChangedFor(nameof(TelemetryGitCockpitShort))]
    private int _gitUnstagedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TelemetryGitText))]
    [NotifyPropertyChangedFor(nameof(TelemetryGitCockpitShort))]
    private int _gitUntrackedCount;

    Task<string> Services.IIdeMcpActions.ExecuteCommandAsync(string commandId, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken) =>
        _ideMcpExecutor.ExecuteAsync(commandId, args, cancellationToken);

}

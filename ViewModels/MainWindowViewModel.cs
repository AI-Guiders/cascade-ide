using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using System.Xml.Linq;
using Avalonia.Threading;
using CascadeIDE.Models;
using DotNetBuildTestParsers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OutWit.Common.Values;

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

    private readonly Services.ContextMinimizer _contextMinimizer;

    public MainWindowViewModel()
    {
        _contextMinimizer = new Services.ContextMinimizer(new Services.CSharpLanguageService());
        _aiProviderManager = new Services.AiProviderManager(_contextMinimizer, ResolveProvider);
        _ideMcpServerEnabled = _settings.IdeMcpServerEnabled;
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
    }

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
    }

    partial void OnIsBuildOutputVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsBuildPanelHidden));
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        if (value)
            BottomPanelTabIndex = 1;
    }

    partial void OnIsInstrumentationDockVisibleChanged(bool value)
    {
        _settings.InstrumentationDockVisible = value;
        SaveSettingsIfChanged();
        if (value)
        {
            BottomPanelTabIndex = 3;
            return;
        }

        if (BottomPanelTabIndex is >= 3 and <= 5)
            BottomPanelTabIndex = IsTerminalVisible ? 0 : IsBuildOutputVisible ? 1 : IsGitPanelVisible ? 2 : 0;
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
        (SendChatCommand as IRelayCommand)?.NotifyCanExecuteChanged();
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

    private static string GetWorkspacePath(string? solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return "";
        var p = Path.GetFullPath(solutionPath.Trim());
        return File.Exists(p) ? Path.GetDirectoryName(p) ?? "" : p;
    }

    private static string NormalizeUiMode(string? mode)
    {
        if (string.Equals(mode, "Focus", StringComparison.OrdinalIgnoreCase))
            return "Focus";
        if (string.Equals(mode, "Power", StringComparison.OrdinalIgnoreCase))
            return "Power";
        return "Balanced";
    }

    private void InitializeAgentUiDefaults()
    {
        // Keep operation/trace feeds empty until real runtime events arrive.
        // This avoids demo-like placeholder content in production UI.
    }

    private void RegisterAgentFeedHandlers()
    {
        AgentToolCalls.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAgentToolCalls));
        AgentTraceSteps.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAgentTraceSteps));
        PowerTaskQueueItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasPowerTaskQueueItems));
        EventTimeline.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasEventTimeline));
        ChatMessages.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasChatMessages));
        FocusPlanItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFocusPlanItems));
    }

    private void ApplyUiModeLayout(string mode, bool persist)
    {
        var normalized = NormalizeUiMode(mode);
        switch (normalized)
        {
            case "Focus":
                IsSolutionExplorerVisible = true;
                IsBuildOutputVisible = false;
                IsTerminalVisible = false;
                IsChatPanelExpanded = true;
                EditorGroupCount = 1;
                break;
            case "Power":
                IsSolutionExplorerVisible = true;
                IsBuildOutputVisible = true;
                IsTerminalVisible = true;
                IsChatPanelExpanded = true;
                EditorGroupCount = 3;
                break;
            default:
                IsSolutionExplorerVisible = true;
                IsBuildOutputVisible = false;
                IsTerminalVisible = false;
                IsChatPanelExpanded = true;
                EditorGroupCount = 2;
                break;
        }

        // Mode-specific visual identity: Power gets cosmic palette; Focus/Balanced keep calmer dark themes.
        _ = normalized switch
        {
            "Power" => Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetPowerThemeJson()),
            "Focus" => Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetDarkThemeJson()),
            _ => Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetCursorLikeThemeJson())
        };

        if (!persist)
            return;

        _settings.UiMode = normalized;
        _settings.SolutionExplorerVisible = IsSolutionExplorerVisible;
        _settings.TerminalVisible = IsTerminalVisible;
        SaveSettingsIfChanged();
    }

    private string GetWorkspacePath() => GetWorkspacePath(SolutionPath);

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

    [ObservableProperty]
    private ObservableCollection<DebugStackFrameViewModel> _debugStackFrames = [];

    [ObservableProperty]
    private ObservableCollection<DebugVariableViewModel> _debugVariables = [];

    /// <summary>Показывать панель отладки (стек/переменные), когда агент присылал состояние.</summary>
    public bool IsDebugPanelVisible => DebugStackFrames.Count > 0 || DebugVariables.Count > 0;

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
    [NotifyCanExecuteChangedFor(nameof(CommitGitFromPanelCommand))]
    [NotifyCanExecuteChangedFor(nameof(PushGitFromPanelCommand))]
    private bool _isGitPanelVisible;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CommitGitFromPanelCommand))]
    [NotifyCanExecuteChangedFor(nameof(PushGitFromPanelCommand))]
    private bool _isGitRepository;

    [ObservableProperty]
    private string _gitBranchLine = "";

    [ObservableProperty]
    private string _gitPanelStatusText = "";

    [ObservableProperty]
    private string _gitCommitMessage = "";

    [ObservableProperty]
    private string _gitDiffText = "";

    [ObservableProperty]
    private string _gitSubmoduleStatusText = "";

    /// <summary>Корень репозитория (git rev-parse --show-toplevel) и каталог решения — для ясности родитель vs вложенный репо.</summary>
    [ObservableProperty]
    private string _gitRepositoryContextText = "";

    /// <summary>Последний вывод команд submodule (update/sync).</summary>
    [ObservableProperty]
    private string _gitSubmoduleCommandOutput = "";

    [ObservableProperty]
    private GitStatusRow? _selectedGitStatusRow;

    public ObservableCollection<GitStatusRow> GitStatusRows { get; } = [];

    partial void OnIsGitPanelVisibleChanged(bool value)
    {
        _settings.GitPanelVisible = value;
        OnPropertyChanged(nameof(IsBottomPanelVisible));
        SaveSettingsIfChanged();
        if (value)
        {
            BottomPanelTabIndex = 2;
            _ = RefreshGitPanelAsync();
        }
        else if (BottomPanelTabIndex == 2)
        {
            BottomPanelTabIndex = IsTerminalVisible ? 0 : IsBuildOutputVisible ? 1 : 0;
        }
    }

    partial void OnSelectedGitStatusRowChanged(GitStatusRow? value)
    {
        _ = LoadGitDiffForSelectionAsync();
        StageGitSelectionCommand.NotifyCanExecuteChanged();
        UnstageGitSelectionCommand.NotifyCanExecuteChanged();
        OpenSubmoduleFolderCommand.NotifyCanExecuteChanged();
        OpenSubmoduleSolutionInIdeCommand.NotifyCanExecuteChanged();
    }

    partial void OnGitCommitMessageChanged(string value)
    {
        CommitGitFromPanelCommand.NotifyCanExecuteChanged();
        CommitGitStagedOnlyCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsGitRepositoryChanged(bool value)
    {
        CommitGitFromPanelCommand.NotifyCanExecuteChanged();
        PushGitFromPanelCommand.NotifyCanExecuteChanged();
        CommitGitStagedOnlyCommand.NotifyCanExecuteChanged();
        StageGitSelectionCommand.NotifyCanExecuteChanged();
        UnstageGitSelectionCommand.NotifyCanExecuteChanged();
        OpenSubmoduleFolderCommand.NotifyCanExecuteChanged();
        GitSubmoduleUpdateInitCommand.NotifyCanExecuteChanged();
        GitSubmoduleSyncCommand.NotifyCanExecuteChanged();
        OpenSubmoduleSolutionInIdeCommand.NotifyCanExecuteChanged();
    }

    public static readonly IReadOnlyList<string> UiModeOptions = ["Focus", "Balanced", "Power"];
    public IReadOnlyList<string> UiModeOptionsList => UiModeOptions;

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
    private string _uiMode = "Balanced";

    /// <summary>Заголовок главного окна (в Power — подпись «Autonomous Agent Cockpit»).</summary>
    public string WindowTitle =>
        IsPowerMode
            ? "CascadeIDE — Power Mode [Autonomous Agent Cockpit]"
            : "CascadeIDE";

    public bool IsFocusMode => string.Equals(UiMode, "Focus", StringComparison.OrdinalIgnoreCase);
    public bool IsBalancedMode => string.Equals(UiMode, "Balanced", StringComparison.OrdinalIgnoreCase);
    public bool IsPowerMode => string.Equals(UiMode, "Power", StringComparison.OrdinalIgnoreCase);
    public bool ShowTaskBar => true;
    public bool ShowQuickActions => !IsFocusMode;
    public bool ShowAgentOperations => true;
    /// <summary>В Focus справа показываем план и гейт; блок «операции» переносим в Balanced/Power.</summary>
    public bool ShowAgentOperationsBlock => !IsFocusMode;
    public bool ShowAgentTrace => IsPowerMode;
    public bool ShowPowerTelemetry => IsPowerMode;
    public bool ShowSafetyControls => IsPowerMode;
    public bool ShowTelemetryHiddenHint => ShowPowerTelemetry && !IsTerminalVisible;
    /// <summary>Полоска build/tests/debug/git — и в Focus (по концепту).</summary>
    public bool ShowTelemetryStrip => true;
    public string TelemetryButtonText => IsTerminalVisible ? "Telemetry: on" : "Show telemetry";
    public bool ShowEditorGroup2 => EditorGroupCount >= 2;
    public bool ShowEditorGroup3 => EditorGroupCount >= 3;

    /// <summary>Balanced/Power: нижние вкладки «События / Тесты / Отладка». В Focus всегда скрыты.</summary>
    public bool ShowInstrumentationTabs =>
        !IsFocusMode && (IsBalancedMode || IsPowerMode) && IsInstrumentationDockVisible;

    /// <summary>Пункт меню для док-панели инструментирования (не в Focus).</summary>
    public bool ShowInstrumentationLayoutMenu => !IsFocusMode;

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

    public bool IsSafetyL1 => string.Equals(SafetyLevel, "L1", StringComparison.OrdinalIgnoreCase);
    public bool IsSafetyL2 => string.Equals(SafetyLevel, "L2", StringComparison.OrdinalIgnoreCase);
    public bool IsSafetyL3 => string.Equals(SafetyLevel, "L3", StringComparison.OrdinalIgnoreCase);

    /// <summary>Подпись режима безопасности (как на мокапе Power).</summary>
    public string SafetyLevelDescription =>
        SafetyLevel switch
        {
            "L1" => "Только чтение: без автоматических правок файлов.",
            "L2" => "Правки после явного подтверждения.",
            "L3" => "Автономный режим: цепочка шагов без запроса.",
            _ => ""
        };

    public double SafetyL1Opacity => IsSafetyL1 ? 1 : 0.38;
    public double SafetyL2Opacity => IsSafetyL2 ? 1 : 0.38;
    public double SafetyL3Opacity => IsSafetyL3 ? 1 : 0.38;

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

    public ObservableCollection<string> AgentToolCalls { get; } = [];
    public ObservableCollection<string> AgentTraceTimeline { get; } = [];
    public ObservableCollection<AgentTraceStepViewModel> AgentTraceSteps { get; } = [];
    public ObservableCollection<string> EventTimeline { get; } = [];
    public bool HasAgentToolCalls => AgentToolCalls.Count > 0;
    public bool HasAgentTraceSteps => AgentTraceSteps.Count > 0;

    /// <summary>Очередь задач (Power mode, под деревом решения).</summary>
    public ObservableCollection<PowerTaskQueueItemViewModel> PowerTaskQueueItems { get; } = [];

    public bool HasPowerTaskQueueItems => PowerTaskQueueItems.Count > 0;

    /// <summary>Снимок раскладки UI (JSON), полоса телеметрии в Power.</summary>
    [ObservableProperty]
    private string _workspaceSnapshotJson = "";

    public bool HasEventTimeline => EventTimeline.Count > 0;
    public bool HasChatMessages => ChatMessages.Count > 0;

    public ObservableCollection<FocusPlanItemViewModel> FocusPlanItems { get; } = [];

    public bool HasFocusPlanItems => FocusPlanItems.Count > 0;

    public bool IsRiskSummaryVisible =>
        !string.IsNullOrWhiteSpace(RiskSummary)
        && !string.Equals(RiskSummary, "Риски не зафиксированы.", StringComparison.Ordinal);

    public bool IsResultSummaryVisible =>
        !string.IsNullOrWhiteSpace(ResultSummary)
        && !string.Equals(ResultSummary, "Результатов пока нет.", StringComparison.Ordinal);

    public bool IsRiskCardVisible => !IsFocusMode && IsRiskSummaryVisible;
    public bool IsResultCardVisible => !IsFocusMode && IsResultSummaryVisible;
    public bool IsComplexityBadgeVisible => ComplexityBadge > 0;
    public bool IsImpactedTestsBadgeVisible => ImpactedTestsBadge > 0;
    public bool IsFilesChangedBadgeVisible => FilesChangedBadge > 0;
    public bool IsActiveTaskProgressVisible => ActiveTaskProgress > 0;

    public string TelemetryBuildText => IsBuilding ? "Build: running…" : "Build: idle";

    /// <summary>Короткий статус для «кольца» сборки в Power cockpit.</summary>
    public string TelemetryBuildCockpitShort => IsBuilding ? "BUILD…" : "READY";

    public string TelemetryTestsText =>
        !string.IsNullOrWhiteSpace(LastTestSummary)
            ? $"Tests: {LastTestSummary}"
            : $"Tests: impacted {ImpactedTestsBadge}";

    /// <summary>Компактная строка тестов для полосы Power.</summary>
    public string TelemetryTestsCockpitShort =>
        !string.IsNullOrWhiteSpace(LastTestSummary)
            ? (LastTestSummary.Length > 36 ? string.Concat(LastTestSummary.AsSpan(0, 33), "…") : LastTestSummary)
            : $"imp {ImpactedTestsBadge}";

    /// <summary>Компактный Git для полосы Power.</summary>
    public string TelemetryGitCockpitShort
    {
        get
        {
            var br = GitBranchSummary ?? "";
            if (br.Length > 16)
                br = string.Concat(br.AsSpan(0, 14), "…");
            var delta = GitStagedCount + GitUnstagedCount + GitUntrackedCount;
            return string.IsNullOrWhiteSpace(br) ? $"Δ{delta}" : $"{br} · Δ{delta}";
        }
    }

    public string TelemetryDebugText =>
        IsDebugPanelVisible
            ? $"Debug: paused (frames {DebugStackFrames.Count}, vars {DebugVariables.Count})"
            : "Debug: idle";

    /// <summary>Короткий статус отладки для Power.</summary>
    public string TelemetryDebugCockpitShort =>
        IsDebugPanelVisible ? $"DBG · {DebugStackFrames.Count}fr" : "DBG · —";

    public string TelemetryGitText
    {
        get
        {
            var branch = string.IsNullOrWhiteSpace(GitBranchSummary) ? "" : $" ({GitBranchSummary})";
            return $"Git: {GitStagedCount} staged, {GitUnstagedCount} unstaged, {GitUntrackedCount} untracked{branch}";
        }
    }

    [ObservableProperty]
    private string _terminalOutput = "";

    [ObservableProperty]
    private string _terminalInput = "";

    public string ChatPanelToggleButtonText => IsChatPanelExpanded ? "◀" : "▶";
    public bool IsSolutionPanelHidden => !IsSolutionExplorerVisible;
    public bool IsBuildPanelHidden => !IsBuildOutputVisible;
    public bool IsChatPanelHidden => !IsChatPanelExpanded;
    public bool IsTerminalPanelHidden => !IsTerminalVisible;
    public bool IsBottomPanelVisible => IsTerminalVisible || IsBuildOutputVisible || ShowInstrumentationTabs || IsGitPanelVisible;

    /// <summary>0 = Terminal, 1 = Build, 2 = Git, 3 = Events, 4 = Tests, 5 = Debug.</summary>
    [ObservableProperty]
    private int _bottomPanelTabIndex;

    [ObservableProperty]
    private string _buildOutput = "";

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

    /// <summary>Накопленный текстовый лог прогонов тестов для вкладки «Тесты».</summary>
    [ObservableProperty]
    private string _testResultsOutput = "";

    public ObservableCollection<ChatMessageViewModel> ChatMessages { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendChatCommand))]
    private string _chatInput = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendChatCommand))]
    private bool _isChatLoading;

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

    partial void OnSelectedOllamaModelChanged(string? value)
    {
        (SendChatCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        if (value == InstallNewSentinel)
        {
            SelectedModelDetails = "";
            return;
        }
        if (!string.IsNullOrEmpty(value))
        {
            LastSelectedRealModel = value;
            _settings.PreferredOllamaModel = value;
            SaveSettingsIfChanged();
            _ = LoadModelDetailsAsync(value);
        }
        else
            SelectedModelDetails = "";
    }

    private async Task LoadModelDetailsAsync(string modelName)
    {
        try
        {
            var details = await _ollama.GetModelDetailsAsync(modelName).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (SelectedOllamaModel == modelName)
                    SelectedModelDetails = details?.ToShortString() ?? "";
            });
        }
        catch
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (SelectedOllamaModel == modelName)
                    SelectedModelDetails = "";
            });
        }
    }

    partial void OnSendMessageKeyChanged(string value)
    {
        _appData.Put("SendMessageKey", value);
    }

    partial void OnUiModeChanged(string value)
    {
        var normalized = NormalizeUiMode(value);
        if (!string.Equals(value, normalized, StringComparison.Ordinal))
        {
            UiMode = normalized;
            return;
        }

        ApplyUiModeLayout(normalized, persist: true);
        if (string.Equals(normalized, "Power", StringComparison.OrdinalIgnoreCase))
            Dispatcher.UIThread.Post(RefreshWorkspaceSnapshotCore, DispatcherPriority.Background);
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
        _ = RefreshGitRepositoryFlagAsync();
        if (IsGitPanelVisible)
            _ = RefreshGitPanelAsync();
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

    private async Task RefreshGitSummaryAsync()
    {
        var result = await RunGitCommandAsync(["status", "--short", "--branch"]).ConfigureAwait(false);
        Dispatcher.UIThread.Post(() =>
        {
            if (!result.Success)
            {
                GitBranchSummary = "";
                GitStagedCount = 0;
                GitUnstagedCount = 0;
                GitUntrackedCount = 0;
                return;
            }

            var parsed = ParseGitStatusShortBranch(result.Output);
            GitBranchSummary = parsed.BranchSummary;
            GitStagedCount = parsed.Staged;
            GitUnstagedCount = parsed.Unstaged;
            GitUntrackedCount = parsed.Untracked;
        });
    }

    private static (string BranchSummary, int Staged, int Unstaged, int Untracked) ParseGitStatusShortBranch(string output)
    {
        // Expected first line:
        // ## main...origin/main [ahead 1]
        // Other lines: XY <path> or ?? <path>
        var branch = "";
        int staged = 0, unstaged = 0, untracked = 0;
        var lines = (output ?? "")
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                branch = line[3..].Trim();
                continue;
            }
            if (line.StartsWith("??", StringComparison.Ordinal))
            {
                untracked++;
                continue;
            }
            if (line.Length < 2)
                continue;
            var x = line[0];
            var y = line[1];
            // X (index): staged changes
            if (x != ' ' && x != '?')
                staged++;
            // Y (worktree): unstaged changes
            if (y != ' ' && y != '?')
                unstaged++;
        }

        return (branch, staged, unstaged, untracked);
    }

    private void AttachBreakpointsFileWatcher(string? solutionPath)
    {
        _breakpointsFileWatcher?.Dispose();
        _breakpointsFileWatcher = null;
        var ws = GetWorkspacePath(solutionPath);
        if (string.IsNullOrEmpty(ws) || !Directory.Exists(ws))
            return;
        try
        {
            _breakpointsFileWatcher = new FileSystemWatcher(ws)
            {
                Filter = Services.BreakpointsFileService.FileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };
            _breakpointsFileWatcher.Changed += (_, _) => Dispatcher.UIThread.Post(() =>
            {
                OnPropertyChanged(nameof(McpFileBreakpointLinesInCurrentFile));
                OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
            });
            _breakpointsFileWatcher.Renamed += (_, _) => Dispatcher.UIThread.Post(() =>
            {
                OnPropertyChanged(nameof(McpFileBreakpointLinesInCurrentFile));
                OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
            });
            _breakpointsFileWatcher.EnableRaisingEvents = true;
        }
        catch { /* нет прав или диск недоступен */ }
    }

    /// <summary>Открыть файл выбранного узла после паузы, чтобы не реагировать на двойное срабатывание/мигание выбора в дереве.</summary>
    private async Task OpenFileAfterDebounceAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(OpenFileDebounceMs, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var value = SelectedSolutionItem;
            if (value?.FullPath is not { } path || !File.Exists(path))
                return;
            var normalizedPath = Path.GetFullPath(path);
            // Уже открыт этот файл и контент загружен — не затирать (защита от сбоя выбора в дереве при появлении превью .md)
            if (string.Equals(CurrentFilePath, normalizedPath, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(EditorText))
                return;
            IsLoadingCurrentFile = true;
            OpenOrActivateDocument(normalizedPath);
            IsLoadingCurrentFile = false;
        });
    }

    /// <summary>Выделить в дереве решения узел, соответствующий текущему открытому файлу (после ide_open_file и т.п.).</summary>
    private void SyncSelectedSolutionItemToCurrentFile()
    {
        var current = CurrentFilePath;
        if (string.IsNullOrEmpty(current))
            return;
        var normalized = Path.GetFullPath(current);
        var item = FindSolutionItemByPath(SolutionRoots, normalized);
        if (item is not null)
            SelectedSolutionItem = item;
    }

    partial void OnSelectedDocumentChanged(OpenDocumentViewModel? value)
    {
        _selectedDocumentContentSubscription?.Dispose();
        _selectedDocumentContentSubscription = null;

        _isSwitchingDocument = true;
        try
        {
            if (value is null)
            {
                CurrentFilePath = null;
                EditorText = "";
                return;
            }

            CurrentFilePath = value.FilePath;
            EditorText = value.Content;
            SyncSelectedSolutionItemToCurrentFile();

            _selectedDocumentContentSubscription = ObservePropertyChanged(value, nameof(OpenDocumentViewModel.Content), () =>
            {
                if (_isSwitchingDocument)
                    return;
                // EditorText is used by MCP tools; keep it synced even if editor binds indirectly.
                if (!string.Equals(EditorText, value.Content, StringComparison.Ordinal))
                    EditorText = value.Content ?? "";
            });
        }
        finally
        {
            _isSwitchingDocument = false;
        }
    }

    partial void OnEditorTextChanged(string value)
    {
        if (_isSwitchingDocument || SelectedDocument is null)
            return;

        SelectedDocument.Content = value ?? "";
        SelectedDocument.IsDirty = !string.Equals(SelectedDocument.Content, SelectedDocument.OriginalContent, StringComparison.Ordinal);
        OnPropertyChanged(nameof(EditorTextGroup2));
        OnPropertyChanged(nameof(EditorTextGroup3));
    }

    private void OpenOrActivateDocument(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return;

        var normalized = Path.GetFullPath(filePath);
        var existing = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, normalized, StringComparison.OrdinalIgnoreCase));
        var targetGroup = Math.Clamp(ActiveEditorGroup, 1, 3);
        if (existing is null)
        {
            var text = SafeReadFile(normalized);
            existing = new OpenDocumentViewModel(normalized, Path.GetFileName(normalized), text)
            {
                GroupIndex = targetGroup
            };
            OpenDocuments.Add(existing);
            GetGroupCollection(targetGroup).Add(existing);

            DockDocuments.Add(new DockDocumentViewModel(existing)
            {
                Id = normalized,
                Title = existing.DisplayTitle
            });
        }
        else
        {
            if (string.IsNullOrEmpty(existing.OriginalContent))
            {
                // If a previous read failed and we opened an "empty" document, try to re-load it
                // so the editor isn't blank for regular files.
                var text = SafeReadFile(normalized);
                existing.ReloadContent(text);
            }

            if (existing.GroupIndex != targetGroup)
            {
                MoveDocumentToGroupInternal(existing, targetGroup);
            }
        }

        ActivateDocumentInternal(existing);
        // Rebuild after setting DockActiveDocument so DockControl sees ActiveDockable.
        RebuildAndReinitDockLayout();
    }

    private static string SafeReadFile(string path)
    {
        // File IO is best-effort; editor should show content whenever possible.
        // Some Windows setups (locks / transient FS issues) can cause File.ReadAllText to throw,
        // so we use a FileStream with sharing and a tiny retry.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);

                using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
                return reader.ReadToEnd();
            }
            catch (IOException) when (attempt < 2)
            {
                Thread.Sleep(40);
            }
            catch (UnauthorizedAccessException) when (attempt < 2)
            {
                Thread.Sleep(40);
            }
            catch
            {
                return "";
            }
        }

        return "";
    }

    partial void OnDockActiveDocumentChanged(IDockable? value)
    {
        if (value is DockDocumentViewModel d)
            ActivateDocumentInternal(d.Doc);
    }

    private Dock.Model.Core.IDock BuildDockLayout()
    {
        var documents = new DocumentDock
        {
            Id = "DocumentsDock",
            Title = "Documents",
            IsCollapsable = false,
            VisibleDockables = DockFactory.CreateList<Dock.Model.Core.IDockable>(DockDocuments.ToArray()),
            ActiveDockable = DockActiveDocument,
            CanCreateDocument = false
        };

        var root = DockFactory.CreateRootDock();
        root.Id = "RootDock";
        root.VisibleDockables = DockFactory.CreateList<Dock.Model.Core.IDockable>(documents);
        root.DefaultDockable = documents;
        root.ActiveDockable = documents;
        return root;
    }

    private void RebuildAndReinitDockLayout()
    {
        // DockLayout.VisibleDockables берётся из DockDocuments.ToArray() во время построения.
        // Поэтому после добавления/удаления документов нужно заново инициализировать DockFactory,
        // иначе UI может показывать "No document open".
        DockLayout = BuildDockLayout();
        DockFactory.InitLayout(DockLayout);
        OnPropertyChanged(nameof(DockLayout));
    }

    private static IDisposable ObservePropertyChanged(INotifyPropertyChanged obj, string propertyName, Action onChanged)
    {
        PropertyChangedEventHandler handler = (_, e) =>
        {
            if (string.Equals(e.PropertyName, propertyName, StringComparison.Ordinal))
                onChanged();
        };
        obj.PropertyChanged += handler;
        return new Subscription(() => obj.PropertyChanged -= handler);
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;
        public void Dispose()
        {
            _dispose?.Invoke();
            _dispose = null;
        }
    }

    private ObservableCollection<OpenDocumentViewModel> GetGroupCollection(int group) =>
        group switch
        {
            2 => Group2Documents,
            3 => Group3Documents,
            _ => Group1Documents
        };

    private void ActivateDocumentInternal(OpenDocumentViewModel doc)
    {
        ActiveEditorGroup = doc.GroupIndex;
        switch (doc.GroupIndex)
        {
            case 2:
                SelectedDocumentGroup2 = doc;
                break;
            case 3:
                SelectedDocumentGroup3 = doc;
                break;
            default:
                SelectedDocument = doc;
                break;
        }

        var dockDoc = DockDocuments
            .OfType<DockDocumentViewModel>()
            .FirstOrDefault(d => string.Equals(d.Doc.FilePath, doc.FilePath, StringComparison.OrdinalIgnoreCase));
        if (dockDoc is not null && !ReferenceEquals(DockActiveDocument, dockDoc))
            DockActiveDocument = dockDoc;
    }

    private void MoveDocumentToGroupInternal(OpenDocumentViewModel doc, int targetGroup)
    {
        var normalizedGroup = Math.Clamp(targetGroup, 1, 3);
        if (doc.GroupIndex == normalizedGroup)
            return;

        var sourceCollection = GetGroupCollection(doc.GroupIndex);
        sourceCollection.Remove(doc);
        var targetCollection = GetGroupCollection(normalizedGroup);
        if (!targetCollection.Contains(doc))
            targetCollection.Add(doc);

        doc.GroupIndex = normalizedGroup;

        if (SelectedDocument == doc && normalizedGroup != 1)
            SelectedDocument = Group1Documents.FirstOrDefault();
        if (SelectedDocumentGroup2 == doc && normalizedGroup != 2)
            SelectedDocumentGroup2 = Group2Documents.FirstOrDefault();
        if (SelectedDocumentGroup3 == doc && normalizedGroup != 3)
            SelectedDocumentGroup3 = Group3Documents.FirstOrDefault();

        ActivateDocumentInternal(doc);
    }

    private static SolutionItem? FindSolutionItemByPath(IEnumerable<SolutionItem> items, string fullPath)
    {
        foreach (var node in items)
        {
            if (node.FullPath is not null && string.Equals(Path.GetFullPath(node.FullPath), fullPath, StringComparison.OrdinalIgnoreCase))
                return node;
            var found = FindSolutionItemByPath(node.Children, fullPath);
            if (found is not null)
                return found;
        }
        return null;
    }

    public async Task RefreshOllamaAsync()
    {
        OllamaStatus = "Проверка Ollama…";
        OllamaAvailable = await _ollama.IsAvailableAsync();
        if (OllamaAvailable)
        {
            var names = await _ollama.GetModelNamesAsync();
            OllamaModels.Clear();
            OllamaModelChoices.Clear();
            foreach (var n in names)
            {
                OllamaModels.Add(n);
                OllamaModelChoices.Add(n);
            }
            OllamaModelChoices.Add(InstallNewSentinel);
            var preferred = _settings.PreferredOllamaModel?.Trim();
            SelectedOllamaModel = !string.IsNullOrEmpty(preferred) && OllamaModels.Contains(preferred)
                ? preferred
                : OllamaModels.FirstOrDefault();
            if (LastSelectedRealModel is null && OllamaModels.Count > 0)
                LastSelectedRealModel = OllamaModels[0];
            OllamaStatus = names.Count > 0
                ? $"Ollama: {names.Count} моделей"
                : "Ollama запущен, моделей нет (ollama pull <model>)";
        }
        else
        {
            OllamaModels.Clear();
            OllamaStatus = "Ollama недоступен (localhost:11434). Установи и запусти Ollama.";
        }
    }

    [RelayCommand]
    private void OpenSolution()
    {
        RequestOpenSolution?.Invoke();
    }

    [RelayCommand]
    private void Exit()
    {
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void About()
    {
        RequestShowAbout?.Invoke();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        RequestOpenSettings?.Invoke();
    }

    [RelayCommand]
    private async Task ApplyDarkThemeAsync()
    {
        await Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetDarkThemeJson());
    }

    [RelayCommand]
    private async Task ApplyLightThemeAsync()
    {
        await Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetLightThemeJson());
    }

    [RelayCommand]
    private async Task ApplyCursorLikeThemeAsync()
    {
        await Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetCursorLikeThemeJson());
    }

    [RelayCommand]
    private async Task OpenThemeFileAsync()
    {
        var path = RequestOpenThemeFile != null ? await RequestOpenThemeFile() : null;
        if (string.IsNullOrEmpty(path))
            return;
        await Services.UiThemeApply.ApplyOnUiThreadAsync(Services.UiThemeApply.GetThemeJsonFromFile(path));
    }

    [RelayCommand]
    private void ToggleSolutionExplorer()
    {
        IsSolutionExplorerVisible = !IsSolutionExplorerVisible;
    }

    [RelayCommand]
    private void ToggleBuildOutput()
    {
        IsBuildOutputVisible = !IsBuildOutputVisible;
        if (IsBuildOutputVisible)
            BottomPanelTabIndex = 1;
    }

    [RelayCommand]
    private void ToggleTerminal()
    {
        IsTerminalVisible = !IsTerminalVisible;
        if (IsTerminalVisible)
            BottomPanelTabIndex = 0;
    }

    [RelayCommand]
    private async Task RefreshGitPanelAsync()
    {
        var ws = GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(ws) || !Directory.Exists(ws))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                GitBranchLine = "";
                GitStatusRows.Clear();
                GitSubmoduleStatusText = "";
                GitDiffText = "";
                GitRepositoryContextText = "";
                GitPanelStatusText = "Откройте решение (.sln / .slnx).";
                IsGitRepository = false;
            });
            return;
        }

        var inside = await RunGitCommandAsync(["rev-parse", "--is-inside-work-tree"]).ConfigureAwait(false);
        var inRepo = inside.Success && string.Equals(inside.Output.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        if (!inRepo)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsGitRepository = false;
                GitBranchLine = "";
                GitStatusRows.Clear();
                GitSubmoduleStatusText = "";
                GitDiffText = "";
                GitRepositoryContextText = "";
                GitPanelStatusText = "Каталог решения не в git-репозитории.";
            });
            return;
        }

        var status = await RunGitCommandAsync(["status", "--short", "--branch"]).ConfigureAwait(false);
        var submodule = await RunGitCommandAsync(["submodule", "status"]).ConfigureAwait(false);
        var subPaths = await LoadSubmodulePathsAsync(ws).ConfigureAwait(false);
        var toplevel = await RunGitCommandAsync(["rev-parse", "--show-toplevel"]).ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsGitRepository = true;
            GitPanelStatusText = status.Success ? "" : status.Output;
            GitBranchLine = ExtractGitBranchLine(status.Output);
            FillGitStatusRows(status.Output, subPaths);
            GitSubmoduleStatusText = submodule.Success
                ? submodule.Output.Trim()
                : $"(submodule: {submodule.Output.Trim()})";
            GitRepositoryContextText = BuildGitRepositoryContext(ws, toplevel.Success ? toplevel.Output : "");
        });

        await LoadGitDiffForSelectionAsync().ConfigureAwait(false);
    }

    private async Task RefreshGitRepositoryFlagAsync()
    {
        var ws = GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(ws) || !Directory.Exists(ws))
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsGitRepository = false);
            return;
        }

        var inside = await RunGitCommandAsync(["rev-parse", "--is-inside-work-tree"]).ConfigureAwait(false);
        var ok = inside.Success && string.Equals(inside.Output.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        await Dispatcher.UIThread.InvokeAsync(() => IsGitRepository = ok);
    }

    private void FillGitStatusRows(string output, HashSet<string> submodulePaths)
    {
        GitStatusRows.Clear();
        foreach (var line in (output ?? "").Replace("\r\n", "\n").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("##", StringComparison.Ordinal))
                continue;
            var rel = ParseGitStatusPath(line);
            var path = rel ?? "";
            var norm = path.Replace('\\', '/').TrimEnd('/');
            var isUntracked = line.StartsWith("??", StringComparison.Ordinal);
            var hasStaged = !isUntracked && line.Length >= 1 && line[0] is not (' ' or '?');
            var isSub = !string.IsNullOrEmpty(norm) && submodulePaths.Contains(norm);
            GitStatusRows.Add(new GitStatusRow
            {
                RawLine = line,
                RelativePath = path,
                IsUntracked = isUntracked,
                HasStagedChanges = hasStaged,
                IsSubmodulePath = isSub
            });
        }
    }

    private async Task<HashSet<string>> LoadSubmodulePathsAsync(string workspace)
    {
        var result = await RunGitCommandAsync(["config", "-f", ".gitmodules", "--get-regexp", "path"]).ConfigureAwait(false);
        if (!result.Success)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in (result.Output ?? "").Replace("\r\n", "\n").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var tab = line.IndexOf('\t');
            if (tab < 0)
                continue;
            var p = line[(tab + 1)..].Trim().Replace('\\', '/').TrimEnd('/');
            if (!string.IsNullOrEmpty(p))
                set.Add(p);
        }

        return set;
    }

    private static string ExtractGitBranchLine(string output)
    {
        var first = (output ?? "").Replace("\r\n", "\n").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return first is { Length: >= 3 } && first.StartsWith("## ", StringComparison.Ordinal) ? first[3..].Trim() : "";
    }

    private static string? ParseGitStatusPath(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;
        if (line.StartsWith("##", StringComparison.Ordinal))
            return null;
        if (line.StartsWith("??", StringComparison.Ordinal))
            return line.Length > 2 ? line[2..].Trim() : null;
        if (line.Length < 4)
            return null;
        var trimmed = line[3..].Trim();
        if (trimmed.Contains(" -> ", StringComparison.Ordinal))
        {
            var parts = trimmed.Split([" -> "], StringSplitOptions.None);
            return parts.Length == 2 ? parts[1].Trim() : parts[0].Trim();
        }

        return trimmed;
    }

    private static string BuildGitRepositoryContext(string workspaceDir, string toplevelRaw)
    {
        if (string.IsNullOrWhiteSpace(workspaceDir))
            return "";
        try
        {
            var wsFull = Path.GetFullPath(workspaceDir);
            var topTrim = (toplevelRaw ?? "").Trim().Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrEmpty(topTrim))
                return $"Каталог решения: {wsFull}";
            var topFull = Path.GetFullPath(topTrim);
            var same = string.Equals(wsFull, topFull, StringComparison.OrdinalIgnoreCase);
            return same
                ? $"Корень git: {topFull}"
                : $"Корень git: {topFull} · каталог решения: {wsFull}";
        }
        catch
        {
            return (toplevelRaw ?? "").Trim();
        }
    }

    /// <summary>Ищет .slnx / .sln в корне submodule и в подпапках первого уровня (предпочтение .slnx).</summary>
    private static string? FindSolutionFileInDirectory(string submoduleRoot)
    {
        if (string.IsNullOrWhiteSpace(submoduleRoot) || !Directory.Exists(submoduleRoot))
            return null;

        static string? PickFirst(IEnumerable<string> files)
        {
            var arr = files as string[] ?? files.ToArray();
            return arr.Length == 0 ? null : arr.OrderBy(static f => f, StringComparer.OrdinalIgnoreCase).First();
        }

        var topSlnx = Directory.GetFiles(submoduleRoot, "*.slnx", SearchOption.TopDirectoryOnly);
        var topSln = Directory.GetFiles(submoduleRoot, "*.sln", SearchOption.TopDirectoryOnly);
        var p = PickFirst(topSlnx) ?? PickFirst(topSln);
        if (p is not null)
            return p;

        foreach (var sub in Directory.GetDirectories(submoduleRoot))
        {
            var innerSlnx = Directory.GetFiles(sub, "*.slnx", SearchOption.TopDirectoryOnly);
            var innerSln = Directory.GetFiles(sub, "*.sln", SearchOption.TopDirectoryOnly);
            p = PickFirst(innerSlnx) ?? PickFirst(innerSln);
            if (p is not null)
                return p;
        }

        return null;
    }

    private async Task LoadGitDiffForSelectionAsync()
    {
        if (SelectedGitStatusRow is not { HasPath: true } row || string.IsNullOrWhiteSpace(row.RelativePath))
        {
            await Dispatcher.UIThread.InvokeAsync(() => GitDiffText = "");
            return;
        }

        var unstaged = await RunGitCommandAsync(["diff", "--", row.RelativePath]).ConfigureAwait(false);
        var staged = await RunGitCommandAsync(["diff", "--staged", "--", row.RelativePath]).ConfigureAwait(false);
        var sb = new StringBuilder();
        if (staged.Success && !string.IsNullOrWhiteSpace(staged.Output))
        {
            sb.AppendLine("--- staged ---");
            sb.AppendLine(staged.Output);
            sb.AppendLine();
        }

        if (unstaged.Success && !string.IsNullOrWhiteSpace(unstaged.Output))
        {
            sb.AppendLine("--- unstaged ---");
            sb.AppendLine(unstaged.Output);
        }

        if (sb.Length == 0)
            sb.AppendLine("(нет diff для выбранного пути)");

        await Dispatcher.UIThread.InvokeAsync(() => GitDiffText = sb.ToString());
    }

    [RelayCommand(CanExecute = nameof(CanCommitGitFromPanel))]
    private async Task CommitGitFromPanelAsync()
    {
        var msg = GitCommitMessage.Trim();
        if (string.IsNullOrWhiteSpace(msg) || !IsGitRepository)
            return;

        GitPanelStatusText = "Коммит…";
        var json = await ((Services.IIdeMcpActions)this).GitCommitAsync(msg, null).ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            GitPanelStatusText = json;
            GitCommitMessage = "";
        });
        await RefreshGitPanelAsync().ConfigureAwait(false);
        await RefreshGitSummaryAsync().ConfigureAwait(false);
    }

    private bool CanCommitGitFromPanel() => IsGitRepository && !string.IsNullOrWhiteSpace(GitCommitMessage);

    [RelayCommand(CanExecute = nameof(CanPushGitFromPanel))]
    private async Task PushGitFromPanelAsync()
    {
        if (!IsGitRepository)
            return;

        GitPanelStatusText = "Push…";
        var json = await ((Services.IIdeMcpActions)this).GitPushAsync(null, null).ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() => GitPanelStatusText = json);
        await RefreshGitPanelAsync().ConfigureAwait(false);
        await RefreshGitSummaryAsync().ConfigureAwait(false);
    }

    private bool CanPushGitFromPanel() => IsGitRepository;

    [RelayCommand(CanExecute = nameof(CanStageGitSelection))]
    private async Task StageGitSelectionAsync()
    {
        if (SelectedGitStatusRow is not { HasPath: true, RelativePath: var rel })
            return;

        GitPanelStatusText = "";
        var r = await RunGitCommandAsync(["add", "--", rel]).ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() => GitPanelStatusText = r.Success ? "" : r.Output);
        await RefreshGitPanelAsync().ConfigureAwait(false);
        await RefreshGitSummaryAsync().ConfigureAwait(false);
    }

    private bool CanStageGitSelection() => IsGitRepository && SelectedGitStatusRow is { HasPath: true };

    [RelayCommand(CanExecute = nameof(CanUnstageGitSelection))]
    private async Task UnstageGitSelectionAsync()
    {
        if (SelectedGitStatusRow is not { HasPath: true, RelativePath: var rel })
            return;

        GitPanelStatusText = "";
        var r = await RunGitCommandAsync(["restore", "--staged", "--", rel]).ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() => GitPanelStatusText = r.Success ? "" : r.Output);
        await RefreshGitPanelAsync().ConfigureAwait(false);
        await RefreshGitSummaryAsync().ConfigureAwait(false);
    }

    private bool CanUnstageGitSelection() =>
        IsGitRepository && SelectedGitStatusRow is { HasPath: true, HasStagedChanges: true };

    [RelayCommand(CanExecute = nameof(CanOpenSubmoduleFolder))]
    private void OpenSubmoduleFolder()
    {
        if (SelectedGitStatusRow is not { IsSubmodulePath: true, RelativePath: var rel })
            return;

        var ws = GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(ws))
            return;

        var full = Path.GetFullPath(Path.Combine(ws, rel.Replace('/', Path.DirectorySeparatorChar)));
        if (!Directory.Exists(full))
            return;

        try
        {
            Process.Start(new ProcessStartInfo { FileName = full, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            GitPanelStatusText = ex.Message;
        }
    }

    private bool CanOpenSubmoduleFolder()
    {
        if (!IsGitRepository || SelectedGitStatusRow is not { IsSubmodulePath: true, RelativePath: var rel })
            return false;

        var ws = GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(ws))
            return false;

        var full = Path.GetFullPath(Path.Combine(ws, rel.Replace('/', Path.DirectorySeparatorChar)));
        return Directory.Exists(full);
    }

    [RelayCommand(CanExecute = nameof(CanOpenSubmoduleSolutionInIde))]
    private void OpenSubmoduleSolutionInIde()
    {
        if (SelectedGitStatusRow is not { IsSubmodulePath: true, RelativePath: var rel })
            return;

        var ws = GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(ws))
            return;

        var dir = Path.GetFullPath(Path.Combine(ws, rel.Replace('/', Path.DirectorySeparatorChar)));
        if (!Directory.Exists(dir))
            return;

        var sln = FindSolutionFileInDirectory(dir);
        if (string.IsNullOrEmpty(sln))
        {
            GitPanelStatusText =
                "В каталоге submodule не найден .sln / .slnx (корень и подпапки первого уровня).";
            return;
        }

        LoadSolution(sln);
    }

    private bool CanOpenSubmoduleSolutionInIde()
    {
        if (!IsGitRepository || SelectedGitStatusRow is not { IsSubmodulePath: true, RelativePath: var rel })
            return false;

        var ws = GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(ws))
            return false;

        var full = Path.GetFullPath(Path.Combine(ws, rel.Replace('/', Path.DirectorySeparatorChar)));
        return Directory.Exists(full) && FindSolutionFileInDirectory(full) is not null;
    }

    [RelayCommand(CanExecute = nameof(CanRunGitSubmoduleCommands))]
    private async Task GitSubmoduleUpdateInitAsync()
    {
        GitSubmoduleCommandOutput = "git submodule update --init --recursive …\n";
        var r = await RunGitCommandAsync(["submodule", "update", "--init", "--recursive"]).ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            GitSubmoduleCommandOutput = TruncateOutput(r.Output, 8000);
            GitPanelStatusText = r.Success ? "" : "Submodule update завершился с ошибкой (см. вывод ниже).";
        });
        await RefreshGitPanelAsync().ConfigureAwait(false);
        await RefreshGitSummaryAsync().ConfigureAwait(false);
    }

    [RelayCommand(CanExecute = nameof(CanRunGitSubmoduleCommands))]
    private async Task GitSubmoduleSyncAsync()
    {
        GitSubmoduleCommandOutput = "git submodule sync --recursive …\n";
        var r = await RunGitCommandAsync(["submodule", "sync", "--recursive"]).ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            GitSubmoduleCommandOutput = TruncateOutput(r.Output, 8000);
            if (!r.Success)
                GitPanelStatusText = "Submodule sync завершился с ошибкой (см. вывод ниже).";
        });
        await RefreshGitPanelAsync().ConfigureAwait(false);
        await RefreshGitSummaryAsync().ConfigureAwait(false);
    }

    private bool CanRunGitSubmoduleCommands() => IsGitRepository;

    [RelayCommand(CanExecute = nameof(CanCommitGitStagedOnly))]
    private async Task CommitGitStagedOnlyAsync()
    {
        var msg = GitCommitMessage.Trim();
        if (string.IsNullOrWhiteSpace(msg) || !IsGitRepository)
            return;

        GitPanelStatusText = "Коммит (только staged)…";
        var result = await RunGitCommandAsync(["commit", "-m", msg]).ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            GitPanelStatusText = result.Success ? "" : result.Output;
            if (result.Success)
                GitCommitMessage = "";
        });
        await RefreshGitPanelAsync().ConfigureAwait(false);
        await RefreshGitSummaryAsync().ConfigureAwait(false);
    }

    private bool CanCommitGitStagedOnly() => IsGitRepository && !string.IsNullOrWhiteSpace(GitCommitMessage);

    [RelayCommand]
    private void ToggleInstrumentationDock() => IsInstrumentationDockVisible = !IsInstrumentationDockVisible;

    [RelayCommand]
    private void SetSingleEditorGroup() => EditorGroupCount = 1;

    [RelayCommand]
    private void SetDualEditorGroup() => EditorGroupCount = 2;

    [RelayCommand]
    private void SetTripleEditorGroup() => EditorGroupCount = 3;

    [RelayCommand]
    private void ActivateDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is null)
            OpenOrActivateDocument(filePath);
        else
            ActivateDocumentInternal(doc);
    }

    [RelayCommand]
    private void CloseDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is null)
            return;

        var index = OpenDocuments.IndexOf(doc);
        GetGroupCollection(doc.GroupIndex).Remove(doc);
        OpenDocuments.Remove(doc);
        var dockDoc = DockDocuments
            .OfType<DockDocumentViewModel>()
            .FirstOrDefault(d => string.Equals(d.Doc.FilePath, doc.FilePath, StringComparison.OrdinalIgnoreCase));
        if (dockDoc is not null)
            DockDocuments.Remove(dockDoc);
        _recentlyClosedDocumentPaths.Push(doc.FilePath);
        _recentlyClosedDocumentCount = _recentlyClosedDocumentPaths.Count;
        ReopenClosedDocumentCommand.NotifyCanExecuteChanged();

        if (OpenDocuments.Count == 0)
        {
            SelectedDocument = null;
            SelectedDocumentGroup2 = null;
            SelectedDocumentGroup3 = null;
            return;
        }

        SelectedDocument = Group1Documents.FirstOrDefault();
        SelectedDocumentGroup2 = Group2Documents.FirstOrDefault();
        SelectedDocumentGroup3 = Group3Documents.FirstOrDefault();

        // Ensure DockActiveDocument always points to a still-open dockable.
        var next =
            SelectedDocument ??
            SelectedDocumentGroup2 ??
            SelectedDocumentGroup3 ??
            OpenDocuments[Math.Clamp(index, 0, OpenDocuments.Count - 1)];

        ActivateDocumentInternal(next);
        RebuildAndReinitDockLayout();
    }

    [RelayCommand]
    private void TogglePinDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            doc.IsPinned = !doc.IsPinned;
    }

    [RelayCommand]
    private void MoveDocumentToGroup1(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            MoveDocumentToGroupInternal(doc, 1);
    }

    [RelayCommand]
    private void MoveDocumentToGroup2(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            MoveDocumentToGroupInternal(doc, 2);
    }

    [RelayCommand]
    private void MoveDocumentToGroup3(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
        var doc = OpenDocuments.FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (doc is not null)
            MoveDocumentToGroupInternal(doc, 3);
    }

    [RelayCommand(CanExecute = nameof(CanReopenClosedDocument))]
    private void ReopenClosedDocument()
    {
        if (_recentlyClosedDocumentPaths.Count == 0)
            return;
        var path = _recentlyClosedDocumentPaths.Pop();
        _recentlyClosedDocumentCount = _recentlyClosedDocumentPaths.Count;
        ReopenClosedDocumentCommand.NotifyCanExecuteChanged();
        OpenOrActivateDocument(path);
    }

    private bool CanReopenClosedDocument() => _recentlyClosedDocumentCount > 0;

    [RelayCommand]
    private void ShowSolutionExplorerPanel() => IsSolutionExplorerVisible = true;

    [RelayCommand]
    private void ShowBuildOutputPanel() => IsBuildOutputVisible = true;

    [RelayCommand]
    private void ShowChatPanel() => IsChatPanelExpanded = true;

    [RelayCommand]
    private void ShowTerminalPanel() => IsTerminalVisible = true;

    [RelayCommand]
    private void SetFocusMode()
    {
        UiMode = "Focus";
    }

    [RelayCommand]
    private void SetBalancedMode()
    {
        UiMode = "Balanced";
    }

    [RelayCommand]
    private void SetPowerMode()
    {
        UiMode = "Power";
    }

    [RelayCommand]
    private void CycleUiMode()
    {
        UiMode = UiMode switch
        {
            "Focus" => "Balanced",
            "Balanced" => "Power",
            _ => "Focus"
        };
    }

    [RelayCommand]
    private void SetSafetyL1() => SafetyLevel = "L1";

    [RelayCommand]
    private void SetSafetyL2() => SafetyLevel = "L2";

    [RelayCommand]
    private void SetSafetyL3() => SafetyLevel = "L3";

    [RelayCommand]
    private void FixFailingTests()
    {
        IsChatPanelExpanded = true;
        ChatInput = "Fix failing tests using minimal-risk changes. Start with ide_run_affected_tests and explain each step.";
    }

    [RelayCommand]
    private void InvestigateNullref()
    {
        IsChatPanelExpanded = true;
        ChatInput = "Investigate possible null reference in current context. Show the shortest safe fix plan.";
    }

    [RelayCommand]
    private void PrepareCommit()
    {
        IsChatPanelExpanded = true;
        ChatInput = "Prepare a clean commit plan grouped by logical changes and include verification steps.";
    }

    [RelayCommand]
    private void ExplainCurrentStep()
    {
        IsChatPanelExpanded = true;
        ChatInput = "Explain the current autonomous step in plain language: intent, tool call, risk, and rollback.";
    }

    [RelayCommand]
    private void ExplainTraceStep(AgentTraceStepViewModel? step)
    {
        if (step is null)
            return;
        IsChatPanelExpanded = true;
        ChatInput =
            $"Объясни шаг трассы [{step.Kind} / {step.Status}] ({step.TimestampText}): {step.Text}. Укажи намерение, риск и откат.";
    }

    [RelayCommand]
    private void RollbackTraceStep(AgentTraceStepViewModel? step)
    {
        if (step is null)
            return;
        EventTimeline.Insert(0, $"{DateTime.Now:HH:mm:ss} — Запрошен откат для шага [{step.Kind}]");
        IsChatPanelExpanded = true;
        ChatInput =
            $"Предложи минимальный откат для шага [{step.Kind}] ({step.TimestampText}): {step.Text}. Проверь состояние workspace.";
    }

    /// <summary>Добавить шаг в Agent Trace Timeline (Power); потокобезопасно.</summary>
    public void AppendAgentTraceStep(string kind, string text, string status, DateTimeOffset? at = null)
    {
        void Add()
        {
            AgentTraceSteps.Add(new AgentTraceStepViewModel(kind, text, status, at));
            while (AgentTraceSteps.Count > 200)
                AgentTraceSteps.RemoveAt(0);
        }

        if (Dispatcher.UIThread.CheckAccess())
            Add();
        else
            Dispatcher.UIThread.Post(Add);
    }

    private void RefreshWorkspaceSnapshotCore()
    {
        try
        {
            var json = GetUiLayoutProvider?.Invoke() ?? "{}";
            if (json.Length > 4000)
                json = json[..4000] + "\n…";
            WorkspaceSnapshotJson = json;
        }
        catch (Exception ex)
        {
            WorkspaceSnapshotJson = JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [RelayCommand]
    private void RefreshWorkspaceSnapshot() => RefreshWorkspaceSnapshotCore();

    [RelayCommand]
    private void EmergencyStop()
    {
        IsBuilding = false;
        ActiveTaskStatus = "Paused";
        ResultSummary = "Autonomous flow paused by operator.";
        EventTimeline.Insert(0, $"{DateTime.Now:HH:mm:ss} — Emergency stop engaged");
    }

    /// <summary>Focus: зафиксировать контрольную точку в таймлайне и кратком результате.</summary>
    [RelayCommand]
    private void FocusCheckpoint()
    {
        var stamp = DateTime.Now;
        EventTimeline.Insert(0, $"{stamp:HH:mm:ss} — Контрольная точка");
        ResultSummary = $"Контрольная точка: {stamp:yyyy-MM-dd HH:mm}";
    }

    /// <summary>Focus: запрос на откат — подсказка в чат и событие в таймлайне.</summary>
    [RelayCommand]
    private void FocusRollback()
    {
        EventTimeline.Insert(0, $"{DateTime.Now:HH:mm:ss} — Запрошен откат");
        IsChatPanelExpanded = true;
        ChatInput = "Помоги безопасно откатить последние изменения (git или патчи). Оцени риск и предложи минимальный набор команд.";
    }

    /// <summary>Focus: подтвердить текущий шаг в гейте.</summary>
    [RelayCommand]
    private void ConfirmFocusStep()
    {
        EventTimeline.Insert(0, $"{DateTime.Now:HH:mm:ss} — Шаг подтверждён");
        ActiveTaskStatus = "В работе";
    }

    /// <summary>Focus: отменить предложенный шаг.</summary>
    [RelayCommand]
    private void CancelFocusStep()
    {
        EventTimeline.Insert(0, $"{DateTime.Now:HH:mm:ss} — Шаг отменён");
        NextActionSummary = "Ожидание следующего шага.";
        ActiveTaskStatus = "Ожидание";
    }

    [RelayCommand]
    private async Task RunTerminalCommandAsync()
    {
        var cmd = TerminalInput?.Trim() ?? "";
        if (string.IsNullOrEmpty(cmd))
            return;
        TerminalInput = "";
        var workDir = !string.IsNullOrWhiteSpace(SolutionPath) && File.Exists(SolutionPath)
            ? Path.GetDirectoryName(SolutionPath) ?? Environment.CurrentDirectory
            : Environment.CurrentDirectory;
        TerminalOutput += $"> {cmd}\r\n";
        try
        {
            var isWin = Environment.OSVersion.Platform == PlatformID.Win32NT;
            var psi = new System.Diagnostics.ProcessStartInfo(isWin ? "cmd" : "sh")
            {
                ArgumentList = { isWin ? "/c" : "-c", cmd },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workDir
            };
            using var process = System.Diagnostics.Process.Start(psi);
            if (process is null)
            {
                TerminalOutput += "Не удалось запустить процесс.\r\n";
                return;
            }
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdout, stderr).ConfigureAwait(true);
            await process.WaitForExitAsync().ConfigureAwait(true);
            var outStr = await stdout;
            var errStr = await stderr;
            if (outStr.Length > 0) TerminalOutput += outStr;
            if (errStr.Length > 0) TerminalOutput += errStr;
            if (process.ExitCode != 0)
                TerminalOutput += $"\r\nExit code: {process.ExitCode}\r\n";
        }
        catch (Exception ex)
        {
            TerminalOutput += ex.Message + "\r\n";
        }
    }

    [RelayCommand(CanExecute = nameof(CanToggleChatPanel))]
    private void ToggleChatPanel()
    {
        IsChatPanelExpanded = !IsChatPanelExpanded;
    }

    private static bool CanToggleChatPanel() => true;

    [RelayCommand(CanExecute = nameof(CanBuildSolution))]
    private async Task BuildSolutionAsync()
    {
        if (string.IsNullOrWhiteSpace(SolutionPath) || !File.Exists(SolutionPath))
            return;

        IsBuilding = true;
        IsBuildOutputVisible = true;
        BuildOutput = $"Сборка: {SolutionPath}\r\n";

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("dotnet")
            {
                ArgumentList = { "build", SolutionPath },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(SolutionPath) ?? ""
            };
            using var process = System.Diagnostics.Process.Start(psi);
            if (process is null)
            {
                BuildOutput += "Не удалось запустить dotnet build.\r\n";
                return;
            }

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdout, stderr);
            await process.WaitForExitAsync();

            BuildOutput += await stdout + "\r\n" + await stderr;
            if (process.ExitCode != 0)
                BuildOutput += $"\r\nКод выхода: {process.ExitCode}";
        }
        catch (Exception ex)
        {
            BuildOutput += "Ошибка: " + ex.Message + "\r\n";
        }
        finally
        {
            IsBuilding = false;
        }
    }

    private bool CanBuildSolution() => !string.IsNullOrWhiteSpace(SolutionPath) && File.Exists(SolutionPath) && !IsBuilding;

    [RelayCommand]
    private void HideBuildOutput()
    {
        IsBuildOutputVisible = false;
    }

    public void LoadSolution(string path)
    {
        _ = LoadSolutionAsync(path);
    }

    /// <summary>Загрузка решения в фоне, чтобы не блокировать UI.</summary>
    public async Task LoadSolutionAsync(string path)
    {
        var loadVersion = Interlocked.Increment(ref _solutionLoadVersion);
        SolutionLoadError = "";
        try
        {
            var (root, error) = await Task.Run(() =>
            {
                var r = Services.SolutionParser.Load(path, out var err);
                return (r, err);
            }).ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Ignore stale completion when a newer ide_load_solution call already started.
                if (loadVersion != Interlocked.Read(ref _solutionLoadVersion))
                    return;

                if (root is null)
                {
                    SolutionLoadError = error ?? "Не удалось загрузить решение.";
                    return;
                }

                var normalizedSolutionPath = root.FullPath;
                if (string.IsNullOrEmpty(normalizedSolutionPath))
                {
                    try { normalizedSolutionPath = Path.GetFullPath(path); }
                    catch { normalizedSolutionPath = path; }
                }

                // New solution becomes authoritative UI context: clear stale editor selection/state.
                _openFileDebounceCts?.Cancel();
                SelectedSolutionItem = null;
                OpenDocuments.Clear();
                Group1Documents.Clear();
                Group2Documents.Clear();
                Group3Documents.Clear();
                DockDocuments.Clear();
                DockActiveDocument = null;
                _recentlyClosedDocumentPaths.Clear();
                _recentlyClosedDocumentCount = 0;
                ReopenClosedDocumentCommand.NotifyCanExecuteChanged();
                SelectedDocument = null;
                SelectedDocumentGroup2 = null;
                SelectedDocumentGroup3 = null;
                CurrentFilePath = null;
                EditorText = "";
                IsLoadingCurrentFile = false;

                SolutionPath = normalizedSolutionPath ?? path;
                SolutionRoots.Clear();
                SolutionRoots.Add(root);

                RebuildAndReinitDockLayout();
            });
        }
        catch (Exception ex)
        {
            SolutionLoadError = "Ошибка загрузки решения: " + ex.Message;
            TryLogLoadSolutionCrash(path, ex);
        }
    }

    private void TryLogLoadSolutionCrash(string? solutionPath, Exception ex)
    {
        try
        {
            var baseDir = "";
            if (!string.IsNullOrWhiteSpace(solutionPath))
            {
                try
                {
                    var full = Path.GetFullPath(solutionPath);
                    baseDir = File.Exists(full) ? (Path.GetDirectoryName(full) ?? "") : full;
                }
                catch
                {
                    baseDir = "";
                }
            }

            if (string.IsNullOrWhiteSpace(baseDir))
                baseDir = Environment.CurrentDirectory;

            var logDir = Path.Combine(baseDir, ".cascade-ide");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "crash-log.txt");
            var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff 'UTC'");
            var payload =
                $"[{stamp}] LoadSolution crash{Environment.NewLine}" +
                $"solution: {solutionPath}{Environment.NewLine}" +
                $"{ex}{Environment.NewLine}" +
                $"---{Environment.NewLine}";
            File.AppendAllText(logPath, payload);
        }
        catch
        {
            // Do not throw from crash logger.
        }
    }

    [RelayCommand(CanExecute = nameof(CanSendChat))]
    private async Task SendChatAsync()
    {
        var input = ChatInput.Trim();
        if (string.IsNullOrEmpty(input))
            return;

        ChatInput = "";
        ChatMessages.Add(new ChatMessageViewModel("user", input));
        IsChatLoading = true;

        try
        {
            var messages = ChatMessages.Take(ChatMessages.Count - 1)
                .Select(m => new Services.ChatMessage(m.Role, m.Content))
                .Append(new Services.ChatMessage("user", input))
                .ToList();
            var assistantMsg = new ChatMessageViewModel("assistant", "");
            ChatMessages.Add(assistantMsg);

            await foreach (var token in _aiProviderManager.StreamChatAsync(
                ActiveAiProvider,
                messages,
                CurrentFilePath,
                EditorText,
                UseMinimizedContext,
                CancellationToken.None))
            {
                assistantMsg.Content += token;
            }
        }
        finally
        {
            IsChatLoading = false;
        }
    }

    private bool CanSendChat() => !string.IsNullOrWhiteSpace(ChatInput)
        && !IsChatLoading
        && (ActiveAiProvider != "Ollama" || (!string.IsNullOrEmpty(SelectedOllamaModel) && SelectedOllamaModel != InstallNewSentinel));

    [RelayCommand(CanExecute = nameof(CanInstallModel))]
    private async Task InstallModelAsync()
    {
        var model = ModelToInstall?.Trim() ?? "";
        if (string.IsNullOrEmpty(model) || !OllamaAvailable)
            return;

        IsPullingModel = true;
        PullModelProgress = $"Скачивание {model}…";

        try
        {
            await foreach (var status in _ollama.PullModelAsync(model, CancellationToken.None))
            {
                var s = status;
                Dispatcher.UIThread.Post(() => PullModelProgress = s);
            }
            PullModelProgress = "Готово.";
            await RefreshOllamaAsync();
        }
        catch (Exception ex)
        {
            PullModelProgress = "Ошибка: " + ex.Message;
        }
        finally
        {
            IsPullingModel = false;
        }
    }

    private bool CanInstallModel() => OllamaAvailable && !string.IsNullOrWhiteSpace(ModelToInstall) && !IsPullingModel;

    async Task<string> Services.IIdeMcpActions.ExecuteCommandAsync(string commandId, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken)
    {
        static string? S(IReadOnlyDictionary<string, JsonElement>? a, string key) => a is not null && a.TryGetValue(key, out var e) ? e.GetString() : null;
        static int I(IReadOnlyDictionary<string, JsonElement>? a, string key, int def = 0) => a is not null && a.TryGetValue(key, out var e) && e.TryGetInt32(out var v) ? v : def;
        static bool B(IReadOnlyDictionary<string, JsonElement>? a, string key, bool def = false) => a is not null && a.TryGetValue(key, out var e) && (e.ValueKind is JsonValueKind.True or JsonValueKind.False) ? e.GetBoolean() : def;
        static IReadOnlyList<string>? SA(IReadOnlyDictionary<string, JsonElement>? a, string key)
        {
            if (a is null || !a.TryGetValue(key, out var e) || e.ValueKind != JsonValueKind.Array)
                return null;
            var values = new List<string>();
            foreach (var item in e.EnumerateArray())
            {
                var value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    values.Add(value);
            }
            return values;
        }

        var a = (Services.IIdeMcpActions)this;
        switch (commandId)
        {
            case Services.IdeCommands.OpenFile:
                if (string.IsNullOrEmpty(S(args, "path"))) return "Missing path";
                a.OpenFile(S(args, "path")!);
                return "OK";
            case Services.IdeCommands.LoadSolution:
                if (string.IsNullOrEmpty(S(args, "path"))) return "Missing path";
                a.LoadSolution(S(args, "path")!);
                return "OK";
            case Services.IdeCommands.Select:
                if (args is null || string.IsNullOrEmpty(S(args, "file_path"))) return "Missing file_path";
                a.SelectInEditor(S(args, "file_path"), I(args, "start_line"), I(args, "start_column"), I(args, "end_line"), I(args, "end_column"));
                return "OK";
            case Services.IdeCommands.SetBreakpoint:
                if (args is null || string.IsNullOrEmpty(S(args, "file_path")) || !args.TryGetValue("line", out _)) return "Missing file_path or line";
                await Dispatcher.UIThread.InvokeAsync(() => a.SetBreakpoint(S(args, "file_path")!, I(args, "line", 1), S(args, "condition")));
                return "OK";
            case Services.IdeCommands.RemoveBreakpoint:
                if (args is null || string.IsNullOrEmpty(S(args, "file_path")) || !args.TryGetValue("line", out _)) return "Missing file_path or line";
                await Dispatcher.UIThread.InvokeAsync(() => a.RemoveBreakpoint(S(args, "file_path")!, I(args, "line", 1)));
                return "OK";
            case Services.IdeCommands.ShowPreview:
                a.ShowPreview(S(args, "title") ?? "", S(args, "content") ?? "");
                return "OK";
            case Services.IdeCommands.ShowEditorPreview:
                a.ShowEditorPreview();
                return "OK";
            case Services.IdeCommands.RequestConfirmation:
                return await a.RequestConfirmationAsync(S(args, "message") ?? "", cancellationToken);
            case Services.IdeCommands.GetEditorState:
                return await a.GetEditorStateAsync(args is not null && args.TryGetValue("max_preview_chars", out var mpc) && mpc.TryGetInt32(out var maxPreview) ? maxPreview : null);
            case Services.IdeCommands.GetEditorContentRange:
                return await a.GetEditorContentRangeAsync(I(args, "start_line", 1), I(args, "end_line", 1));
            case Services.IdeCommands.ApplyEdit:
                if (args is null || string.IsNullOrEmpty(S(args, "file_path")) || !args.TryGetValue("new_text", out _)) return "Missing arguments";
                a.ApplyEdit(S(args, "file_path")!, I(args, "start_line"), I(args, "start_column"), I(args, "end_line"), I(args, "end_column"), S(args, "new_text") ?? "");
                return "OK";
            case Services.IdeCommands.GoToPosition:
                if (args is null || string.IsNullOrEmpty(S(args, "file_path")) || !args.TryGetValue("line", out _) || !args.TryGetValue("column", out _)) return "Missing file_path, line or column";
                int? endLine = args.TryGetValue("end_line", out var el) && el.TryGetInt32(out var endL) ? endL : null;
                int? endCol = args.TryGetValue("end_column", out var ec) && ec.TryGetInt32(out var endC) ? endC : null;
                a.GoToPosition(S(args, "file_path"), I(args, "line"), I(args, "column"), endLine, endCol);
                return "OK";
            case Services.IdeCommands.GetSolutionInfo:
                return a.GetSolutionInfo();
            case Services.IdeCommands.GetWorkspaceState:
                return await a.GetWorkspaceStateAsync();
            case Services.IdeCommands.GetSolutionFiles:
                return await a.GetSolutionFilesAsync();
            case Services.IdeCommands.GetCurrentFileDiagnostics:
                return await a.GetCurrentFileDiagnosticsAsync();
            case Services.IdeCommands.Build:
                return await a.BuildAsync();
            case Services.IdeCommands.BuildStructured:
                return await a.BuildStructuredAsync();
            case Services.IdeCommands.RunTests:
                return await a.RunTestsAsync();
            case Services.IdeCommands.RunAffectedTests:
                return await a.RunAffectedTestsAsync(SA(args, "changed_paths"));
            case Services.IdeCommands.RunCodeCleanup:
                return await a.RunCodeCleanupAsync(S(args, "include_path"));
            case Services.IdeCommands.GetCodeMetrics:
                return await a.GetCodeMetricsAsync(S(args, "scope"), S(args, "path"));
            case Services.IdeCommands.GitStatus:
                return await a.GitStatusAsync();
            case Services.IdeCommands.GitDiff:
                return await a.GitDiffAsync(S(args, "path"), B(args, "staged"));
            case Services.IdeCommands.GitCommit:
                if (string.IsNullOrWhiteSpace(S(args, "message"))) return "Missing message";
                return await a.GitCommitAsync(S(args, "message")!, SA(args, "paths"));
            case Services.IdeCommands.GitPush:
                return await a.GitPushAsync(S(args, "remote"), S(args, "branch"));
            case Services.IdeCommands.GetBuildOutput:
                return a.GetBuildOutput();
            case Services.IdeCommands.FocusEditor:
                a.FocusEditor();
                return "OK";
            case Services.IdeCommands.GetUiTheme:
                return a.GetUiTheme();
            case Services.IdeCommands.SetUiTheme:
                return await a.SetUiThemeAsync(S(args, "theme") ?? "");
            case Services.IdeCommands.GetUiLayout:
                return await a.GetUiLayoutAsync();
            case Services.IdeCommands.GetColorsUnderCursor:
                return await a.GetColorsUnderCursorAsync();
            case Services.IdeCommands.GetControlAppearance:
                return await a.GetControlAppearanceAsync(S(args, "name"));
            case Services.IdeCommands.SetControlLayout:
                if (args is null || string.IsNullOrEmpty(S(args, "name"))) return "Missing name or layout";
                return await a.SetControlLayoutAsync(S(args, "name")!, S(args, "layout") ?? "{}");
            case Services.IdeCommands.SetControlText:
                return await a.SetControlTextAsync(S(args, "name") ?? "", S(args, "text") ?? "");
            case Services.IdeCommands.ClickControl:
                return await a.ClickControlAsync(S(args, "name"));
            case Services.IdeCommands.SendKeys:
                return await a.SendKeysAsync(S(args, "name"), S(args, "keys") ?? "");
            case Services.IdeCommands.SetFocus:
                return await a.SetFocusAsync(S(args, "name"));
            case Services.IdeCommands.HighlightControl:
                return await a.HighlightControlAsync(S(args, "name"));
            case Services.IdeCommands.SetPanelSize:
                double? w = args is not null && args.TryGetValue("width", out var pw) && pw.TryGetDouble(out var wv) ? wv : null;
                double? h = args is not null && args.TryGetValue("height", out var ph) && ph.TryGetDouble(out var hv) ? hv : null;
                return await a.SetPanelSizeAsync(S(args, "panel") ?? "", w, h);
            case Services.IdeCommands.GetSupportedEditorLanguages:
                return a.GetSupportedEditorLanguages();
            case Services.IdeCommands.ShowBreakpoints:
                return ParseAndShowDebugBreakpoints(a, args);
            case Services.IdeCommands.ShowDebugPosition:
                a.ShowDebugPosition(S(args, "file_path"), I(args, "line"));
                return "OK";
            case Services.IdeCommands.ShowDebugState:
                return ParseAndShowDebugState(a, args);
#if DEBUG
            case Services.IdeCommands.AddControl:
                return await a.AddControlAsync(S(args, "parent_name") ?? "", S(args, "control_type") ?? "", S(args, "content"), S(args, "name"));
#endif
            case Services.IdeCommands.WriteAgentNotes:
                return await a.WriteAgentNotesAsync(S(args, "content") ?? "", cancellationToken);
            case Services.IdeCommands.ReadAgentNotes:
                return await a.ReadAgentNotesAsync(cancellationToken);
            default:
                return $"Unknown command: {commandId}";
        }
    }

    private static string ParseAndShowDebugBreakpoints(Services.IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null || !args.TryGetValue("breakpoints", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return "Missing breakpoints (array of { file_path, line })";
        var list = new List<(string, int)>();
        foreach (var item in arr.EnumerateArray())
        {
            if (!item.TryGetProperty("file_path", out var fp) || !item.TryGetProperty("line", out var ln)) continue;
            var path = fp.GetString();
            if (string.IsNullOrEmpty(path)) continue;
            list.Add((path, ln.GetInt32()));
        }
        actions.ShowDebugBreakpoints(list);
        return "OK";
    }

    private static string ParseAndShowDebugState(Services.IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        var stackFrames = new List<(string, string?, int)>();
        var variables = new List<(string, string)>();
        if (args is not null)
        {
            if (args.TryGetValue("stack_frames", out var sf) && sf.ValueKind == JsonValueKind.Array)
                foreach (var item in sf.EnumerateArray())
                    stackFrames.Add((item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "", item.TryGetProperty("file", out var f) ? f.GetString() : null, item.TryGetProperty("line", out var l) ? l.GetInt32() : 0));
            if (args.TryGetValue("variables", out var v) && v.ValueKind == JsonValueKind.Array)
                foreach (var item in v.EnumerateArray())
                    if (item.TryGetProperty("name", out var vn) && item.TryGetProperty("value", out var vv))
                        variables.Add((vn.GetString() ?? "", vv.GetString() ?? ""));
        }
        actions.ShowDebugState(stackFrames, variables);
        return "OK";
    }

    void Services.IIdeMcpActions.OpenFile(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        var pathCopy = path;
        Dispatcher.UIThread.Post(() =>
        {
            if (!File.Exists(pathCopy))
                return;
            var normalizedPath = Path.GetFullPath(pathCopy);
            IsLoadingCurrentFile = true;
            try
            {
                OpenOrActivateDocument(normalizedPath);
            }
            finally
            {
                IsLoadingCurrentFile = false;
            }
        });
    }

    void Services.IIdeMcpActions.LoadSolution(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        var pathCopy = path;
        Dispatcher.UIThread.Post(() => LoadSolution(pathCopy));
    }

    void Services.IIdeMcpActions.SelectInEditor(string? filePath, int startLine, int startColumn, int endLine, int endColumn)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!string.IsNullOrEmpty(filePath) && filePath != CurrentFilePath && File.Exists(filePath))
            {
                IsLoadingCurrentFile = true;
                try
                {
                    OpenOrActivateDocument(filePath);
                }
                finally { IsLoadingCurrentFile = false; }
            }
            var text = EditorText ?? "";
            int start = LineColumnToOffset(text, startLine, startColumn);
            int end = LineColumnToOffset(text, endLine, endColumn);
            if (start < 0 || end < 0)
                return;
            int len = Math.Max(0, end - start);
            EditorSelectionStart = start;
            EditorSelectionLength = len;
        });
    }

    private static int LineColumnToOffset(string text, int line, int column)
    {
        if (line < 1 || column < 1)
            return -1;
        var lines = text.Split('\n');
        if (line > lines.Length)
            return -1;
        int offset = 0;
        for (int i = 0; i < line - 1; i++)
            offset += lines[i].Length + 1; // +1 for \n
        int lineLen = lines[line - 1].Length;
        int col = Math.Min(column, lineLen + 1);
        return offset + (col - 1);
    }

    async Task<string> Services.IIdeMcpActions.GetEditorStateAsync(int? maxPreviewChars)
    {
        var tcs = new TaskCompletionSource<string>();
        var preview = maxPreviewChars ?? 2000;
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var dto = _editorStateProvider?.Invoke(preview) ?? new Services.EditorStateDto();
                tcs.SetResult(JsonSerializer.Serialize(dto));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return await tcs.Task.ConfigureAwait(false);
    }

    async Task<string> Services.IIdeMcpActions.GetEditorContentRangeAsync(int startLine, int endLine)
    {
        var tcs = new TaskCompletionSource<string>();
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var content = _editorContentRangeProvider?.Invoke(startLine, endLine);
                var obj = new
                {
                    file_path = CurrentFilePath,
                    start_line = startLine,
                    end_line = endLine,
                    content = content ?? ""
                };
                tcs.SetResult(JsonSerializer.Serialize(obj));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return await tcs.Task.ConfigureAwait(false);
    }

    void Services.IIdeMcpActions.ApplyEdit(string filePath, int startLine, int startColumn, int endLine, int endColumn, string newText)
    {
        Dispatcher.UIThread.Post(() => _applyEditAction?.Invoke(filePath, startLine, startColumn, endLine, endColumn, newText));
    }

    void Services.IIdeMcpActions.GoToPosition(string? filePath, int line, int column, int? endLine, int? endColumn)
    {
        ((Services.IIdeMcpActions)this).SelectInEditor(filePath, line, column, endLine ?? line, endColumn ?? column);
    }

    string Services.IIdeMcpActions.GetSolutionInfo()
    {
        var path = SolutionPath ?? "";
        var current = CurrentFilePath ?? "";
        var projects = CollectProjectPaths(SolutionRoots).ToList();
        var selected = SelectedSolutionItem?.FullPath ?? "";
        return JsonSerializer.Serialize(new { solution_path = path, current_file_path = current, project_paths = projects, selected_solution_path = selected });
    }

    string Services.IIdeMcpActions.GetBuildOutput()
    {
        var (bg, fg) = Services.UiThemeSnapshot.GetBuildOutputTheme();
        return JsonSerializer.Serialize(new { text = BuildOutput ?? "", theme = new { background = bg, foreground = fg } });
    }

    async Task<string> Services.IIdeMcpActions.GetWorkspaceStateAsync()
    {
        var diagnosticsJson = await ((Services.IIdeMcpActions)this).GetCurrentFileDiagnosticsAsync().ConfigureAwait(false);
        JsonElement diagnostics;
        try { diagnostics = JsonSerializer.Deserialize<JsonElement>(diagnosticsJson); }
        catch { diagnostics = JsonSerializer.SerializeToElement(Array.Empty<object>()); }

        var buildText = BuildOutput ?? "";
        if (buildText.Length > 2000)
            buildText = buildText[..2000] + "\n... (output truncated)";

        var state = new
        {
            solution_path = SolutionPath,
            current_file_path = CurrentFilePath,
            selected_solution_path = SelectedSolutionItem?.FullPath,
            editor = new
            {
                content_length = (EditorText ?? "").Length,
                selection_start = EditorSelectionStart,
                selection_length = EditorSelectionLength
            },
            breakpoints = new
            {
                current_file = AllBreakpointLinesInCurrentFile,
                debugger_count = _debuggerBreakpoints.Count
            },
            debug = new
            {
                position_file = DebugPositionFile,
                position_line = DebugPositionLine,
                stack_count = DebugStackFrames.Count,
                variables_count = DebugVariables.Count
            },
            build = new
            {
                is_visible = IsBuildOutputVisible,
                output_preview = buildText,
                binlog_path = _lastBuildBinlogPath
            },
            diagnostics
        };
        return JsonSerializer.Serialize(state);
    }

    async Task<string> Services.IIdeMcpActions.GetCodeMetricsAsync(string? scope, string? path)
    {
        var files = ResolveMetricFiles(scope, path).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (files.Count == 0)
            return JsonSerializer.Serialize(new { success = false, error = "No C# files resolved for metrics." });

        var perFile = new List<object>();
        var topMethods = new List<(string File, string Method, int Line, int Complexity)>();
        int totalLoc = 0, totalClasses = 0, totalMethods = 0, complexityTotal = 0, complexityMax = 0;

        foreach (var file in files)
        {
            string text;
            try { text = await File.ReadAllTextAsync(file).ConfigureAwait(false); }
            catch { continue; }

            var tree = CSharpSyntaxTree.ParseText(text);
            var root = await tree.GetRootAsync().ConfigureAwait(false);
            var methods = root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>().ToList();
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Count();
            var fileLoc = text.Split('\n').Count(static l => !string.IsNullOrWhiteSpace(l));

            int fileComplexity = 0;
            int fileMaxMethodComplexity = 0;
            foreach (var method in methods)
            {
                var complexity = CalculateCyclomaticComplexity(method);
                fileComplexity += complexity;
                if (complexity > fileMaxMethodComplexity)
                    fileMaxMethodComplexity = complexity;

                if (complexity >= 10)
                {
                    var methodName = method switch
                    {
                        MethodDeclarationSyntax m => m.Identifier.Text,
                        ConstructorDeclarationSyntax c => c.Identifier.Text,
                        DestructorDeclarationSyntax d => d.Identifier.Text,
                        _ => "method"
                    };
                    var line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    topMethods.Add((file, methodName, line, complexity));
                }
            }

            totalLoc += fileLoc;
            totalClasses += classes;
            totalMethods += methods.Count;
            complexityTotal += fileComplexity;
            complexityMax = Math.Max(complexityMax, fileMaxMethodComplexity);

            perFile.Add(new
            {
                file,
                loc = fileLoc,
                class_count = classes,
                method_count = methods.Count,
                cyclomatic_total = fileComplexity,
                cyclomatic_max_method = fileMaxMethodComplexity
            });
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            scope = string.IsNullOrWhiteSpace(scope) ? "current_file" : scope,
            file_count = perFile.Count,
            totals = new
            {
                loc = totalLoc,
                class_count = totalClasses,
                method_count = totalMethods,
                cyclomatic_total = complexityTotal,
                cyclomatic_max_method = complexityMax
            },
            files = perFile,
            hot_methods = topMethods
                .OrderByDescending(x => x.Complexity)
                .Take(20)
                .Select(x => new { file = x.File, method = x.Method, line = x.Line, complexity = x.Complexity })
        });
    }

    Task<string> Services.IIdeMcpActions.GitStatusAsync() => RunGitCommandJsonAsync(["status", "--short", "--branch"]);

    Task<string> Services.IIdeMcpActions.GitDiffAsync(string? path, bool staged)
    {
        var args = new List<string> { "diff" };
        if (staged)
            args.Add("--staged");
        if (!string.IsNullOrWhiteSpace(path))
        {
            args.Add("--");
            args.Add(path!);
        }
        return RunGitCommandJsonAsync(args);
    }

    async Task<string> Services.IIdeMcpActions.GitCommitAsync(string message, IReadOnlyList<string>? paths)
    {
        if (string.IsNullOrWhiteSpace(message))
            return JsonSerializer.Serialize(new { success = false, error = "Commit message is required." });

        var addArgs = new List<string> { "add" };
        if (paths is { Count: > 0 })
            addArgs.AddRange(paths.Where(p => !string.IsNullOrWhiteSpace(p)));
        else
            addArgs.Add("-A");

        var addResult = await RunGitCommandAsync(addArgs).ConfigureAwait(false);
        if (!addResult.Success)
            return JsonSerializer.Serialize(new { success = false, step = "add", exit_code = addResult.ExitCode, output = addResult.Output });

        var commitResult = await RunGitCommandAsync(["commit", "-m", message]).ConfigureAwait(false);
        _ = RefreshGitSummaryAsync();
        return JsonSerializer.Serialize(new
        {
            success = commitResult.Success,
            exit_code = commitResult.ExitCode,
            output = TruncateOutput(commitResult.Output, 4000)
        });
    }

    Task<string> Services.IIdeMcpActions.GitPushAsync(string? remote, string? branch)
    {
        var args = new List<string> { "push" };
        if (!string.IsNullOrWhiteSpace(remote))
            args.Add(remote!);
        if (!string.IsNullOrWhiteSpace(branch))
            args.Add(branch!);
        _ = RefreshGitSummaryAsync();
        return RunGitCommandJsonAsync(args);
    }

    private IReadOnlyList<string> ResolveMetricFiles(string? scope, string? path)
    {
        var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "current_file" : scope.Trim().ToLowerInvariant();
        return normalizedScope switch
        {
            "file" => ResolveFilesFromPath(path),
            "path" => ResolveFilesFromPath(path),
            "solution" => CollectFileEntries(SolutionRoots)
                .Select(e => e.FullPath)
                .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && File.Exists(p))
                .ToList(),
            _ => ResolveFilesFromPath(path ?? CurrentFilePath)
        };
    }

    private static IReadOnlyList<string> ResolveFilesFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Array.Empty<string>();

        try
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
                return fullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ? [fullPath] : Array.Empty<string>();
            if (!Directory.Exists(fullPath))
                return Array.Empty<string>();
            return Directory.EnumerateFiles(fullPath, "*.cs", SearchOption.AllDirectories).ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static int CalculateCyclomaticComplexity(SyntaxNode node)
    {
        var complexity = 1;
        foreach (var child in node.DescendantNodes())
        {
            switch (child)
            {
                case IfStatementSyntax:
                case ForStatementSyntax:
                case ForEachStatementSyntax:
                case WhileStatementSyntax:
                case DoStatementSyntax:
                case CaseSwitchLabelSyntax:
                case CatchClauseSyntax:
                case ConditionalExpressionSyntax:
                    complexity++;
                    break;
                case BinaryExpressionSyntax b when b.IsKind(SyntaxKind.LogicalAndExpression) || b.IsKind(SyntaxKind.LogicalOrExpression):
                    complexity++;
                    break;
            }
        }
        return complexity;
    }

    private async Task<string> RunGitCommandJsonAsync(IReadOnlyList<string> args)
    {
        var result = await RunGitCommandAsync(args).ConfigureAwait(false);
        return JsonSerializer.Serialize(new
        {
            success = result.Success,
            exit_code = result.ExitCode,
            output = TruncateOutput(result.Output, 4000)
        });
    }

    private async Task<(bool Success, int ExitCode, string Output)> RunGitCommandAsync(IReadOnlyList<string> args)
    {
        var workspace = GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(workspace) || !Directory.Exists(workspace))
            return (false, -1, "Workspace path is not available.");

        try
        {
            var psi = new ProcessStartInfo("git")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workspace
            };
            foreach (var arg in args)
                psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi);
            if (process is null)
                return (false, -1, "Failed to start git process.");

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);

            var output = (await stdout) + "\n" + (await stderr);
            return (process.ExitCode == 0, process.ExitCode, output.Trim());
        }
        catch (Exception ex)
        {
            return (false, -1, ex.Message);
        }
    }

    private static string TruncateOutput(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        return text.Length > maxChars ? text[..maxChars] + "\n... (output truncated)" : text;
    }

    private static IEnumerable<string> CollectProjectPaths(ObservableCollection<SolutionItem> roots)
    {
        foreach (var item in roots)
        {
            if (item.FullPath is { } p && (p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)))
                yield return p;
            foreach (var child in CollectProjectPaths(item.Children))
                yield return child;
        }
    }

    private static IEnumerable<(string Title, string FullPath)> CollectFileEntries(ObservableCollection<SolutionItem> roots)
    {
        foreach (var item in roots)
        {
            if (item.FullPath is { } p && !p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && !p.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) && !p.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
                yield return (item.Title, p);
            foreach (var child in CollectFileEntries(item.Children))
                yield return child;
        }
    }

    private static string? GetRelativePath(string? solutionPath, string? fullPath)
    {
        if (string.IsNullOrEmpty(solutionPath) || string.IsNullOrEmpty(fullPath))
            return null;
        var solutionDir = Path.GetDirectoryName(solutionPath);
        if (string.IsNullOrEmpty(solutionDir))
            return null;
        try
        {
            return Path.GetRelativePath(solutionDir, fullPath);
        }
        catch
        {
            return null;
        }
    }

    private static object BuildSolutionTreeNode(SolutionItem item, string? solutionPath)
    {
        var relative = GetRelativePath(solutionPath, item.FullPath);
        var path = item.FullPath;
        var title = item.Title;
        if (item.Children.Count == 0)
            return new { title, path, relative_path = relative };
        var children = item.Children.Select(c => BuildSolutionTreeNode(c, solutionPath)).ToList();
        return new { title, path, relative_path = relative, children };
    }

    /// <summary>Диагностики открытого .cs файла (ошибки и предупреждения Roslyn). JSON: массив { id, message, severity, line, column }. Для не-C# или при отсутствии файла — [].</summary>
    async Task<string> Services.IIdeMcpActions.GetCurrentFileDiagnosticsAsync()
    {
        var (path, text) = await Dispatcher.UIThread.InvokeAsync(() => (CurrentFilePath ?? "", EditorText ?? "")).GetTask();
        return await Task.Run(() => _contextMinimizer.GetDiagnosticsJson(path, text)).ConfigureAwait(false);
    }

    /// <summary>Список файлов и дерево решения. file_entries — плоский список с path, title, relative_path. solution_tree — иерархия (solution → projects → folders → files). Выполняется в UI-потоке.</summary>
    Task<string> Services.IIdeMcpActions.GetSolutionFilesAsync() =>
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var solutionPath = SolutionPath;
            var entries = CollectFileEntries(SolutionRoots).Select(e => new
            {
                path = e.FullPath,
                title = e.Title,
                relative_path = GetRelativePath(solutionPath, e.FullPath)
            }).ToList();
            var tree = SolutionRoots.Select(r => BuildSolutionTreeNode(r, solutionPath)).ToList();
            return JsonSerializer.Serialize(new { file_entries = entries, solution_tree = tree });
        }).GetTask();

    async Task<string> Services.IIdeMcpActions.BuildAsync()
    {
        var path = SolutionPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            var msg = "No solution loaded or file not found.";
            Dispatcher.UIThread.Post(() => { BuildOutput = msg + "\r\n"; IsBuildOutputVisible = true; });
            return msg;
        }
        try
        {
            var artifactsDir = Path.Combine(Path.GetDirectoryName(path) ?? "", ".cascade-ide", "build-artifacts");
            Directory.CreateDirectory(artifactsDir);
            var binlogPath = Path.Combine(artifactsDir, $"build-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.binlog");
            var psi = new System.Diagnostics.ProcessStartInfo("dotnet")
            {
                ArgumentList = { "build", path },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(path) ?? ""
            };
            psi.ArgumentList.Add($"-bl:{binlogPath}");
            using var process = System.Diagnostics.Process.Start(psi);
            if (process is null)
            {
                var msg = "Failed to start dotnet build.";
                Dispatcher.UIThread.Post(() => { BuildOutput = msg + "\r\n"; IsBuildOutputVisible = true; });
                return msg;
            }
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);
            var outStr = await stdout + "\r\n" + await stderr;
            if (process.ExitCode != 0)
                outStr += $"\r\nExit code: {process.ExitCode}";
            var pathCopy = path;
            Dispatcher.UIThread.Post(() =>
            {
                BuildOutput = $"Сборка: {pathCopy}\r\n{outStr}";
                IsBuildOutputVisible = true;
                _lastBuildBinlogPath = binlogPath;
            });
            return outStr;
        }
        catch (Exception ex)
        {
            var msg = "Error: " + ex.Message;
            Dispatcher.UIThread.Post(() => { BuildOutput = msg + "\r\n"; IsBuildOutputVisible = true; });
            return msg;
        }
    }

    async Task<string> Services.IIdeMcpActions.BuildStructuredAsync()
    {
        var raw = await ((Services.IIdeMcpActions)this).BuildAsync().ConfigureAwait(false);
        var parsed = BuildOutputParser.Parse(raw);
        const int maxRawChars = 4000;
        var rawTruncated = raw.Length > maxRawChars ? raw[..maxRawChars] + "\n... (output truncated)" : raw;
        var result = new
        {
            success = parsed.Success,
            exit_code = parsed.ExitCode,
            errors = parsed.Errors.Select(e => new { e.File, e.Line, e.Column, e.Code, e.Message }).ToList(),
            warnings = parsed.Warnings.Select(w => new { w.File, w.Line, w.Column, w.Code, w.Message }).ToList(),
            binlog_path = _lastBuildBinlogPath,
            raw_output = rawTruncated
        };
        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
    }

    async Task<string> Services.IIdeMcpActions.RunTestsAsync()
    {
        return await RunTestsInternalAsync(filterExpression: null, mode: "all").ConfigureAwait(false);
    }

    async Task<string> Services.IIdeMcpActions.RunAffectedTestsAsync(IReadOnlyList<string>? changedPaths)
    {
        var tokens = BuildAffectedTestTokens(changedPaths);
        if (tokens.Count == 0)
            return await RunTestsInternalAsync(filterExpression: null, mode: "fallback_all").ConfigureAwait(false);

        var filter = string.Join('|', tokens.Select(t => $"FullyQualifiedName~{t}"));
        return await RunTestsInternalAsync(filter, mode: "affected", tokens).ConfigureAwait(false);
    }

    private async Task<string> RunTestsInternalAsync(string? filterExpression, string mode, IReadOnlyList<string>? tokens = null)
    {
        var path = SolutionPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return JsonSerializer.Serialize(new { success = false, error = "No solution loaded or file not found.", mode });

        try
        {
            var resultsDir = Path.Combine(Path.GetDirectoryName(path) ?? "", ".cascade-ide", "test-artifacts");
            Directory.CreateDirectory(resultsDir);
            var trxFileName = $"tests-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.trx";
            var trxPath = Path.Combine(resultsDir, trxFileName);

            var psi = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(path) ?? ""
            };
            psi.ArgumentList.Add("test");
            psi.ArgumentList.Add(path);
            psi.ArgumentList.Add("--logger");
            psi.ArgumentList.Add("console;verbosity=detailed");
            psi.ArgumentList.Add("--logger");
            psi.ArgumentList.Add($"trx;LogFileName={trxFileName}");
            psi.ArgumentList.Add("--results-directory");
            psi.ArgumentList.Add(resultsDir);
            if (!string.IsNullOrWhiteSpace(filterExpression))
            {
                psi.ArgumentList.Add("--filter");
                psi.ArgumentList.Add(filterExpression);
            }

            using var process = Process.Start(psi);
            if (process is null)
                return JsonSerializer.Serialize(new { success = false, error = "Failed to start dotnet test.", mode, filter = filterExpression });

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);

            var outStr = await stdout + "\n" + await stderr;
            var parsed = File.Exists(trxPath)
                ? ParseTrx(trxPath) ?? TestOutputParser.Parse(outStr)
                : TestOutputParser.Parse(outStr);

            Dispatcher.UIThread.Post(() =>
            {
                LastTestSummary = $"{parsed.Passed}/{parsed.Total} passed, {parsed.Failed} failed";
                const int maxLogChars = 120_000;
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var block = $"=== {stamp} ===\n{LastTestSummary}\n\n{outStr}\n\n";
                var combined = TestResultsOutput + block;
                if (combined.Length > maxLogChars)
                    combined = combined[^maxLogChars..];
                TestResultsOutput = combined;
                if (ShowInstrumentationTabs)
                    BottomPanelTabIndex = 4;
            });
            var result = new
            {
                success = parsed.Success,
                total = parsed.Total,
                passed = parsed.Passed,
                failed = parsed.Failed,
                skipped = parsed.Skipped,
                failed_tests = parsed.FailedTests.Select(t => new { t.Name, t.Message, duration_ms = t.DurationMs }).ToList(),
                mode,
                filter = filterExpression,
                tokens,
                trx_path = File.Exists(trxPath) ? trxPath : null
            };
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                const int maxLogChars = 120_000;
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var block = $"=== {stamp} (ошибка) ===\n{ex.Message}\n\n";
                var combined = TestResultsOutput + block;
                if (combined.Length > maxLogChars)
                    combined = combined[^maxLogChars..];
                TestResultsOutput = combined;
            });
            return JsonSerializer.Serialize(new { success = false, error = ex.Message, mode, filter = filterExpression });
        }
    }

    private static TestParseResult? ParseTrx(string trxPath)
    {
        try
        {
            var doc = XDocument.Load(trxPath);
            var root = doc.Root;
            if (root is null)
                return null;

            XNamespace ns = root.Name.Namespace;
            var counters = doc.Descendants(ns + "Counters").FirstOrDefault();
            int total = ParseInt(counters?.Attribute("total")?.Value);
            int passed = ParseInt(counters?.Attribute("passed")?.Value);
            int failed = ParseInt(counters?.Attribute("failed")?.Value);
            int skipped = ParseInt(counters?.Attribute("notExecuted")?.Value);

            var failedTests = new List<TestResultItem>();
            foreach (var unitTestResult in doc.Descendants(ns + "UnitTestResult"))
            {
                var outcome = unitTestResult.Attribute("outcome")?.Value;
                if (!string.Equals(outcome, "Failed", StringComparison.OrdinalIgnoreCase))
                    continue;

                var name = unitTestResult.Attribute("testName")?.Value ?? "";
                var duration = ParseDurationMs(unitTestResult.Attribute("duration")?.Value);
                var message = unitTestResult
                    .Descendants(ns + "Message")
                    .Select(m => m.Value)
                    .FirstOrDefault() ?? "";
                failedTests.Add(new TestResultItem(name, Passed: false, Message: message, DurationMs: duration));
            }

            return new TestParseResult(
                Total: total,
                Passed: passed,
                Failed: failed,
                Skipped: skipped,
                FailedTests: failedTests);
        }
        catch
        {
            return null;
        }

        static int ParseInt(string? raw) => int.TryParse(raw, out var value) ? value : 0;
        static int? ParseDurationMs(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            return TimeSpan.TryParse(raw, out var ts) ? (int)ts.TotalMilliseconds : null;
        }
    }

    private static IReadOnlyList<string> BuildAffectedTestTokens(IReadOnlyList<string>? changedPaths)
    {
        if (changedPaths is null || changedPaths.Count == 0)
            return Array.Empty<string>();

        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawPath in changedPaths)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                continue;

            var fileName = Path.GetFileNameWithoutExtension(rawPath);
            if (string.IsNullOrWhiteSpace(fileName))
                continue;

            // Prefer explicit test-like names; this keeps filter broad enough but still targeted.
            if (fileName.Contains("test", StringComparison.OrdinalIgnoreCase))
            {
                tokens.Add(fileName);
                continue;
            }

            tokens.Add(fileName + "Test");
            tokens.Add(fileName + "Tests");
        }
        return tokens.Take(24).ToList();
    }

    async Task<string> Services.IIdeMcpActions.RunCodeCleanupAsync(string? includePath)
    {
        var path = SolutionPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return JsonSerializer.Serialize(new { success = false, error = "No solution loaded or file not found." });

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(path) ?? ""
            };
            psi.ArgumentList.Add("format");
            psi.ArgumentList.Add(path);
            psi.ArgumentList.Add("--no-restore");
            psi.ArgumentList.Add("--verbosity");
            psi.ArgumentList.Add("minimal");

            if (!string.IsNullOrWhiteSpace(includePath))
            {
                string includeArg;
                try
                {
                    includeArg = Path.GetFullPath(includePath);
                }
                catch
                {
                    includeArg = includePath;
                }
                psi.ArgumentList.Add("--include");
                psi.ArgumentList.Add(includeArg);
            }

            using var process = System.Diagnostics.Process.Start(psi);
            if (process is null)
                return JsonSerializer.Serialize(new { success = false, error = "Failed to start dotnet format." });

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);

            var outStr = await stdout + "\n" + await stderr;
            const int maxRawChars = 4000;
            var rawTruncated = outStr.Length > maxRawChars ? outStr[..maxRawChars] + "\n... (output truncated)" : outStr;

            var pathCopy = path;
            Dispatcher.UIThread.Post(() =>
            {
                BuildOutput = $"Code cleanup: {pathCopy}\r\n{outStr}";
                IsBuildOutputVisible = true;
            });

            return JsonSerializer.Serialize(new
            {
                success = process.ExitCode == 0,
                exit_code = process.ExitCode,
                raw_output = rawTruncated
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    void Services.IIdeMcpActions.FocusEditor()
    {
        Dispatcher.UIThread.Post(() => _focusEditorAction?.Invoke());
    }

    void Services.IIdeMcpActions.SetBreakpoint(string filePath, int line, string? condition)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1)
            return;
        var path = Path.GetFullPath(filePath);
        if (_breakpoints.Any(b => string.Equals(Path.GetFullPath(b.FilePath), path, StringComparison.OrdinalIgnoreCase) && b.Line == line))
            return;
        _breakpoints.Add((path, line));
        OnPropertyChanged(nameof(BreakpointLinesInCurrentFile));
    }

    void Services.IIdeMcpActions.RemoveBreakpoint(string filePath, int line)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1)
            return;
        var path = Path.GetFullPath(filePath);
        var removed = _breakpoints.RemoveAll(b => string.Equals(Path.GetFullPath(b.FilePath), path, StringComparison.OrdinalIgnoreCase) && b.Line == line) > 0;
        if (removed)
            OnPropertyChanged(nameof(BreakpointLinesInCurrentFile));
    }

    /// <summary>Переключить брейкпоинт в .dotnet-debug-mcp-breakpoints.json для текущего файла и строки (клик по полю в редакторе).</summary>
    public void ToggleBreakpointInFile(int line)
    {
        if (line < 1 || string.IsNullOrEmpty(CurrentFilePath))
            return;
        var ws = GetWorkspacePath();
        if (string.IsNullOrEmpty(ws))
            return;
        Services.BreakpointsFileService.ToggleBreakpoint(ws, CurrentFilePath, line);
        OnPropertyChanged(nameof(McpFileBreakpointLinesInCurrentFile));
        OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
    }

    void Services.IIdeMcpActions.ShowPreview(string title, string content)
    {
        var t = title ?? "Превью";
        var c = content ?? "";
        Dispatcher.UIThread.Post(() => RequestShowMarkdownPreviewWindow?.Invoke(t, c));
    }

    void Services.IIdeMcpActions.ShowEditorPreview()
    {
        Dispatcher.UIThread.Post(() => RequestShowMarkdownPreviewForEditor?.Invoke());
    }

    [RelayCommand]
    private void OpenPreviewWindow()
    {
        RequestShowMarkdownPreviewForEditor?.Invoke();
    }

    Task<string> Services.IIdeMcpActions.RequestConfirmationAsync(string message, CancellationToken cancellationToken)
    {
        var request = RequestConfirmation;
        if (request is null)
            return Task.FromResult(Services.ConfirmationResponses.Ok);
        return request(message ?? "", cancellationToken);
    }

    string Services.IIdeMcpActions.GetUiTheme() => Services.UiThemeSnapshot.GetJson();

    async Task<string> Services.IIdeMcpActions.SetUiThemeAsync(string themeJson) =>
        await Services.UiThemeApply.ApplyOnUiThreadAsync(themeJson ?? "");

    async Task<string> Services.IIdeMcpActions.GetUiLayoutAsync()
    {
        var provider = GetUiLayoutProvider;
        if (provider is null)
            return "{}";
        return await Dispatcher.UIThread.InvokeAsync(() => provider() ?? "{}");
    }

    async Task<string> Services.IIdeMcpActions.GetColorsUnderCursorAsync()
    {
        var provider = GetColorsUnderCursorProvider;
        if (provider is null)
            return "{}";
        return await Dispatcher.UIThread.InvokeAsync(() => provider() ?? "{}");
    }

    async Task<string> Services.IIdeMcpActions.GetControlAppearanceAsync(string? name)
    {
        var provider = GetControlAppearanceProvider;
        if (provider is null)
            return "{}";
        return await Dispatcher.UIThread.InvokeAsync(() => provider(name) ?? "{}");
    }

    async Task<string> Services.IIdeMcpActions.SetControlLayoutAsync(string controlName, string layoutJson)
    {
        var provider = SetControlLayoutProvider;
        if (provider is null)
            return "No layout provider.";
        return await Dispatcher.UIThread.InvokeAsync(() => provider(controlName, layoutJson ?? "{}"));
    }

    async Task<string> Services.IIdeMcpActions.AddControlAsync(string parentName, string controlType, string? content, string? name)
    {
        var provider = AddControlProvider;
        if (provider is null)
            return "AddControl disabled.";
        return await Dispatcher.UIThread.InvokeAsync(() => provider(parentName, controlType ?? "", content, name));
    }

    async Task<string> Services.IIdeMcpActions.SetControlTextAsync(string controlName, string text)
    {
        var provider = SetControlTextProvider;
        if (provider is null)
            return "No provider.";
        return await Dispatcher.UIThread.InvokeAsync(() => provider(controlName, text ?? ""));
    }

    async Task<string> Services.IIdeMcpActions.ClickControlAsync(string? controlName)
    {
        var provider = ClickControlProvider;
        if (provider is null)
            return "No provider.";
        return await Dispatcher.UIThread.InvokeAsync(() => provider(controlName));
    }

    async Task<string> Services.IIdeMcpActions.SendKeysAsync(string? controlName, string keys)
    {
        var provider = SendKeysProvider;
        if (provider is null)
            return "No provider.";
        return await Dispatcher.UIThread.InvokeAsync(() => provider(controlName, keys ?? ""));
    }

    async Task<string> Services.IIdeMcpActions.SetFocusAsync(string? controlName)
    {
        var provider = SetFocusProvider;
        if (provider is null)
            return "No provider.";
        return await Dispatcher.UIThread.InvokeAsync(() => provider(controlName));
    }

    async Task<string> Services.IIdeMcpActions.HighlightControlAsync(string? controlName)
    {
        var provider = HighlightControlProvider;
        if (provider is null)
            return "No provider.";
        return await Dispatcher.UIThread.InvokeAsync(() => provider(controlName));
    }

    async Task<string> Services.IIdeMcpActions.SetPanelSizeAsync(string panel, double? width, double? height)
    {
        var provider = SetPanelSizeProvider;
        if (provider is null)
            return "No provider.";
        return await Dispatcher.UIThread.InvokeAsync(() => provider(panel, width, height));
    }

    string Services.IIdeMcpActions.GetSupportedEditorLanguages() => Services.EditorLanguageSupport.GetJson();

    void Services.IIdeMcpActions.ShowDebugBreakpoints(IReadOnlyList<(string FilePath, int Line)> breakpoints)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _debuggerBreakpoints.Clear();
            foreach (var (path, line) in breakpoints)
                _debuggerBreakpoints.Add((Path.GetFullPath(path), line));
            OnPropertyChanged(nameof(DebuggerBreakpointLinesInCurrentFile));
            OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
        });
    }

    void Services.IIdeMcpActions.ShowDebugPosition(string? filePath, int line)
    {
        Dispatcher.UIThread.Post(() =>
        {
            DebugPositionFile = filePath is not null ? Path.GetFullPath(filePath) : null;
            DebugPositionLine = line;
            if (filePath is not null && !string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                var normalized = Path.GetFullPath(filePath);
                if (!string.Equals(CurrentFilePath, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    IsLoadingCurrentFile = true;
                    try
                    {
                        OpenOrActivateDocument(normalized);
                    }
                    finally { IsLoadingCurrentFile = false; }
                }
            }
        });
    }

    void Services.IIdeMcpActions.ShowDebugState(IReadOnlyList<(string Name, string? File, int Line)> stackFrames, IReadOnlyList<(string Name, string Value)> variables)
    {
        Dispatcher.UIThread.Post(() =>
        {
            DebugStackFrames.Clear();
            foreach (var f in stackFrames)
                DebugStackFrames.Add(new DebugStackFrameViewModel(f.Name, f.File, f.Line));
            DebugVariables.Clear();
            foreach (var v in variables)
                DebugVariables.Add(new DebugVariableViewModel(v.Name, v.Value));
            OnPropertyChanged(nameof(IsDebugPanelVisible));
            OnPropertyChanged(nameof(TelemetryDebugText));
            OnPropertyChanged(nameof(TelemetryDebugCockpitShort));
        });
    }

    private const string AgentNotesFileName = "agent-notes.md";

    Task<string> Services.IIdeMcpActions.WriteAgentNotesAsync(string content, CancellationToken cancellationToken)
    {
        var solutionPath = SolutionPath;
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            return Task.FromResult("Error: solution not loaded. Open a solution first.");
        var solutionDir = Path.GetDirectoryName(solutionPath);
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("Error: invalid solution path.");
        var dir = Path.Combine(solutionDir, ".cascade-ide");
        var filePath = Path.Combine(dir, AgentNotesFileName);
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, content, System.Text.Encoding.UTF8);
            return Task.FromResult("OK");
        }
        catch (Exception ex)
        {
            return Task.FromResult("Error: " + ex.Message);
        }
    }

    Task<string> Services.IIdeMcpActions.ReadAgentNotesAsync(CancellationToken cancellationToken)
    {
        var solutionPath = SolutionPath;
        if (string.IsNullOrWhiteSpace(solutionPath) || !File.Exists(solutionPath))
            return Task.FromResult("");
        var solutionDir = Path.GetDirectoryName(solutionPath);
        if (string.IsNullOrEmpty(solutionDir))
            return Task.FromResult("");
        var filePath = Path.Combine(solutionDir, ".cascade-ide", AgentNotesFileName);
        if (!File.Exists(filePath))
            return Task.FromResult("");
        try
        {
            return Task.FromResult(File.ReadAllText(filePath, System.Text.Encoding.UTF8));
        }
        catch
        {
            return Task.FromResult("");
        }
    }
}

public partial class OpenDocumentViewModel : ObservableObject
{
    public OpenDocumentViewModel(string filePath, string title, string content)
    {
        FilePath = filePath;
        Title = title;
        OriginalContent = content;
        _content = content;
    }

    public string FilePath { get; }
    public string Title { get; }
    public string OriginalContent { get; private set; }
    public string DisplayTitle => IsPinned ? $"[P] {Title}{(IsDirty ? "*" : "")}" : $"{Title}{(IsDirty ? "*" : "")}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private string _content;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private bool _isPinned;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private bool _isDirty;

    [ObservableProperty]
    private int _groupIndex = 1;

    public void ReloadContent(string newContent)
    {
        OriginalContent = newContent ?? "";
        Content = OriginalContent;
        IsDirty = false;
    }
}

/// <summary>Элемент стека вызовов для панели отладки.</summary>
public sealed class DebugStackFrameViewModel(string name, string? file, int line)
{
    public string Name { get; } = name;
    public string? File { get; } = file;
    public int Line { get; } = line;

    public string DisplayText => $"{Name} — {File ?? ""}:{Line}";
}

/// <summary>Переменная для панели отладки.</summary>
public sealed class DebugVariableViewModel(string name, string value)
{
    public string Name { get; } = name;
    public string Value { get; } = value;

    public string DisplayText => $"{Name} = {Value}";
}

/// <summary>Structured trace step for Power-mode timeline cards.</summary>
public sealed class AgentTraceStepViewModel
{
    public AgentTraceStepViewModel(string kind, string text, string status, DateTimeOffset? at = null)
    {
        Kind = kind;
        Text = text;
        Status = status;
        var t = at ?? DateTimeOffset.Now;
        TimestampText = t.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }

    public string Kind { get; }
    public string Text { get; }
    public string Status { get; }
    public string TimestampText { get; }

    public bool IsPlan => string.Equals(Kind, "PLAN", StringComparison.OrdinalIgnoreCase);
    public bool IsAction => string.Equals(Kind, "ACTION", StringComparison.OrdinalIgnoreCase);
    public bool IsObservation => string.Equals(Kind, "OBSERVATION", StringComparison.OrdinalIgnoreCase);
    public bool IsNext => string.Equals(Kind, "NEXT", StringComparison.OrdinalIgnoreCase);

    public bool IsSuccess => string.Equals(Status, "SUCCESS", StringComparison.OrdinalIgnoreCase);
    public bool IsWarning => string.Equals(Status, "WARNING", StringComparison.OrdinalIgnoreCase);
    public bool IsPending => string.Equals(Status, "PENDING", StringComparison.OrdinalIgnoreCase);
}

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using Avalonia.Threading;
using CascadeIDE.Models;
using DotNetBuildTestParsers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        SaveSettingsIfChanged();
    }

    partial void OnIsTerminalVisibleChanged(bool value)
    {
        _settings.TerminalVisible = value;
        SaveSettingsIfChanged();
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

    private readonly List<(string FilePath, int Line)> _breakpoints = [];
    private readonly List<(string FilePath, int Line)> _debuggerBreakpoints = [];
    private FileSystemWatcher? _breakpointsFileWatcher;
    private CancellationTokenSource? _openFileDebounceCts;
    private long _solutionLoadVersion;
    private const int OpenFileDebounceMs = 100;

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
    private bool _isTerminalVisible;

    [ObservableProperty]
    private string _terminalOutput = "";

    [ObservableProperty]
    private string _terminalInput = "";

    public string ChatPanelToggleButtonText => IsChatPanelExpanded ? "◀" : "▶";

    [ObservableProperty]
    private string _buildOutput = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuildSolutionCommand))]
    private bool _isBuilding;

    [ObservableProperty]
    private bool _isBuildOutputVisible;

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
            CurrentFilePath = normalizedPath;
            EditorText = "";
            _ = LoadFileContentAsync(normalizedPath);
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

    private async Task LoadFileContentAsync(string path)
    {
        var pathToMatch = Path.GetFullPath(path);
        try
        {
            var text = await Task.Run(() => File.ReadAllText(path)).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var current = CurrentFilePath is not null ? Path.GetFullPath(CurrentFilePath) : "";
                if (string.Equals(current, pathToMatch, StringComparison.OrdinalIgnoreCase))
                    EditorText = text;
                IsLoadingCurrentFile = false;
            });
        }
        catch
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var current = CurrentFilePath is not null ? Path.GetFullPath(CurrentFilePath) : "";
                if (string.Equals(current, pathToMatch, StringComparison.OrdinalIgnoreCase))
                    EditorText = "";
                IsLoadingCurrentFile = false;
            });
        }
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
    }

    [RelayCommand]
    private void ToggleTerminal()
    {
        IsTerminalVisible = !IsTerminalVisible;
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
            CurrentFilePath = null;
            EditorText = "";
            IsLoadingCurrentFile = false;

            SolutionPath = normalizedSolutionPath ?? path;
            SolutionRoots.Clear();
            SolutionRoots.Add(root);
        });
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
                CurrentFilePath = normalizedPath;
                try
                {
                    EditorText = File.ReadAllText(normalizedPath);
                }
                catch
                {
                    EditorText = "";
                }
                SyncSelectedSolutionItemToCurrentFile();
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
                    CurrentFilePath = Path.GetFullPath(filePath);
                    try { EditorText = File.ReadAllText(filePath); }
                    catch { EditorText = ""; }
                    SyncSelectedSolutionItemToCurrentFile();
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
            var psi = new System.Diagnostics.ProcessStartInfo("dotnet")
            {
                ArgumentList = { "build", path },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(path) ?? ""
            };
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
            raw_output = rawTruncated
        };
        return System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
    }

    async Task<string> Services.IIdeMcpActions.RunTestsAsync()
    {
        var path = SolutionPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            var msg = JsonSerializer.Serialize(new { success = false, error = "No solution loaded or file not found." });
            return msg;
        }
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("dotnet")
            {
                ArgumentList = { "test", path, "--logger", "console;verbosity=detailed" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(path) ?? ""
            };
            using var process = System.Diagnostics.Process.Start(psi);
            if (process is null)
            {
                return JsonSerializer.Serialize(new { success = false, error = "Failed to start dotnet test." });
            }
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);
            var outStr = await stdout + "\n" + await stderr;
            var parsed = TestOutputParser.Parse(outStr);
            var result = new
            {
                success = parsed.Success,
                total = parsed.Total,
                passed = parsed.Passed,
                failed = parsed.Failed,
                skipped = parsed.Skipped,
                failed_tests = parsed.FailedTests.Select(t => new { t.Name, t.Message, duration_ms = t.DurationMs }).ToList()
            };
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });
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
                        CurrentFilePath = normalized;
                        try { EditorText = File.ReadAllText(normalized); }
                        catch { EditorText = ""; }
                        SyncSelectedSolutionItemToCurrentFile();
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

/// <summary>Элемент стека вызовов для панели отладки.</summary>
public sealed class DebugStackFrameViewModel(string name, string? file, int line)
{
    public string Name { get; } = name;
    public string? File { get; } = file;
    public int Line { get; } = line;
}

/// <summary>Переменная для панели отладки.</summary>
public sealed class DebugVariableViewModel(string name, string value)
{
    public string Name { get; } = name;
    public string Value { get; } = value;
}

using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using Avalonia.Threading;
using CascadeIDE.Models;
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

    private Func<Services.EditorStateDto?>? _editorStateProvider;
    private Action<string, int, int, int, int, string>? _applyEditAction;
    private Action? _focusEditorAction;

    public static readonly IReadOnlyList<string> AiProviderKeys = ["Ollama", "Anthropic", "OpenAI", "DeepSeek"];
    public IReadOnlyList<string> AiProviderKeysList => AiProviderKeys;

    public MainWindowViewModel()
    {
        var minimizer = new Services.ContextMinimizer(new Services.CSharpLanguageService());
        _aiProviderManager = new Services.AiProviderManager(minimizer, ResolveProvider);
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

    public void SetEditorStateProvider(Func<Services.EditorStateDto?> provider) => _editorStateProvider = provider;
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
    /// <summary>Показать превью Markdown в отдельном окне (контент от агента).</summary>
    public Action<string, string>? RequestShowMarkdownPreviewWindow { get; set; }
    /// <summary>Показать превью текущего редактора в отдельном окне (живое обновление).</summary>
    public Action? RequestShowMarkdownPreviewForEditor { get; set; }
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
    private string? _currentFilePath;

    /// <summary>True, если открыт файл .md или .markdown — показываем превью.</summary>
    public bool IsMarkdownFile =>
        !string.IsNullOrEmpty(CurrentFilePath)
        && (CurrentFilePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            || CurrentFilePath.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase));

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
        if (value?.FullPath is not { } path || !File.Exists(path))
            return;
        CurrentFilePath = path;
        EditorText = ""; // сразу очищаем, потом подставим содержимое
        _ = LoadFileContentAsync(path);
    }

    private async Task LoadFileContentAsync(string path)
    {
        try
        {
            var text = await Task.Run(() => File.ReadAllText(path)).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (CurrentFilePath == path)
                    EditorText = text;
            });
        }
        catch
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (CurrentFilePath == path)
                    EditorText = "";
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
        SolutionLoadError = "";
        var (root, error) = await Task.Run(() =>
        {
            var r = Services.SolutionParser.Load(path, out var err);
            return (r, err);
        }).ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (root is null)
            {
                SolutionLoadError = error ?? "Не удалось загрузить решение.";
                return;
            }
            SolutionPath = path;
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

    void Services.IIdeMcpActions.OpenFile(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        var pathCopy = path;
        Dispatcher.UIThread.Post(() =>
        {
            if (!File.Exists(pathCopy))
                return;
            CurrentFilePath = pathCopy;
            try
            {
                EditorText = File.ReadAllText(pathCopy);
            }
            catch
            {
                EditorText = "";
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
                CurrentFilePath = filePath;
                try { EditorText = File.ReadAllText(filePath); }
                catch { EditorText = ""; }
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

    async Task<string> Services.IIdeMcpActions.GetEditorStateAsync()
    {
        var tcs = new TaskCompletionSource<string>();
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var dto = _editorStateProvider?.Invoke() ?? new Services.EditorStateDto();
                tcs.SetResult(JsonSerializer.Serialize(dto));
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
        return JsonSerializer.Serialize(new { solution_path = path, current_file_path = current, project_paths = projects });
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

    void Services.IIdeMcpActions.FocusEditor()
    {
        Dispatcher.UIThread.Post(() => _focusEditorAction?.Invoke());
    }

    void Services.IIdeMcpActions.SetBreakpoint(string filePath, int line, string? condition)
    {
        // TODO: отобразить в UI (маргина или список брейкпоинтов), передать в debug-mcp
        System.Diagnostics.Debug.WriteLine($"Breakpoint: {filePath}:{line} {condition}");
    }

    void Services.IIdeMcpActions.ShowPreview(string title, string content)
    {
        var t = title ?? "Превью";
        var c = content ?? "";
        Dispatcher.UIThread.Post(() => RequestShowMarkdownPreviewWindow?.Invoke(t, c));
    }

    [RelayCommand]
    private void OpenPreviewWindow()
    {
        RequestShowMarkdownPreviewForEditor?.Invoke();
    }

    Task<string> Services.IIdeMcpActions.RequestConfirmationAsync(string message, CancellationToken cancellationToken)
    {
        // TODO: модальное окно с Ok/Cancel; пока возвращаем ok
        return Task.FromResult("ok");
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
}

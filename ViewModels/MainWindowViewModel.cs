using System.ComponentModel;
using System.Text.Json;
using Avalonia.Threading;
using CascadeIDE.Features.AutonomousAgent;
using CascadeIDE.Features.Build;
using CascadeIDE.Features.Chat;
using CascadeIDE.Features.Debug;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.Git;
using CascadeIDE.Features.Instrumentation;
using CascadeIDE.Features.Terminal;
using CascadeIDE.Cockpit.Channels.Eicas;
using CascadeIDE.Cockpit.Channels.EnvironmentReadiness;
using CascadeIDE.Cockpit.Channels.WorkspaceHealth;
using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Contracts;
using CascadeIDE.Cockpit.Composition.EnvironmentReadiness;
using CascadeIDE.Cockpit.Composition.WorkspaceHealth;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Models;
using CascadeIDE.Features.Os.DataAcquisition;
using CascadeIDE.Features.Workspace.Application;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Главный композитор окна (partial-класс, несколько <c>MainWindowViewModel*.cs</c>).
/// Карта файлов и ответственности — <c>docs/architecture-migration.md</c>, раздел «Срез MainWindowViewModel».
/// </summary>
[DataBusPublisher("ide-health & related domain signals")]
public partial class MainWindowViewModel : ViewModelBase, Services.IIdeMcpActions, IAutonomousAgentSessionHost,
    IMainWindowHostSurfaceInput
{
    public const string InstallNewSentinel = "— Установить модель… —";

    private readonly Services.IOllamaService _ollama = new Services.OllamaService();
    private readonly Services.AiProviderManager _aiProviderManager;
    private readonly CascadeIdeSettings _settings = Services.SettingsService.Load();
    internal CascadeIdeSettings GetCascadeSettingsForExecutor() => _settings;
    private AiKeys _aiKeys = Services.AiKeysStorage.Load();
    private CascadeIdeSettings? _lastSavedSettings;
    private AiKeys? _lastSavedAiKeys;

    /// <summary>ADR 0083: значения <c>ai.mode</c> в TOML.</summary>
    public static readonly IReadOnlyList<string> AiModeOptions = ["local", "acp", "mcp_only", "cloud"];

    public IReadOnlyList<string> AiModeOptionsList => AiModeOptions;

    /// <summary>Для <c>mode = cloud</c>: <c>ai.cloud.active_provider</c>.</summary>
    public static readonly IReadOnlyList<string> AiCloudProviderKeys = ["anthropic", "openai", "deepseek"];

    public IReadOnlyList<string> AiCloudProviderKeysList => AiCloudProviderKeys;

    private readonly Services.CSharpLanguageService _csharpLanguageService;
    private readonly Services.ContextMinimizer _contextMinimizer;
    private readonly Services.WorkspaceDiagnosticsCoordinator _workspaceDiagnostics;
    private readonly Services.Capabilities.SimpleCapabilityRegistry _capabilities = new();
    private CSharpLspDiagnosticsHost? _csharpLspHost;
    private MarkdownLspDiagnosticsHost? _markdownLspHost;
    private readonly IdeMcpCommandExecutor _ideMcpExecutor;
    private readonly Services.IdeDapDebugSession _dapDebug;
    private readonly IDataBus _ideDataBus;
    private readonly HybridIndexOrchestrator _hybridIndex;
    private readonly IIdeHealthChannel _workspaceHealth;
    private readonly IIdeHealthSurfaceCompositor _workspaceHealthSurfaceCompositor;
    private readonly IEicasFeed _eicasFeed;
    private readonly IEnvironmentReadinessChannel _environmentReadinessChannel;
    private readonly IEnvironmentReadinessSurfaceCompositor _environmentReadinessSurfaceCompositor;
    private readonly Services.Presentation.PresentationParseResult _presentationParse;
    private readonly bool _presentationDedicatedMfdSecondScreen;
    private readonly bool _presentationTripleOneAnchorPerZone;
    private readonly bool _presentationMfdHostTopology;
    private readonly bool _presentationPmForwardTwoScreen;
    private readonly bool _presentationPmHostTopology;
    private readonly IInstrumentMountPolicyResolver _instrumentMountPolicyResolver;
    private bool _suppressMfdColumnForMfdHostWindow;
    private bool _suppressPfdColumnForPfdHostWindow;

    private Services.McpClientService _mcpClientService;
    private AutonomousAgentService _autonomousAgentService;

    private readonly IOsShellLauncher _osShell;

    /// <summary>DAP-сессия (netcoredbg): launch/attach и обновление панели отладки.</summary>
    public Services.IdeDapDebugSession DapDebug => _dapDebug;

    /// <summary>Solution/workspace state and background loading.</summary>
    public SolutionWorkspaceViewModel Workspace { get; }

    /// <summary>Git-строки для Workspace Health и bloom при смене UI-режима.</summary>
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
                OnPropertyChanged(nameof(BreakpointLinesInCurrentFile));
                OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
                BuildSolutionCommand.NotifyCanExecuteChanged();
                ImportLaunchSettingsFromSelectionCommand.NotifyCanExecuteChanged();
                HandleSolutionPathChanged(Workspace.SolutionPath);
                break;
            case nameof(SolutionWorkspaceViewModel.SelectedSolutionItem):
                HandleSelectedSolutionItemChanged(Workspace.SelectedSolutionItem);
                SetStartupProjectFromSelectionCommand.NotifyCanExecuteChanged();
                ImportLaunchSettingsFromSelectionCommand.NotifyCanExecuteChanged();
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
        UiScheduler.Default.Post(() => GotoActiveEditorLineColumnRequested?.Invoke(line, col), DispatcherPriority.Loaded);
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
            msg => UiScheduler.Default.Post(() => InstrumentationPanel.EventTimeline.Insert(0, msg)));

    /// <summary>
    /// Полоса Workspace Health читает счётчики отладки с главного VM; при смене MCP-стека обновляем строки.
    /// </summary>
    private void OnInstrumentationPanelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(InstrumentationPanelViewModel.IsDebugPanelVisible))
            return;
        OnPropertyChanged(nameof(IdeHealthDebugText));
        OnPropertyChanged(nameof(IdeHealthDebugCockpitShort));
    }

    /// <summary>Вывод сборки (нижняя вкладка Build output).</summary>
    public BuildOutputPanelViewModel BuildOutputPanel { get; }

    /// <summary>Ошибки и предупреждения по открытым .cs (Roslyn).</summary>
    public ProblemsPanelViewModel ProblemsPanel { get; }

    /// <summary>Markdown preview как shared state для MFD page/tool.</summary>
    public MarkdownPreviewToolViewModel MarkdownPreviewTool { get; }

    /// <summary>Терминал (нижняя вкладка Terminal).</summary>
    public TerminalPanelViewModel TerminalPanel { get; }

    /// <summary>Панель Git (нижняя вкладка); состояние и команды вынесены из <see cref="MainWindowViewModel"/>.</summary>
    public GitPanelViewModel GitPanel { get; }

    /// <summary>Чат с LLM (правая колонка): история, ввод, отправка.</summary>
    public ChatPanelViewModel ChatPanel { get; }

    /// <summary>Инструментирование: трасса агента, события, тесты, стек MCP-отладки. В разметке — <c>DataContext="{Binding InstrumentationPanel}"</c>.</summary>
    public InstrumentationPanelViewModel InstrumentationPanel { get; }

    /// <summary>Гипотезы отладки (JSON в workspace). Вкладка нижней панели в режиме Debug.</summary>
    public HypothesesPanelViewModel HypothesesPanel { get; }

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

    private Microsoft.Extensions.AI.IChatClient? TryCreateCloudMafIChatClientForChatPanel()
    {
        return ActiveAiProvider switch
        {
            "Anthropic" => Services.CascadeIdeMafChatClientFactories.CreateAnthropicChatClientOrNull(
                AnthropicApiKey,
                _settings.Ai.Cloud.Anthropic.Model),
            "OpenAI" => Services.CascadeIdeMafChatClientFactories.CreateOpenAiCompatibleChatClientOrNull(
                OpenAiApiKey,
                _settings.Ai.Cloud.OpenAi.BaseUrl,
                _settings.Ai.Cloud.OpenAi.Model),
            "DeepSeek" => Services.CascadeIdeMafChatClientFactories.CreateOpenAiCompatibleChatClientOrNull(
                DeepSeekApiKey,
                _settings.Ai.Cloud.DeepSeek.BaseUrl,
                _settings.Ai.Cloud.DeepSeek.Model),
            _ => null,
        };
    }

    private (Services.IAiChatProvider? Provider, string Model) ResolveProvider(string providerKey)
    {
        var key = providerKey ?? _settings.Ai.ResolveEffectiveProviderUiKey();
        return key switch
        {
            "Anthropic" => (new Services.AnthropicProvider(_aiKeys.AnthropicApiKey ?? "", _settings.Ai.Cloud.Anthropic.Model), _settings.Ai.Cloud.Anthropic.Model),
            "OpenAI" => (new Services.OpenAiCompatibleProvider(_settings.Ai.Cloud.OpenAi.BaseUrl, _aiKeys.OpenAiApiKey ?? "", _settings.Ai.Cloud.OpenAi.Model), _settings.Ai.Cloud.OpenAi.Model),
            "DeepSeek" => (new Services.OpenAiCompatibleProvider(_settings.Ai.Cloud.DeepSeek.BaseUrl, _aiKeys.DeepSeekApiKey ?? "", _settings.Ai.Cloud.DeepSeek.Model), _settings.Ai.Cloud.DeepSeek.Model),
            "CursorACP" => (null, ""),
            _ => (new Services.OllamaProvider(_ollama), SelectedOllamaModel ?? _settings.Ai.Local.Ollama.Model)
        };
    }

    private readonly Services.AppDataService _appData = new();
    private readonly IGitCommandRunner _gitRunner = new GitCommandRunner();
    private readonly IDotnetCommandRunner _dotnetRunner = new DotnetCommandRunner();
    private readonly Services.McpDotnetBuildTestService _mcpBuildTest;
    private readonly Services.McpAgentNotesService _mcpAgentNotes;

    private CancellationTokenSource? _openFileDebounceCts;
    // Solution load version is owned by Workspace.
    private string? _lastBuildBinlogPath;

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
        if (string.IsNullOrWhiteSpace(value))
        {
            ClearStartupProjectInMemoryOnly();
            RefreshLaunchProfilePickerFromStore();
        }

        var ws = WorkspaceDirectoryFromSolutionPath.Resolve(value);
        UiModeCatalog.ApplyRepositoryWorkspaceOverlay(ws);
        NotifyDockedInstrumentSlotBindings();
        OnPropertyChanged(nameof(ChatPanelColumnPixelWidth));
        OnPropertyChanged(nameof(IsChatPanelColumnVisible));
        OnPropertyChanged(nameof(IsMfdColumnVisible));
        OnPropertyChanged(nameof(IsPfdRegionExpanded));
        OnPropertyChanged(nameof(IsPfdColumnVisible));

        ChatPanel.DisposeCursorAcpSession();
        ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(pokeWhenAutoReindex: true);
        AttachBreakpointsFileWatcher(value);
        _ = RefreshGitSummaryAsync();
        _ = GitPanel.RefreshRepositoryFlagAsync();
        if (IsGitPanelVisible)
            _ = GitPanel.RefreshGitPanelAsync();
        _ = RestartCSharpLanguageServerAsync();
        _ = RestartMarkdownLanguageServerAsync();
        HypothesesPanel.LoadFromWorkspace();
    }

    private (string WorkspaceRoot, string? SolutionPath) ResolveHybridIndexScope(string workspaceRoot, string? solutionPath) =>
        HybridIndexScopeResolver.ApplyScopeMode(_settings.HybridIndex.ScopeMode, workspaceRoot, solutionPath);

    /// <summary>
    /// ADR 0106: синхронизация оркестратора HCI с <see cref="CascadeIdeSettings.HybridIndex"/> и текущим решением в <see cref="SolutionWorkspaceViewModel"/>.
    /// </summary>
    private void ApplyHybridCodebaseIndexOrchestrationForCurrentSolution(bool pokeWhenAutoReindex)
    {
        var value = Workspace.SolutionPath ?? "";
        var ws = WorkspaceDirectoryFromSolutionPath.Resolve(value);
        if (string.IsNullOrWhiteSpace(ws))
            return;

        HybridIndexOrchestrationPolicy.ApplyForCurrentScope(
            _hybridIndex,
            _settings.HybridIndex,
            ChatMcpOnly,
            ws,
            value,
            pokeWhenAutoReindex);
    }

    private string? BuildChatMinimizedContextBlockCore()
    {
        if (!UseMinimizedContext)
            return null;
        if (string.IsNullOrEmpty(CurrentFilePath) || string.IsNullOrEmpty(EditorText))
            return null;
        var minimized = _contextMinimizer.Minimize(CurrentFilePath, EditorText, CancellationToken.None);
        return string.IsNullOrWhiteSpace(minimized) ? null : minimized;
    }

    /// <summary>MCP и агент вызывают с фона; весь разбор команд и доступ к VM — на UI-потоке. Тяжёлые операции внутри хендлеров сами уходят с UI (<c>ConfigureAwait(false)</c>, <c>Task.Run</c>, <c>Post</c> обратно).</summary>
    Task<string> Services.IIdeMcpActions.ExecuteCommandAsync(string commandId, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken) =>
        UiScheduler.Default.InvokeAsync(() => _ideMcpExecutor.ExecuteAsync(commandId, args, cancellationToken));

}

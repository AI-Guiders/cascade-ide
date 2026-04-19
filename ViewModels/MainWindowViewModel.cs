using System.ComponentModel;
using System.Text.Json;
using System.Threading;
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
using CascadeIDE.Cockpit.Composition.EnvironmentReadiness;
using CascadeIDE.Cockpit.Composition.WorkspaceHealth;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;
using CascadeIDE.Services.Lsp;
namespace CascadeIDE.ViewModels;

/// <summary>
/// Главный композитор окна (partial-класс, несколько <c>MainWindowViewModel*.cs</c>).
/// Карта файлов и ответственности — <c>docs/architecture-migration.md</c>, раздел «Срез MainWindowViewModel».
/// </summary>
public partial class MainWindowViewModel : ViewModelBase, Services.IIdeMcpActions, IAutonomousAgentSessionHost
{
    public const string InstallNewSentinel = "— Установить модель… —";

    private readonly Services.IOllamaService _ollama = new Services.OllamaService();
    private readonly Services.AiProviderManager _aiProviderManager;
    private readonly CascadeIdeSettings _settings = Services.SettingsService.Load();
    private AiKeys _aiKeys = Services.AiKeysStorage.Load();
    private CascadeIdeSettings? _lastSavedSettings;
    private AiKeys? _lastSavedAiKeys;

    public static readonly IReadOnlyList<string> AiProviderKeys = ["Ollama", "Anthropic", "OpenAI", "DeepSeek", "CursorACP"];
    public IReadOnlyList<string> AiProviderKeysList => AiProviderKeys;

    private readonly Services.CSharpLanguageService _csharpLanguageService;
    private readonly Services.ContextMinimizer _contextMinimizer;
    private readonly Services.WorkspaceDiagnosticsCoordinator _workspaceDiagnostics;
    private readonly Services.Capabilities.SimpleCapabilityRegistry _capabilities = new();
    private CSharpLspDiagnosticsHost? _csharpLspHost;
    private MarkdownLspDiagnosticsHost? _markdownLspHost;
    private readonly IdeMcpCommandExecutor _ideMcpExecutor;
    private readonly Services.IdeDapDebugSession _dapDebug;
    private readonly IWorkspaceHealthChannel _workspaceHealth;
    private readonly IWorkspaceHealthSurfaceCompositor _workspaceHealthSurfaceCompositor;
    private readonly IEicasFeed _eicasFeed;
    private readonly IEnvironmentReadinessChannel _environmentReadinessChannel;
    private readonly IEnvironmentReadinessSurfaceCompositor _environmentReadinessSurfaceCompositor;
    private readonly Services.Presentation.PresentationParseResult _presentationParse;
    private readonly bool _presentationDedicatedMfdSecondScreen;
    private readonly bool _presentationTripleOneAnchorPerZone;
    private readonly bool _presentationMfdHostTopology;
    private readonly IInstrumentMountPolicyResolver _instrumentMountPolicyResolver;
    private bool _suppressMfdColumnForMfdHostWindow;
    private bool _suppressPfdColumnForPfdHostWindow;

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
        _acpAutoInjectIdeMcp = _settings.Mcp.AcpAutoInjectIdeMcp;
        _markdownKrokiEnabled = _settings.Markdown.Diagrams.Kroki;
        _markdownKrokiBaseUrl = string.IsNullOrWhiteSpace(_settings.Markdown.Diagrams.KrokiUrl)
            ? "https://kroki.io"
            : _settings.Markdown.Diagrams.KrokiUrl.Trim();
        _externalMcpServersJson = _settings.Mcp.ExternalServersJson;
        _activeAiProvider = _settings.Ai.Provider;
        _chatMcpOnly = _settings.Ai.ChatMcpOnly;
        _cursorAcpAgentPath = _settings.Ai.CursorAcpPath ?? "";
        _anthropicApiKey = _aiKeys.AnthropicApiKey ?? "";
        _openAiApiKey = _aiKeys.OpenAiApiKey ?? "";
        _deepSeekApiKey = _aiKeys.DeepSeekApiKey ?? "";
        _isPfdRegionExpanded = _settings.Workspace.PfdExpanded;
        _isTerminalVisible = _settings.Workspace.ShowTerminal;
        _isGitPanelVisible = _settings.Workspace.ShowGit;
        _isInstrumentationDockVisible = _settings.Workspace.ShowInstrumentation;
        _uiMode = NormalizeUiMode(_settings.Workspace.Mode);
        InitializeAgentUiDefaults();
        RegisterAgentFeedHandlers();
        ApplyUiModeLayout(_uiMode, persist: false);
        if (UiModeFamily.IsPowerFamily())
            UiScheduler.Default.Post(RefreshWorkspaceSnapshotCore, DispatcherPriority.Background);

        Documents.InitializeDock();

        _lastSavedSettings = (CascadeIdeSettings)_settings.Clone();
        _lastSavedAiKeys = (AiKeys)_aiKeys.Clone();

        _semanticMapPresentation = SemanticMapPresentationKind.Normalize(_settings.SemanticMap.View);
        _semanticMapLevel = SemanticMapLevelKind.Normalize(_settings.SemanticMap.Depth);
        _workspaceSplittersLocked = _settings.Workspace.SplittersLocked;

        BuildOutputPanel = new BuildOutputPanelViewModel();
        TerminalPanel = new TerminalPanelViewModel(() => Workspace.SolutionPath);
        GitPanel = new GitPanelViewModel(_gitRunner, GetWorkspacePath, this, LoadSolution, RefreshGitSummaryAsync);
        ChatPanel = new ChatPanelViewModel(
            _aiProviderManager,
            () => ActiveAiProvider,
            () => SelectedOllamaModel,
            () => ChatMcpOnly,
            () => UseMinimizedContext,
            () => CurrentFilePath,
            () => EditorText,
            GetWorkspacePath,
            () => CursorAcpAgentPath,
            () => Services.McpExternalServersJsonResolver.ResolveEffectiveJson(_settings),
            () => AcpAutoInjectIdeMcp,
            appendAcpTerminal: text => UiScheduler.Default.Post(() => TerminalPanel.AppendOutput(text)),
            showAcpTerminal: () => UiScheduler.Default.Post(() =>
            {
                if (ShowTerminalPanelCommand.CanExecute(null))
                    ShowTerminalPanelCommand.Execute(null);
            }));
        InstrumentationPanel = new InstrumentationPanelViewModel();
        InstrumentationPanel.PropertyChanged += OnInstrumentationPanelPropertyChanged;
        HypothesesPanel = new HypothesesPanelViewModel(GetWorkspacePath);

        ProblemsPanel = new ProblemsPanelViewModel(NavigateToProblemFromList);
        _workspaceDiagnostics = new Services.WorkspaceDiagnosticsCoordinator(_csharpLanguageService, ProblemsPanel);
        _workspaceDiagnostics.Attach(this);
        _workspaceDiagnostics.DiagnosticsChanged += OnWorkspaceDiagnosticsChangedForHud;
        MarkdownPreviewTool = new MarkdownPreviewToolViewModel();
        MarkdownPreviewTool.AttachToEditor(this);

        // Capabilities: v1 registry (code-first, explicit module list).
        new UiChromeCapabilitiesModule().Register(_capabilities);
        new Features.Markdown.MarkdownCapabilitiesModule().Register(_capabilities);

        _csharpLspProvider = string.IsNullOrEmpty(_settings.Languages.CSharp.Provider)
            ? CSharpLspProviderIds.ParseOnly
            : _settings.Languages.CSharp.Provider;
        _csharpLspExecutable = _settings.Languages.CSharp.Executable ?? "";
        _csharpLspArguments = _settings.Languages.CSharp.Arguments ?? "";
        _markdownLspProvider = string.IsNullOrEmpty(_settings.Languages.Markdown.Provider)
            ? MarkdownLspProviderIds.Off
            : _settings.Languages.Markdown.Provider;
        _markdownLspExecutable = _settings.Languages.Markdown.Executable ?? "";
        _markdownLspArguments = _settings.Languages.Markdown.Arguments ?? "";

        _mcpClientService = new Services.McpClientService(Services.McpExternalServersJsonResolver.ResolveEffectiveJson(_settings));
        _autonomousAgentService = CreateAutonomousAgentService(_mcpClientService);
        Autonomous = new AutonomousAgentSessionViewModel(_autonomousAgentService, this);
        _dapDebug = new Services.IdeDapDebugSession((file, line, stack, vars) =>
        {
            UiScheduler.Default.Post(() =>
            {
                ((Services.IIdeMcpActions)this).ShowDebugPosition(file, line);
                ((Services.IIdeMcpActions)this).ShowDebugState(stack, vars);
            });
        });
        _dapDebug.StateChanged += (_, _) => NotifyDebugRelayCommandsChanged();
        _ideMcpExecutor = new IdeMcpCommandExecutor(this);
        _mcpBuildTest = new Services.McpDotnetBuildTestService(_dotnetRunner);
        _mcpAgentNotes = new Services.McpAgentNotesService();

        _workspaceHealth = new WorkspaceHealthProvider(
            () => IsBuilding,
            () => LastTestSummary,
            () => ImpactedTestsBadge,
            _dapDebug,
            () => InstrumentationPanel.DebugStackFrames.Count,
            () => InstrumentationPanel.DebugVariables.Count,
            () => Chrome.WorkspaceHealthGitText,
            () => Chrome.WorkspaceHealthGitCockpitShort);
        _workspaceHealthSurfaceCompositor = new WorkspaceHealthSurfaceCompositor();

        _eicasFeed = new EmptyEicasFeed();
        _eicasFeed.MessagesChanged += (_, _) => RebuildEicas();
        _environmentReadinessChannel = new EnvironmentReadinessChannel();
        _environmentReadinessSurfaceCompositor = new EnvironmentReadinessSurfaceCompositor();

        Workspace.PropertyChanged += (_, e) => OnWorkspacePropertyChanged(e.PropertyName);
        Chrome.PropertyChanged += OnChromePropertyChangedForWorkspaceHealth;
        RebuildWorkspaceHealth();
        RebuildEicas();

        // Топология presentation / колонки MainGrid — снимок на старте; смена строки в settings.toml — после перезапуска (ADR 0017).
        var pg = _settings.GetEffectivePresentationGrammar();
        var grammar = Services.Presentation.PresentationGrammarTokens.FromSettings(
            pg.Brackets,
            pg.BetweenScreens,
            pg.BetweenZones,
            pg.Pfd,
            pg.Forward,
            pg.Mfd);
        _presentationParse = Services.Presentation.PresentationParser.Parse(_settings.GetEffectivePresentationLine(), grammar);
        _presentationDedicatedMfdSecondScreen = _presentationParse.IsSuccess
            && Services.Presentation.PresentationLayoutAnalyzer.IsDedicatedMfdSecondScreenPreset(_presentationParse.Screens);
        _presentationTripleOneAnchorPerZone = _presentationParse.IsSuccess
            && Services.Presentation.PresentationLayoutAnalyzer.IsTripleOneAnchorPerZonePreset(_presentationParse.Screens);
        _presentationMfdHostTopology = _presentationDedicatedMfdSecondScreen || _presentationTripleOneAnchorPerZone;
        _instrumentMountPolicyResolver = new SettingsBackedInstrumentMountPolicyResolver();

        NotifyDockedInstrumentSlotBindings();
    }

    private void OnChromePropertyChangedForWorkspaceHealth(object? _, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(UiChromeViewModel.WorkspaceHealthGitText)
            or nameof(UiChromeViewModel.WorkspaceHealthGitCockpitShort))
            RebuildWorkspaceHealth();
    }

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
                OnPropertyChanged(nameof(McpFileBreakpointLinesInCurrentFile));
                OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
                BuildSolutionCommand.NotifyCanExecuteChanged();
                HandleSolutionPathChanged(Workspace.SolutionPath);
                break;
            case nameof(SolutionWorkspaceViewModel.SelectedSolutionItem):
                HandleSelectedSolutionItemChanged(Workspace.SelectedSolutionItem);
                SetStartupProjectFromSelectionCommand.NotifyCanExecuteChanged();
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
        OnPropertyChanged(nameof(WorkspaceHealthDebugText));
        OnPropertyChanged(nameof(WorkspaceHealthDebugCockpitShort));
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

    private (Services.IAiChatProvider? Provider, string Model) ResolveProvider(string providerKey)
    {
        var key = providerKey ?? _settings.Ai.Provider;
        return key switch
        {
            "Anthropic" => (new Services.AnthropicProvider(_aiKeys.AnthropicApiKey ?? "", _settings.Ai.AnthropicModel), _settings.Ai.AnthropicModel),
            "OpenAI" => (new Services.OpenAiCompatibleProvider(_settings.Ai.OpenAiBaseUrl, _aiKeys.OpenAiApiKey ?? "", _settings.Ai.OpenAiModel), _settings.Ai.OpenAiModel),
            "DeepSeek" => (new Services.OpenAiCompatibleProvider(_settings.Ai.DeepSeekBaseUrl, _aiKeys.DeepSeekApiKey ?? "", _settings.Ai.DeepSeekModel), _settings.Ai.DeepSeekModel),
            "CursorACP" => (null, ""),
            _ => (new Services.OllamaProvider(_ollama), SelectedOllamaModel ?? _settings.Ai.DefaultOllamaModel)
        };
    }

    private readonly Services.AppDataService _appData = new();
    private readonly Services.IGitCommandRunner _gitRunner = new Services.GitCommandRunner();
    private readonly Services.IDotnetCommandRunner _dotnetRunner = new Services.DotnetCommandRunner();
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
            ClearStartupProjectInMemoryOnly();

        UiModeCatalog.ApplyRepositoryWorkspaceOverlay(GetWorkspacePath(value));
        NotifyDockedInstrumentSlotBindings();
        OnPropertyChanged(nameof(ChatPanelColumnPixelWidth));
        OnPropertyChanged(nameof(IsChatPanelColumnVisible));
        OnPropertyChanged(nameof(IsMfdColumnVisible));
        OnPropertyChanged(nameof(IsPfdRegionExpanded));
        OnPropertyChanged(nameof(IsPfdColumnVisible));

        ChatPanel.DisposeCursorAcpSession();
        AttachBreakpointsFileWatcher(value);
        _ = RefreshGitSummaryAsync();
        _ = GitPanel.RefreshRepositoryFlagAsync();
        if (IsGitPanelVisible)
            _ = GitPanel.RefreshGitPanelAsync();
        _ = RestartCSharpLanguageServerAsync();
        _ = RestartMarkdownLanguageServerAsync();
        HypothesesPanel.LoadFromWorkspace();
    }

    /// <summary>MCP и агент вызывают с фона; весь разбор команд и доступ к VM — на UI-потоке. Тяжёлые операции внутри хендлеров сами уходят с UI (<c>ConfigureAwait(false)</c>, <c>Task.Run</c>, <c>Post</c> обратно).</summary>
    Task<string> Services.IIdeMcpActions.ExecuteCommandAsync(string commandId, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken cancellationToken) =>
        UiScheduler.Default.InvokeAsync(() => _ideMcpExecutor.ExecuteAsync(commandId, args, cancellationToken));

}

using Avalonia.Threading;
using CascadeIDE.Cockpit.Channels.Eicas;
using CascadeIDE.Cockpit.Channels.EnvironmentReadiness;
using CascadeIDE.Cockpit.Composition.EnvironmentReadiness;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Cockpit.Composition.WorkspaceHealth;
using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.AutonomousAgent;
using CascadeIDE.Features.Build;
using CascadeIDE.Features.Chat;
using CascadeIDE.Features.Debug;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.Git;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Features.Instrumentation;
using CascadeIDE.Features.Markdown;
using CascadeIDE.Features.Os.DataAcquisition;
using CascadeIDE.Features.WebAiPortal.Application;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.Terminal;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Конструктор и композиция shell: дочерние VM, шина, DAP/HCI, топология presentation (ADR 0017).
/// </summary>
public partial class MainWindowViewModel
{
    public MainWindowViewModel(IOsShellLauncher? osShell = null)
    {
        _osShell = osShell ?? OsShell.Default;
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
#pragma warning disable MVVMTK0034 // Bootstrap from disk before first UI bind; avoid SaveSettings from OnAiModeChanged.
        _aiMode = AiSettings.NormalizeMode(_settings.Ai.Mode);
        _cloudActiveProvider = AiSettings.NormalizeCloudProvider(_settings.Ai.Cloud.ActiveProvider);
#pragma warning restore MVVMTK0034
        _showThinkingInHistory = _settings.Ai.Chat.ShowThinkingInHistory;
        _cursorAcpAgentPath = _settings.Ai.Acp.CursorAcpPath ?? "";
        _cursorAcpModelId = _settings.Ai.Acp.CursorAcpModelId ?? "";
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

        _codeNavigationMapPresentation = CodeNavigationMapPresentationKind.Normalize(_settings.CodeNavigationMap.View);
        _codeNavigationMapLevel = CodeNavigationMapLevelKind.Normalize(_settings.CodeNavigationMap.Depth);
        _workspaceSplittersLocked = _settings.Workspace.SplittersLocked;

        _hciIntegrationEnabled = _settings.HybridIndex.Enabled;
        _hciIndexDir = ShellSettingsOrchestrator.NormalizeHybridIndexDir(_settings.HybridIndex.IndexDir);
        _hciDebounceMs = Math.Clamp(_settings.HybridIndex.DebounceMs, 0, 60_000);
        _hciAutoReindexOnSolutionOpen = _settings.HybridIndex.AutoReindexOnSolutionOpen;
        _hciWatchFiles = _settings.HybridIndex.WatchFiles;
        _hciScopeMode = ShellSettingsOrchestrator.NormalizeHybridIndexScopeMode(_settings.HybridIndex.ScopeMode);
        _hciPauseWhenMcpStdioHost = _settings.HybridIndex.PauseWhenMcpStdioHost;

        _ideMcpExecutor = new IdeMcpCommandExecutor(this);
        _webAiPortalBridge = new WebAiPortalCommandBridge(this);

        BuildOutputPanel = new BuildOutputPanelViewModel();
        TerminalPanel = new TerminalPanelViewModel(() => Workspace.SolutionPath);
        GitPanel = new GitPanelViewModel(_gitRunner, GetWorkspacePath, this, LoadSolution, RefreshGitSummaryAsync, osShell: _osShell);
        ChatPanel = new ChatPanelViewModel(
            _aiProviderManager,
            () => ActiveAiProvider,
            () => SelectedOllamaModel,
            () => ChatMcpOnly,
            () => ShowThinkingInHistory,
            () => UseMinimizedContext,
            () => CurrentFilePath,
            () => EditorText,
            GetWorkspacePath,
            () => CursorAcpAgentPath,
            () => Services.McpExternalServersJsonResolver.ResolveEffectiveJson(_settings),
            () => AcpAutoInjectIdeMcp,
            () => string.IsNullOrWhiteSpace(CursorAcpModelId) ? null : CursorAcpModelId.Trim(),
            id => CursorAcpModelId = id ?? "",
            appendAcpTerminal: text => UiScheduler.Default.Post(() => TerminalPanel.AppendOutput(text)),
            showAcpTerminal: () => UiScheduler.Default.Post(() =>
            {
                if (ShowTerminalPanelCommand.CanExecute(null))
                    ShowTerminalPanelCommand.Execute(null);
            }),
            executeIdeCommandForMafAgent: (commandId, args, ct) => ((Services.IIdeMcpActions)this).ExecuteCommandAsync(commandId, args, ct),
            getLocalOllamaEndpoint: () => new Uri(Services.OllamaService.DefaultBaseUriString),
            getEffectiveOllamaModelId: () => EffectiveOllamaModelId,
            tryCreateCloudMafIChatClient: TryCreateCloudMafIChatClientForChatPanel,
            getChatMinimizedContextBlock: BuildChatMinimizedContextBlockCore,
            getSendMessageKey: () => SendMessageKey);
        InstrumentationPanel = new InstrumentationPanelViewModel();
        InstrumentationPanel.PropertyChanged += OnInstrumentationPanelPropertyChanged;
        HypothesesPanel = new HypothesesPanelViewModel(GetWorkspacePath);

        ProblemsPanel = new ProblemsPanelViewModel(NavigateToProblemFromList);
        _workspaceDiagnostics = new Services.WorkspaceDiagnosticsCoordinator(_csharpLanguageService, ProblemsPanel);
        _workspaceDiagnostics.Attach(this);
        _workspaceDiagnostics.DiagnosticsChanged += OnWorkspaceDiagnosticsChangedForHud;
        MarkdownPreviewTool = new MarkdownPreviewToolViewModel();
        MarkdownPreviewTool.AttachToEditor(this);

        new UiChromeCapabilitiesModule().Register(_capabilities);
        new MarkdownCapabilitiesModule().Register(_capabilities);

        var csharpLsp = _settings.Languages.CSharp.ResolveForRuntime();
        _csharpLspProvider = csharpLsp.Mode;
        _csharpLspExecutable = csharpLsp.Executable;
        _csharpLspArguments = csharpLsp.Arguments;
        var markdownLsp = _settings.Languages.Markdown.ResolveForRuntime();
        _markdownLspProvider = markdownLsp.Mode;
        _markdownLspExecutable = markdownLsp.Executable;
        _markdownLspArguments = markdownLsp.Arguments;

        _mcpClientService = new Services.McpClientService(Services.McpExternalServersJsonResolver.ResolveEffectiveJson(_settings));
        _autonomousAgentService = CreateAutonomousAgentService(_mcpClientService);
        Autonomous = new AutonomousAgentSessionViewModel(_autonomousAgentService, this);
        _ideDataBus = new InMemoryDataBus(asynchronousDispatch: false);
        _hybridIndex = new HybridIndexOrchestrator(
            _ideDataBus,
            HybridIndexIndexDirectoryRelative.ResolveOrDefault(_settings.HybridIndex.IndexDir));
        _dapDebug = new Services.IdeDapDebugSession(() =>
        {
            UiScheduler.Default.Post(ApplyDapDebugSnapshotToUi);
        }, _ideDataBus);
        _dapDebug.StateChanged += (_, _) => NotifyDebugRelayCommandsChanged();
        _mcpBuildTest = new Services.McpDotnetBuildTestService(_dotnetRunner);
        _mcpAgentNotes = new Services.McpAgentNotesService(() => _settings);

        _workspaceHealth = new IdeHealthSnapshotUnit(_ideDataBus);
        SeedIdeHealthDataBus();
        Chrome.AfterGitWorkspaceHealthSummaryApplied = PublishGitToIdeDataBusAndRebuildIdeHealth;
        _workspaceHealthSurfaceCompositor = new IdeHealthSurfaceCompositor();

        _eicasFeed = new EmptyEicasFeed();
        _eicasFeed.MessagesChanged += (_, _) => RebuildEicas();
        _environmentReadinessChannel = new EnvironmentReadinessChannel();
        _environmentReadinessSurfaceCompositor = new EnvironmentReadinessSurfaceCompositor();

        Workspace.PropertyChanged += (_, e) => OnWorkspacePropertyChanged(e.PropertyName);
        RebuildIdeHealth();
        RebuildEicas();

        var pg = _settings.GetEffectivePresentationGrammar();
        var grammar = PresentationGrammarTokens.FromSettings(
            pg.Brackets,
            pg.BetweenScreens,
            pg.BetweenZones,
            pg.Pfd,
            pg.Forward,
            pg.Mfd);
        _presentationParse = PresentationParser.Parse(_settings.GetEffectivePresentationLine(), grammar);
        _presentationDedicatedMfdSecondScreen = _presentationParse.IsSuccess
            && PresentationLayoutAnalyzer.IsDedicatedMfdSecondScreenPreset(_presentationParse.Screens);
        _presentationTripleOneAnchorPerZone = _presentationParse.IsSuccess
            && PresentationLayoutAnalyzer.IsTripleOneAnchorPerZonePreset(_presentationParse.Screens);
        _presentationMfdHostTopology = _presentationDedicatedMfdSecondScreen || _presentationTripleOneAnchorPerZone;
        _presentationPmForwardTwoScreen = _presentationParse.IsSuccess
            && PresentationLayoutAnalyzer.IsPmPlusForwardTwoScreenPreset(_presentationParse.Screens);
        _presentationPmHostTopology = _presentationPmForwardTwoScreen;
        _instrumentMountPolicyResolver = new SettingsBackedInstrumentMountPolicyResolver();

        SyncMfdShellPageForPrimaryWorkSurface();
        ChatPanel.IsForwardIntercomLayout = PrimaryWorkSurface == PrimaryWorkSurfaceKind.Intercom;
        NotifyDockedInstrumentSlotBindings();
    }
}

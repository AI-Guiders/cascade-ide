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
using CascadeIDE.Features.Editor;
using CascadeIDE.Features.Shell;
using CascadeIDE.Features.Git;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.Instrumentation;
using CascadeIDE.Features.Markdown;
using CascadeIDE.Features.Os.DataAcquisition;
using CascadeIDE.Features.WebAiPortal.Application;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.Terminal;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Features.Workspace.Application;
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
        Shell = new ShellChromeViewModel(this);
        Shell.ApplyBootstrapFromSettings(_settings);
        Shell.PropertyChanged += OnShellChromePropertyChanged;
        ApplicationShell = new MainWindowApplicationShellViewModel(this);
        Build = new MainWindowBuildSessionViewModel(this);

        Editor = new EditorWorkspaceViewModel(this);
        Editor.PropertyChanged += OnEditorWorkspacePropertyChanged;
        Documents = new DocumentsWorkspaceViewModel(this, Workspace);
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
        _cursorAcpAgentPath = _settings.Ai.Acp.ResolveCursorAcpPath();
        _cursorAcpModelId = _settings.Ai.Acp.CursorAcpModelId ?? "";
        _anthropicApiKey = _aiKeys.AnthropicApiKey ?? "";
        _openAiApiKey = _aiKeys.OpenAiApiKey ?? "";
        _deepSeekApiKey = _aiKeys.DeepSeekApiKey ?? "";
        InitializeAgentUiDefaults();
        RegisterAgentFeedHandlers();
        ApplyUiModeLayout(Shell.UiMode, persist: false);
        if (UiModeFamily.IsPowerFamily())
            UiScheduler.Default.Post(RefreshWorkspaceSnapshotCore, DispatcherPriority.Background);

        Documents.InitializeDock();

        InitializeWorkspaceNavigationMap();
        NavigationMap.CodeNavigationMapPresentation =
            CodeNavigationMapPresentationKind.Normalize(_settings.CodeNavigationMap.View);
        NavigationMap.CodeNavigationMapLevel = CodeNavigationMapLevelKind.Normalize(_settings.CodeNavigationMap.Depth);
        NavigationMap.CodeNavigationMapControlFlowMainAxis =
            CodeNavigationMapControlFlowMainAxisKind.Normalize(_settings.CodeNavigationMap.ControlFlowMainAxis);

        _lastSavedSettings = (CascadeIdeSettings)_settings.Clone();
        _lastSavedAiKeys = (AiKeys)_aiKeys.Clone();
        _workspaceSplittersLocked = _settings.Workspace.SplittersLocked;

        _hciIntegrationEnabled = _settings.HybridIndex.Enabled;
        _hciIndexDir = ShellSettingsPresentationProjection.NormalizeHybridIndexDir(_settings.HybridIndex.IndexDir);
        _hciDebounceMs = Math.Clamp(_settings.HybridIndex.DebounceMs, 0, 60_000);
        _hciAutoReindexOnSolutionOpen = _settings.HybridIndex.AutoReindexOnSolutionOpen;
        _hciWatchFiles = _settings.HybridIndex.WatchFiles;
        _hciScopeMode = ShellSettingsPresentationProjection.NormalizeHybridIndexScopeMode(_settings.HybridIndex.ScopeMode);
        _hciPauseWhenMcpStdioHost = _settings.HybridIndex.PauseWhenMcpStdioHost;

        var transport = _settings.Intercom.Transport;
        _intercomTransportEnabled = transport.Enabled;
        _intercomTransportBaseUrl = transport.BaseUrl;
        _intercomTransportLocalServerPath = transport.LocalServerPath;
        _intercomTransportTeamId = transport.TeamId;
        _intercomTransportDefaultTopicId = transport.DefaultTopicId;
        _intercomTransportOAuthProvider = string.IsNullOrWhiteSpace(transport.OAuthProvider) ? "github" : transport.OAuthProvider;
        _intercomTransportDevTeamToken = transport.DevTeamToken;
        _intercomTransportSseReconnectBackoffMs = transport.SseReconnectBackoffMs;
        _intercomTransportAutoConnectOnSend = transport.AutoConnectOnSend;
        _intercomTransportSyncAgentChannelMessages = transport.SyncAgentChannelMessages;

        _ideMcpHost = new MainWindowIdeMcpHost(this);
        _webAiPortalBridge = new WebAiPortalCommandBridge(IdeMcp);

        _ideDataBus = new InMemoryDataBus(asynchronousDispatch: false);
        _buildTestJobService = new DotNetBuildTest.Core.BuildTestJobService();
        _agentEnvironment = new Features.Agent.Environment.AgentEnvironmentService(
            _ideDataBus,
            _settings.Agent.Environment,
            _buildTestJobService,
            _csharpLanguageService,
            GetOpenCsDocumentsForDiagnoseFiles,
            _gitRunner,
            () => WorkspaceDirectoryFromSolutionPath.Resolve(Workspace.SolutionPath),
            () => Workspace.SolutionPath,
            GetDiagnoseFilesWarmupCsFilePaths);

        BuildOutputPanel = new BuildOutputPanelViewModel();
        TerminalPanel = new TerminalPanelViewModel(() => Workspace.SolutionPath);
        GitPanel = new GitPanelViewModel(_gitRunner, GetWorkspacePath, IdeMcp, LoadSolution, RefreshGitSummaryAsync, osShell: _osShell);
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
            executeIdeCommandForMafAgent: (commandId, args, ct) => IdeMcp.ExecuteCommandAsync(commandId, args, ct),
            revealIntercomAttachmentInIde: (anchor, select, ct) =>
                RevealIntercomAttachmentInIdeAsync(anchor, select, ct),
            getLocalOllamaEndpoint: () => new Uri(Services.OllamaService.DefaultBaseUriString),
            getEffectiveOllamaModelId: () => EffectiveOllamaModelId,
            tryCreateCloudMafIChatClient: TryCreateCloudMafIChatClientForChatPanel,
            getChatMinimizedContextBlock: BuildChatMinimizedContextBlockCore,
            getSendMessageKey: () => SendMessageKey,
            getComposerNewLineKey: () => ComposerNewLineKey,
            getSolutionPath: () => Workspace.SolutionPath,
            getSolutionRoots: () => Workspace.SolutionRoots,
            getEditorSelectionStart: () => EditorSelectionStart,
            getEditorSelectionLength: () => EditorSelectionLength,
            getEditorCaretOffset: () => NavigationMap.EditorCaretOffset,
            getTextEditorForAbsoluteFilePath: path =>
                string.IsNullOrWhiteSpace(path)
                    ? null
                    : EditorActiveDockResolver.TryGetEditor(this, path),
            agentEnvironment: _agentEnvironment,
            getSolutionPathForAgent: () => Workspace.SolutionPath);
        ChatPanel.SetIntercomFontsSettings(_settings.Fonts.Intercom);
        ChatPanel.ApplyIntercomPresentationSettings(_settings.Intercom);
        CockpitCommandLineOverlay = new CockpitCommandLineOverlayViewModel(
            ChatPanel,
            () => PrimaryWorkSurface,
            () => CommandPaletteHost);
        ChatPanel.SetCascadeSettingsAccessor(() => _settings);
        ChatPanel.SetIntercomTransportCoordinator(_intercomTransport);
        ChatPanel.SetIntercomAdminRunner((handlerId, argsTail, ct) =>
            RunIntercomAdminSlashAsync(handlerId, argsTail, ct));
        InstrumentationPanel = new InstrumentationPanelViewModel();
        InstrumentationPanel.PropertyChanged += OnInstrumentationPanelPropertyChanged;
        HypothesesPanel = new HypothesesPanelViewModel(GetWorkspacePath);

        ProblemsPanel = new ProblemsPanelViewModel(NavigateToProblemFromList);
        _workspaceDiagnostics = new Services.WorkspaceDiagnosticsCoordinator(_csharpLanguageService, ProblemsPanel);
        _workspaceDiagnostics.Attach(this);
        _workspaceDiagnostics.DiagnosticsChanged += OnWorkspaceDiagnosticsChangedForHud;
        MarkdownPreviewTool = new MarkdownPreviewToolViewModel();
        MarkdownPreviewTool.AttachToEditor(this);
        StartMagicLinkListener();

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
        _hybridIndex = new HybridIndexOrchestrator(
            _ideDataBus,
            HybridIndexIndexDirectoryRelative.ResolveOrDefault(_settings.HybridIndex.IndexDir));
        _dapDebug = new Services.IdeDapDebugSession(() =>
        {
            UiScheduler.Default.Post(_ideMcpHost.ApplyDapDebugSnapshotToUi);
        }, _ideDataBus);
        Debug = new MainWindowDebugSessionViewModel(this);
        _dapDebug.StateChanged += (_, _) => Debug.NotifyRelayCommandsChanged();
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
        EnsureAgentEnvironmentWiring();
    }

    private IReadOnlyList<(string Path, string Content)> GetOpenCsDocumentsForDiagnoseFiles()
    {
        var list = new List<(string Path, string Content)>();
        foreach (var doc in Documents.OpenDocuments)
        {
            if (!doc.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                continue;
            list.Add((doc.FilePath, doc.Content ?? ""));
        }

        return list;
    }

    private IReadOnlyList<string> GetDiagnoseFilesWarmupCsFilePaths()
    {
        var warmup = _settings.SolutionWarmup;
        return Features.Agent.Environment.AgentDiagnoseFilesWarmupPathCollector.Collect(
            warmup.Enabled,
            warmup.WarmActiveFileOnSolutionOpen,
            warmup.WarmOpenDocuments,
            warmup.WarmRecentCsFiles,
            warmup.MaxOpenDocumentFiles,
            () => Documents.OpenDocuments
                .Select(d => d.FilePath)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList(),
            () => CurrentFilePath);
    }
}

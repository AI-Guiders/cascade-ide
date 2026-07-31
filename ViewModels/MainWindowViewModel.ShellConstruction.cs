using Avalonia.Threading;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.AutonomousAgent;
using CascadeIDE.Features.Build;
using CascadeIDE.Features.Debug;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.Editor;
using CascadeIDE.Features.Shell;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Features.Os.DataAcquisition;
using CascadeIDE.Features.WebAiPortal.Application;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Конструктор и композиция shell: дочерние VM, шина, DAP/HCI (ADR 0017).
/// Panels → <c>ShellConstruction.Panels</c>; health/presentation → <c>ShellConstruction.HealthPresentation</c>;
/// glass patch → <c>ShellConstruction.GlassPatch</c>; diagnose → <c>ShellConstruction.Diagnose</c>.
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
        InitializeWorkspaceNavigationMap();
        NavigationMap.CodeNavigationMapPresentation =
            CodeNavigationMapPresentationKind.Normalize(_settings.CodeNavigationMap.View);
        NavigationMap.CodeNavigationMapLevel = CodeNavigationMapLevelKind.Normalize(_settings.CodeNavigationMap.Depth);
        NavigationMap.CodeNavigationMapControlFlowMainAxis =
            CodeNavigationMapControlFlowMainAxisKind.Normalize(_settings.CodeNavigationMap.ControlFlowMainAxis);
        ApplyUiModeLayout(Shell.UiMode, persist: false);
        if (UiModeFamily.IsPowerFamily())
            UiScheduler.Default.Post(RefreshWorkspaceSnapshotCore, DispatcherPriority.Background);

        Documents.InitializeDock();

        InitializeEditorNavigation();

        _lastSavedSettings = (CascadeIdeSettings)_settings.Clone();
        _lastSavedAiKeys = (AiKeys)_aiKeys.Clone();
        _workspaceSplittersLocked = _settings.Workspace.SplittersLocked;
        ApplySolutionExplorerSettingsFromModel(_settings.Workspace.SolutionExplorer);

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

        _ideDataBus = new InMemoryDataBus(asynchronousDispatch: false, DataBusEventPolicyLoader.Load());
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

        var panels = CreateShellPanels();
        BuildOutputPanel = panels.BuildOutput;
        TerminalPanel = panels.Terminal;
        GitPanel = panels.Git;
        ChatPanel = panels.Chat;
        CockpitCommandLineOverlay = panels.CockpitCommandLine;
        InstrumentationPanel = panels.Instrumentation;
        HypothesesPanel = panels.Hypotheses;
        ProblemsPanel = panels.Problems;
        _workspaceDiagnostics = panels.WorkspaceDiagnostics;
        MarkdownPreviewTool = panels.MarkdownPreview;
        WireShellPanelsAfterConstruct();

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

        var health = CreateHealthAndPresentation();
        _workspaceHealth = health.WorkspaceHealth;
        _workspaceHealthSurfaceCompositor = health.WorkspaceHealthSurface;
        _eicasFeed = health.EicasFeed;
        _environmentReadinessChannel = health.EnvironmentReadiness;
        _environmentReadinessSurfaceCompositor = health.EnvironmentReadinessSurface;
        _presentationParse = health.PresentationParse;
        _presentationDedicatedMfdSecondScreen = health.DedicatedMfdSecondScreen;
        _presentationTripleOneAnchorPerZone = health.TripleOneAnchorPerZone;
        _presentationMfdHostTopology = health.MfdHostTopology;
        _presentationPmForwardTwoScreen = health.PmForwardTwoScreen;
        _presentationPmHostTopology = health.PmHostTopology;
        _instrumentMountPolicyResolver = health.InstrumentMountPolicy;
        WireHealthAndPresentationAfterConstruct();
    }
}

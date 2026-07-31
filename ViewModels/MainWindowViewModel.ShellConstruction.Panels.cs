using CascadeIDE.Features.Build;
using CascadeIDE.Features.Chat;
using CascadeIDE.Features.Debug;
using CascadeIDE.Features.Git;
using CascadeIDE.Features.Instrumentation;
using CascadeIDE.Features.Markdown;
using CascadeIDE.Features.Shell;
using CascadeIDE.Features.Terminal;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Views;

namespace CascadeIDE.ViewModels;

/// <summary>Ctor panel factory — returns VMs for readonly/get-only assign in the constructor.</summary>
public partial class MainWindowViewModel
{
    readonly record struct ShellPanelsBundle(
        BuildOutputPanelViewModel BuildOutput,
        TerminalPanelViewModel Terminal,
        GitPanelViewModel Git,
        ChatPanelViewModel Chat,
        CockpitCommandLineOverlayViewModel CockpitCommandLine,
        InstrumentationPanelViewModel Instrumentation,
        HypothesesPanelViewModel Hypotheses,
        ProblemsPanelViewModel Problems,
        Services.WorkspaceDiagnosticsCoordinator WorkspaceDiagnostics,
        MarkdownPreviewToolViewModel MarkdownPreview);

    ShellPanelsBundle CreateShellPanels()
    {
        var buildOutput = new BuildOutputPanelViewModel();
        var terminal = new TerminalPanelViewModel(() => Workspace.SolutionPath);
        var git = new GitPanelViewModel(_gitRunner, GetWorkspacePath, IdeMcp, LoadSolution, RefreshGitSummaryAsync, osShell: _osShell);
        var chat = new ChatPanelViewModel(
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
            () => ResolveAcpAutoInjectIdeMcp(),
            () => string.IsNullOrWhiteSpace(CursorAcpModelId) ? null : CursorAcpModelId.Trim(),
            id => CursorAcpModelId = id ?? "",
            appendAcpTerminal: text => UiScheduler.Default.Post(() => terminal.AppendOutput(text)),
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
            revealAgentRangeInEditor: (path, startLine, endLine) =>
            {
                var dock = EditorActiveDockResolver.TryGetDockDocumentView(this, path);
                return dock?.RevealAgentRangeAsync(startLine, endLine, persistent: true) ?? Task.CompletedTask;
            },
            clearAgentRevealInEditor: path =>
                EditorActiveDockResolver.TryGetDockDocumentView(this, path)?.ClearAgentReveal(),
            agentEnvironment: _agentEnvironment,
            getSolutionPathForAgent: () => Workspace.SolutionPath);

        var cockpitCommandLine = new CockpitCommandLineOverlayViewModel(
            chat,
            () => PrimaryWorkSurface,
            () => CommandPaletteHost);
        var instrumentation = new InstrumentationPanelViewModel();
        var hypotheses = new HypothesesPanelViewModel(GetWorkspacePath);
        var problems = new ProblemsPanelViewModel(NavigateToProblemFromList, AttachSelectedProblemToIntercom);
        var workspaceDiagnostics = new Services.WorkspaceDiagnosticsCoordinator(_csharpLanguageService, problems);
        var markdownPreview = new MarkdownPreviewToolViewModel();

        return new ShellPanelsBundle(
            buildOutput,
            terminal,
            git,
            chat,
            cockpitCommandLine,
            instrumentation,
            hypotheses,
            problems,
            workspaceDiagnostics,
            markdownPreview);
    }

    void WireShellPanelsAfterConstruct()
    {
        ChatPanel.SetIntercomFontsSettings(_settings.Fonts.Intercom);
        ChatPanel.ApplyIntercomPresentationSettings(_settings.Intercom);
        ChatPanel.ShowMarkdownPreview = (title, content) => RequestShowMarkdownPreviewWindow?.Invoke(title, content);
        ChatPanel.SetCascadeSettingsAccessor(() => _settings);
        ChatPanel.SetFmOpenAiCredentialsAccessor(ResolveFmOpenAiCredentialsForCatalog);
        ChatPanel.SetIntercomTransportCoordinator(_intercomTransport);
        ChatPanel.SetIntercomAdminRunner((handlerId, argsTail, ct) =>
            RunIntercomAdminSlashAsync(handlerId, argsTail, ct));
        InstrumentationPanel.PropertyChanged += OnInstrumentationPanelPropertyChanged;
        _workspaceDiagnostics.Attach(this);
        _workspaceDiagnostics.DiagnosticsChanged += OnWorkspaceDiagnosticsChangedForHud;
        WireIntercomAttachAffordances();
        MarkdownPreviewTool.AttachToEditor(this);
        StartMagicLinkListener();

        new UiChromeCapabilitiesModule().Register(_capabilities);
        new MarkdownCapabilitiesModule().Register(_capabilities);
    }
}

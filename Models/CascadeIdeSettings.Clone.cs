using OutWit.Common.Abstract;

namespace CascadeIDE.Models;

public sealed partial class CascadeIdeSettings
{
    public override ModelBase Clone()
    {
        return new CascadeIdeSettings
        {
            Ai = new AiSettings
            {
                Mode = Ai.Mode,
                Local = new AiLocalSettings
                {
                    Backend = Ai.Local.Backend,
                    Ollama = new AiLocalOllamaSettings { Model = Ai.Local.Ollama.Model },
                },
                Acp = new AiAcpSettings
                {
                    CursorAcpPath = Ai.Acp.CursorAcpPath,
                    CursorAcpPathEnv = Ai.Acp.CursorAcpPathEnv,
                    CursorAcpModelId = Ai.Acp.CursorAcpModelId,
                },
                McpOnly = new AiMcpOnlySettings(),
                Cloud = new AiCloudSettings
                {
                    ActiveProvider = Ai.Cloud.ActiveProvider,
                    Anthropic = new AiCloudAnthropicSettings { Model = Ai.Cloud.Anthropic.Model },
                    OpenAi = new AiCloudOpenAiSettings
                    {
                        BaseUrl = Ai.Cloud.OpenAi.BaseUrl,
                        Model = Ai.Cloud.OpenAi.Model,
                    },
                    DeepSeek = new AiCloudDeepSeekSettings
                    {
                        BaseUrl = Ai.Cloud.DeepSeek.BaseUrl,
                        Model = Ai.Cloud.DeepSeek.Model,
                    },
                },
                Chat = new AiChatSettings
                {
                    SettingsPresentation = Ai.Chat.SettingsPresentation,
                    ShowThinkingInHistory = Ai.Chat.ShowThinkingInHistory,
                },
            },
            Mcp = new McpSettings
            {
                ExternalServersJson = Mcp.ExternalServersJson,
                AcpAutoInjectIdeMcp = Mcp.AcpAutoInjectIdeMcp,
                ExternalServersJsonPath = Mcp.ExternalServersJsonPath,
            },
            HybridIndex = new HybridIndexSettings
            {
                Enabled = HybridIndex.Enabled,
                IndexDir = HybridIndex.IndexDir,
                DebounceMs = HybridIndex.DebounceMs,
                AutoReindexOnSolutionOpen = HybridIndex.AutoReindexOnSolutionOpen,
                WatchFiles = HybridIndex.WatchFiles,
                ScopeMode = HybridIndex.ScopeMode,
                PauseWhenMcpStdioHost = HybridIndex.PauseWhenMcpStdioHost,
            },
            SolutionWarmup = new SolutionWarmupSettings
            {
                Enabled = SolutionWarmup.Enabled,
                WarmActiveFileOnSolutionOpen = SolutionWarmup.WarmActiveFileOnSolutionOpen,
                WarmFeedAnchorsAfterSymbolSidecar = SolutionWarmup.WarmFeedAnchorsAfterSymbolSidecar,
                WarmOpenDocuments = SolutionWarmup.WarmOpenDocuments,
                WarmRecentCsFiles = SolutionWarmup.WarmRecentCsFiles,
                MaxParallelFileJobs = SolutionWarmup.MaxParallelFileJobs,
                MaxOpenDocumentFiles = SolutionWarmup.MaxOpenDocumentFiles,
                ShowBackgroundStatusOnPfd = SolutionWarmup.ShowBackgroundStatusOnPfd,
            },
            CommandPalette = new CommandPaletteSettings
            {
                GoToSearch = new CommandPaletteGoToSearchSettings { Backend = CommandPalette.GoToSearch.Backend },
            },
            AgentNotes = new AgentNotesSettings
            {
                ConfigPath = AgentNotes.ConfigPath,
                ConfigPathEnv = AgentNotes.ConfigPathEnv,
                KbBaseOverlayPath = AgentNotes.KbBaseOverlayPath,
                KbBaseOverlayPathEnv = AgentNotes.KbBaseOverlayPathEnv,
            },
            Agent = new AgentSettings
            {
                Environment = new AgentEnvironmentSettings
                {
                    DefaultVerifyPolicy = Agent.Environment.DefaultVerifyPolicy,
                    DefaultSandboxProfile = Agent.Environment.DefaultSandboxProfile,
                    RunnerMaxConcurrency = Agent.Environment.RunnerMaxConcurrency,
                    CoalesceWindowMs = Agent.Environment.CoalesceWindowMs,
                    ShellEscapeTier = Agent.Environment.ShellEscapeTier,
                    LongRunSandboxProfile = Agent.Environment.LongRunSandboxProfile,
                    BuildVerifyHost = Agent.Environment.BuildVerifyHost,
                    BuildVerifyWorkerAssemblyPath = Agent.Environment.BuildVerifyWorkerAssemblyPath,
                    DevServices = new AgentDevServiceContractSettings
                    {
                        RequireConfigOverride = Agent.Environment.DevServices.RequireConfigOverride,
                        GateTestScopedOnViolation = Agent.Environment.DevServices.GateTestScopedOnViolation,
                    },
                    Ladder = new AgentEnvironmentLadderSettings
                    {
                        DiagnoseFilesEnabled = Agent.Environment.Ladder.DiagnoseFilesEnabled,
                        TestFullRequireExplicit = Agent.Environment.Ladder.TestFullRequireExplicit,
                        DiagnoseFilesCsScope = Agent.Environment.Ladder.DiagnoseFilesCsScope,
                        DiagnoseFilesGitDirtyMaxFiles = Agent.Environment.Ladder.DiagnoseFilesGitDirtyMaxFiles,
                        DiagnoseFilesIncludeWarmupCs = Agent.Environment.Ladder.DiagnoseFilesIncludeWarmupCs,
                        DiagnoseFilesWarmupMaxFiles = Agent.Environment.Ladder.DiagnoseFilesWarmupMaxFiles,
                        TestScopedTouchedTestsOnly = Agent.Environment.Ladder.TestScopedTouchedTestsOnly,
                    },
                    TimeAccounting = new AgentEnvironmentTimeAccountingSettings
                    {
                        ShowInChat = Agent.Environment.TimeAccounting.ShowInChat,
                        PfdInstrumentEnabled = Agent.Environment.TimeAccounting.PfdInstrumentEnabled,
                        ShowTaskProgressInChat = Agent.Environment.TimeAccounting.ShowTaskProgressInChat,
                        IdleUserThresholdMs = Agent.Environment.TimeAccounting.IdleUserThresholdMs,
                    },
                },
                Harness = new AgentHarnessSettings
                {
                    LoadHotContextOnSessionStart = Agent.Harness.LoadHotContextOnSessionStart,
                    LoadHotContextOnTopicFork = Agent.Harness.LoadHotContextOnTopicFork,
                    HotContextActiveScope = Agent.Harness.HotContextActiveScope,
                    CheckpointEnabled = Agent.Harness.CheckpointEnabled,
                    CheckpointThresholdUserTurns = Agent.Harness.CheckpointThresholdUserTurns,
                    CheckpointRepeatEveryUserTurns = Agent.Harness.CheckpointRepeatEveryUserTurns,
                    AutoVerifyAfterCsWrite = Agent.Harness.AutoVerifyAfterCsWrite,
                    SuppressAcpIdeStdioInject = Agent.Harness.SuppressAcpIdeStdioInject,
                    CheckpointOnContextPressure = Agent.Harness.CheckpointOnContextPressure,
                    ContextPressureThreadMessageThreshold = Agent.Harness.ContextPressureThreadMessageThreshold,
                    ContextPressureRepeatEveryMessages = Agent.Harness.ContextPressureRepeatEveryMessages,
                    InjectHarnessTelemetryInContext = Agent.Harness.InjectHarnessTelemetryInContext,
                    InjectTopicForkBrief = Agent.Harness.InjectTopicForkBrief,
                    TopicForkBriefTemplate = Agent.Harness.TopicForkBriefTemplate,
                },
            },
            Workspace = new WorkspaceSettings
            {
                PfdExpanded = Workspace.PfdExpanded,
                ShowTerminal = Workspace.ShowTerminal,
                ShowGit = Workspace.ShowGit,
                ShowInstrumentation = Workspace.ShowInstrumentation,
                Mode = Workspace.Mode,
                Culture = Workspace.Culture,
                SplittersLocked = Workspace.SplittersLocked,
                PrimaryWorkSurface = Workspace.PrimaryWorkSurface,
                SolutionExplorer = new SolutionExplorerSettings
                {
                    TrackActiveItem = Workspace.SolutionExplorer.TrackActiveItem,
                    CompactTree = Workspace.SolutionExplorer.CompactTree,
                },
            },
            CodeNavigationMap = new CodeNavigationMapSettings
            {
                View = CodeNavigationMap.View,
                Depth = CodeNavigationMap.Depth,
                DetailLevel = CodeNavigationMap.DetailLevel,
                RelatedGraphLayout = CodeNavigationMap.RelatedGraphLayout,
                ControlFlowMainAxis = CodeNavigationMap.ControlFlowMainAxis,
                ControlFlowGrain = CodeNavigationMap.ControlFlowGrain,
                ConditionBranchLabelPreset = CodeNavigationMap.ConditionBranchLabelPreset,
                ConditionBranch = new CodeNavigationMapConditionBranchToml
                {
                    Presets = CodeNavigationMap.ConditionBranch.Presets
                        .Select(p => new CodeNavigationMapConditionBranchPresetEntry
                        {
                            Id = p.Id,
                            Positive = p.Positive,
                            Negative = p.Negative
                        })
                        .ToList()
                },
                ConditionBranchPositive = CodeNavigationMap.ConditionBranchPositive,
                ConditionBranchNegative = CodeNavigationMap.ConditionBranchNegative,
            },
            Languages = new LanguagesSettings
            {
                CSharp = new CSharpLanguageServerSettings
                {
                    Mode = Languages.CSharp.Mode,
                    ParseOnly = new LanguageServerLaunchProfile
                    {
                        Executable = Languages.CSharp.ParseOnly.Executable,
                        ExecutableEnv = Languages.CSharp.ParseOnly.ExecutableEnv,
                        Arguments = Languages.CSharp.ParseOnly.Arguments,
                        ArgumentsEnv = Languages.CSharp.ParseOnly.ArgumentsEnv,
                    },
                    OmniSharp = new LanguageServerLaunchProfile
                    {
                        Executable = Languages.CSharp.OmniSharp.Executable,
                        ExecutableEnv = Languages.CSharp.OmniSharp.ExecutableEnv,
                        Arguments = Languages.CSharp.OmniSharp.Arguments,
                        ArgumentsEnv = Languages.CSharp.OmniSharp.ArgumentsEnv,
                    },
                    CSharpLs = new LanguageServerLaunchProfile
                    {
                        Executable = Languages.CSharp.CSharpLs.Executable,
                        ExecutableEnv = Languages.CSharp.CSharpLs.ExecutableEnv,
                        Arguments = Languages.CSharp.CSharpLs.Arguments,
                        ArgumentsEnv = Languages.CSharp.CSharpLs.ArgumentsEnv,
                    },
                    Custom = new LanguageServerLaunchProfile
                    {
                        Executable = Languages.CSharp.Custom.Executable,
                        ExecutableEnv = Languages.CSharp.Custom.ExecutableEnv,
                        Arguments = Languages.CSharp.Custom.Arguments,
                        ArgumentsEnv = Languages.CSharp.Custom.ArgumentsEnv,
                    },
                },
                Markdown = new MarkdownLanguageServerSettings
                {
                    Mode = Languages.Markdown.Mode,
                    Off = new LanguageServerLaunchProfile
                    {
                        Executable = Languages.Markdown.Off.Executable,
                        ExecutableEnv = Languages.Markdown.Off.ExecutableEnv,
                        Arguments = Languages.Markdown.Off.Arguments,
                        ArgumentsEnv = Languages.Markdown.Off.ArgumentsEnv,
                    },
                    Marksman = new LanguageServerLaunchProfile
                    {
                        Executable = Languages.Markdown.Marksman.Executable,
                        ExecutableEnv = Languages.Markdown.Marksman.ExecutableEnv,
                        Arguments = Languages.Markdown.Marksman.Arguments,
                        ArgumentsEnv = Languages.Markdown.Marksman.ArgumentsEnv,
                    },
                    Custom = new LanguageServerLaunchProfile
                    {
                        Executable = Languages.Markdown.Custom.Executable,
                        ExecutableEnv = Languages.Markdown.Custom.ExecutableEnv,
                        Arguments = Languages.Markdown.Custom.Arguments,
                        ArgumentsEnv = Languages.Markdown.Custom.ArgumentsEnv,
                    },
                },
            },
            Markdown = new MarkdownSettings
            {
                Diagrams = new MarkdownDiagramSettings
                {
                    Kroki = Markdown.Diagrams.Kroki,
                    KrokiUrl = Markdown.Diagrams.KrokiUrl,
                },
            },
            Display = new DisplaySettings
            {
                MaximizeHostsOnDedicatedScreens = Display.MaximizeHostsOnDedicatedScreens,
                PreferRepoInstruments = Display.PreferRepoInstruments,
                Instruments = Display.Instruments is { Count: > 0 } ir
                    ? new Dictionary<string, string>(ir, StringComparer.OrdinalIgnoreCase)
                    : null,
                Pfd = new DisplayPfdHostSettings
                {
                    OpenOnStartup = Display.Pfd.OpenOnStartup,
                    PixelX = Display.Pfd.PixelX,
                    PixelY = Display.Pfd.PixelY,
                    Width = Display.Pfd.Width,
                    Height = Display.Pfd.Height,
                },
                Mfd = new DisplayMfdHostSettings
                {
                    OpenOnStartup = Display.Mfd.OpenOnStartup,
                    PixelX = Display.Mfd.PixelX,
                    PixelY = Display.Mfd.PixelY,
                    Width = Display.Mfd.Width,
                    Height = Display.Mfd.Height,
                },
                Pm = new DisplayPmHostSettings
                {
                    OpenOnStartup = Display.Pm.OpenOnStartup,
                    PixelX = Display.Pm.PixelX,
                    PixelY = Display.Pm.PixelY,
                    Width = Display.Pm.Width,
                    Height = Display.Pm.Height,
                },
                Skia = new DisplaySkiaSettings
                {
                    ZoneGeometryOverlay = Display.Skia.ZoneGeometryOverlay,
                    InstrumentMount = Display.Skia.InstrumentMount,
                },
                Mount = new DisplayMountSettings
                {
                    DefaultStyle = Display.Mount.DefaultStyle,
                    EnforceEligibility = Display.Mount.EnforceEligibility,
                    MinSa = Display.Mount.MinSa,
                    MinPerformance = Display.Mount.MinPerformance,
                    MaxWorkload = Display.Mount.MaxWorkload,
                    RequireScores = Display.Mount.RequireScores,
                    Rules = Display.Mount.Rules
                        .Select(static r => new InstrumentMountPolicyRuleSettings
                        {
                            Surface = r.Surface,
                            Slot = r.Slot,
                            Instrument = r.Instrument,
                            Style = r.Style,
                            SaScore = r.SaScore,
                            PerformanceScore = r.PerformanceScore,
                            WorkloadScore = r.WorkloadScore,
                        })
                        .ToList(),
                },
                Screens = new DisplayScreensSettings
                {
                    Topology = Display.Screens.Topology,
                    Grammar = new PresentationGrammarSettings
                    {
                        Brackets = Display.Screens.Grammar.Brackets,
                        BetweenScreens = Display.Screens.Grammar.BetweenScreens,
                        BetweenZones = Display.Screens.Grammar.BetweenZones,
                        Pfd = Display.Screens.Grammar.Pfd,
                        Forward = Display.Screens.Grammar.Forward,
                        Mfd = Display.Screens.Grammar.Mfd,
                    },
                },
                Presentation = new DisplayPresentationSettings
                {
                    Tier = Display.Presentation.Tier,
                    CockpitMinTotalWidthPx = Display.Presentation.CockpitMinTotalWidthPx,
                    CockpitMinAnchorWidthPx = Display.Presentation.CockpitMinAnchorWidthPx,
                    CompactIntercomPlacement = Display.Presentation.CompactIntercomPlacement,
                    UltrawideCockpitEnabled = Display.Presentation.UltrawideCockpitEnabled,
                    TierFirstRunCompleted = Display.Presentation.TierFirstRunCompleted,
                    CompactAuxiliaryPanelWidthPx = Display.Presentation.CompactAuxiliaryPanelWidthPx,
                },
            },
            Editor = new EditorSettings
            {
                InlineHints = new EditorInlineHintsSettings
                {
                    Enabled = Editor.InlineHints.Enabled,
                    ParameterNames = Editor.InlineHints.ParameterNames,
                    VariableTypes = Editor.InlineHints.VariableTypes,
                },
                DebugHints = new EditorDebugHintsSettings
                {
                    Enabled = Editor.DebugHints.Enabled,
                    ShowAssignments = Editor.DebugHints.ShowAssignments,
                    ShowConditions = Editor.DebugHints.ShowConditions,
                },
            },
            CodeNavigation = new CodeNavigationSettings
            {
                Presets = CodeNavigation.Presets
                    .Select(p => new CodeNavigationPresetEntry
                    {
                        Id = p.Id,
                        IncludeKinds = p.IncludeKinds?.ToList(),
                        ExcludeKinds = p.ExcludeKinds?.ToList(),
                    })
                    .ToList(),
            },
            Intercom = new IntercomSettings
            {
                FeedMetrics = Intercom.FeedMetrics,
                TciValidationIcon = Intercom.TciValidationIcon,
                Attachments = new IntercomAttachmentsSettings
                {
                    Code = new IntercomAttachmentsCodeSettings
                    {
                        Navigate = Intercom.Attachments.Code.Navigate,
                        RevealLoadSolution = Intercom.Attachments.Code.RevealLoadSolution,
                    },
                },
                Transport = new IntercomTransportSettings
                {
                    Enabled = Intercom.Transport.Enabled,
                    BaseUrl = Intercom.Transport.BaseUrl,
                    BaseUrlEnv = Intercom.Transport.BaseUrlEnv,
                    LocalServerPath = Intercom.Transport.LocalServerPath,
                    LocalServerPathEnv = Intercom.Transport.LocalServerPathEnv,
                    TeamId = Intercom.Transport.TeamId,
                    DefaultTopicId = Intercom.Transport.DefaultTopicId,
                    OAuthProvider = Intercom.Transport.OAuthProvider,
                    InviteToken = Intercom.Transport.InviteToken,
                    DevTeamToken = Intercom.Transport.DevTeamToken,
                    WorkspaceHints = Intercom.Transport.WorkspaceHints.ToDictionary(
                        kv => kv.Key,
                        kv => new IntercomWorkspaceHintEntry
                        {
                            TeamId = kv.Value.TeamId,
                            ProjectId = kv.Value.ProjectId,
                            UpdatedAtUtc = kv.Value.UpdatedAtUtc,
                            Source = kv.Value.Source,
                        },
                        StringComparer.OrdinalIgnoreCase),
                    SseReconnectBackoffMs = Intercom.Transport.SseReconnectBackoffMs,
                    AutoConnectOnSend = Intercom.Transport.AutoConnectOnSend,
                    SyncAgentChannelMessages = Intercom.Transport.SyncAgentChannelMessages,
                    SelectedAgentMemberId = Intercom.Transport.SelectedAgentMemberId,
                    SelectedAgentDisplayName = Intercom.Transport.SelectedAgentDisplayName,
                },
            },
        };
    }

}

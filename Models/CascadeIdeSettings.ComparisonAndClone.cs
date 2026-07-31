using OutWit.Common.Abstract;
using OutWit.Common.Values;

namespace CascadeIDE.Models;

public sealed partial class CascadeIdeSettings
{
    public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
    {
        if (modelBase is not CascadeIdeSettings o)
            return false;
        return AiEquals(Ai, o.Ai)
            && McpEquals(Mcp, o.Mcp)
            && AgentNotesEquals(AgentNotes, o.AgentNotes)
            && AgentEquals(Agent, o.Agent)
            && WorkspaceEquals(Workspace, o.Workspace)
            && HybridIndexEquals(HybridIndex, o.HybridIndex)
            && SolutionWarmupEquals(SolutionWarmup, o.SolutionWarmup)
            && CommandPaletteEquals(CommandPalette, o.CommandPalette)
            && CodeNavigationMapEquals(CodeNavigationMap, o.CodeNavigationMap)
            && LanguagesEquals(Languages, o.Languages)
            && MarkdownEquals(Markdown, o.Markdown)
            && DisplayEquals(Display, o.Display)
            && EditorEquals(Editor, o.Editor)
            && CodeNavigationEquals(CodeNavigation, o.CodeNavigation)
            && IntercomEquals(Intercom, o.Intercom);
    }

    private static bool AiEquals(AiSettings? a, AiSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Mode.Is(b.Mode)
            && a.Local.Backend.Is(b.Local.Backend)
            && a.Local.Ollama.Model.Is(b.Local.Ollama.Model)
            && a.Acp.CursorAcpPath.Is(b.Acp.CursorAcpPath)
            && a.Acp.CursorAcpPathEnv.Is(b.Acp.CursorAcpPathEnv)
            && a.Acp.CursorAcpModelId.Is(b.Acp.CursorAcpModelId)
            && a.Cloud.ActiveProvider.Is(b.Cloud.ActiveProvider)
            && a.Cloud.Anthropic.Model.Is(b.Cloud.Anthropic.Model)
            && a.Cloud.OpenAi.BaseUrl.Is(b.Cloud.OpenAi.BaseUrl)
            && a.Cloud.OpenAi.Model.Is(b.Cloud.OpenAi.Model)
            && a.Cloud.DeepSeek.BaseUrl.Is(b.Cloud.DeepSeek.BaseUrl)
            && a.Cloud.DeepSeek.Model.Is(b.Cloud.DeepSeek.Model)
            && a.Chat.SettingsPresentation.Is(b.Chat.SettingsPresentation)
            && a.Chat.ShowThinkingInHistory == b.Chat.ShowThinkingInHistory;
    }

    private static bool McpEquals(McpSettings? a, McpSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.ExternalServersJson.Is(b.ExternalServersJson)
            && a.AcpAutoInjectIdeMcp == b.AcpAutoInjectIdeMcp
            && a.ExternalServersJsonPath.Is(b.ExternalServersJsonPath);
    }

    private static bool AgentNotesEquals(AgentNotesSettings? a, AgentNotesSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.ConfigPath.Is(b.ConfigPath)
            && a.ConfigPathEnv.Is(b.ConfigPathEnv)
            && a.KbBaseOverlayPath.Is(b.KbBaseOverlayPath)
            && a.KbBaseOverlayPathEnv.Is(b.KbBaseOverlayPathEnv);
    }

    private static bool AgentEquals(AgentSettings? a, AgentSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        var ea = a.Environment;
        var eb = b.Environment;
        return ea.DefaultVerifyPolicy == eb.DefaultVerifyPolicy
            && ea.DefaultSandboxProfile == eb.DefaultSandboxProfile
            && ea.RunnerMaxConcurrency == eb.RunnerMaxConcurrency
            && ea.CoalesceWindowMs == eb.CoalesceWindowMs
            && ea.ShellEscapeTier == eb.ShellEscapeTier
            && ea.LongRunSandboxProfile == eb.LongRunSandboxProfile
            && ea.BuildVerifyHost == eb.BuildVerifyHost
            && ea.BuildVerifyWorkerAssemblyPath == eb.BuildVerifyWorkerAssemblyPath
            && ea.Ladder.DiagnoseFilesEnabled == eb.Ladder.DiagnoseFilesEnabled
            && ea.Ladder.TestFullRequireExplicit == eb.Ladder.TestFullRequireExplicit
            && ea.Ladder.DiagnoseFilesCsScope == eb.Ladder.DiagnoseFilesCsScope
            && ea.Ladder.DiagnoseFilesGitDirtyMaxFiles == eb.Ladder.DiagnoseFilesGitDirtyMaxFiles
            && ea.Ladder.DiagnoseFilesIncludeWarmupCs == eb.Ladder.DiagnoseFilesIncludeWarmupCs
            && ea.Ladder.DiagnoseFilesWarmupMaxFiles == eb.Ladder.DiagnoseFilesWarmupMaxFiles
            && ea.Ladder.TestScopedTouchedTestsOnly == eb.Ladder.TestScopedTouchedTestsOnly
            && ea.DevServices.RequireConfigOverride == eb.DevServices.RequireConfigOverride
            && ea.DevServices.GateTestScopedOnViolation == eb.DevServices.GateTestScopedOnViolation
            && ea.TimeAccounting.ShowInChat == eb.TimeAccounting.ShowInChat
            && ea.TimeAccounting.PfdInstrumentEnabled == eb.TimeAccounting.PfdInstrumentEnabled
            && ea.TimeAccounting.ShowTaskProgressInChat == eb.TimeAccounting.ShowTaskProgressInChat
            && ea.TimeAccounting.IdleUserThresholdMs == eb.TimeAccounting.IdleUserThresholdMs
            && HarnessEquals(a.Harness, b.Harness);
    }

    private static bool HarnessEquals(AgentHarnessSettings? a, AgentHarnessSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.LoadHotContextOnSessionStart == b.LoadHotContextOnSessionStart
            && a.LoadHotContextOnTopicFork == b.LoadHotContextOnTopicFork
            && a.HotContextActiveScope == b.HotContextActiveScope
            && a.CheckpointEnabled == b.CheckpointEnabled
            && a.CheckpointThresholdUserTurns == b.CheckpointThresholdUserTurns
            && a.CheckpointRepeatEveryUserTurns == b.CheckpointRepeatEveryUserTurns
            && a.AutoVerifyAfterCsWrite == b.AutoVerifyAfterCsWrite
            && a.SuppressAcpIdeStdioInject == b.SuppressAcpIdeStdioInject
            && a.CheckpointOnContextPressure == b.CheckpointOnContextPressure
            && a.ContextPressureThreadMessageThreshold == b.ContextPressureThreadMessageThreshold
            && a.ContextPressureRepeatEveryMessages == b.ContextPressureRepeatEveryMessages
            && a.InjectHarnessTelemetryInContext == b.InjectHarnessTelemetryInContext
            && a.InjectTopicForkBrief == b.InjectTopicForkBrief
            && a.TopicForkBriefTemplate == b.TopicForkBriefTemplate;
    }

    private static bool HybridIndexEquals(HybridIndexSettings? a, HybridIndexSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Enabled == b.Enabled
            && a.DebounceMs == b.DebounceMs
            && a.AutoReindexOnSolutionOpen == b.AutoReindexOnSolutionOpen
            && a.WatchFiles == b.WatchFiles
            && a.PauseWhenMcpStdioHost == b.PauseWhenMcpStdioHost
            && a.IndexDir.Is(b.IndexDir)
            && a.ScopeMode.Is(b.ScopeMode);
    }

    private static bool SolutionWarmupEquals(SolutionWarmupSettings? a, SolutionWarmupSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Enabled == b.Enabled
            && a.WarmActiveFileOnSolutionOpen == b.WarmActiveFileOnSolutionOpen
            && a.WarmFeedAnchorsAfterSymbolSidecar == b.WarmFeedAnchorsAfterSymbolSidecar
            && a.WarmOpenDocuments == b.WarmOpenDocuments
            && a.WarmRecentCsFiles == b.WarmRecentCsFiles
            && a.MaxParallelFileJobs == b.MaxParallelFileJobs
            && a.MaxOpenDocumentFiles == b.MaxOpenDocumentFiles
            && a.ShowBackgroundStatusOnPfd == b.ShowBackgroundStatusOnPfd;
    }

    private static bool CommandPaletteEquals(CommandPaletteSettings? a, CommandPaletteSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return string.Equals(a.GoToSearch.Backend, b.GoToSearch.Backend, StringComparison.Ordinal);
    }

    private static bool WorkspaceEquals(WorkspaceSettings? a, WorkspaceSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.PfdExpanded == b.PfdExpanded
            && a.ShowTerminal == b.ShowTerminal
            && a.ShowGit == b.ShowGit
            && a.ShowInstrumentation == b.ShowInstrumentation
            && a.Mode.Is(b.Mode)
            && a.Culture.Is(b.Culture)
            && a.SplittersLocked == b.SplittersLocked
            && a.PrimaryWorkSurface.Is(b.PrimaryWorkSurface)
            && a.SolutionExplorer.TrackActiveItem == b.SolutionExplorer.TrackActiveItem
            && a.SolutionExplorer.CompactTree == b.SolutionExplorer.CompactTree;
    }

    private static bool CodeNavigationMapEquals(CodeNavigationMapSettings? a, CodeNavigationMapSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.View.Is(b.View)
            && a.Depth.Is(b.Depth)
            && a.DetailLevel.Is(b.DetailLevel)
            && a.RelatedGraphLayout.Is(b.RelatedGraphLayout)
            && a.ControlFlowMainAxis.Is(b.ControlFlowMainAxis)
            && CodeNavigationMapControlFlowGrainKind.Normalize(a.ControlFlowGrain)
                .Is(CodeNavigationMapControlFlowGrainKind.Normalize(b.ControlFlowGrain))
            && a.NormalizedConditionBranchLabelPreset.Is(b.NormalizedConditionBranchLabelPreset)
            && ConditionBranchPresetListsEqual(a.ConditionBranch.Presets, b.ConditionBranch.Presets)
            && a.ConditionBranchPositive.Is(b.ConditionBranchPositive)
            && a.ConditionBranchNegative.Is(b.ConditionBranchNegative);
    }

    private static bool ConditionBranchPresetListsEqual(
        IReadOnlyList<CodeNavigationMapConditionBranchPresetEntry>? a,
        IReadOnlyList<CodeNavigationMapConditionBranchPresetEntry>? b)
    {
        a ??= [];
        b ??= [];
        if (a.Count != b.Count)
            return false;
        for (var i = 0; i < a.Count; i++)
        {
            if (!a[i].Id.Is(b[i].Id)
                || !(a[i].Positive ?? "").Is(b[i].Positive ?? "")
                || !(a[i].Negative ?? "").Is(b[i].Negative ?? ""))
                return false;
        }

        return true;
    }

    private static bool LanguagesEquals(LanguagesSettings? a, LanguagesSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return CSharpLanguageServerSettingsEquals(a.CSharp, b.CSharp)
            && MarkdownLanguageServerSettingsEquals(a.Markdown, b.Markdown);
    }

    private static bool CSharpLanguageServerSettingsEquals(CSharpLanguageServerSettings? a, CSharpLanguageServerSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Mode.Is(b.Mode)
            && LanguageServerLaunchProfileEquals(a.ParseOnly, b.ParseOnly)
            && LanguageServerLaunchProfileEquals(a.OmniSharp, b.OmniSharp)
            && LanguageServerLaunchProfileEquals(a.CSharpLs, b.CSharpLs)
            && LanguageServerLaunchProfileEquals(a.Custom, b.Custom);
    }

    private static bool LanguageServerLaunchProfileEquals(LanguageServerLaunchProfile? a, LanguageServerLaunchProfile? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Executable.Is(b.Executable)
            && a.ExecutableEnv.Is(b.ExecutableEnv)
            && a.Arguments.Is(b.Arguments)
            && a.ArgumentsEnv.Is(b.ArgumentsEnv);
    }

    private static bool MarkdownLanguageServerSettingsEquals(MarkdownLanguageServerSettings? a, MarkdownLanguageServerSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Mode.Is(b.Mode)
            && LanguageServerLaunchProfileEquals(a.Off, b.Off)
            && LanguageServerLaunchProfileEquals(a.Marksman, b.Marksman)
            && LanguageServerLaunchProfileEquals(a.Custom, b.Custom);
    }

    private static bool MarkdownEquals(MarkdownSettings? a, MarkdownSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return MarkdownDiagramsEquals(a.Diagrams, b.Diagrams);
    }

    private static bool MarkdownDiagramsEquals(MarkdownDiagramSettings? a, MarkdownDiagramSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Kroki == b.Kroki && a.KrokiUrl.Is(b.KrokiUrl);
    }

    private static bool DisplayEquals(DisplaySettings? a, DisplaySettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.MaximizeHostsOnDedicatedScreens == b.MaximizeHostsOnDedicatedScreens
            && a.PreferRepoInstruments == b.PreferRepoInstruments
            && a.Pfd.OpenOnStartup == b.Pfd.OpenOnStartup
            && a.Pfd.PixelX == b.Pfd.PixelX
            && a.Pfd.PixelY == b.Pfd.PixelY
            && Nullable.Equals(a.Pfd.Width, b.Pfd.Width)
            && Nullable.Equals(a.Pfd.Height, b.Pfd.Height)
            && a.Mfd.OpenOnStartup == b.Mfd.OpenOnStartup
            && a.Mfd.PixelX == b.Mfd.PixelX
            && a.Mfd.PixelY == b.Mfd.PixelY
            && Nullable.Equals(a.Mfd.Width, b.Mfd.Width)
            && Nullable.Equals(a.Mfd.Height, b.Mfd.Height)
            && a.Pm.OpenOnStartup == b.Pm.OpenOnStartup
            && a.Pm.PixelX == b.Pm.PixelX
            && a.Pm.PixelY == b.Pm.PixelY
            && Nullable.Equals(a.Pm.Width, b.Pm.Width)
            && Nullable.Equals(a.Pm.Height, b.Pm.Height)
            && a.Skia.ZoneGeometryOverlay == b.Skia.ZoneGeometryOverlay
            && a.Skia.InstrumentMount == b.Skia.InstrumentMount
            && a.Mount.DefaultStyle.Is(b.Mount.DefaultStyle)
            && a.Mount.EnforceEligibility == b.Mount.EnforceEligibility
            && a.Mount.MinSa.Equals(b.Mount.MinSa)
            && a.Mount.MinPerformance.Equals(b.Mount.MinPerformance)
            && a.Mount.MaxWorkload.Equals(b.Mount.MaxWorkload)
            && a.Mount.RequireScores == b.Mount.RequireScores
            && InstrumentMountPolicyRulesEqual(a.Mount.Rules, b.Mount.Rules)
            && StringDictionaryEqualOrdinalIgnoreCase(a.Instruments, b.Instruments)
            && DisplayScreensEquals(a.Screens, b.Screens)
            && DisplayPresentationEquals(a.Presentation, b.Presentation);
    }

    private static bool DisplayPresentationEquals(DisplayPresentationSettings? a, DisplayPresentationSettings? b)
    {
        if (a is null || b is null)
            return a == b;

        return a.Tier.Is(b.Tier)
            && a.CockpitMinTotalWidthPx == b.CockpitMinTotalWidthPx
            && a.CockpitMinAnchorWidthPx == b.CockpitMinAnchorWidthPx
            && a.CompactIntercomPlacement.Is(b.CompactIntercomPlacement)
            && a.UltrawideCockpitEnabled == b.UltrawideCockpitEnabled
            && a.TierFirstRunCompleted == b.TierFirstRunCompleted
            && a.CompactAuxiliaryPanelWidthPx == b.CompactAuxiliaryPanelWidthPx;
    }

    private static bool DisplayScreensEquals(DisplayScreensSettings? a, DisplayScreensSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        if (!a.Topology.Is(b.Topology))
            return false;
        return PresentationGrammarEquals(a.Grammar, b.Grammar);
    }

    private static bool PresentationGrammarEquals(PresentationGrammarSettings? a, PresentationGrammarSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Brackets.Is(b.Brackets)
            && a.BetweenScreens.Is(b.BetweenScreens)
            && a.BetweenZones.Is(b.BetweenZones)
            && a.Pfd.Is(b.Pfd)
            && a.Forward.Is(b.Forward)
            && a.Mfd.Is(b.Mfd);
    }

    private static bool InstrumentMountPolicyRulesEqual(
        IReadOnlyList<InstrumentMountPolicyRuleSettings>? x,
        IReadOnlyList<InstrumentMountPolicyRuleSettings>? y)
    {
        if (x is null && y is null)
            return true;
        if (x is null || y is null)
            return false;
        if (x.Count != y.Count)
            return false;

        static string Normalize(string? value) => (value ?? string.Empty).Trim();
        static string Key(InstrumentMountPolicyRuleSettings r) =>
            $"{Normalize(r.Surface)}|{Normalize(r.Slot)}|{Normalize(r.Instrument)}|{Normalize(r.Style)}|{r.SaScore}|{r.PerformanceScore}|{r.WorkloadScore}";

        var left = x.Select(Key).OrderBy(static s => s, StringComparer.Ordinal).ToList();
        var right = y.Select(Key).OrderBy(static s => s, StringComparer.Ordinal).ToList();
        for (var i = 0; i < left.Count; i++)
        {
            if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    private static bool StringDictionaryEqualOrdinalIgnoreCase(
        IReadOnlyDictionary<string, string>? a,
        IReadOnlyDictionary<string, string>? y)
    {
        if (a is null && y is null)
            return true;
        if (a is null || y is null)
            return false;

        var na = a
            .Where(static kv => !string.IsNullOrWhiteSpace(kv.Key))
            .ToDictionary(static kv => kv.Key.Trim(), static kv => kv.Value ?? "", StringComparer.OrdinalIgnoreCase);
        var ny = y
            .Where(static kv => !string.IsNullOrWhiteSpace(kv.Key))
            .ToDictionary(static kv => kv.Key.Trim(), static kv => kv.Value ?? "", StringComparer.OrdinalIgnoreCase);
        if (na.Count != ny.Count)
            return false;

        foreach (var kv in na)
        {
            if (!ny.TryGetValue(kv.Key, out var other))
                return false;
            if (!string.Equals(kv.Value, other, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static bool IntercomEquals(IntercomSettings? a, IntercomSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.FeedMetrics.Is(b.FeedMetrics)
            && a.TciValidationIcon.Is(b.TciValidationIcon)
            && a.Attachments.Code.Navigate.Is(b.Attachments.Code.Navigate)
            && a.Attachments.Code.RevealLoadSolution.Is(b.Attachments.Code.RevealLoadSolution)
            && a.Transport.Enabled == b.Transport.Enabled
            && a.Transport.BaseUrl.Is(b.Transport.BaseUrl)
            && a.Transport.BaseUrlEnv.Is(b.Transport.BaseUrlEnv)
            && a.Transport.LocalServerPath.Is(b.Transport.LocalServerPath)
            && a.Transport.LocalServerPathEnv.Is(b.Transport.LocalServerPathEnv)
            && a.Transport.TeamId.Is(b.Transport.TeamId)
            && a.Transport.DefaultTopicId.Is(b.Transport.DefaultTopicId)
            && a.Transport.OAuthProvider.Is(b.Transport.OAuthProvider)
            && a.Transport.InviteToken.Is(b.Transport.InviteToken)
            && a.Transport.DevTeamToken.Is(b.Transport.DevTeamToken)
            && WorkspaceHintsEqual(a.Transport.WorkspaceHints, b.Transport.WorkspaceHints)
            && a.Transport.SseReconnectBackoffMs == b.Transport.SseReconnectBackoffMs
            && a.Transport.AutoConnectOnSend == b.Transport.AutoConnectOnSend
            && a.Transport.SyncAgentChannelMessages == b.Transport.SyncAgentChannelMessages;
    }

    private static bool WorkspaceHintsEqual(
        Dictionary<string, IntercomWorkspaceHintEntry>? a,
        Dictionary<string, IntercomWorkspaceHintEntry>? b)
    {
        if (a is null || b is null)
            return a == b;
        if (a.Count != b.Count)
            return false;
        foreach (var (key, av) in a)
        {
            if (!b.TryGetValue(key, out var bv))
                return false;
            if (!av.TeamId.Is(bv.TeamId)
                || !av.ProjectId.Is(bv.ProjectId)
                || !av.UpdatedAtUtc.Is(bv.UpdatedAtUtc)
                || !av.Source.Is(bv.Source))
                return false;
        }

        return true;
    }

    private static bool CodeNavigationEquals(CodeNavigationSettings? a, CodeNavigationSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return CodeNavigationPresetListsEqual(a.Presets, b.Presets);
    }

    private static bool EditorEquals(EditorSettings? a, EditorSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return InlineHintsEquals(a.InlineHints, b.InlineHints)
            && DebugHintsEquals(a.DebugHints, b.DebugHints);
    }

    private static bool InlineHintsEquals(EditorInlineHintsSettings? a, EditorInlineHintsSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Enabled == b.Enabled
            && a.ParameterNames == b.ParameterNames
            && a.VariableTypes == b.VariableTypes;
    }

    private static bool DebugHintsEquals(EditorDebugHintsSettings? a, EditorDebugHintsSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Enabled == b.Enabled
            && a.ShowAssignments == b.ShowAssignments
            && a.ShowConditions == b.ShowConditions;
    }

    private static bool CodeNavigationPresetListsEqual(
        IReadOnlyList<CodeNavigationPresetEntry> a,
        IReadOnlyList<CodeNavigationPresetEntry> b)
    {
        var da = a.Where(p => !string.IsNullOrWhiteSpace(p.Id)).ToDictionary(x => x.Id.Trim(), StringComparer.OrdinalIgnoreCase);
        var db = b.Where(p => !string.IsNullOrWhiteSpace(p.Id)).ToDictionary(x => x.Id.Trim(), StringComparer.OrdinalIgnoreCase);
        if (da.Count != db.Count)
            return false;
        foreach (var kv in da)
        {
            if (!db.TryGetValue(kv.Key, out var other))
                return false;
            if (!CodeNavigationPresetEntryEquals(kv.Value, other))
                return false;
        }

        return true;
    }

    private static bool CodeNavigationPresetEntryEquals(CodeNavigationPresetEntry a, CodeNavigationPresetEntry b)
    {
        if (!string.Equals(a.Id?.Trim(), b.Id?.Trim(), StringComparison.Ordinal))
            return false;
        if (!StringListEqual(a.IncludeKinds, b.IncludeKinds))
            return false;
        if (!StringListEqual(a.ExcludeKinds, b.ExcludeKinds))
            return false;
        return true;
    }

    private static bool StringListEqual(IReadOnlyList<string>? x, IReadOnlyList<string>? y)
    {
        if (x is null && y is null)
            return true;
        if (x is null || y is null)
            return false;
        if (x.Count != y.Count)
            return false;
        var sa = x.OrderBy(s => s, StringComparer.Ordinal).ToList();
        var sb = y.OrderBy(s => s, StringComparer.Ordinal).ToList();
        for (var i = 0; i < sa.Count; i++)
        {
            if (!string.Equals(sa[i], sb[i], StringComparison.Ordinal))
                return false;
        }

        return true;
    }
}

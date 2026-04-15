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
            && WorkspaceUiEquals(WorkspaceUi, o.WorkspaceUi)
            && CSharpLspEquals(CSharpLsp, o.CSharpLsp)
            && MarkdownLspEquals(MarkdownLsp, o.MarkdownLsp)
            && MarkdownDiagramsEquals(MarkdownDiagrams, o.MarkdownDiagrams)
            && DisplayEquals(Display, o.Display)
            && PresentationGrammarEquals(o.PresentationGrammar)
            && WorkspaceNavigationContextEquals(WorkspaceNavigationContext, o.WorkspaceNavigationContext);
    }

    public override ModelBase Clone()
    {
        return new CascadeIdeSettings
        {
            Ai = new AiSettings
            {
                DefaultOllamaModel = Ai.DefaultOllamaModel,
                Provider = Ai.Provider,
                AnthropicModel = Ai.AnthropicModel,
                OpenAiBaseUrl = Ai.OpenAiBaseUrl,
                OpenAiModel = Ai.OpenAiModel,
                DeepSeekBaseUrl = Ai.DeepSeekBaseUrl,
                DeepSeekModel = Ai.DeepSeekModel,
                CursorAcpPath = Ai.CursorAcpPath,
                AiChatSettingsPresentation = Ai.AiChatSettingsPresentation,
                ChatMcpOnly = Ai.ChatMcpOnly,
            },
            Mcp = new McpSettings
            {
                ExternalServersJson = Mcp.ExternalServersJson,
                AcpAutoInjectIdeMcp = Mcp.AcpAutoInjectIdeMcp,
            },
            WorkspaceUi = new WorkspaceUiSettings
            {
                ShowSolutionExplorer = WorkspaceUi.ShowSolutionExplorer,
                ShowTerminal = WorkspaceUi.ShowTerminal,
                ShowGit = WorkspaceUi.ShowGit,
                ShowInstrumentation = WorkspaceUi.ShowInstrumentation,
                Mode = WorkspaceUi.Mode,
                Culture = WorkspaceUi.Culture,
            },
            CSharpLsp = new CSharpLspSettings
            {
                Provider = CSharpLsp.Provider,
                Executable = CSharpLsp.Executable,
                Arguments = CSharpLsp.Arguments,
            },
            MarkdownLsp = new MarkdownLspSettings
            {
                Provider = MarkdownLsp.Provider,
                Executable = MarkdownLsp.Executable,
                Arguments = MarkdownLsp.Arguments,
            },
            MarkdownDiagrams = new MarkdownDiagramSettings
            {
                KrokiEnabled = MarkdownDiagrams.KrokiEnabled,
                KrokiBaseUrl = MarkdownDiagrams.KrokiBaseUrl,
            },
            Display = new DisplaySettings
            {
                Presentation = Display.Presentation,
                ZoneScreenLayout = Display.ZoneScreenLayout,
                OpenMfdHostWindowOnStartup = Display.OpenMfdHostWindowOnStartup,
                MfdHostWindowPixelX = Display.MfdHostWindowPixelX,
                MfdHostWindowPixelY = Display.MfdHostWindowPixelY,
                MfdHostWindowWidth = Display.MfdHostWindowWidth,
                MfdHostWindowHeight = Display.MfdHostWindowHeight,
                UseSkiaZoneGeometryPreview = Display.UseSkiaZoneGeometryPreview,
                UseSkiaInstrumentWave3Preview = Display.UseSkiaInstrumentWave3Preview,
                InstrumentMountSlotPolicy = Display.InstrumentMountSlotPolicy,
                EnforceInstrumentMountPolicyEligibility = Display.EnforceInstrumentMountPolicyEligibility,
                InstrumentMountPolicyMinSaScore = Display.InstrumentMountPolicyMinSaScore,
                InstrumentMountPolicyMinPerformanceScore = Display.InstrumentMountPolicyMinPerformanceScore,
                InstrumentMountPolicyMaxWorkloadScore = Display.InstrumentMountPolicyMaxWorkloadScore,
                RequireInstrumentMountPolicyScores = Display.RequireInstrumentMountPolicyScores,
                InstrumentMountPolicyRules = Display.InstrumentMountPolicyRules
                    .Select(static r => new InstrumentMountPolicyRuleSettings
                    {
                        SurfaceId = r.SurfaceId,
                        SlotId = r.SlotId,
                        InstrumentId = r.InstrumentId,
                        SlotPolicy = r.SlotPolicy,
                        SaScore = r.SaScore,
                        PerformanceScore = r.PerformanceScore,
                        WorkloadScore = r.WorkloadScore,
                    })
                    .ToList(),
            },
            PresentationGrammar = new PresentationGrammarSettings
            {
                ScreenMarkers = PresentationGrammar.ScreenMarkers,
                ScreenSeparator = PresentationGrammar.ScreenSeparator,
                ZoneSeparator = PresentationGrammar.ZoneSeparator,
                PfdZoneIdentifier = PresentationGrammar.PfdZoneIdentifier,
                ForwardZoneIdentifier = PresentationGrammar.ForwardZoneIdentifier,
                MfdZoneIdentifier = PresentationGrammar.MfdZoneIdentifier,
            },
            WorkspaceNavigationContext = new WorkspaceNavigationContextSettings
            {
                Presets = WorkspaceNavigationContext.Presets
                    .Select(p => new WorkspaceNavigationPresetEntry
                    {
                        Id = p.Id,
                        IncludeKinds = p.IncludeKinds?.ToList(),
                        ExcludeKinds = p.ExcludeKinds?.ToList()
                    })
                    .ToList()
            },
        };
    }

    private static bool AiEquals(AiSettings? a, AiSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.DefaultOllamaModel.Is(b.DefaultOllamaModel)
            && a.Provider.Is(b.Provider)
            && a.AnthropicModel.Is(b.AnthropicModel)
            && a.OpenAiBaseUrl.Is(b.OpenAiBaseUrl)
            && a.OpenAiModel.Is(b.OpenAiModel)
            && a.DeepSeekBaseUrl.Is(b.DeepSeekBaseUrl)
            && a.DeepSeekModel.Is(b.DeepSeekModel)
            && a.CursorAcpPath.Is(b.CursorAcpPath)
            && a.AiChatSettingsPresentation.Is(b.AiChatSettingsPresentation)
            && a.ChatMcpOnly == b.ChatMcpOnly;
    }

    private static bool McpEquals(McpSettings? a, McpSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.ExternalServersJson.Is(b.ExternalServersJson)
            && a.AcpAutoInjectIdeMcp == b.AcpAutoInjectIdeMcp;
    }

    private static bool WorkspaceUiEquals(WorkspaceUiSettings? a, WorkspaceUiSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.ShowSolutionExplorer == b.ShowSolutionExplorer
            && a.ShowTerminal == b.ShowTerminal
            && a.ShowGit == b.ShowGit
            && a.ShowInstrumentation == b.ShowInstrumentation
            && a.Mode.Is(b.Mode)
            && a.Culture.Is(b.Culture);
    }

    private static bool CSharpLspEquals(CSharpLspSettings? a, CSharpLspSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Provider.Is(b.Provider) && a.Executable.Is(b.Executable) && a.Arguments.Is(b.Arguments);
    }

    private static bool MarkdownLspEquals(MarkdownLspSettings? a, MarkdownLspSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Provider.Is(b.Provider) && a.Executable.Is(b.Executable) && a.Arguments.Is(b.Arguments);
    }

    private static bool MarkdownDiagramsEquals(MarkdownDiagramSettings? a, MarkdownDiagramSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.KrokiEnabled == b.KrokiEnabled && a.KrokiBaseUrl.Is(b.KrokiBaseUrl);
    }

    private static bool DisplayEquals(DisplaySettings? a, DisplaySettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Presentation.Is(b.Presentation)
            && a.ZoneScreenLayout.Is(b.ZoneScreenLayout)
            && a.OpenMfdHostWindowOnStartup == b.OpenMfdHostWindowOnStartup
            && a.MfdHostWindowPixelX == b.MfdHostWindowPixelX
            && a.MfdHostWindowPixelY == b.MfdHostWindowPixelY
            && Nullable.Equals(a.MfdHostWindowWidth, b.MfdHostWindowWidth)
            && Nullable.Equals(a.MfdHostWindowHeight, b.MfdHostWindowHeight)
            && a.UseSkiaZoneGeometryPreview == b.UseSkiaZoneGeometryPreview
            && a.UseSkiaInstrumentWave3Preview == b.UseSkiaInstrumentWave3Preview
            && a.InstrumentMountSlotPolicy.Is(b.InstrumentMountSlotPolicy)
            && a.EnforceInstrumentMountPolicyEligibility == b.EnforceInstrumentMountPolicyEligibility
            && a.InstrumentMountPolicyMinSaScore.Equals(b.InstrumentMountPolicyMinSaScore)
            && a.InstrumentMountPolicyMinPerformanceScore.Equals(b.InstrumentMountPolicyMinPerformanceScore)
            && a.InstrumentMountPolicyMaxWorkloadScore.Equals(b.InstrumentMountPolicyMaxWorkloadScore)
            && a.RequireInstrumentMountPolicyScores == b.RequireInstrumentMountPolicyScores
            && InstrumentMountPolicyRulesEqual(a.InstrumentMountPolicyRules, b.InstrumentMountPolicyRules);
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
            $"{Normalize(r.SurfaceId)}|{Normalize(r.SlotId)}|{Normalize(r.InstrumentId)}|{Normalize(r.SlotPolicy)}|{r.SaScore}|{r.PerformanceScore}|{r.WorkloadScore}";

        var left = x.Select(Key).OrderBy(static s => s, StringComparer.Ordinal).ToList();
        var right = y.Select(Key).OrderBy(static s => s, StringComparer.Ordinal).ToList();
        for (var i = 0; i < left.Count; i++)
        {
            if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    private static bool WorkspaceNavigationContextEquals(WorkspaceNavigationContextSettings? a, WorkspaceNavigationContextSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return WorkspaceNavigationPresetListsEqual(a.Presets, b.Presets);
    }

    private static bool WorkspaceNavigationPresetListsEqual(
        IReadOnlyList<WorkspaceNavigationPresetEntry> a,
        IReadOnlyList<WorkspaceNavigationPresetEntry> b)
    {
        var da = a.Where(p => !string.IsNullOrWhiteSpace(p.Id)).ToDictionary(x => x.Id.Trim(), StringComparer.OrdinalIgnoreCase);
        var db = b.Where(p => !string.IsNullOrWhiteSpace(p.Id)).ToDictionary(x => x.Id.Trim(), StringComparer.OrdinalIgnoreCase);
        if (da.Count != db.Count)
            return false;
        foreach (var kv in da)
        {
            if (!db.TryGetValue(kv.Key, out var other))
                return false;
            if (!WorkspaceNavigationPresetEntryEquals(kv.Value, other))
                return false;
        }

        return true;
    }

    private static bool WorkspaceNavigationPresetEntryEquals(WorkspaceNavigationPresetEntry a, WorkspaceNavigationPresetEntry b)
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

    private bool PresentationGrammarEquals(PresentationGrammarSettings? o)
    {
        if (o is null)
            return false;
        var a = PresentationGrammar;
        return a.ScreenMarkers.Is(o.ScreenMarkers)
            && a.ScreenSeparator.Is(o.ScreenSeparator)
            && a.ZoneSeparator.Is(o.ZoneSeparator)
            && a.PfdZoneIdentifier.Is(o.PfdZoneIdentifier)
            && a.ForwardZoneIdentifier.Is(o.ForwardZoneIdentifier)
            && a.MfdZoneIdentifier.Is(o.MfdZoneIdentifier);
    }
}

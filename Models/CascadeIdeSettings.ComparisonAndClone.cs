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
            && WorkspaceEquals(Workspace, o.Workspace)
            && CodeNavigationMapEquals(CodeNavigationMap, o.CodeNavigationMap)
            && LanguagesEquals(Languages, o.Languages)
            && MarkdownEquals(Markdown, o.Markdown)
            && DisplayEquals(Display, o.Display)
            && EditorEquals(Editor, o.Editor)
            && CodeNavigationEquals(CodeNavigation, o.CodeNavigation);
    }

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
            Workspace = new WorkspaceSettings
            {
                PfdExpanded = Workspace.PfdExpanded,
                ShowTerminal = Workspace.ShowTerminal,
                ShowGit = Workspace.ShowGit,
                ShowInstrumentation = Workspace.ShowInstrumentation,
                Mode = Workspace.Mode,
                Culture = Workspace.Culture,
                SplittersLocked = Workspace.SplittersLocked,
            },
            CodeNavigationMap = new CodeNavigationMapSettings
            {
                View = CodeNavigationMap.View,
                Depth = CodeNavigationMap.Depth,
                DetailLevel = CodeNavigationMap.DetailLevel,
            },
            Languages = new LanguagesSettings
            {
                CSharp = new CSharpLanguageServerSettings
                {
                    Mode = Languages.CSharp.Mode,
                    ParseOnly = new LanguageServerLaunchProfile
                    {
                        Executable = Languages.CSharp.ParseOnly.Executable,
                        Arguments = Languages.CSharp.ParseOnly.Arguments,
                    },
                    OmniSharp = new LanguageServerLaunchProfile
                    {
                        Executable = Languages.CSharp.OmniSharp.Executable,
                        Arguments = Languages.CSharp.OmniSharp.Arguments,
                    },
                    CSharpLs = new LanguageServerLaunchProfile
                    {
                        Executable = Languages.CSharp.CSharpLs.Executable,
                        Arguments = Languages.CSharp.CSharpLs.Arguments,
                    },
                    Custom = new LanguageServerLaunchProfile
                    {
                        Executable = Languages.CSharp.Custom.Executable,
                        Arguments = Languages.CSharp.Custom.Arguments,
                    },
                },
                Markdown = new MarkdownLanguageServerSettings
                {
                    Mode = Languages.Markdown.Mode,
                    Off = new LanguageServerLaunchProfile
                    {
                        Executable = Languages.Markdown.Off.Executable,
                        Arguments = Languages.Markdown.Off.Arguments,
                    },
                    Marksman = new LanguageServerLaunchProfile
                    {
                        Executable = Languages.Markdown.Marksman.Executable,
                        Arguments = Languages.Markdown.Marksman.Arguments,
                    },
                    Custom = new LanguageServerLaunchProfile
                    {
                        Executable = Languages.Markdown.Custom.Executable,
                        Arguments = Languages.Markdown.Custom.Arguments,
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
            },
            Editor = new EditorSettings
            {
                InlineHints = new EditorInlineHintsSettings
                {
                    Enabled = Editor.InlineHints.Enabled,
                    ParameterNames = Editor.InlineHints.ParameterNames,
                    VariableTypes = Editor.InlineHints.VariableTypes,
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
        };
    }

    private static bool AiEquals(AiSettings? a, AiSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Mode.Is(b.Mode)
            && a.Local.Backend.Is(b.Local.Backend)
            && a.Local.Ollama.Model.Is(b.Local.Ollama.Model)
            && a.Acp.CursorAcpPath.Is(b.Acp.CursorAcpPath)
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
            && a.SplittersLocked == b.SplittersLocked;
    }

    private static bool CodeNavigationMapEquals(CodeNavigationMapSettings? a, CodeNavigationMapSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.View.Is(b.View) && a.Depth.Is(b.Depth) && a.DetailLevel.Is(b.DetailLevel);
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
        return a.Executable.Is(b.Executable) && a.Arguments.Is(b.Arguments);
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
            && DisplayScreensEquals(a.Screens, b.Screens);
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
        return InlineHintsEquals(a.InlineHints, b.InlineHints);
    }

    private static bool InlineHintsEquals(EditorInlineHintsSettings? a, EditorInlineHintsSettings? b)
    {
        if (a is null || b is null)
            return a == b;
        return a.Enabled == b.Enabled
            && a.ParameterNames == b.ParameterNames
            && a.VariableTypes == b.VariableTypes;
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

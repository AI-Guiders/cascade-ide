using System.Text.Json;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;
using Tomlyn;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Контракт TOML: вложенные секции (<c>[markdown.diagrams]</c>, <c>[display.screens.grammar]</c>) и snake_case.
/// </summary>
public sealed class CascadeIdeSettingsTomlDeserializeTests
{
    private static readonly TomlSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private static CascadeIdeSettings Deserialize(string text) =>
        TomlSerializer.Deserialize<CascadeIdeSettings>(text, Options)
        ?? throw new InvalidOperationException("Deserialize returned null");

    [Fact]
    public void Deserialize_FontsIntercomAndEditor_ParsesExpected()
    {
        const string text =
            """
            [fonts.intercom]
            prose_pt = 13
            prose_pt_forward = 12
            composer_pt = 15
            chrome_title_pt = 14
            chrome_subtitle_pt = 11
            chrome_heading_pt = 16
            card_title_pt = 15
            panel_title_pt = 16
            panel_input_pt = 14
            prose_family = "Segoe UI"
            mono_family = "Cascadia Mono,Consolas"
            chip_id_family = "Consolas"

            [fonts.editor]
            size_pt = 14
            family = "Consolas,Cascadia Code,monospace"
            """;

        var s = Deserialize(text);
        Assert.Equal(13, s.Fonts.Intercom.ProsePt);
        Assert.Equal(12, s.Fonts.Intercom.ProsePtForward);
        Assert.Equal(15, s.Fonts.Intercom.ComposerPt);
        Assert.Equal(14, s.Fonts.Intercom.ChromeTitlePt);
        Assert.Equal(15f, s.Fonts.Intercom.ResolveComposerPt(forwardHost: false));
        Assert.Equal(14f, s.Fonts.Intercom.ResolveChromeTitlePt());
        Assert.Equal(15f, s.Fonts.Intercom.ResolveCardTitleLineHeight(forwardHost: false));
        Assert.Equal(16f, s.Fonts.Intercom.ResolvePanelTitlePt());
        Assert.Equal(14f, s.Fonts.Intercom.ResolvePanelInputPt());
        Assert.Equal("Segoe UI", s.Fonts.Intercom.ProseFamily);
        Assert.Equal("Cascadia Mono,Consolas", s.Fonts.Intercom.MonoFamily);
        Assert.Equal("Consolas", s.Fonts.Intercom.ChipIdFamily);
        Assert.Equal(14, s.Fonts.Editor.SizePt);
        Assert.Equal("Consolas,Cascadia Code,monospace", s.Fonts.Editor.Family);
        Assert.Equal(12f, s.Fonts.Intercom.ResolveProsePt(forwardHost: true));
        Assert.Equal(13f, s.Fonts.Intercom.ResolveProsePt(forwardHost: false));
        Assert.Equal(14f, s.Fonts.Editor.ResolveSizePt());
        Assert.Equal("Segoe UI", s.Fonts.Intercom.ResolveProseFamily());
        Assert.Equal("Consolas,Cascadia Code,monospace", s.Fonts.Editor.ResolveFamily());
    }

    [Fact]
    public void Deserialize_LoggingIntercomSendTrace_ParsesExpected()
    {
        const string text =
            """
            [logging.intercom]
            send_trace = true
            """;

        var s = Deserialize(text);
        Assert.True(s.Logging.Intercom.SendTrace);
    }

    [Fact]
    public void Deserialize_DisplayScreensAndMarkdownNested_ParsesExpected()
    {
        const string text =
            """
            [display.screens]
            topology = "(P+F) (M)"

            [display.screens.grammar]
            pfd = "P"
            forward = "F"
            mfd = "M"

            [markdown.diagrams]
            kroki = true
            kroki_url = "https://kroki.io"
            """;

        var s = Deserialize(text);
        Assert.Equal("(P+F) (M)", s.Display.Screens.Topology);
        Assert.Equal("P", s.Display.Screens.Grammar.Pfd);
        Assert.True(s.Markdown.Diagrams.Kroki);
        Assert.Equal("https://kroki.io", s.Markdown.Diagrams.KrokiUrl);
    }

    [Fact]
    public void Serialize_RoundTrip_PreservesDisplayScreensTopology()
    {
        var original = new CascadeIdeSettings
        {
            Display = new DisplaySettings
            {
                Screens = new DisplayScreensSettings
                {
                    Topology = "(0.5PFD + 0.5Forward) (MFD)",
                },
            },
        };
        var toml = TomlSerializer.Serialize(original, Options);
        Assert.Contains("[display.screens]", toml, StringComparison.Ordinal);
        var roundtrip = Deserialize(toml);
        Assert.Equal("(0.5PFD + 0.5Forward) (MFD)", roundtrip.GetEffectivePresentationLine());
    }

    [Fact]
    public void Deserialize_DisplayScreensTopology_AndGrammar_UsedForEffective()
    {
        const string text =
            """
            [display.screens]
            topology = "(P)(F)(M)"

            [display.screens.grammar]
            pfd = "P"
            forward = "F"
            mfd = "M"
            """;

        var s = Deserialize(text);
        Assert.Equal("(P)(F)(M)", s.GetEffectivePresentationLine());
        var g = s.GetEffectivePresentationGrammar();
        Assert.Equal("P", g.Pfd);
        Assert.Equal("F", g.Forward);
        Assert.Equal("M", g.Mfd);
    }

    [Fact]
    public void Deserialize_CodeNavigationMapSection_ParsesViewAndDepth()
    {
        const string text =
            """
            [code_navigation_map]
            view = "both"
            depth = "controlFlow"
            detail_level = "glance"
            """;

        var s = Deserialize(text);
        Assert.True(s.CodeNavigationMap.WantsCodeNavigationMapList);
        Assert.True(s.CodeNavigationMap.WantsCodeNavigationMapGraph);
        Assert.True(s.CodeNavigationMap.IsControlFlowDepth);
        Assert.Equal("both", CodeNavigationMapSettings.NormalizeView(s.CodeNavigationMap.View));
        Assert.Equal(CodeNavigationMapLevelKind.ControlFlow, CodeNavigationMapSettings.NormalizeDepth(s.CodeNavigationMap.Depth));
        Assert.Equal(CodeNavigationMapDetailLevel.Glance, s.CodeNavigationMap.NormalizedDetailLevel);
    }

    [Fact]
    public void Deserialize_DisplayScreensTopology_WithoutGrammar_ParsesAsTwoScreens()
    {
        const string text =
            """
            [display.screens]
            topology = "(P+F) (M)"
            """;

        var s = Deserialize(text);
        Assert.Equal("(P+F) (M)", s.GetEffectivePresentationLine());
        var grammar = PresentationGrammarTokens.FromSettings(
            s.Display.Screens.Grammar.Brackets,
            s.Display.Screens.Grammar.BetweenScreens,
            s.Display.Screens.Grammar.BetweenZones,
            s.Display.Screens.Grammar.Pfd,
            s.Display.Screens.Grammar.Forward,
            s.Display.Screens.Grammar.Mfd);
        var parse = PresentationParser.Parse(s.GetEffectivePresentationLine(), grammar);
        Assert.True(parse.IsSuccess);
        Assert.Equal(2, parse.Screens.Count);
        Assert.True(PresentationLayoutAnalyzer.IsDedicatedMfdSecondScreenPreset(parse.Screens));
    }

    [Fact]
    public void Deserialize_AiNested_Adr0083_ParsesExpected()
    {
        const string text =
            """
            [ai]
            mode = "local"

            [ai.local]
            backend = "ollama"

            [ai.local.ollama]
            model = "qwen2.5-coder:7b"

            [ai.chat]
            settings_presentation = "mfd"
            show_thinking_in_history = true
            """;

        var s = Deserialize(text);
        Assert.Equal("local", s.Ai.Mode);
        Assert.Equal("ollama", s.Ai.Local.Backend);
        Assert.Equal("qwen2.5-coder:7b", s.Ai.Local.Ollama.Model);
        Assert.Equal("mfd", s.Ai.Chat.SettingsPresentation);
        Assert.True(s.Ai.Chat.ShowThinkingInHistory);
    }

    [Fact]
    public void Deserialize_LanguagesCSharpNestedMode_ParsesExpected()
    {
        const string text =
            """
            [languages.csharp]
            mode = "OmniSharp"

            [languages.csharp.omni_sharp]
            executable = "C:\\omnisharp-win-x64-net6.0\\OmniSharp.exe"
            arguments = "--languageserver --hostPID 1234"
            """;

        var s = Deserialize(text);
        var runtime = s.Languages.CSharp.ResolveForRuntime();
        Assert.Equal("OmniSharp", runtime.Mode);
        Assert.Equal("C:\\omnisharp-win-x64-net6.0\\OmniSharp.exe", runtime.Executable);
        Assert.Equal("--languageserver --hostPID 1234", runtime.Arguments);
    }

    [Fact]
    public void Deserialize_LanguagesMarkdownNestedMode_ParsesExpected()
    {
        const string text =
            """
            [languages.markdown]
            mode = "Marksman"

            [languages.markdown.marksman]
            executable = "C:\\tools\\marksman.exe"
            arguments = "--stdio"
            """;

        var s = Deserialize(text);
        var runtime = s.Languages.Markdown.ResolveForRuntime();
        Assert.Equal("Marksman", runtime.Mode);
        Assert.Equal("C:\\tools\\marksman.exe", runtime.Executable);
        Assert.Equal("--stdio", runtime.Arguments);
    }

    [Fact]
    public void Deserialize_AgentNotes_ConfigPath_ParsesExpected()
    {
        const string toml = """
            [agent_notes]
            config_path = "D:/agent-notes-mcp/agent-notes-mcp.toml"
            """;
        var s = CascadeTomlSerializer.Deserialize<CascadeIdeSettings>(toml)!;
        Assert.Equal("D:/agent-notes-mcp/agent-notes-mcp.toml", s.AgentNotes.ConfigPath);
    }

    [Fact]
    public void Deserialize_AgentNotes_KbBaseOverlayPath_ParsesExpected()
    {
        const string text =
            """
            [agent_notes]
            kb_base_overlay_path = "D:\\vault\\agent-notes"
            """;

        var s = Deserialize(text);
        Assert.Equal("D:\\vault\\agent-notes", s.AgentNotes.KbBaseOverlayPath);
    }

    [Fact]
    public void Deserialize_IntercomAttachmentsCodeNavigate_ParsesExpected()
    {
        const string text =
            """
            [intercom.attachments.code]
            navigate = "select"
            """;

        var s = Deserialize(text);
        Assert.True(s.Intercom.Attachments.Code.DefaultNavigateSelects());
        Assert.Equal("select", s.Intercom.Attachments.Code.Navigate);
    }

    [Fact]
    public void Deserialize_IntercomAttachmentsCodeRevealLoadSolution_ParsesExpected()
    {
        const string text =
            """
            [intercom.attachments.code]
            reveal_load_solution = "never"
            """;

        var s = Deserialize(text);
        Assert.False(s.Intercom.Attachments.Code.ShouldLoadSolutionBeforeReveal());
        Assert.Equal("never", s.Intercom.Attachments.Code.RevealLoadSolution);
    }

    [Fact]
    public void Deserialize_IntercomFeedMetrics_ParsesCompact()
    {
        const string text =
            """
            [intercom]
            feed_metrics = "compact"
            """;

        var s = Deserialize(text);
        Assert.Equal(IntercomFeedMetricsModes.Compact, s.Intercom.FeedMetrics);
        Assert.False(s.Intercom.UseComfortableFeedMetrics());
    }

    [Fact]
    public void Deserialize_IntercomAttachmentsCode_DefaultsWhenSectionMissing()
    {
        const string text =
            """
            [hybrid_index]
            enabled = false
            """;

        var s = Deserialize(text);
        Assert.False(s.Intercom.Attachments.Code.DefaultNavigateSelects());
        Assert.Equal("reveal", s.Intercom.Attachments.Code.Navigate);
        Assert.True(s.Intercom.Attachments.Code.ShouldLoadSolutionBeforeReveal());
    }

    [Fact]
    public void Deserialize_HybridIndexSection_ParsesExpected()
    {
        const string text =
            """
            [hybrid_index]
            enabled = true
            index_dir = ".hci"
            debounce_ms = 1234
            auto_reindex_on_solution_open = false
            watch_files = false
            scope_mode = "workspace"
            pause_when_mcp_stdio_host = true
            """;

        var s = Deserialize(text);
        Assert.True(s.HybridIndex.Enabled);
        Assert.Equal(".hci", s.HybridIndex.IndexDir);
        Assert.Equal(1234, s.HybridIndex.DebounceMs);
        Assert.False(s.HybridIndex.AutoReindexOnSolutionOpen);
        Assert.False(s.HybridIndex.WatchFiles);
        Assert.Equal("workspace", s.HybridIndex.ScopeMode);
        Assert.True(s.HybridIndex.PauseWhenMcpStdioHost);
    }

    [Fact]
    public void CascadeIdeSettings_Clone_CopiesHybridIndex()
    {
        var s = new CascadeIdeSettings
        {
            HybridIndex = new HybridIndexSettings
            {
                Enabled = false,
                IndexDir = ".hci2",
                DebounceMs = 999,
                AutoReindexOnSolutionOpen = false,
                WatchFiles = false,
                ScopeMode = "workspace",
                PauseWhenMcpStdioHost = true,
            },
        };
        var c = (CascadeIdeSettings)s.Clone();
        Assert.False(c.HybridIndex.Enabled);
        Assert.Equal(".hci2", c.HybridIndex.IndexDir);
        Assert.Equal(999, c.HybridIndex.DebounceMs);
        Assert.False(c.HybridIndex.AutoReindexOnSolutionOpen);
        Assert.False(c.HybridIndex.WatchFiles);
        Assert.Equal("workspace", c.HybridIndex.ScopeMode);
        Assert.True(c.HybridIndex.PauseWhenMcpStdioHost);
    }

    [Fact]
    public void CascadeIdeSettings_Is_ComparesHybridIndex()
    {
        var a = new CascadeIdeSettings();
        var b = new CascadeIdeSettings();
        Assert.True(a.Is(b));

        b.HybridIndex.DebounceMs = 123;
        Assert.False(a.Is(b));
    }

    [Fact]
    public void Deserialize_CommandPaletteGoToSearch_ParsesBackend()
    {
        const string text =
            """
            [command_palette.go_to_search]
            backend = "hci"
            """;

        var s = Deserialize(text);
        Assert.Equal("hci", s.CommandPalette.GoToSearch.Backend);
        Assert.Equal(CommandPaletteGoToSearchBackendKind.Hci, CommandPaletteGoToSearchBackendNormalizer.Parse(s.CommandPalette.GoToSearch.Backend));
    }

    [Fact]
    public void NormalizeHybridIndexScopeMode_DefaultsExpected()
    {
        Assert.Equal("workspace", ShellSettingsPresentationProjection.NormalizeHybridIndexScopeMode("workspace"));
        Assert.Equal("workspace+solution", ShellSettingsPresentationProjection.NormalizeHybridIndexScopeMode(""));
        Assert.Equal("workspace+solution", ShellSettingsPresentationProjection.NormalizeHybridIndexScopeMode("garbage"));
    }

}

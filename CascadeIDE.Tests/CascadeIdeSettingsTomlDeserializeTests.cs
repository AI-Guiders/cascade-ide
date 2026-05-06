using System.Text.Json;
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

}

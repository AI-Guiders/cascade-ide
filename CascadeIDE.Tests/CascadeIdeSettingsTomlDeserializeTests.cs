using System.Text.Json;
using CascadeIDE.Models;
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
    public void Deserialize_SemanticMapSection_ParsesViewAndDepth()
    {
        const string text =
            """
            [semantic_map]
            view = "both"
            depth = "controlFlow"
            detail_level = "glance"
            """;

        var s = Deserialize(text);
        Assert.True(s.SemanticMap.WantsSemanticMapList);
        Assert.True(s.SemanticMap.WantsSemanticMapGraph);
        Assert.True(s.SemanticMap.IsControlFlowDepth);
        Assert.Equal("both", SemanticMapSettings.NormalizeView(s.SemanticMap.View));
        Assert.Equal(SemanticMapLevelKind.ControlFlow, SemanticMapSettings.NormalizeDepth(s.SemanticMap.Depth));
        Assert.Equal(SemanticMapDetailLevel.Glance, s.SemanticMap.NormalizedDetailLevel);
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
}

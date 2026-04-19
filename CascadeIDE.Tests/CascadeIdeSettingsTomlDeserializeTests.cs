using System.Text.Json;
using CascadeIDE.Models;
using Tomlyn;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Контракт TOML: вложенные секции (<c>[markdown.diagrams]</c>, <c>[presentation.grammar]</c>) и snake_case.
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
    public void Deserialize_PresentationAndMarkdownNested_ParsesExpected()
    {
        const string text =
            """
            [presentation]
            line = "(P+F) (M)"

            [presentation.grammar]
            pfd = "P"
            forward = "F"
            mfd = "M"

            [markdown.diagrams]
            kroki = true
            kroki_url = "https://kroki.io"
            """;

        var s = Deserialize(text);
        Assert.Equal("(P+F) (M)", s.Presentation.Line);
        Assert.Equal("P", s.Presentation.Grammar.Pfd);
        Assert.True(s.Markdown.Diagrams.Kroki);
        Assert.Equal("https://kroki.io", s.Markdown.Diagrams.KrokiUrl);
    }

    [Fact]
    public void Serialize_RoundTrip_PreservesPresentationLine()
    {
        var original = new CascadeIdeSettings
        {
            Presentation = new PresentationLayoutSettings { Line = "(0.5PFD + 0.5Forward) (MFD)" },
        };
        var toml = TomlSerializer.Serialize(original, Options);
        Assert.Contains("[presentation]", toml, StringComparison.Ordinal);
        var roundtrip = Deserialize(toml);
        Assert.Equal("(0.5PFD + 0.5Forward) (MFD)", roundtrip.GetEffectivePresentationLine());
    }

    [Fact]
    public void Deserialize_DisplayScreensTopology_TakesPriorityOverPresentationLine()
    {
        const string text =
            """
            [presentation]
            line = "(legacy)"

            [display.screens]
            topology = "(P)(F)(M)"

            [display.screens.grammar]
            pfd = "P"
            forward = "F"
            mfd = "M"
            """;

        var s = Deserialize(text);
        Assert.Equal("(P)(F)(M)", s.GetEffectivePresentationLine());
        Assert.Equal("(legacy)", s.Presentation.Line);
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
}

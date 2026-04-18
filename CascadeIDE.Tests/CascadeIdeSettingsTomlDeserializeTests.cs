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
}

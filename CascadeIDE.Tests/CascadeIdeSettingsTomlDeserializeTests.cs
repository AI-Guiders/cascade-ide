using System.Text.Json;
using CascadeIDE.Models;
using Tomlyn;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Контракт TOML: ключи между <c>[markdown_diagrams]</c> и следующей секцией относятся к <c>markdown_diagrams</c>,
/// а не к корню <see cref="CascadeIdeSettings"/> — иначе <c>presentation</c> не попадает в модель.
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

    private const string SampleLikeUserFile =
        """
        [markdown_diagrams]
        kroki_enabled = true
        kroki_base_url = "https://kroki.io"

        presentation = "(0.25P + 0.75F) (M)"
        zone_screen_layout = ""

        [presentation_grammar]
        pfd_zone_identifier = "P"
        forward_zone_identifier = "F"
        mfd_zone_identifier = "M"
        """;

    private const string PresentationAtRoot =
        """
        presentation = "(0.25P + 0.75F) (M)"
        zone_screen_layout = ""

        [markdown_diagrams]
        kroki_enabled = true
        kroki_base_url = "https://kroki.io"

        [presentation_grammar]
        pfd_zone_identifier = "P"
        forward_zone_identifier = "F"
        mfd_zone_identifier = "M"
        """;

    private const string DisplaySection =
        """
        [display]
        presentation = "(0.25P + 0.75F) (M)"
        zone_screen_layout = ""

        [markdown_diagrams]
        kroki_enabled = true
        kroki_base_url = "https://kroki.io"

        [presentation_grammar]
        pfd_zone_identifier = "P"
        forward_zone_identifier = "F"
        mfd_zone_identifier = "M"
        """;

    [Fact]
    public void Deserialize_WhenPresentationNestedUnderMarkdownDiagrams_PresentationIsEmpty()
    {
        var s = Deserialize(SampleLikeUserFile);
        Assert.Equal("", s.Presentation);
        Assert.Equal("P", s.PresentationGrammar.PfdZoneIdentifier);
    }

    [Fact]
    public void Deserialize_WhenPresentationOnlyAtRoot_IgnoredMustUseDisplaySection()
    {
        var s = Deserialize(PresentationAtRoot);
        Assert.Equal("", s.Presentation);
        Assert.Equal("", s.Display.Presentation);
        Assert.Equal("P", s.PresentationGrammar.PfdZoneIdentifier);
    }

    [Fact]
    public void Deserialize_WhenDisplaySection_PresentationIsSet()
    {
        var s = Deserialize(DisplaySection);
        Assert.Equal("(0.25P + 0.75F) (M)", s.Display.Presentation);
        Assert.Equal("(0.25P + 0.75F) (M)", s.Presentation);
        Assert.Equal("P", s.PresentationGrammar.PfdZoneIdentifier);
    }

    [Fact]
    public void Serialize_RoundTrip_PrefersDisplaySection()
    {
        var original = new CascadeIdeSettings
        {
            Display = new DisplaySettings { Presentation = "(0.5PFD + 0.5Forward) (MFD)" }
        };
        var toml = TomlSerializer.Serialize(original, Options);
        Assert.Contains("[display]", toml, StringComparison.Ordinal);
        var roundtrip = Deserialize(toml);
        Assert.Equal("(0.5PFD + 0.5Forward) (MFD)", roundtrip.GetEffectivePresentationLine());
    }
}

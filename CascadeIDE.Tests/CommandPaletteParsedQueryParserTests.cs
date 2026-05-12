using CascadeIDE.Features.Search.Application;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>Порядок разбора: <c>c:</c> → go-to → каталог (ADR 0112).</summary>
public sealed class CommandPaletteParsedQueryParserTests
{
    [Theory]
    [InlineData("c:gs", "gs")]
    [InlineData("  c:br  ", "br")]
    public void Parse_After_c_colon_IsMelody(string raw, string expectedTail)
    {
        var q = CommandPaletteParsedQueryParser.Parse(raw);
        var m = Assert.IsType<CommandPaletteParsedQuery.Melody>(q);
        Assert.Equal(expectedTail, m.TailNormalized);
    }

    [Fact]
    public void Parse_f_colon_IsGoTo()
    {
        var q = CommandPaletteParsedQueryParser.Parse(" f: Foo ");
        var g = Assert.IsType<CommandPaletteParsedQuery.GoTo>(q);
        Assert.Equal('f', g.Query.Prefix);
        Assert.Equal("Foo ", g.Query.Term);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("")]
    [InlineData("  ")]
    public void Parse_WithoutRecognizedPrefix_IsCatalog(string raw)
    {
        var q = CommandPaletteParsedQueryParser.Parse(raw);
        var c = Assert.IsType<CommandPaletteParsedQuery.Catalog>(q);
        Assert.Equal(raw.Trim(), c.TrimmedRaw);
    }

    [Fact]
    public void Parse_f_colon_WithNoTerm_IsGoTo_emptyTerm()
    {
        var q = CommandPaletteParsedQueryParser.Parse("f:");
        var g = Assert.IsType<CommandPaletteParsedQuery.GoTo>(q);
        Assert.Equal('f', g.Query.Prefix);
        Assert.Equal("", g.Query.Term);
    }

    [Theory]
    [InlineData('t', "Newtonsoft")]
    [InlineData('m', "Dispose")]
    [InlineData('x', "TODO")]
    public void Parse_go_toPrefixes_PrefixAndTerm(char expectedPrefix, string term)
    {
        var raw = $"{expectedPrefix}:{term}";
        var q = CommandPaletteParsedQueryParser.Parse(raw);
        var g = Assert.IsType<CommandPaletteParsedQuery.GoTo>(q);
        Assert.Equal(expectedPrefix, g.Query.Prefix);
        Assert.Equal(term, g.Query.Term);
    }

    /// <summary><c>c:</c> только в начале строки после trim — иначе каталог.</summary>
    [Fact]
    public void Parse_c_insidePlainText_IsCatalog()
    {
        var q = CommandPaletteParsedQueryParser.Parse("search c:gits");
        var c = Assert.IsType<CommandPaletteParsedQuery.Catalog>(q);
        Assert.Equal("search c:gits", c.TrimmedRaw);
    }
}

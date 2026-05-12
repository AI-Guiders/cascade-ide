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
}

using System.Text.Json;
using CascadeIDE.Services.Lsp;
using Xunit;

namespace CascadeIDE.Tests;

public class LspHoverContentFormatterTests
{
    [Fact]
    public void Format_String_ReturnsLiteral()
    {
        using var doc = JsonDocument.Parse("\"hello\"");
        var s = LspHoverContentFormatter.Format(doc.RootElement);
        Assert.Equal("hello", s);
    }

    [Fact]
    public void Format_MarkupContent_ReturnsValue()
    {
        using var doc = JsonDocument.Parse("""{"kind":"markdown","value":"Line1"}""");
        var s = LspHoverContentFormatter.Format(doc.RootElement);
        Assert.Equal("Line1", s);
    }

    [Fact]
    public void Format_Array_JoinsMarkedStrings()
    {
        using var doc = JsonDocument.Parse("""[{"language":"csharp","value":"a"},{"value":"b"}]""");
        var s = LspHoverContentFormatter.Format(doc.RootElement);
        Assert.Contains("a", s, StringComparison.Ordinal);
        Assert.Contains("b", s, StringComparison.Ordinal);
    }
}

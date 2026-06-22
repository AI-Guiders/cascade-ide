using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class EditorForwardHostKindParserTests
{
    [Theory]
    [InlineData("monaco_webview2", EditorForwardHostKind.MonacoWebView2)]
    [InlineData("avalonia_edit", EditorForwardHostKind.AvaloniaEdit)]
    [InlineData(null, EditorForwardHostKind.AvaloniaEdit)]
    [InlineData("unknown", EditorForwardHostKind.AvaloniaEdit)]
    public void Parse_maps_toml_values(string? raw, EditorForwardHostKind expected) =>
        Assert.Equal(expected, EditorForwardHostKindParser.Parse(raw));

    [Fact]
    public void ToToml_roundtrip_monaco() =>
        Assert.Equal("monaco_webview2", EditorForwardHostKindParser.ToToml(EditorForwardHostKind.MonacoWebView2));
}

using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class EditorForwardHostKindParserTests
{
    [Theory]
    [InlineData("monaco_webview2", EditorForwardHostKind.MonacoWebView2)]
    [InlineData("avalonia_edit", EditorForwardHostKind.MonacoWebView2)]
    [InlineData(null, EditorForwardHostKind.MonacoWebView2)]
    [InlineData("", EditorForwardHostKind.MonacoWebView2)]
    [InlineData("unknown", EditorForwardHostKind.MonacoWebView2)]
    public void Parse_maps_toml_values(string? raw, EditorForwardHostKind expected) =>
        Assert.Equal(expected, EditorForwardHostKindParser.Parse(raw));

    [Fact]
    public void ToToml_always_monaco() =>
        Assert.Equal("monaco_webview2", EditorForwardHostKindParser.ToToml(EditorForwardHostKind.MonacoWebView2));

    [Fact]
    public void IsDeprecatedValue_detects_avalonia_edit() =>
        Assert.True(EditorForwardHostKindParser.IsDeprecatedValue("avalonia_edit"));
}

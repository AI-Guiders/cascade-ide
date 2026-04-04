using System.Text.Json;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class McpCommandJsonArgsTests
{
    [Fact]
    public void String_NullArgs_ReturnsNull()
    {
        Assert.Null(McpCommandJsonArgs.String(null, "k"));
    }

    [Fact]
    public void String_MissingKey_ReturnsNull()
    {
        var args = Args(("other", JsonSerializer.SerializeToElement("x")));
        Assert.Null(McpCommandJsonArgs.String(args, "k"));
    }

    [Fact]
    public void String_Present_ReturnsValue()
    {
        var args = Args(("k", JsonSerializer.SerializeToElement("hello")));
        Assert.Equal("hello", McpCommandJsonArgs.String(args, "k"));
    }

    [Fact]
    public void Int_MissingOrInvalid_UsesDefault()
    {
        Assert.Equal(0, McpCommandJsonArgs.Int(null, "n"));
        var args = Args(("n", JsonSerializer.SerializeToElement("not int")));
        Assert.Equal(7, McpCommandJsonArgs.Int(args, "n", 7));
    }

    [Fact]
    public void Int_Present_ReturnsValue()
    {
        var args = Args(("n", JsonSerializer.SerializeToElement(42)));
        Assert.Equal(42, McpCommandJsonArgs.Int(args, "n", 0));
    }

    [Fact]
    public void Bool_NonBoolean_UsesDefault()
    {
        var args = Args(("b", JsonSerializer.SerializeToElement("x")));
        Assert.False(McpCommandJsonArgs.Bool(args, "b", false));
        Assert.True(McpCommandJsonArgs.Bool(args, "b", true));
    }

    [Fact]
    public void Bool_Present_ReturnsValue()
    {
        var t = Args(("b", JsonSerializer.SerializeToElement(true)));
        var f = Args(("b", JsonSerializer.SerializeToElement(false)));
        Assert.True(McpCommandJsonArgs.Bool(t, "b"));
        Assert.False(McpCommandJsonArgs.Bool(f, "b"));
    }

    [Fact]
    public void StringList_NullOrNotArray_ReturnsNull()
    {
        Assert.Null(McpCommandJsonArgs.StringList(null, "a"));
        var args = Args(("a", JsonSerializer.SerializeToElement("s")));
        Assert.Null(McpCommandJsonArgs.StringList(args, "a"));
    }

    [Fact]
    public void StringList_FiltersWhitespaceAndEmpty()
    {
        var args = Args(("a", JsonSerializer.SerializeToElement(new[] { "a", "", "  ", "b" })));
        var list = McpCommandJsonArgs.StringList(args, "a");
        Assert.NotNull(list);
        Assert.Equal(new[] { "a", "b" }, list);
    }

    private static Dictionary<string, JsonElement> Args(params (string key, JsonElement el)[] pairs)
    {
        var d = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var (key, el) in pairs)
            d[key] = el;
        return d;
    }
}

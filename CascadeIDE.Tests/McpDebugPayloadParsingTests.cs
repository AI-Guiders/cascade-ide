using System.Text.Json;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class McpDebugPayloadParsingTests
{
    [Fact]
    public void TryParseBreakpoints_NullArgs_FailsWithCanonicalMessage()
    {
        Assert.False(McpDebugPayloadParsing.TryParseBreakpoints(null, out var list, out var err));
        Assert.Empty(list);
        Assert.Equal(McpDebugPayloadParsing.MissingBreakpointsMessage, err);
    }

    [Fact]
    public void TryParseBreakpoints_ValidArray_ParsesPathsAndLines()
    {
        var args = new Dictionary<string, JsonElement>
        {
            ["breakpoints"] = JsonSerializer.SerializeToElement(new[]
            {
                new { file_path = "C:\\a\\x.cs", line = 10 },
                new { file_path = "", line = 1 },
                new { file_path = "y.cs", line = 2 }
            })
        };

        Assert.True(McpDebugPayloadParsing.TryParseBreakpoints(args, out var list, out var err));
        Assert.Empty(err);
        Assert.Equal(2, list.Count);
        Assert.Equal(("C:\\a\\x.cs", 10), list[0]);
        Assert.Equal(("y.cs", 2), list[1]);
    }

    [Fact]
    public void ParseDebugState_NullArgs_EmptyLists()
    {
        McpDebugPayloadParsing.ParseDebugState(null, out var sf, out var v);
        Assert.Empty(sf);
        Assert.Empty(v);
    }

    [Fact]
    public void ParseDebugState_FullPayload_ParsesStacksAndVariables()
    {
        var args = new Dictionary<string, JsonElement>
        {
            ["stack_frames"] = JsonSerializer.SerializeToElement(new[]
            {
                new { name = "Main", file = "Program.cs", line = 5 }
            }),
            ["variables"] = JsonSerializer.SerializeToElement(new[]
            {
                new { name = "x", value = "1" }
            })
        };

        McpDebugPayloadParsing.ParseDebugState(args, out var sf, out var v);
        Assert.Single(sf);
        Assert.Equal("Main", sf[0].Name);
        Assert.Equal("Program.cs", sf[0].File);
        Assert.Equal(5, sf[0].Line);
        Assert.Single(v);
        Assert.Equal("x", v[0].Name);
        Assert.Equal("1", v[0].Value);
    }
}

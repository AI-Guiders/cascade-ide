using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SemanticMapSettingsTests
{
    [Theory]
    [InlineData("file", SemanticMapLevelKind.File)]
    [InlineData("FILE", SemanticMapLevelKind.File)]
    [InlineData("controlFlow", SemanticMapLevelKind.ControlFlow)]
    [InlineData("CONTROLFLOW", SemanticMapLevelKind.ControlFlow)]
    [InlineData("unknown", SemanticMapLevelKind.File)]
    public void NormalizeLevel_ReturnsKnownValue(string input, string expected)
    {
        var actual = SemanticMapSettings.NormalizeLevel(input);
        Assert.Equal(expected, actual);
    }
}

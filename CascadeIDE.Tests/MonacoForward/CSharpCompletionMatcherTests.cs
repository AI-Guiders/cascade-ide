using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests.MonacoForward;

[Trait("Category", "MonacoForward")]
public sealed class CSharpCompletionMatcherTests
{
    [Theory]
    [InlineData("StringBuilder", "SB", true)]
    [InlineData("SByte", "SB", true)]
    [InlineData("StringBuilder", "Stri", true)]
    [InlineData("System", "Sy", true)]
    [InlineData("StringBuilder", "XYZ", false)]
    [InlineData("Append", "SB", false)]
    public void Matches_prefix_and_acronym(string name, string prefix, bool expected) =>
        Assert.Equal(expected, CSharpCompletionMatcher.Matches(name, prefix));

    [Fact]
    public void CompareByRelevance_prefix_before_acronym()
    {
        var prefix = "SB";
        Assert.True(CSharpCompletionMatcher.CompareByRelevance("SByte", "StringBuilder", prefix) < 0);
    }
}

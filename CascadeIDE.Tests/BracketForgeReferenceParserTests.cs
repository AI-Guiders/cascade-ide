using CascadeIDE.Models.Forge;
using CascadeIDE.Services.Forge;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class BracketForgeReferenceParserTests
{
    [Theory]
    [InlineData("[FRG:pilot/issues/7]", "pilot", ForgeArtifactKind.Issue, 7)]
    [InlineData("[FRG:cad-tools/mr/3]", "cad-tools", ForgeArtifactKind.MergeRequest, 3)]
    public void Parses_issue_and_mr(string bracket, string repo, ForgeArtifactKind kind, int number)
    {
        Assert.True(BracketForgeReferenceParser.TryParse(bracket, out var artifact, out var error), error);
        Assert.Equal(repo, artifact.Repo);
        Assert.Equal(kind, artifact.Kind);
        Assert.Equal(number, artifact.Number);
        Assert.Null(artifact.CodeBracket);
    }

    [Fact]
    public void Parses_compound_code_tail()
    {
        const string bracket = "[FRG:pilot/issues/7; F:src/Foo.cs; L:10]";
        Assert.True(BracketForgeReferenceParser.TryParse(bracket, out var artifact, out var error), error);
        Assert.Equal("pilot", artifact.Repo);
        Assert.Equal(ForgeArtifactKind.Issue, artifact.Kind);
        Assert.Equal(7, artifact.Number);
        Assert.Contains("F:src/Foo.cs", artifact.CodeBracket, StringComparison.Ordinal);
    }

    [Fact]
    public void Facade_discriminates_forge_vs_code()
    {
        Assert.True(BracketReferenceParser.TryParse("[FRG:pilot/issues/1]", out var kind, out var forge, out _, out _));
        Assert.Equal(BracketReferenceKind.Forge, kind);
        Assert.Equal("pilot", forge.Repo);

        Assert.True(BracketReferenceParser.TryParse("[F:src/A.cs; L:1]", out kind, out _, out var code, out _));
        Assert.Equal(BracketReferenceKind.Code, kind);
        Assert.Equal("src/A.cs", code.File);
    }
}

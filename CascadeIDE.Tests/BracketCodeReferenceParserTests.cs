using CascadeIDE.Services.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class BracketCodeReferenceParserTests
{
    [Fact]
    public void TryParse_parses_F_M_axes()
    {
        Assert.True(BracketCodeReferenceParser.TryParse(
            "[F:Features/Chat/Foo.cs M:RunAsync]",
            out var reference,
            out _));
        Assert.Equal("Features/Chat/Foo.cs", reference.File);
        Assert.Equal("RunAsync", reference.MemberKey);
    }

    [Fact]
    public void EnumerateInProse_skips_fenced_code()
    {
        const string md = """
            prose [F:src/A.cs M:Bar] here
            ```csharp
            var x = [F:ignored.cs M:Ignored];
            ```
            """;

        var hits = BracketCodeReferenceParser.EnumerateInProse(md);
        Assert.Single(hits);
        Assert.Equal("src/A.cs", hits[0].Reference.File);
    }

    [Fact]
    public void EnumerateInProse_skips_markdown_link()
    {
        const string md = "See [M:Run](docs/adr/0001.md) for details.";

        Assert.Empty(BracketCodeReferenceParser.EnumerateInProse(md));
    }
}

using CascadeIDE.Features.Chat;
using CascadeIDE.Services.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class BracketCodeReferenceParserTests
{
    [Theory]
    [InlineData("[M:GetUserAsync]", null, "GetUserAsync", null, null)]
    [InlineData("M:Foo.Bar", null, "Foo.Bar", null, null)]
    [InlineData("[Foo.cs M:Bar]", "Foo.cs", "Bar", null, null)]
    [InlineData("[F:src/A.cs; M:Method; L:10-20]", "src/A.cs", "Method", 10, 20)]
    public void TryParse_CommonForms(
        string input,
        string? file,
        string member,
        int? lineStart,
        int? lineEnd)
    {
        Assert.True(BracketCodeReferenceParser.TryParse(input, out var reference, out var err), err);
        Assert.Equal(file, reference.File);
        Assert.Equal(member, reference.MemberKey);
        Assert.Equal(lineStart, reference.LineStart);
        Assert.Equal(lineEnd, reference.LineEnd);
    }

    [Fact]
    public void TryBuildBracketCodeRef_IncludesActiveFile()
    {
        Assert.True(ChatSlashParametricArgsBuilder.TryBuild(
            "editor.select_code",
            "[M:Run]",
            new ChatSlashEditorContext(@"D:\ws\src\Foo.cs", ""),
            out var args,
            out var err), err);

        Assert.NotNull(args);
        Assert.Equal("[M:Run]", args!["code_ref"].GetString());
        Assert.Equal(@"D:\ws\src\Foo.cs", args["active_file"].GetString());
    }
}

using CascadeIDE.Features.Chat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class BracketCodeReferenceParserTests
{
    [Theory]
    [InlineData("[M:GetUserAsync]", null, "GetUserAsync", null, null, null, null)]
    [InlineData("M:Foo.Bar", null, "Foo.Bar", null, null, null, null)]
    [InlineData("[Foo.cs M:Bar]", "Foo.cs", "Bar", null, null, null, null)]
    [InlineData("[F:src/A.cs; M:Method; L:10-20]", "src/A.cs", "Method", 10, 20, null, null)]
    [InlineData("[M:Run S:for:2]", null, "Run", null, null, "for", 2)]
    [InlineData("[M:Run S:for(2)]", null, "Run", null, null, "for", 2)]
    [InlineData("[F:src/A.cs; M:Run; S:for:2]", "src/A.cs", "Run", null, null, "for", 2)]
    [InlineData("[F:Views/Chat/Skia/SkiaChatBubbleRenderer.cs M:Measure]", "Views/Chat/Skia/SkiaChatBubbleRenderer.cs", "Measure", null, null, null, null)]
    public void TryParse_CommonForms(
        string input,
        string? file,
        string member,
        int? lineStart,
        int? lineEnd,
        string? scopeKind,
        int? scopeIndex)
    {
        Assert.True(BracketCodeReferenceParser.TryParse(input, out var reference, out var err), err);
        Assert.Equal(file, reference.File);
        Assert.Equal(member, reference.MemberKey);
        Assert.Equal(lineStart, reference.LineStart);
        Assert.Equal(lineEnd, reference.LineEnd);
        Assert.Equal(scopeKind, reference.ScopeKind);
        Assert.Equal(scopeIndex, reference.ScopeIndexInParent);
    }

    [Fact]
    public void TryToAttachmentAnchor_IncludesSyntaxScope()
    {
        Assert.True(BracketCodeReferenceParser.TryParse("[M:Run S:for:2]", out var reference, out var err), err);
        Assert.True(
            BracketCodeReferenceParser.TryToAttachmentAnchor(reference, "src/Foo.cs", null, out var anchor, out err),
            err);
        Assert.NotNull(anchor.SyntaxScope);
        Assert.True(AttachmentSyntaxScope.TryParse(anchor.SyntaxScope, out var scope));
        Assert.Equal("for", scope!.Kind);
        Assert.Equal(2, scope.IndexInParent);
        Assert.Equal("Run", scope.ParentMemberKey);
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

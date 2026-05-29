using CascadeIDE.Features.MagicLink;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CideMagicLinkUriTests
{
    [Fact]
    public void TryParse_RevealWithFileAndLine()
    {
        const string uri = "cide://reveal?root=D:%2Frepo&f=Features%2FFoo.cs&l=10&le=12";
        Assert.True(CideMagicLinkUri.TryParse(uri, out var req, out var err), err);
        Assert.Equal(CideMagicLinkAction.Reveal, req.Action);
        Assert.Equal("D:\\repo", req.WorkspaceRoot);
        Assert.Equal("Features/Foo.cs", req.File);
        Assert.Equal(10, req.LineStart);
        Assert.Equal(12, req.LineEnd);
    }

    [Fact]
    public void TryParse_RevealWithBracket()
    {
        const string uri = "cide://reveal?root=D:%2Frepo&b=F%3AFeatures%2FFoo.cs%3B%20M%3ABar";
        Assert.True(CideMagicLinkUri.TryParse(uri, out var req, out _));
        Assert.Equal("F:Features/Foo.cs; M:Bar", req.BracketInner);
    }

    [Fact]
    public void TryParse_MdRequiresDoc()
    {
        Assert.False(CideMagicLinkUri.TryParse("cide://md?root=D:%2Fr", out _, out var err));
        Assert.Contains("doc", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WorkspaceGuard_RejectsTraversal()
    {
        Assert.False(CideMagicLinkWorkspaceGuard.TryResolveUnderRoot(
            @"C:\ws",
            "../outside.cs",
            out _,
            out var error));
        Assert.Contains("traversal", error, StringComparison.OrdinalIgnoreCase);
    }
}

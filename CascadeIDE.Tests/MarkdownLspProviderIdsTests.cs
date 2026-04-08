using CascadeIDE.Services.Lsp;
using Xunit;

namespace CascadeIDE.Tests;

public class MarkdownLspProviderIdsTests
{
    [Fact]
    public void Resolve_Marksman_DefaultExe()
    {
        var (exe, args) = MarkdownLspProviderIds.ResolveProcessArgs(MarkdownLspProviderIds.Marksman, null, null);
        Assert.Equal("marksman", exe);
        Assert.Equal("", args);
    }

    [Fact]
    public void Resolve_Custom_RequiresUserExecutable()
    {
        var (exe, args) = MarkdownLspProviderIds.ResolveProcessArgs(MarkdownLspProviderIds.Custom, null, " --verbose ");
        Assert.Equal("", exe);
        Assert.Equal("--verbose", args);
    }
}

using CascadeIDE.Models;
using CascadeIDE.Models.Shell;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ShellContractsTests
{
    [Fact]
    public void MfdShellPageDescriptor_ShellSurfaceId_IsStableLowercasePrefix()
    {
        var d = new MfdShellPageDescriptor(MfdShellPage.Chat);
        Assert.Equal("mfd.chat", d.ShellSurfaceId);
    }

    [Fact]
    public void PfdLayouts_Default_MatchesPrimaryV1()
    {
        Assert.Equal(PfdLayouts.PrimaryV1, PfdLayouts.Default.LayoutId);
    }

    [Fact]
    public void AsShellPage_Extension_MatchesDescriptor()
    {
        IMfdShellPage a = MfdShellPage.Terminal.AsShellPage();
        IMfdShellPage b = new MfdShellPageDescriptor(MfdShellPage.Terminal);
        Assert.Equal(a.ShellSurfaceId, b.ShellSurfaceId);
        Assert.Equal(MfdShellPage.Terminal, a.Page);
    }
}

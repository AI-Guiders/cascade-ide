using CascadeIDE.Features.Search.Application;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CommandPaletteHciQueryExtensionsTests
{
    [Fact]
    public void TryBuildFtsSurfaceQuery_EmptyOrWhitespace_Returns_null()
    {
        Assert.Null(CommandPaletteHciQueryExtensions.TryBuildFtsSurfaceQuery(new GoToAllQuery('x', "")));
        Assert.Null(CommandPaletteHciQueryExtensions.TryBuildFtsSurfaceQuery(new GoToAllQuery('x', "   ")));
    }

    [Fact]
    public void TryBuildFtsSurfaceQuery_NonEmpty_ReturnsTrimmedTerm()
    {
        Assert.Equal(
            "LoadSolution",
            CommandPaletteHciQueryExtensions.TryBuildFtsSurfaceQuery(new GoToAllQuery('x', " LoadSolution ")));
    }

    [Theory]
    [InlineData('f')]
    [InlineData('x')]
    public void FtsIncludeExtensions_F_or_x_NoExtensionFilter(char prefix)
    {
        Assert.Null(CommandPaletteHciQueryExtensions.FtsIncludeExtensions(new GoToAllQuery(prefix, "Foo")));
    }

    [Theory]
    [InlineData('t')]
    [InlineData('m')]
    public void FtsIncludeExtensions_t_or_m_Only_cs(char prefix)
    {
        var ext = CommandPaletteHciQueryExtensions.FtsIncludeExtensions(new GoToAllQuery(prefix, "Foo"));
        Assert.NotNull(ext);
        Assert.Single(ext!);
        Assert.Equal(".cs", ext![0]);
    }
}

using CascadeIDE.Features.Chat;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>ADR 0150: резолв по longest catalog path, не только parser head/action.</summary>
public sealed class ChatSlashCatalogPathResolveTests
{
    [Theory]
    [InlineData("/map type file", "/map type file", IdeCommands.SetCodeNavigationMapLevel, "file")]
    [InlineData("/map type controlflow", "/map type controlflow", IdeCommands.SetCodeNavigationMapLevel, "controlFlow")]
    [InlineData("/solution explorer show", "/solution explorer show", IdeCommands.ShowSolutionExplorerPage, null)]
    public void TryResolveInput_threeTokenCatalogPaths(
        string line,
        string expectedPath,
        string expectedCommandId,
        string? expectedMapLevel)
    {
        Assert.True(ChatSlashCommandCatalog.TryResolveInput(line, out var descriptor, out _));
        Assert.Equal(expectedPath, descriptor.SlashPath);
        Assert.Equal(expectedCommandId, descriptor.CommandId);
        Assert.Equal(expectedMapLevel, descriptor.MapLevel);
    }

    [Fact]
    public void TryResolveCanonical_doesNotDependOnParserShape()
    {
        Assert.True(ChatSlashCommandCatalog.TryResolveCanonical("/map type file", "", out var descriptor));
        Assert.Equal(IdeCommands.SetCodeNavigationMapLevel, descriptor.CommandId);
        Assert.Equal("file", descriptor.MapLevel);
    }

    [Fact]
    public void SlashLineResolver_mapTypeFile_isRunnable()
    {
        Assert.True(SlashLineResolver.TryResolveSlashLine("/map type file", out var line));
        Assert.Equal("/map type file", line.CanonicalPath);
        Assert.True(line.IsRunnable);
    }
}

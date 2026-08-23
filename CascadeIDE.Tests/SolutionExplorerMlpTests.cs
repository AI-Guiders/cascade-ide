using CascadeIDE.Features.Settings.DataAcquisition;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SolutionExplorerMlpTests
{
    [Theory]
    [InlineData("file_csproj", "csproj")]
    [InlineData("file_fsproj", "fsproj")]
    [InlineData("file_vbproj", "vbproj")]
    [InlineData("file_cs", "cs")]
    [InlineData("file_axaml", "axaml")]
    [InlineData("file_toml", "toml")]
    [InlineData("file_md", "md")]
    public void IconKeys_MapCommonExtensions(string iconKey, string expected) =>
        Assert.Equal(expected, SolutionExplorerIconKeys.ResolveAssetName(iconKey));

    [Fact]
    public void IconKeys_PowerMode_UsesSameVscodeIconsSubset()
    {
        Assert.Equal("cs", SolutionExplorerIconKeys.ResolveAssetName("file_cs", powerMonochrome: true));
        Assert.Equal("axaml", SolutionExplorerIconKeys.ResolveAssetName("file_axaml", powerMonochrome: true));
        Assert.Equal("csproj", SolutionExplorerIconKeys.ResolveAssetName("file_csproj", powerMonochrome: true));
    }

    [Fact]
    public void SolutionItem_CsprojIconKey_IsLanguageSpecific()
    {
        var item = SolutionItem.CreateProject("P", @"C:\ws\P\P.csproj");
        Assert.Equal("file_csproj", item.IconKey);
    }

    [Fact]
    public void Deserialize_WorkspaceSolutionExplorer_ParsesToggles()
    {
        const string text =
            """
            [workspace.solution_explorer]
            track_active_item = false
            compact_tree = false
            """;

        var settings = CascadeTomlSerializer.Deserialize<CascadeIdeSettings>(text);
        Assert.NotNull(settings);
        Assert.False(settings!.Workspace.SolutionExplorer.TrackActiveItem);
        Assert.False(settings.Workspace.SolutionExplorer.CompactTree);
    }

    [Fact]
    public void HotkeysToml_ContainsSolutionExplorerFilterShortcut()
    {
        Assert.True(
            BundledAppContent.TryReadDiskThenEmbedded("Hotkeys/hotkeys.toml", out var hotkeyText));
        Assert.Contains("focus_solution_explorer_filter", hotkeyText!, StringComparison.Ordinal);
        Assert.Contains("Ctrl+Oem1", hotkeyText!, StringComparison.Ordinal);
    }

    [Fact]
    public void WindowsShellReveal_ReturnsFalseForMissingPath() =>
        Assert.False(WindowsShellReveal.TryRevealInExplorer(@"C:\no-such-path-0167\missing.cs"));
}

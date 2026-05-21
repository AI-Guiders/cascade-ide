using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SettingsDefaultsLoaderTests
{
    [Fact]
    public void GetEmbeddedDefaultsToml_ContainsFontsIntercom()
    {
        var text = SettingsDefaultsLoader.GetEmbeddedDefaultsToml();
        Assert.Contains("[fonts.intercom]", text, StringComparison.Ordinal);
        Assert.Contains("prose_pt = 13", text, StringComparison.Ordinal);
        Assert.Contains("prose_pt_forward = 12", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DeserializeEffective_WithoutUserToml_UsesEmbeddedProsePt()
    {
        var s = SettingsDefaultsLoader.CreateDefault();
        Assert.Equal(13, s.Fonts.Intercom.ProsePt);
        Assert.Equal(12, s.Fonts.Intercom.ProsePtForward);
        Assert.Equal(15, s.Fonts.Intercom.ComposerPt);
        Assert.Equal(14, s.Fonts.Editor.SizePt);
        Assert.Equal(13f, s.Fonts.Intercom.ResolveProsePt(forwardHost: false));
        Assert.Equal(12f, s.Fonts.Intercom.ResolveProsePt(forwardHost: true));
        Assert.Equal(15f, s.Fonts.Intercom.ResolveComposerPt(forwardHost: false));
    }

    [Fact]
    public void CreateDefault_MatchesFactoryAiAndWorkspace()
    {
        var s = SettingsDefaultsLoader.CreateDefault();
        Assert.Equal("local", s.Ai.Mode);
        Assert.Equal("Flight", s.Workspace.Mode);
        Assert.Equal("editor", s.Workspace.PrimaryWorkSurface);
        Assert.True(s.HybridIndex.Enabled);
        Assert.Equal("rg", s.CommandPalette.GoToSearch.Backend);
    }

    [Fact]
    public void DeserializeEffective_UserOverridesOnlyProsePt_KeepsForwardFromDefaults()
    {
        const string user =
            """
            [fonts.intercom]
            prose_pt = 18
            """;

        var s = SettingsDefaultsLoader.DeserializeEffective(user);
        Assert.Equal(18, s.Fonts.Intercom.ProsePt);
        Assert.Equal(12, s.Fonts.Intercom.ProsePtForward);
        Assert.Equal("Segoe UI", s.Fonts.Intercom.ProseFamily);
    }

    [Fact]
    public void DeserializeEffective_UserAiSection_DoesNotClearEmbeddedFonts()
    {
        const string user =
            """
            [ai]
            mode = "mcp_only"
            """;

        var s = SettingsDefaultsLoader.DeserializeEffective(user);
        Assert.Equal("mcp_only", s.Ai.Mode);
        Assert.Equal(13, s.Fonts.Intercom.ProsePt);
    }
}

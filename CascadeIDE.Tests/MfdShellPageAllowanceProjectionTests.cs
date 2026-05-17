using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MfdShellPageAllowanceProjectionTests
{
    private static readonly MfdShellPageAllowanceProjection.Snapshot AllDenied = new(
        ShowIdeHealthMfdPage: false,
        IsDockedMfdSolutionExplorerTree: false,
        IsTerminalVisible: false,
        IsBuildOutputVisible: false,
        IsProblemsPanelVisible: false,
        IsGitPanelVisible: false,
        InstrumentationTabs: false,
        HypothesesTab: false,
        IsIntercomPrimaryWorkSurface: false);

    [Fact]
    public void Core_pages_still_blocked_when_snapshot_denies()
    {
        Assert.False(MfdShellPageAllowanceProjection.IsAllowed(MfdShellPage.WorkspaceHealth, AllDenied));
        Assert.False(MfdShellPageAllowanceProjection.IsAllowed(MfdShellPage.SolutionExplorer, AllDenied));
        Assert.False(MfdShellPageAllowanceProjection.IsAllowed(MfdShellPage.Terminal, AllDenied));
    }

    [Fact]
    public void Related_related_markdown_indexes_always_allowed()
    {
        Assert.True(MfdShellPageAllowanceProjection.IsAllowed(MfdShellPage.RelatedFiles, AllDenied));
        Assert.True(MfdShellPageAllowanceProjection.IsAllowed(MfdShellPage.HybridIndex, AllDenied));
        Assert.True(MfdShellPageAllowanceProjection.IsAllowed(MfdShellPage.WebAiPortal, AllDenied));
    }

    [Fact]
    public void First_allowed_follows_page_order_among_neutral_surfaces()
    {
        // Related/Markdown/Hybrid/Chat/… остаются разрешёнными независимо от флагов доков.
        Assert.Equal(MfdShellPage.RelatedFiles, MfdShellPageAllowanceProjection.FirstAllowedOrChat(AllDenied));
    }

    [Fact]
    public void Terminal_allowed_when_visibility_flag()
    {
        var on = AllDenied with { IsTerminalVisible = true };
        Assert.True(MfdShellPageAllowanceProjection.IsAllowed(MfdShellPage.Terminal, on));
    }

    [Fact]
    public void Intercom_primary_swaps_editor_and_chat_mfd_pages()
    {
        var intercom = AllDenied with { IsIntercomPrimaryWorkSurface = true };
        Assert.True(MfdShellPageAllowanceProjection.IsAllowed(MfdShellPage.Editor, intercom));
        Assert.False(MfdShellPageAllowanceProjection.IsAllowed(MfdShellPage.Chat, intercom));
    }
}

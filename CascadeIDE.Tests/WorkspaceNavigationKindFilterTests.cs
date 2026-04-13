using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceNavigationKindFilterTests
{
    [Fact]
    public void Create_Default_Allows_All_Kinds()
    {
        var f = WorkspaceNavigationKindFilter.Create(null, null);
        Assert.True(f.Allows(WorkspaceNavigationRelatedKinds.ProjectPeer));
        Assert.True(f.Allows(WorkspaceNavigationRelatedKinds.SameNamespace));
    }

    [Fact]
    public void Exclude_Removes_Kind()
    {
        var f = WorkspaceNavigationKindFilter.Create(null, ["same_namespace", "same_directory"]);
        Assert.True(f.Allows(WorkspaceNavigationRelatedKinds.ProjectPeer));
        Assert.False(f.Allows(WorkspaceNavigationRelatedKinds.SameNamespace));
        Assert.False(f.Allows(WorkspaceNavigationRelatedKinds.SameDirectory));
    }

    [Fact]
    public void Include_Whitelist_Only()
    {
        var f = WorkspaceNavigationKindFilter.Create(["project_peer", "partial_peer"], null);
        Assert.True(f.Allows(WorkspaceNavigationRelatedKinds.ProjectPeer));
        Assert.True(f.Allows(WorkspaceNavigationRelatedKinds.PartialPeer));
        Assert.False(f.Allows(WorkspaceNavigationRelatedKinds.TestCounterpart));
    }

    [Fact]
    public void Include_And_Exclude_Compose()
    {
        var f = WorkspaceNavigationKindFilter.Create(["project_peer", "test_counterpart"], ["test_counterpart"]);
        Assert.True(f.Allows(WorkspaceNavigationRelatedKinds.ProjectPeer));
        Assert.False(f.Allows(WorkspaceNavigationRelatedKinds.TestCounterpart));
    }

    [Fact]
    public void Unknown_Tokens_In_Lists_Are_Ignored()
    {
        var f = WorkspaceNavigationKindFilter.Create(["not_a_kind", "PROJECT_PEER"], null);
        Assert.True(f.Allows(WorkspaceNavigationRelatedKinds.ProjectPeer));
        Assert.False(f.Allows(WorkspaceNavigationRelatedKinds.SameNamespace));
    }

    [Fact]
    public void Include_Only_Unknown_Falls_Back_To_All()
    {
        var f = WorkspaceNavigationKindFilter.Create(["typo", "nope"], null);
        Assert.True(f.Allows(WorkspaceNavigationRelatedKinds.SameNamespace));
    }

    [Fact]
    public void Effective_Include_Is_Null_When_Unrestricted()
    {
        var f = WorkspaceNavigationKindFilter.Create(null, null);
        Assert.Null(f.EffectiveIncludeKinds);
    }

    [Fact]
    public void Effective_Include_Lists_Canonical_Names()
    {
        var f = WorkspaceNavigationKindFilter.Create(["PROJECT_PEER", "partial_peer"], null);
        Assert.NotNull(f.EffectiveIncludeKinds);
        Assert.Equal(2, f.EffectiveIncludeKinds!.Count);
        Assert.Equal(WorkspaceNavigationRelatedKinds.PartialPeer, f.EffectiveIncludeKinds[0]);
        Assert.Equal(WorkspaceNavigationRelatedKinds.ProjectPeer, f.EffectiveIncludeKinds[1]);
    }

    [Fact]
    public void Effective_Exclude_Is_Empty_When_None()
    {
        var f = WorkspaceNavigationKindFilter.Create(null, null);
        Assert.NotNull(f.EffectiveExcludeKinds);
        Assert.Empty(f.EffectiveExcludeKinds);
    }
}

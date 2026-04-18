using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationKindFilterTests
{
    [Fact]
    public void Create_Default_Allows_All_Kinds()
    {
        var f = CodeNavigationKindFilter.Create(null, null);
        Assert.True(f.Allows(CodeNavigationRelatedKinds.ProjectPeer));
        Assert.True(f.Allows(CodeNavigationRelatedKinds.SameNamespace));
    }

    [Fact]
    public void Exclude_Removes_Kind()
    {
        var f = CodeNavigationKindFilter.Create(null, ["same_namespace", "same_directory"]);
        Assert.True(f.Allows(CodeNavigationRelatedKinds.ProjectPeer));
        Assert.False(f.Allows(CodeNavigationRelatedKinds.SameNamespace));
        Assert.False(f.Allows(CodeNavigationRelatedKinds.SameDirectory));
    }

    [Fact]
    public void Include_Whitelist_Only()
    {
        var f = CodeNavigationKindFilter.Create(["project_peer", "partial_peer"], null);
        Assert.True(f.Allows(CodeNavigationRelatedKinds.ProjectPeer));
        Assert.True(f.Allows(CodeNavigationRelatedKinds.PartialPeer));
        Assert.False(f.Allows(CodeNavigationRelatedKinds.TestCounterpart));
    }

    [Fact]
    public void Include_And_Exclude_Compose()
    {
        var f = CodeNavigationKindFilter.Create(["project_peer", "test_counterpart"], ["test_counterpart"]);
        Assert.True(f.Allows(CodeNavigationRelatedKinds.ProjectPeer));
        Assert.False(f.Allows(CodeNavigationRelatedKinds.TestCounterpart));
    }

    [Fact]
    public void Unknown_Tokens_In_Lists_Are_Ignored()
    {
        var f = CodeNavigationKindFilter.Create(["not_a_kind", "PROJECT_PEER"], null);
        Assert.True(f.Allows(CodeNavigationRelatedKinds.ProjectPeer));
        Assert.False(f.Allows(CodeNavigationRelatedKinds.SameNamespace));
    }

    [Fact]
    public void Include_Only_Unknown_Falls_Back_To_All()
    {
        var f = CodeNavigationKindFilter.Create(["typo", "nope"], null);
        Assert.True(f.Allows(CodeNavigationRelatedKinds.SameNamespace));
    }

    [Fact]
    public void Effective_Include_Is_Null_When_Unrestricted()
    {
        var f = CodeNavigationKindFilter.Create(null, null);
        Assert.Null(f.EffectiveIncludeKinds);
    }

    [Fact]
    public void Effective_Include_Lists_Canonical_Names()
    {
        var f = CodeNavigationKindFilter.Create(["PROJECT_PEER", "partial_peer"], null);
        Assert.NotNull(f.EffectiveIncludeKinds);
        Assert.Equal(2, f.EffectiveIncludeKinds!.Count);
        Assert.Equal(CodeNavigationRelatedKinds.PartialPeer, f.EffectiveIncludeKinds[0]);
        Assert.Equal(CodeNavigationRelatedKinds.ProjectPeer, f.EffectiveIncludeKinds[1]);
    }

    [Fact]
    public void Effective_Exclude_Is_Empty_When_None()
    {
        var f = CodeNavigationKindFilter.Create(null, null);
        Assert.NotNull(f.EffectiveExcludeKinds);
        Assert.Empty(f.EffectiveExcludeKinds);
    }
}

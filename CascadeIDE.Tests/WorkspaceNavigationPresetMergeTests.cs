using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Тесты <see cref="WorkspaceNavigationPresetMerge"/> не используют диск и <see cref="AppContext.BaseDirectory"/>.
/// Вход для merge — JSON из <see cref="WorkspaceNavigationPresetsLoader.ToPresetMergeJsonFromBundledToml"/> по встроенному бандлу (<see cref="WorkspaceNavigationPresetsLoader.GetEmbeddedBundledPresetsToml"/>), как в рантайме без дублирования литералов.
/// </summary>
public sealed class WorkspaceNavigationPresetMergeTests
{
    private static readonly string DefaultBundledMergeJson =
        WorkspaceNavigationPresetsLoader.ToPresetMergeJsonFromBundledToml(
            WorkspaceNavigationPresetsLoader.GetEmbeddedBundledPresetsToml());

    [Fact]
    public void Unknown_Preset_Returns_Error()
    {
        var (inc, exc, err) = WorkspaceNavigationPresetMerge.Merge(
            "no_such",
            DefaultBundledMergeJson,
            null,
            null);
        Assert.Null(inc);
        Assert.Null(exc);
        Assert.Contains("Неизвестный", err ?? "");
    }

    [Fact]
    public void Peers_Only_From_Default_Presets()
    {
        var (inc, exc, err) = WorkspaceNavigationPresetMerge.Merge(
            "peers_only",
            DefaultBundledMergeJson,
            null,
            null);
        Assert.Null(err);
        Assert.NotNull(inc);
        Assert.Contains(WorkspaceNavigationRelatedKinds.PartialPeer, inc);
        Assert.Contains(WorkspaceNavigationRelatedKinds.ProjectPeer, inc);
        Assert.Equal(2, inc!.Count);
        Assert.NotNull(exc);
        Assert.Empty(exc!);
    }

    [Fact]
    public void Request_Include_Overrides_Preset_Include()
    {
        var (inc, exc, err) = WorkspaceNavigationPresetMerge.Merge(
            "peers_only",
            DefaultBundledMergeJson,
            [WorkspaceNavigationRelatedKinds.SameNamespace],
            null);
        Assert.Null(err);
        Assert.NotNull(inc);
        Assert.Single(inc!);
        Assert.Equal(WorkspaceNavigationRelatedKinds.SameNamespace, inc[0]);
    }

    [Fact]
    public void Request_Exclude_Merges_With_Preset_Exclude()
    {
        var (inc, exc, err) = WorkspaceNavigationPresetMerge.Merge(
            "no_namespace_noise",
            DefaultBundledMergeJson,
            null,
            [WorkspaceNavigationRelatedKinds.TestCounterpart]);
        Assert.Null(err);
        Assert.Null(inc);
        Assert.NotNull(exc);
        Assert.Contains(WorkspaceNavigationRelatedKinds.SameNamespace, exc);
        Assert.Contains(WorkspaceNavigationRelatedKinds.SameDirectory, exc);
        Assert.Contains(WorkspaceNavigationRelatedKinds.TestCounterpart, exc);
    }
}

using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Тесты <see cref="CodeNavigationPresetMerge"/> не используют диск и <see cref="AppContext.BaseDirectory"/>.
/// Вход для merge — JSON из <see cref="CodeNavigationPresetsLoader.ToPresetMergeJsonFromBundledToml"/> по встроенному бандлу (<see cref="CodeNavigationPresetsLoader.GetEmbeddedBundledPresetsToml"/>), как в рантайме без дублирования литералов.
/// </summary>
public sealed class CodeNavigationPresetMergeTests
{
    private static readonly string DefaultBundledMergeJson =
        CodeNavigationPresetsLoader.ToPresetMergeJsonFromBundledToml(
            CodeNavigationPresetsLoader.GetEmbeddedBundledPresetsToml());

    [Fact]
    public void Unknown_Preset_Returns_Error()
    {
        var (inc, exc, err) = CodeNavigationPresetMerge.Merge(
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
        var (inc, exc, err) = CodeNavigationPresetMerge.Merge(
            "peers_only",
            DefaultBundledMergeJson,
            null,
            null);
        Assert.Null(err);
        Assert.NotNull(inc);
        Assert.Contains(CodeNavigationRelatedKinds.PartialPeer, inc);
        Assert.Contains(CodeNavigationRelatedKinds.ProjectPeer, inc);
        Assert.Equal(2, inc!.Count);
        Assert.NotNull(exc);
        Assert.Empty(exc!);
    }

    [Fact]
    public void Request_Include_Overrides_Preset_Include()
    {
        var (inc, exc, err) = CodeNavigationPresetMerge.Merge(
            "peers_only",
            DefaultBundledMergeJson,
            [CodeNavigationRelatedKinds.SameNamespace],
            null);
        Assert.Null(err);
        Assert.NotNull(inc);
        Assert.Single(inc!);
        Assert.Equal(CodeNavigationRelatedKinds.SameNamespace, inc[0]);
    }

    [Fact]
    public void Request_Exclude_Merges_With_Preset_Exclude()
    {
        var (inc, exc, err) = CodeNavigationPresetMerge.Merge(
            "no_namespace_noise",
            DefaultBundledMergeJson,
            null,
            [CodeNavigationRelatedKinds.TestCounterpart]);
        Assert.Null(err);
        Assert.Null(inc);
        Assert.NotNull(exc);
        Assert.Contains(CodeNavigationRelatedKinds.SameNamespace, exc);
        Assert.Contains(CodeNavigationRelatedKinds.SameDirectory, exc);
        Assert.Contains(CodeNavigationRelatedKinds.TestCounterpart, exc);
    }
}

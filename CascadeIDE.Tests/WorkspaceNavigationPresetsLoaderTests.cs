using System.Text.Json;
using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceNavigationPresetsLoaderTests
{
    [Fact]
    public void ToPresetMergeJsonFromBundledToml_Contains_Bundled_Preset_Ids()
    {
        var json = WorkspaceNavigationPresetsLoader.ToPresetMergeJsonFromBundledToml(
            WorkspaceNavigationPresetsLoader.GetEmbeddedBundledPresetsToml());
        Assert.Contains("peers_only", json);
        Assert.Contains("no_namespace_noise", json);
    }

    [Fact]
    public void MergeBundledWithUser_Adds_New_Preset()
    {
        var bundled = new List<WorkspaceNavigationPresetEntry>
        {
            new() { Id = "a", IncludeKinds = ["partial_peer"] }
        };
        var user = new List<WorkspaceNavigationPresetEntry>
        {
            new() { Id = "b", ExcludeKinds = ["same_namespace"] }
        };
        var merged = WorkspaceNavigationPresetsLoader.MergeBundledWithUser(bundled, user);
        Assert.Equal(2, merged.Count);
        Assert.Contains(merged, x => x.Id == "a");
        Assert.Contains(merged, x => x.Id == "b");
    }

    [Fact]
    public void MergeBundledWithUser_User_Replaces_Same_Id()
    {
        var bundled = new List<WorkspaceNavigationPresetEntry>
        {
            new() { Id = "peers_only", IncludeKinds = ["partial_peer"] }
        };
        var user = new List<WorkspaceNavigationPresetEntry>
        {
            new() { Id = "peers_only", IncludeKinds = ["project_peer"] }
        };
        var merged = WorkspaceNavigationPresetsLoader.MergeBundledWithUser(bundled, user);
        Assert.Single(merged);
        Assert.Equal("project_peer", Assert.Single(merged[0].IncludeKinds!));
    }

    [Fact]
    public void GetEffectivePresetsJson_Repository_overrides_bundled_User_overrides_repository()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "cascade_nav_repo_" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(tmp, ".cascade"));
        File.WriteAllText(
            Path.Combine(tmp, ".cascade", "workspace.toml"),
            """
            [[workspace_navigation.presets]]
            id = "peers_only"
            include_kinds = ["same_namespace"]
            """);

        var jsonRepoOnly = WorkspaceNavigationPresetsLoader.GetEffectivePresetsJson(new NavigationSettings(), tmp);
        using (var doc = JsonDocument.Parse(jsonRepoOnly))
        {
            var inc = doc.RootElement.GetProperty("peers_only").GetProperty("include_kinds").EnumerateArray().Select(x => x.GetString()).ToArray();
            Assert.Contains("same_namespace", inc);
        }

        var user = new NavigationSettings
        {
            Presets =
            [
                new WorkspaceNavigationPresetEntry { Id = "peers_only", IncludeKinds = ["project_peer"] }
            ]
        };
        var jsonUserWins = WorkspaceNavigationPresetsLoader.GetEffectivePresetsJson(user, tmp);
        using (var doc = JsonDocument.Parse(jsonUserWins))
        {
            var inc = doc.RootElement.GetProperty("peers_only").GetProperty("include_kinds").EnumerateArray().Select(x => x.GetString()).ToArray();
            Assert.Single(inc);
            Assert.Equal("project_peer", inc[0]);
        }
    }
}

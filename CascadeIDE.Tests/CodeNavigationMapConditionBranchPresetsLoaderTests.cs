using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationMapConditionBranchPresetsLoaderTests
{
    [Fact]
    public void BundledToml_Contains_Standard_Preset_Ids()
    {
        var text = CodeNavigationMapConditionBranchPresetsLoader.GetEmbeddedBundledToml();
        Assert.Contains("plus_minus", text, StringComparison.Ordinal);
        Assert.Contains("true_false", text, StringComparison.Ordinal);
        Assert.Contains("one_zero", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GetEffectivePresets_Repository_overrides_bundled_User_overrides_repository()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "cascade_branch_repo_" + Guid.NewGuid());
        Directory.CreateDirectory(Path.Combine(tmp, ".cascade"));
        File.WriteAllText(
            Path.Combine(tmp, ".cascade", "workspace.toml"),
            """
            [[code_navigation_map.condition_branch.presets]]
            id = "plus_minus"
            positive = "Y"
            negative = "N"
            """);

        var repoOnly = CodeNavigationMapConditionBranchPresetsLoader.GetEffectivePresets(
            new CodeNavigationMapSettings { ConditionBranchLabelPreset = "plus_minus" },
            tmp);
        var repoEntry = Assert.Single(repoOnly, e => e.Id == "plus_minus");
        Assert.Equal("Y", repoEntry.Positive);
        Assert.Equal("N", repoEntry.Negative);

        var userMap = new CodeNavigationMapSettings
        {
            ConditionBranchLabelPreset = "plus_minus",
            ConditionBranch = new CodeNavigationMapConditionBranchToml
            {
                Presets =
                [
                    new CodeNavigationMapConditionBranchPresetEntry
                    {
                        Id = "plus_minus",
                        Positive = "✓",
                        Negative = "✗"
                    }
                ]
            }
        };
        var userWins = CodeNavigationMapConditionBranchPresetsLoader.GetEffectivePresets(userMap, tmp);
        var userEntry = Assert.Single(userWins, e => e.Id == "plus_minus");
        Assert.Equal("✓", userEntry.Positive);
        Assert.Equal("✗", userEntry.Negative);

        var pair = CodeNavigationMapConditionBranchLabels.Resolve(userMap, tmp);
        Assert.Equal("✓", pair.Positive);
        Assert.Equal("✗", pair.Negative);
    }

    [Fact]
    public void MergeBundledWithUser_User_Replaces_Same_Id()
    {
        var bundled = new List<CodeNavigationMapConditionBranchPresetEntry>
        {
            new() { Id = "plus_minus", Positive = "+", Negative = "-" }
        };
        var user = new List<CodeNavigationMapConditionBranchPresetEntry>
        {
            new() { Id = "plus_minus", Positive = "✓", Negative = "✗" }
        };
        var merged = CodeNavigationMapConditionBranchPresetsLoader.MergeBundledWithUser(bundled, user);
        Assert.Single(merged);
        Assert.Equal("✓", merged[0].Positive);
        Assert.Equal("✗", merged[0].Negative);
    }

    [Fact]
    public void Resolve_FromBundled_true_false()
    {
        var map = new CodeNavigationMapSettings
        {
            ConditionBranchLabelPreset = CodeNavigationMapConditionBranchLabels.PresetTrueFalse
        };
        var pair = CodeNavigationMapConditionBranchLabels.Resolve(map);
        Assert.Equal("true", pair.Positive);
        Assert.Equal("false", pair.Negative);
    }

    [Fact]
    public void Deserialize_ConditionBranchPresets_ArrayOfTables()
    {
        const string text =
            """
            [code_navigation_map]
            condition_branch_label_preset = "ja"

            [[code_navigation_map.condition_branch.presets]]
            id = "ja"
            positive = "はい"
            negative = "いいえ"
            """;

        var s = CascadeTomlSerializer.Deserialize<CascadeIdeSettings>(text)
            ?? throw new InvalidOperationException("null settings");
        Assert.Single(s.CodeNavigationMap.ConditionBranch.Presets);
        Assert.Equal("ja", s.CodeNavigationMap.ConditionBranch.Presets[0].Id);
        Assert.Equal("はい", s.CodeNavigationMap.ConditionBranch.Presets[0].Positive);

        var merged = CodeNavigationMapConditionBranchPresetsLoader.GetEffectivePresets(s.CodeNavigationMap);
        Assert.Contains(merged, e => e.Id == "ja");
        var pair = CodeNavigationMapConditionBranchLabels.Resolve(s.CodeNavigationMap);
        Assert.Equal("はい", pair.Positive);
        Assert.Equal("いいえ", pair.Negative);
    }
}

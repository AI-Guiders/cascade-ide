using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CascadeIdeMafProjectAgentRulesTests
{
    [Fact]
    public void TryLoadMerged_MergesSingleFileAndFragmentsInOrder()
    {
        var root = Path.Combine(Path.GetTempPath(), "cascade-rules-test-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path.Combine(root, ".cascade-ide", "maf-project-rules"));

        File.WriteAllText(Path.Combine(root, ".cascade-ide", "maf-project-rules.md"), "Alpha rule.");
        File.WriteAllText(Path.Combine(root, ".cascade-ide", "maf-project-rules", "b.md"), "Beta.");
        File.WriteAllText(Path.Combine(root, ".cascade-ide", "maf-project-rules", "a.md"), "Gamma.");

        try
        {
            var merged = CascadeIdeMafProjectAgentRules.TryLoadMerged(root);
            Assert.NotNull(merged);
            Assert.Contains("Alpha rule.", merged, StringComparison.Ordinal);
            Assert.Contains("## a.md", merged, StringComparison.Ordinal);
            Assert.Contains("Gamma.", merged, StringComparison.Ordinal);
            Assert.Contains("## b.md", merged, StringComparison.Ordinal);
            Assert.Contains("Beta.", merged, StringComparison.Ordinal);
            Assert.True(merged.IndexOf("Gamma.", StringComparison.Ordinal) < merged.IndexOf("Beta.", StringComparison.Ordinal),
                "Fragments should be merged in filename sort order (a.md before b.md).");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); }
            catch { /* temp cleanup best-effort */ }
        }
    }

    [Fact]
    public void BuildInstructions_AppendsProjectBlockWhenProvided()
    {
        var prompts = new MafIdeAgentPrompts.PromptPack(
            AgentSystem: "CORE",
            SalvageRecapSystem: "S",
            SalvageUserMessageTemplate: "{{USER_QUERY}}\n{{TOOL_PAYLOAD}}",
            OptionalSections: new Dictionary<string, string>
            {
                ["pack_mode_debug"] = "DEBUG PACK",
                ["pack_domain_csharp_roslyn"] = "ROSLYN PACK",
            });
        var merged = CascadeIdeMafIdeAgentChat.BuildInstructions(
            prompts,
            cascadeConversation:
            [
                new ChatMessage("user", "Приложение падает в .cs файле, помоги с debug"),
            ],
            minimizedContextBlock: "Program.cs: exception",
            projectAgentRulesMarkdown: "## rule\nhello");
        Assert.Contains("CORE", merged, StringComparison.Ordinal);
        Assert.Contains("Проектные правила", merged, StringComparison.Ordinal);
        Assert.Contains("hello", merged, StringComparison.Ordinal);
    }

    [Fact]
    public void PromptPackRouter_SelectsPacks_ForClearDebugAndCSharpSignal()
    {
        var prompts = new MafIdeAgentPrompts.PromptPack(
            AgentSystem: "CORE",
            SalvageRecapSystem: "S",
            SalvageUserMessageTemplate: "{{USER_QUERY}}\n{{TOOL_PAYLOAD}}",
            OptionalSections: new Dictionary<string, string>
            {
                ["pack_mode_debug"] = "DEBUG PACK",
                ["pack_domain_csharp_roslyn"] = "ROSLYN PACK",
            });

        var route = MafPromptPackRouter.Route(
            prompts,
            [
                new ChatMessage("tool", "error: null reference"),
                new ChatMessage("user", "Fix debug bug in .cs class, stack error in service"),
            ],
            minimizedContextBlock: "using System;\nclass Service {}",
            budgetChars: 1_200);

        Assert.True(route.Selections.Count > 0);
        Assert.Contains(route.Selections, s => string.Equals(s.Key, "pack_mode_debug", StringComparison.Ordinal));
    }
}

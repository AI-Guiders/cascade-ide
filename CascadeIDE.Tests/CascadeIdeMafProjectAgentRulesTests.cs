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
        var merged = CascadeIdeMafIdeAgentChat.BuildInstructions("CORE", "## rule\nhello");
        Assert.Contains("CORE", merged, StringComparison.Ordinal);
        Assert.Contains("Проектные правила", merged, StringComparison.Ordinal);
        Assert.Contains("hello", merged, StringComparison.Ordinal);
    }
}

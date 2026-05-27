using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>ADR 0150: канонический каталог без legacy requires_arg_tail.</summary>
public sealed class IntentCatalogArgTailPolicyTests
{
    [Fact]
    public void BundledIntentCatalog_HasNoLegacyRequiresArgTail()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var catalogPath = Path.Combine(repoRoot, IntentMelodyAliases.BundledRelativePath);
        Assert.True(File.Exists(catalogPath), catalogPath);

        var text = File.ReadAllText(catalogPath);
        Assert.DoesNotContain("requires_arg_tail =", text, StringComparison.Ordinal);
    }
}

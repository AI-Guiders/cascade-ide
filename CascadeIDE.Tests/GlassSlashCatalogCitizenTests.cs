#nullable enable
using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassSlashCatalogCitizenTests
{
    [Fact]
    public void Suggest_and_resolve_citizen()
    {
        var hits = GlassSlashCatalog.Suggest("/cit");
        Assert.Contains(hits, h => h.InsertText.StartsWith("/citizen", StringComparison.Ordinal));

        Assert.True(GlassSlashCatalog.TryResolve("/citizen привет", out var cmd, out var args));
        Assert.Equal("citizen", cmd.Id);
        Assert.Equal("привет", args);

        Assert.Contains("/citizen", GlassSlashCatalog.FormatHelp(), StringComparison.Ordinal);
    }
}

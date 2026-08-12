#nullable enable

using CascadeIDE.SoftInstrument;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassGitIgnoreFilterTests
{
    [Fact]
    public void DropIgnored_empty_passthrough()
    {
        var rows = GlassGitIgnoreFilter.DropIgnored(null, []);
        Assert.Empty(rows);
    }

    [Fact]
    public void DropIgnored_keeps_source_when_no_git()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "glass-git-ignore-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            var input = GlassGitPorcelainParse.Parse(" M a.cs\n?? b.txt\n");
            var kept = GlassGitIgnoreFilter.DropIgnored(tmp, input);
            Assert.Equal(input.Count, kept.Count);
        }
        finally
        {
            try { Directory.Delete(tmp, recursive: true); } catch { /* ignore */ }
        }
    }
}

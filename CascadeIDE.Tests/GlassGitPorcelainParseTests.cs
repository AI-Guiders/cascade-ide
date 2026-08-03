#nullable enable

using CascadeIDE.SoftOrgan;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassGitPorcelainParseTests
{
    [Fact]
    public void Parse_porcelain_rows()
    {
        var rows = GlassGitPorcelainParse.Parse(
            """
            M  a.cs
             M b.cs
            R  old.cs -> new.cs
            ?? untracked.txt
            """);

        Assert.Equal(4, rows.Count);
        Assert.Equal("M ", rows[0].Xy);
        Assert.Equal("a.cs", rows[0].Path);
        Assert.True(rows[0].IsStaged);
        Assert.Equal(" M", rows[1].Xy);
        Assert.True(rows[1].IsUnstaged);
        Assert.Equal("old.cs", rows[2].OrigPath);
        Assert.Equal("new.cs", rows[2].Path);
        Assert.Equal("??", rows[3].Xy);
    }
}

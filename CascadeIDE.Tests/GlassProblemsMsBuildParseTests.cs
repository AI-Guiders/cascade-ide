#nullable enable
using CascadeIDE.SoftInstrument;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassProblemsMsBuildParseTests
{
    [Fact]
    public void Parse_msbuild_error_and_warning_lines()
    {
        var text = """
            D:\\src\\A.cs(10,5): error CS1002: ; expected
            D:\\src\\B.cs(2): warning CS0168: The variable 'x' is declared but never used
            Build succeeded.
            """;

        var rows = GlassProblemsMsBuildParse.Parse(text);
        Assert.Equal(2, rows.Count);
        Assert.Equal("error", rows[0].Severity);
        Assert.Equal("CS1002", rows[0].Id);
        Assert.Equal(10, rows[0].Line);
        Assert.Equal(5, rows[0].Column);
        Assert.True(rows[0].IsError);
        Assert.Equal("warning", rows[1].Severity);
        Assert.Equal(2, rows[1].Line);
        Assert.Equal(1, rows[1].Column);
        Assert.True(rows[1].IsWarning);
    }

    [Fact]
    public void Parse_dedupes_identical_lines()
    {
        var line = @"C:\a.cs(1,1): error CS0001: boom";
        var rows = GlassProblemsMsBuildParse.Parse(line + "\n" + line);
        Assert.Single(rows);
    }
}

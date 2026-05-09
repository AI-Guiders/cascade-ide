using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CanonicalFilePathTests
{
    [Fact]
    public void Equals_NormalizesAndIgnoresCase()
    {
        var dir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), nameof(CanonicalFilePathTests) + "_dir"));
        var a = Path.Combine(dir, "FILE.cs");
        var b = Path.Combine(dir, "file.cs");
        Assert.True(CanonicalFilePath.Equals(a, b));
    }

    [Fact]
    public void EqualsNormalized_Matches_GetAllBreakpointLines_pattern()
    {
        var dir = Path.GetFullPath(Path.Combine(Path.GetTempPath(), nameof(CanonicalFilePathTests) + "_n"));
        var reference = CanonicalFilePath.Normalize(Path.Combine(dir, "X.cs"));
        Assert.True(CanonicalFilePath.EqualsNormalized(reference, Path.Combine(dir, "x.cs")));
    }

    [Fact]
    public void Equals_Empty_is_false()
    {
        Assert.False(CanonicalFilePath.Equals("", "a"));
        Assert.False(CanonicalFilePath.Equals("a", null));
    }
}

#nullable enable

using CascadeIDE.SoftInstrument;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GlassGitDiffLineTests
{
    [Theory]
    [InlineData("@@ -1,2 +1,3 @@", GlassGitDiffLineKind.Hunk)]
    [InlineData("diff --git a/x b/x", GlassGitDiffLineKind.Meta)]
    [InlineData("--- a/x", GlassGitDiffLineKind.Meta)]
    [InlineData("+++ b/x", GlassGitDiffLineKind.Meta)]
    [InlineData("+added", GlassGitDiffLineKind.Add)]
    [InlineData("-removed", GlassGitDiffLineKind.Delete)]
    [InlineData(" context", GlassGitDiffLineKind.Context)]
    [InlineData("", GlassGitDiffLineKind.Context)]
    public void Classify_unified_markers(string line, GlassGitDiffLineKind kind) =>
        Assert.Equal(kind, GlassGitDiffLine.Classify(line));

    [Theory]
    [InlineData(" M", GlassGitStatusTone.Change)]
    [InlineData("M ", GlassGitStatusTone.Change)]
    [InlineData("??", GlassGitStatusTone.Untracked)]
    [InlineData(" D", GlassGitStatusTone.Delete)]
    [InlineData("A ", GlassGitStatusTone.Add)]
    [InlineData("MD", GlassGitStatusTone.Delete)]
    public void Status_tone_buckets(string xy, string tone) =>
        Assert.Equal(tone, GlassGitStatusTone.Name(xy));
}

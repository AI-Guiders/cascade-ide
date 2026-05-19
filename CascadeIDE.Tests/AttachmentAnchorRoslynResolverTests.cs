using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AttachmentAnchorRoslynResolverTests
{
    private const string SampleSource =
        """
        namespace Sample;

        public class Worker
        {
            public void Run()
            {
                for (var i = 0; i < 1; i++) { }
                for (var j = 0; j < 2; j++) { }
            }
        }
        """;

    [Fact]
    public void TryResolveLineRange_FindsMethodBySimpleName()
    {
        var path = writeTempCs();
        Assert.True(
            AttachmentAnchorRoslynResolver.TryResolveLineRange(path, "Run", null, out var lines, out var detail),
            detail);
        Assert.True(lines.Start.Value >= 5);
        Assert.True(lines.End.Value >= lines.Start.Value);
    }

    [Fact]
    public void TryResolveLineRange_FindsSecondForLoop()
    {
        var path = writeTempCs();
        Assert.True(
            AttachmentAnchorRoslynResolver.TryResolveLineRange(
                path, null, new AttachmentSyntaxScope("for", 1, "Run"), out var first, out _));
        Assert.True(
            AttachmentAnchorRoslynResolver.TryResolveLineRange(
                path, null, new AttachmentSyntaxScope("for", 2, "Run"), out var second, out var detail),
            detail);
        Assert.True(second.Start.Value > first.Start.Value);
    }

    private static string writeTempCs()
    {
        var dir = Path.Combine(Path.GetTempPath(), nameof(AttachmentAnchorRoslynResolverTests) + "_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "Worker.cs");
        File.WriteAllText(path, SampleSource);
        return path;
    }
}

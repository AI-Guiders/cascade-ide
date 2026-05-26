#nullable enable

using CascadeIDE.Services.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AnchorDraftPreviewResolverTests
{
    [Fact]
    public void TryResolve_MemberOnly_InfersFileFromSidecar()
    {
        var dir = createWorkspace();
        seedMember(dir, "src/A.cs", "Foo.Bar", 10, 20);

        Assert.True(
            AnchorDraftPreviewResolver.TryResolve(
                "[M:Foo.Bar]",
                activeFilePath: null,
                workspaceRoot: dir,
                solutionPath: null,
                indexDirectoryRelative: null,
                out var preview,
                out var error),
            error);

        Assert.Equal(Path.Combine(dir, "src", "A.cs").Replace('\\', '/'), preview.AbsoluteFilePath.Replace('\\', '/'));
        Assert.Equal(10, preview.StartLine);
        Assert.Equal(20, preview.EndLine);
    }

    private static void seedMember(string workspace, string relativePath, string memberKey, int lineStart, int lineEnd)
    {
        var cache = IntercomAttachResolveCacheContext.From(workspace, null, relativePath);
        var absolute = Path.Combine(workspace, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        if (!File.Exists(absolute))
            File.WriteAllText(absolute, "// stub\n");

        var mtime = File.GetLastWriteTimeUtc(absolute).Ticks;
        IntercomSymbolLineIndex.ReplaceFileSymbols(
            cache,
            relativePath,
            mtime,
            [new IntercomSymbolLineEntry("docid", memberKey, lineStart, lineEnd)]);
    }

    private static string createWorkspace()
    {
        var dir = Path.Combine(Path.GetTempPath(), nameof(AnchorDraftPreviewResolverTests) + "_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}

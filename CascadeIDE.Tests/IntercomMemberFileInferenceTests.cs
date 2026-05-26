#nullable enable

using CascadeIDE.Services.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomMemberFileInferenceTests
{
    [Fact]
    public void TryResolveRelativeFile_UniqueMemberInSidecar_InfersFile()
    {
        var dir = createWorkspace();
        seedMember(dir, "src/A.cs", "Foo.Bar", 10, 20);
        seedMember(dir, "src/B.cs", "Other.Baz", 1, 5);

        Assert.True(
            IntercomMemberFileInference.TryResolveRelativeFile(
                explicitRelativeFile: null,
                memberKey: "Foo.Bar",
                activeFilePath: null,
                workspaceRoot: dir,
                solutionPath: null,
                indexDirectoryRelative: null,
                out var file,
                out var error),
            error);

        Assert.Equal("src/A.cs", file);
    }

    [Fact]
    public void TryResolveRelativeFile_AmbiguousMember_ReturnsError()
    {
        var dir = createWorkspace();
        seedMember(dir, "src/A.cs", "Run", 10, 20);
        seedMember(dir, "src/B.cs", "Run", 30, 40);

        Assert.False(
            IntercomMemberFileInference.TryResolveRelativeFile(
                null,
                "Run",
                activeFilePath: null,
                workspaceRoot: dir,
                solutionPath: null,
                indexDirectoryRelative: null,
                out _,
                out var error));

        Assert.Contains("нескольких файлах", error);
    }

    [Fact]
    public void TryResolveRelativeFile_ActiveFilePreferredWhenAlsoInIndex()
    {
        var dir = createWorkspace();
        seedMember(dir, "src/A.cs", "Run", 10, 20);
        seedMember(dir, "src/B.cs", "Run", 30, 40);
        var active = Path.Combine(dir, "src", "B.cs");

        Assert.True(
            IntercomMemberFileInference.TryResolveRelativeFile(
                null,
                "Run",
                active,
                dir,
                solutionPath: null,
                indexDirectoryRelative: null,
                out var file,
                out var error),
            error);

        Assert.Equal("src/B.cs", file);
    }

    [Fact]
    public void IntercomCodeRefParser_BareMember_ResolvesFileFromSidecar()
    {
        var dir = createWorkspace();
        seedMember(dir, "src/A.cs", "Foo.Bar", 10, 20);

        var editor = IntercomAttachmentResolveAtSend.EditorSnapshot.ForMcpBracketResolve(null);
        Assert.True(
            IntercomCodeRefParser.TryParse(
                "M:Foo.Bar",
                editor,
                dir,
                solutionPath: null,
                out var query,
                out var error,
                indexDirectoryRelative: null),
            error);

        Assert.Equal("src/A.cs", query.File.Replace('\\', '/'));
        Assert.Equal("Foo.Bar", query.MemberKey);
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
        var dir = Path.Combine(Path.GetTempPath(), nameof(IntercomMemberFileInferenceTests) + "_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}

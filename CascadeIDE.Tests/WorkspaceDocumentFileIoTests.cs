using CascadeIDE.Features.IdeMcp.Application;
using Xunit;
using CascadeIDE.Features.Workspace.DataAcquisition;

namespace CascadeIDE.Tests;

public sealed class WorkspaceDocumentFileIoTests
{
    [Fact]
    public void TryResolvePath_RejectsOutsideWorkspace()
    {
        var root = Path.GetTempPath();
        var outside = Path.GetFullPath(Path.Combine(root, "cide-outside-" + Guid.NewGuid().ToString("N"), "a.txt"));

        Assert.False(WorkspaceDocumentFileIo.TryResolvePath(root, [root], outside, out _, out var error));
        Assert.Contains("outside", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryReadText_ReturnsSlice()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cide-wsio-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "sample.txt");
        File.WriteAllText(file, "line1\nline2\nline3\n");

        try
        {
            Assert.True(WorkspaceDocumentFileIo.TryResolvePath(dir, [dir], file, out var full, out _));
            Assert.True(WorkspaceDocumentFileIo.TryReadText(full, offsetLine: 2, limitLines: 1, maxChars: null, out var json, out _));
            Assert.Contains("line2", json, StringComparison.Ordinal);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void TryReplaceTextRange_ReplacesMiddle()
    {
        const string source = "ab\ncd\n";
        Assert.True(IdeMcpEditorOrchestrator.TryReplaceTextRange(source, 2, 1, 2, 2, "X", out var updated));
        Assert.Equal("ab\nX\n", updated);
    }
}

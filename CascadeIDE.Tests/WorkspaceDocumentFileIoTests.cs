using CascadeIDE.Features.IdeMcp.Application;
using Xunit;
using CascadeIDE.Features.Workspace.DataAcquisition;

namespace CascadeIDE.Tests;

public sealed class WorkspaceDocumentFileIoTests
{
    [Fact]
    public void TryResolvePath_RejectsOutsideWorkspace()
    {
        var root = Path.Combine(Path.GetTempPath(), "cide-wsio-root-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var outside = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(root)!, "cide-outside-" + Guid.NewGuid().ToString("N"), "a.txt"));

        try
        {
            Assert.False(WorkspaceDocumentFileIo.TryResolvePath(root, [root], outside, out _, out var error));
            Assert.Contains("outside", error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
        }
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
        Assert.True(IdeMcpEditorOrchestrator.TryReplaceTextRange(source, 2, 1, 2, 3, "X", out var updated));
        Assert.Equal("ab\nX\n", updated);
    }
}

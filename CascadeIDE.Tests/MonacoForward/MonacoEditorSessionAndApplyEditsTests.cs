using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Features.IdeMcp.Application;
using Xunit;

namespace CascadeIDE.Tests.MonacoForward;

[Trait("Category", "MonacoForward")]
public sealed class MonacoEditorSessionAndApplyEditsTests
{
    [Fact]
    public void SessionState_applyInbound_updates_version_and_text()
    {
        var session = new MonacoEditorSessionState();
        session.Seed("old", version: 1);

        session.ApplyInbound(new CideEditorInboundMessage(
            CideEditorBusManifest.Editor.DidChange,
            Version: 2,
            Text: "new",
            CaretOffset: 3,
            SelectionStart: 1,
            SelectionLength: 2,
            RequestId: null,
            Line: null,
            Column: null,
            TopLine: null,
            LensId: null,
            FilePath: null,
            Error: null));

        session.ReadSnapshot(out var version, out var text, out var caret, out var selStart, out var selLen);
        Assert.Equal(2, version);
        Assert.Equal("new", text);
        Assert.Equal(3, caret);
        Assert.Equal(1, selStart);
        Assert.Equal(2, selLen);
    }

    [Fact]
    public void SessionState_version_mismatch_detected_for_stale_push()
    {
        var session = new MonacoEditorSessionState();
        session.Seed("v1", version: 5);
        session.ReadSnapshot(out var version, out _, out _, out _, out _);
        Assert.NotEqual(4, version);
        Assert.Equal(5, version);
    }

    [Fact]
    public void TryReplaceTextRange_mcp_edit_replaces_span()
    {
        const string text = "line1\nline2\nline3";
        Assert.True(
            IdeMcpEditorOrchestrator.TryReplaceTextRange(text, 2, 2, 2, 4, "XX", out var updated));
        Assert.Equal("line1\nlXXe2\nline3", updated);
    }

    [Fact]
    public void TryReplaceTextRange_invalid_range_returns_false()
    {
        Assert.False(
            IdeMcpEditorOrchestrator.TryReplaceTextRange("a", 9, 1, 9, 1, "b", out _));
    }
}

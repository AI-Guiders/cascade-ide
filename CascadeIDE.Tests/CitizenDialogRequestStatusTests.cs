#nullable enable
using CascadeIDE.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CitizenDialogRequestStatusTests
{
    [Fact]
    public void TryPaint_pending_running_done_error()
    {
        var pending = CitizenDialogRequestStatus.TryPaint(
            """{"schema":"citizen_dialog_request/v0","id":"abc123456789","status":"pending"}""");
        Assert.NotNull(pending);
        Assert.Equal("pending", pending!.Status);
        Assert.Contains("waiting habitat bridge", pending.StatusLine, StringComparison.Ordinal);

        var running = CitizenDialogRequestStatus.TryPaint(
            """{"id":"abc123456789","status":"running"}""");
        Assert.Contains("running", running!.StatusLine, StringComparison.Ordinal);

        var done = CitizenDialogRequestStatus.TryPaint(
            """{"id":"abc123456789","status":"done"}""");
        Assert.Contains("done", done!.StatusLine, StringComparison.Ordinal);
        Assert.Null(done.Peer);

        var err = CitizenDialogRequestStatus.TryPaint(
            """{"id":"abc123456789","status":"error","error":"boom"}""");
        Assert.Contains("error · boom", err!.StatusLine, StringComparison.Ordinal);

        var reconnect = CitizenDialogRequestStatus.TryPaint(
            """{"id":"abc123456789","status":"reconnecting","error":"reconnecting 1/3 · timeout"}""");
        Assert.Contains("reconnecting 1/3", reconnect!.StatusLine, StringComparison.Ordinal);
    }

    [Fact]
    public void TryPaint_done_surfaces_peer_ack_tip()
    {
        var done = CitizenDialogRequestStatus.TryPaint(
            """{"id":"abc123456789","status":"done","peer":"ok · gen=1 · mcp=live · compact=no · ack=1/1 · go=plan"}""");
        Assert.NotNull(done);
        Assert.Contains("ack=1/1", done!.Peer!, StringComparison.Ordinal);
        Assert.Contains("done · ok · gen=1", done.StatusLine, StringComparison.Ordinal);
        Assert.Contains("ack=1/1", done.StatusLine, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatLine_shortens_id()
    {
        var line = CitizenDialogRequestStatus.FormatLine("abcdefghijkl", "pending", null);
        Assert.StartsWith("glass · citizen · queued abcdefgh", line, StringComparison.Ordinal);
        Assert.DoesNotContain("abcdefghijkl", line, StringComparison.Ordinal);
    }
}

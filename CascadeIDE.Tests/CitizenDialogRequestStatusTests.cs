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

        var err = CitizenDialogRequestStatus.TryPaint(
            """{"id":"abc123456789","status":"error","error":"boom"}""");
        Assert.Contains("error · boom", err!.StatusLine, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatLine_shortens_id()
    {
        var line = CitizenDialogRequestStatus.FormatLine("abcdefghijkl", "pending", null);
        Assert.StartsWith("glass · citizen · queued abcdefgh", line, StringComparison.Ordinal);
        Assert.DoesNotContain("abcdefghijkl", line, StringComparison.Ordinal);
    }
}

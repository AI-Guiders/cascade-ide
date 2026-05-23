#nullable enable

using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomAttachmentAnchorRefresherTests
{
    [Fact]
    public void NeedsRefresh_member_not_found_true()
    {
        var anchor = new AttachmentAnchor
        {
            File = "Foo.cs",
            MemberKey = "Run",
            ResolveOutcome = IntercomAttachmentRevealPlan.OutcomeMemberNotFound,
        };
        Assert.True(IntercomAttachmentAnchorRefresher.NeedsRefresh(anchor));
    }

    [Fact]
    public void NeedsRefresh_resolved_false()
    {
        var anchor = new AttachmentAnchor
        {
            File = "Foo.cs",
            MemberKey = "Run",
            ResolveOutcome = IntercomAttachmentRevealPlan.OutcomeResolved,
        };
        Assert.False(IntercomAttachmentAnchorRefresher.NeedsRefresh(anchor));
    }

    [Fact]
    public void Refresh_updates_outcome_when_member_resolves()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cascade-ide-attach-refresh-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var file = Path.Combine(dir, "Worker.cs");
            File.WriteAllText(
                file,
                """
                namespace Sample;
                public class Worker {
                  public void Run() { }
                }
                """);

            var anchor = new AttachmentAnchor
            {
                File = "Worker.cs",
                MemberKey = "Run",
                ResolveOutcome = IntercomAttachmentRevealPlan.OutcomeMemberNotFound,
            };

            var refreshed = IntercomAttachmentAnchorRefresher.Refresh(anchor, dir, null);
            Assert.Equal(IntercomAttachmentRevealPlan.OutcomeResolved, refreshed.ResolveOutcome);
            Assert.Equal(3, refreshed.LineStart);
            Assert.Equal(3, refreshed.LineEnd);
        }
        finally
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}

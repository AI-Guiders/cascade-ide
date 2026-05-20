#nullable enable

using CascadeIDE.Features.Chat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomAttachmentMessageBuilderTests
{
    [Fact]
    public void TryBuild_replaces_bracket_with_marker_and_attachment()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), nameof(IntercomAttachmentMessageBuilderTests) + "_" + Guid.NewGuid().ToString("N"))).FullName;
        var file = Path.Combine(dir, "Foo.cs");
        File.WriteAllText(file, "class C { void Run() { } }");

        var editor = new IntercomAttachmentResolveAtSend.EditorSnapshot(
            file,
            File.ReadAllText(file),
            null,
            null,
            0);

        Assert.True(
            IntercomAttachmentMessageBuilder.TryBuild(
                "see [M:Run]",
                new Dictionary<string, AttachmentAnchor>(),
                editor,
                dir,
                null,
                out var outbound,
                out var err),
            err);

        Assert.Contains("⟦a:", outbound.Content, StringComparison.Ordinal);
        Assert.Single(outbound.Attachments);
        Assert.Equal("Run", outbound.Attachments[0].MemberKey);
        Assert.False(string.IsNullOrWhiteSpace(outbound.Attachments[0].Excerpt));
    }

    [Fact]
    public void TryBuild_fails_when_member_key_not_in_file()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), nameof(IntercomAttachmentMessageBuilderTests) + "_fail_" + Guid.NewGuid().ToString("N"))).FullName;
        var file = Path.Combine(dir, "Foo.cs");
        File.WriteAllText(file, "class C { void Other() { } }");

        var editor = new IntercomAttachmentResolveAtSend.EditorSnapshot(file, File.ReadAllText(file), null, null, 0);

        Assert.False(
            IntercomAttachmentMessageBuilder.TryBuild(
                "see [M:Missing]",
                new Dictionary<string, AttachmentAnchor>(),
                editor,
                dir,
                null,
                out _,
                out var err));
        Assert.Contains("Missing", err, StringComparison.Ordinal);
    }

    [Fact]
    public void TryPrepare_degraded_when_member_key_not_in_file()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), nameof(IntercomAttachmentMessageBuilderTests) + "_deg_" + Guid.NewGuid().ToString("N"))).FullName;
        var file = Path.Combine(dir, "Foo.cs");
        File.WriteAllText(file, "class C { void Other() { } }");

        var editor = new IntercomAttachmentResolveAtSend.EditorSnapshot(file, File.ReadAllText(file), null, null, 0);

        Assert.True(
            IntercomAttachmentMessageBuilder.TryPrepare(
                "see [M:Missing]",
                new Dictionary<string, AttachmentAnchor>(),
                editor,
                dir,
                null,
                out var prepared));

        Assert.Equal(IntercomMessagePrepareStatus.PartialSuccess, prepared.Status);
        Assert.True(prepared.IsCommittable);
        Assert.Contains("⟦a:", prepared.Outbound.Content, StringComparison.Ordinal);
        Assert.Single(prepared.Outbound.Attachments);
        Assert.Equal("Missing", prepared.Outbound.Attachments[0].MemberKey);
    }

    [Fact]
    public void TryPrepareForMcp_fast_path_preserves_L_without_excerpt_and_defers_M()
    {
        var dir = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), nameof(IntercomAttachmentMessageBuilderTests) + "_mcp_" + Guid.NewGuid().ToString("N")))
            .FullName;
        var file = Path.Combine(dir, "Foo.cs");
        File.WriteAllText(
            file,
            """
            namespace Sample;
            public class C {
              public void Run() { }
            }
            """);

        var rel = "Foo.cs";
        Assert.True(
            IntercomAttachmentMessageBuilder.TryPrepareForMcp(
                $"line [F:{rel}; L:2-3] member [M:Run]",
                new Dictionary<string, AttachmentAnchor>(),
                IntercomAttachmentResolveAtSend.EditorSnapshot.ForMcpBracketResolve(file),
                dir,
                null,
                out var prepared));
        Assert.True(prepared.IsCommittable);
        Assert.Equal(2, prepared.Outbound.Attachments.Count);

        var lineAnchor = prepared.Outbound.Attachments[0];
        Assert.Equal(2, lineAnchor.LineStart);
        Assert.Equal(3, lineAnchor.LineEnd);
        Assert.Null(lineAnchor.Excerpt);

        var memberAnchor = prepared.Outbound.Attachments[1];
        Assert.Equal("Run", memberAnchor.MemberKey);
        Assert.Equal(3, memberAnchor.LineStart);
        Assert.Equal(3, memberAnchor.LineEnd);
        Assert.Equal(IntercomAttachmentRevealPlan.OutcomeResolved, memberAnchor.ResolveOutcome);
        Assert.Null(memberAnchor.Excerpt);
    }

    [Fact]
    public void SplitFeedSegments_splits_marker_into_attach_segment()
    {
        var anchors = new[]
        {
            new AttachmentAnchor { Id = "abcd1234", DisplayLabel = "Run" },
        };
        var segments = IntercomAttachmentMarkers.SplitFeedSegments("x ⟦a:abcd1234⟧ y", anchors);
        Assert.Equal(3, segments.Count);
        Assert.Equal(IntercomAttachmentFeedSegmentKind.Attachment, segments[1].Kind);
        Assert.Equal("[Run]", segments[1].Text);
    }
}

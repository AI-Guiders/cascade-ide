#nullable enable

using CascadeIDE.Features.Chat;
using CascadeIDE.Features.Intercom.Transport;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;
using System.Text.Json;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomMessageRangeRelatedTests
{
    [Fact]
    public void Project_MessageRangeRelated_RoundTripsAnchor()
    {
        var threadId = Guid.NewGuid();
        var anchor = new AttachmentAnchor
        {
            Id = "a1",
            AttachmentShape = "member",
            File = "src/Foo.cs",
            MemberKey = "M:Foo.Bar",
            LineStart = 10,
            LineEnd = 20,
        };
        var payload = new ChatHistoryMessageRangeRelatedPayload(
            threadId.ToString("N"),
            2,
            4,
            anchor,
            "slash");

        var events = new[]
        {
            new ChatHistoryEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                ChatHistoryEventKind.MessageRangeRelated,
                ChatHistoryJson.Serialize(payload)),
        };

        var relates = IntercomMessageRangeRelatedProjector.Project(events);
        Assert.Single(relates);
        Assert.Equal(threadId, relates[0].ThreadId);
        Assert.Equal(2, relates[0].StartOrdinal);
        Assert.Equal(4, relates[0].EndOrdinal);
        Assert.Single(relates[0].OrdinalSegments);
        Assert.Equal("M:Foo.Bar", relates[0].CodeRef.MemberKey);
    }

    [Fact]
    public void Project_DisjointOrdinalSegments_RoundTripsAllSegments()
    {
        var threadId = Guid.NewGuid();
        var anchor = new AttachmentAnchor { File = "src/Foo.cs", MemberKey = "M:Foo.Bar" };
        var payload = IntercomMessageRangeRelatedSupport.CreatePayload(
            threadId.ToString("N"),
            [
                new ChatHistoryMessageOrdinalSegment(2, 3),
                new ChatHistoryMessageOrdinalSegment(8, 10),
            ],
            anchor,
            "slash");

        var events = new[]
        {
            new ChatHistoryEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                ChatHistoryEventKind.MessageRangeRelated,
                ChatHistoryJson.Serialize(payload)),
        };

        var relates = IntercomMessageRangeRelatedProjector.Project(events);
        Assert.Single(relates);
        Assert.Equal(2, relates[0].OrdinalSegments.Count);
        Assert.Equal(2, relates[0].StartOrdinal);
        Assert.Equal(10, relates[0].EndOrdinal);
    }

    [Fact]
    public void BuildCombined_ExplicitRelate_FoundByMemberKey()
    {
        var messageId = Guid.NewGuid();
        var lane = new[]
        {
            new IntercomMessageCodeCorrespondenceProjector.LaneMessage(1, 0, messageId, []),
            new IntercomMessageCodeCorrespondenceProjector.LaneMessage(2, 1, Guid.NewGuid(), []),
            new IntercomMessageCodeCorrespondenceProjector.LaneMessage(3, 2, Guid.NewGuid(), []),
        };

        var anchor = new AttachmentAnchor
        {
            File = "src/Foo.cs",
            MemberKey = "M:Foo.Bar",
            LineStart = 1,
            LineEnd = 2,
            AttachmentShape = "member",
        };

        var relates = new[]
        {
            new IntercomMessageRangeRelatedProjector.ExplicitRelate(
                Guid.NewGuid(),
                2,
                3,
                [new ChatHistoryMessageOrdinalSegment(2, 3)],
                anchor,
                "slash"),
        };

        var entries = IntercomMessageCodeCorrespondenceProjector.BuildCombined(lane, relates);
        var query = IntercomCodeRefQuery.FromAnchor(new AttachmentAnchor
        {
            File = "src/Foo.cs",
            MemberKey = "M:Foo.Bar",
            LineStart = 99,
            LineEnd = 99,
        });
        var hits = IntercomMessageCodeCorrespondenceProjector.Find(entries, query, null);

        Assert.Equal(2, hits.Count);
        Assert.All(hits, h => Assert.Equal(IntercomMessageCodeCorrespondenceProjector.MatchKindExplicit, h.MatchKind));
        Assert.Equal(2, hits[0].Ordinal);
        Assert.Equal(3, hits[1].Ordinal);
    }

    [Fact]
    public void BuildCombined_DisjointExplicitRelate_MapsOnlySegmentOrdinals()
    {
        var lane = new[]
        {
            new IntercomMessageCodeCorrespondenceProjector.LaneMessage(1, 0, Guid.NewGuid(), []),
            new IntercomMessageCodeCorrespondenceProjector.LaneMessage(2, 1, Guid.NewGuid(), []),
            new IntercomMessageCodeCorrespondenceProjector.LaneMessage(3, 2, Guid.NewGuid(), []),
            new IntercomMessageCodeCorrespondenceProjector.LaneMessage(4, 3, Guid.NewGuid(), []),
            new IntercomMessageCodeCorrespondenceProjector.LaneMessage(5, 4, Guid.NewGuid(), []),
            new IntercomMessageCodeCorrespondenceProjector.LaneMessage(6, 5, Guid.NewGuid(), []),
            new IntercomMessageCodeCorrespondenceProjector.LaneMessage(7, 6, Guid.NewGuid(), []),
            new IntercomMessageCodeCorrespondenceProjector.LaneMessage(8, 7, Guid.NewGuid(), []),
            new IntercomMessageCodeCorrespondenceProjector.LaneMessage(9, 8, Guid.NewGuid(), []),
            new IntercomMessageCodeCorrespondenceProjector.LaneMessage(10, 9, Guid.NewGuid(), []),
        };

        var anchor = new AttachmentAnchor
        {
            File = "src/Foo.cs",
            MemberKey = "M:Foo.Bar",
            AttachmentShape = "member",
        };

        var relates = new[]
        {
            new IntercomMessageRangeRelatedProjector.ExplicitRelate(
                Guid.NewGuid(),
                2,
                9,
                [
                    new ChatHistoryMessageOrdinalSegment(2, 3),
                    new ChatHistoryMessageOrdinalSegment(8, 9),
                ],
                anchor,
                "slash"),
        };

        var entries = IntercomMessageCodeCorrespondenceProjector.BuildCombined(lane, relates);
        var query = IntercomCodeRefQuery.FromAnchor(new AttachmentAnchor
        {
            File = "src/Foo.cs",
            MemberKey = "M:Foo.Bar",
        });
        var hits = IntercomMessageCodeCorrespondenceProjector.Find(entries, query, null);

        Assert.Equal(4, hits.Count);
        Assert.Equal([2, 3, 8, 9], hits.Select(h => h.Ordinal).OrderBy(o => o));
        Assert.DoesNotContain(hits, h => h.Ordinal is 4 or 5 or 6 or 7);
    }

    [Theory]
    [InlineData("3:5 relate selection", 3, 5, "selection")]
    [InlineData("2 selection", 2, 2, "selection")]
    [InlineData("[3;5] [8;15] relate selection", 3, 5, "selection")]
    public void RelateArgs_ParseRangeAndCodeRef(string tail, int start, int end, string codeRef)
    {
        Assert.True(IntercomMessageRelateArgs.TryParse(tail, out var segments, out var code, out var err), err);
        Assert.Equal(codeRef, code);
        Assert.Contains(segments, s => s.Start == start && s.End == end);
    }

    [Fact]
    public void RelateArgs_Disjoint_ParseTwoSegments()
    {
        Assert.True(
            IntercomMessageRelateArgs.TryParse("[3;5] [8;15] relate selection", out var segments, out _, out var err),
            err);
        Assert.Equal(2, segments.Count);
        Assert.Equal(new ParametricIntRange(3, 5), segments[0]);
        Assert.Equal(new ParametricIntRange(8, 15), segments[1]);
    }

    [Fact]
    public void ResolveInput_MessageRelate()
    {
        const string line = "/intercom message 3:5 relate selection";
        Assert.True(SlashLineResolver.TryResolveSlashLine(line, out var resolved));
        Assert.Equal("/intercom message relate", resolved.CanonicalPath);
        Assert.Equal("3:5 selection", resolved.ArgTail);
        ChatSlashCatalogTestSupport.AssertResolves(line, "/intercom message relate", "3:5 selection");
        Assert.True(IntentSlashCatalog.TryGetRoute("/intercom message relate", out var route));
        Assert.Equal("message_relate", route.IntercomHandlerId);
    }
}

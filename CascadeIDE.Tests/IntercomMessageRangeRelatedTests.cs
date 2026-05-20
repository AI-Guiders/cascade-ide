#nullable enable

using CascadeIDE.Features.Chat;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;
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
        Assert.Equal("M:Foo.Bar", relates[0].CodeRef.MemberKey);
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
            new IntercomMessageRangeRelatedProjector.ExplicitRelate(Guid.NewGuid(), 2, 3, anchor, "slash"),
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

    [Theory]
    [InlineData("3:5 relate selection", 3, 5, "selection")]
    [InlineData("2 selection", 2, 2, "selection")]
    public void RelateArgs_ParseRangeAndCodeRef(string tail, int start, int end, string codeRef)
    {
        Assert.True(IntercomMessageRelateArgs.TryParse(tail, out var s, out var e, out var code, out var err), err);
        Assert.Equal(start, s);
        Assert.Equal(end, e);
        Assert.Equal(codeRef, code);
    }

    [Fact]
    public void Parse_MessageRelate_SubActionAndTail()
    {
        var parse = ChatSlashCommandParser.TryParse("/intercom message 3:5 relate selection");
        Assert.True(parse.IsSlashLine);
        Assert.Equal("message", parse.Action);
        Assert.Equal("relate", parse.SubAction);
        Assert.Equal("3:5 selection", parse.ArgsTail);

        Assert.True(IntercomSlashPathBuilder.TryBuildPath(parse, out var path));
        Assert.Equal("/intercom message relate", path);
        Assert.True(IntentSlashCatalog.TryGetRoute(path, out var route));
        Assert.Equal("message_relate", route.IntercomHandlerId);
    }
}

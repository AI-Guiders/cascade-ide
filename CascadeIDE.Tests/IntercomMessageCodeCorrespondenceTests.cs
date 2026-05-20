#nullable enable

using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomMessageCodeCorrespondenceTests
{
    [Fact]
    public void Find_OverlappingLineRanges_ReturnsOrdinal()
    {
        var lane = new[]
        {
            laneMessage(1, 0, attach("src/Foo.cs", 10, 20)),
            laneMessage(2, 1, attach("src/Other.cs", 1, 5)),
        };

        var entries = IntercomMessageCodeCorrespondenceProjector.BuildInferred(lane);
        var query = new IntercomCodeRefQuery("src/Foo.cs", 15, 18);
        var hits = IntercomMessageCodeCorrespondenceProjector.Find(entries, query, workspaceRoot: null);

        Assert.Single(hits);
        Assert.Equal(1, hits[0].Ordinal);
    }

    [Fact]
    public void Find_NonOverlapping_ReturnsEmpty()
    {
        var lane = new[] { laneMessage(1, 0, attach("src/Foo.cs", 10, 20)) };
        var entries = IntercomMessageCodeCorrespondenceProjector.BuildInferred(lane);
        var query = new IntercomCodeRefQuery("src/Foo.cs", 50, 60);
        var hits = IntercomMessageCodeCorrespondenceProjector.Find(entries, query, null);

        Assert.Empty(hits);
    }

    [Fact]
    public void Find_FileOnlyQuery_MatchesAnchorWithoutLines()
    {
        var lane = new[] { laneMessage(1, 0, new AttachmentAnchor { File = "src/Bar.cs" }) };
        var entries = IntercomMessageCodeCorrespondenceProjector.BuildInferred(lane);
        var query = new IntercomCodeRefQuery("src/Bar.cs", null, null);
        var hits = IntercomMessageCodeCorrespondenceProjector.Find(entries, query, null);

        Assert.Single(hits);
    }

    [Fact]
    public void Find_SameFileDifferentMemberKeys_MatchesByMemberOnly()
    {
        var lane = new[]
        {
            laneMessage(1, 0, attach("src/Foo.cs", 10, 20, "M:Foo.Bar")),
            laneMessage(2, 1, attach("src/Foo.cs", 10, 20, "M:Foo.Baz")),
        };

        var entries = IntercomMessageCodeCorrespondenceProjector.BuildInferred(lane);
        var query = IntercomCodeRefQuery.FromAnchor(new AttachmentAnchor
        {
            File = "src/Foo.cs",
            LineStart = 99,
            LineEnd = 99,
            MemberKey = "M:Foo.Bar",
        });
        var hits = IntercomMessageCodeCorrespondenceProjector.Find(entries, query, null);

        Assert.Single(hits);
        Assert.Equal(1, hits[0].Ordinal);
    }

    [Fact]
    public void TryParse_LineLiteral_RequiresActiveFile()
    {
        var editor = new IntercomAttachmentResolveAtSend.EditorSnapshot(null, "", null, null, null);
        Assert.False(IntercomCodeRefParser.TryParse("L:10-20", editor, null, null, out _, out var err));
        Assert.Contains("активного файла", err, StringComparison.Ordinal);
    }

    private static IntercomMessageCodeCorrespondenceProjector.LaneMessage laneMessage(
        int ordinal,
        int messageIndex,
        params AttachmentAnchor[] attachments) =>
        new(ordinal, messageIndex, Guid.NewGuid(), attachments);

    private static AttachmentAnchor attach(string file, int start, int end, string? memberKey = null) =>
        new()
        {
            File = file,
            LineStart = start,
            LineEnd = end,
            MemberKey = memberKey,
            AttachmentShape = memberKey is not null ? "member" : "text-range",
        };
}

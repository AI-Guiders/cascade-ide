using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ParametricSegmentListParserTests
{
    [Theory]
    [InlineData("5", 1, 5, 5)]
    [InlineData("5 10", 1, 5, 10)]
    [InlineData("5:10", 1, 5, 10)]
    [InlineData("5;10", 1, 5, 10)]
    public void TryParse_LegacySingleContiguous(string tail, int count, int start, int end)
    {
        Assert.True(ParametricSegmentListParser.TryParse(tail, out var segments, out var error), error);
        Assert.Equal(count, segments.Count);
        Assert.Equal(start, segments[0].Start);
        Assert.Equal(end, segments[0].End);
    }

    [Fact]
    public void TryParse_BracketDisjointSegments()
    {
        Assert.True(ParametricSegmentListParser.TryParse("[3;5] [8;15] [20]", out var segments, out var error), error);
        Assert.Equal(3, segments.Count);
        Assert.Equal(3, segments[0].Start);
        Assert.Equal(5, segments[0].End);
        Assert.Equal(8, segments[1].Start);
        Assert.Equal(15, segments[1].End);
        Assert.Equal(20, segments[2].Start);
        Assert.Equal(20, segments[2].End);
    }

    [Fact]
    public void TryParse_BracketSingleLine()
    {
        Assert.True(ParametricSegmentListParser.TryParse("[20]", out var segments, out _));
        Assert.Single(segments);
        Assert.Equal(20, segments[0].Start);
        Assert.Equal(20, segments[0].End);
    }

    [Fact]
    public void TryParse_RejectsLegacyMixedWithBrackets()
    {
        Assert.False(ParametricSegmentListParser.TryParse("3:5 [8;15]", out _, out var error));
        Assert.Contains("скобках", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParseSingleContiguous_RejectsDisjoint()
    {
        Assert.False(ParametricSegmentListParser.TryParseSingleContiguous("[3;5] [8;15]", out _, out var error));
        Assert.Contains("disjoint", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryParseLineRangeTail_RejectsThreeTokens_Still()
    {
        Assert.False(ChatSlashParametricArgsBuilder.TryParseLineRangeTail("3 5 7", out _, out _, out var error));
        Assert.Contains("Ожидается", error);
    }

    [Fact]
    public void FormatSummary_MultiSegment()
    {
        var segments = new[]
        {
            new ParametricIntRange(3, 5),
            new ParametricIntRange(8, 15),
            new ParametricIntRange(20, 20),
        };
        var summary = ParametricSegmentListParser.FormatSummary(segments, "Строки");
        Assert.Contains("3–5", summary);
        Assert.Contains("8–15", summary);
        Assert.Contains("20", summary);
        Assert.Contains("12", summary);
        Assert.Contains("строк", summary);
    }

    [Fact]
    public void SlashCommandPreviewEvaluator_MessageSelect()
    {
        Assert.True(SlashCommandPreviewEvaluator.TryEvaluateSummary(
            "/intercom message select [3;5] [8;15] [20]",
            out var summary));
        Assert.Contains("Сообщения", summary);
        Assert.Contains("3–5", summary);
    }
}

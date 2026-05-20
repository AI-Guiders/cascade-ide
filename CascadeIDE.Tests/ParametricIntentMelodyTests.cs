using CascadeIDE.Models.Editor;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ParametricIntentMelodyTests
{
    [Theory]
    [InlineData("wai:", true, "")]
    [InlineData("wai:google.com", true, "google.com")]
    [InlineData("wai:https://host/p", true, "https://host/p")]
    [InlineData("wa", false, null)]
    [InlineData("wai", true, "")]
    public void TryParseWebAiPortalMelodyTail_PrefixAndPayload(string tail, bool expectedOk, string? expectedPayload)
    {
        var ok = ParametricIntentMelody.TryParseWebAiPortalMelodyTail(tail, out var slugOut, out var payload);
        Assert.Equal(expectedOk, ok);
        Assert.Equal(expectedPayload, ok ? payload : null);
        if (ok)
            Assert.Equal("wai", slugOut);
    }

    [Fact]
    public void TryParseLineRangeTail_Parses_EditorLineAliases()
    {
        Assert.True(ParametricIntentMelody.TryParseLineRangeTail("els:5:15", out var parsed));
        Assert.NotNull(parsed);
        Assert.Equal("els", parsed!.Alias);
        Assert.Equal(5, parsed.Lines.Start.Value);
        Assert.Equal(15, parsed.Lines.End.Value);
    }

    [Fact]
    public void TryParseLineRangeTail_Normalizes_space_separated_three_tokens()
    {
        Assert.True(ParametricIntentMelody.TryParseLineRangeTail("els 5 15", out var parsed));
        Assert.NotNull(parsed);
        Assert.Equal("els", parsed!.Alias);
        Assert.Equal("els:5:15", parsed.DisplayTail);
        Assert.Equal(5, parsed.Lines.Start.Value);
        Assert.Equal(15, parsed.Lines.End.Value);
    }

    [Fact]
    public void TryBuildExecutionArgs_ForSelect_UsesFullLineColumns()
    {
        var parsed = ParsedLineRange("els", "els:2:3", 2, 3);
        var text = "one\ntwo\nthree\nfour";

        var ok = ParametricIntentMelody.TryBuildExecutionArgs(
            parsed,
            @"D:\repo\file.txt",
            text,
            out var commandId,
            out var argsJson,
            out var error);

        Assert.True(ok, error);
        Assert.Equal(IdeCommands.Select, commandId);
        Assert.Contains(@"""start_line"":2", argsJson, StringComparison.Ordinal);
        Assert.Contains(@"""start_column"":1", argsJson, StringComparison.Ordinal);
        Assert.Contains(@"""end_line"":3", argsJson, StringComparison.Ordinal);
        Assert.Contains(@"""end_column"":6", argsJson, StringComparison.Ordinal);
    }

    [Fact]
    public void TryBuildExecutionArgs_ForDelete_RemovesWholeLines()
    {
        var parsed = ParsedLineRange("eld", "eld:2:3", 2, 3);
        var text = "one\ntwo\nthree\nfour";

        var ok = ParametricIntentMelody.TryBuildExecutionArgs(
            parsed,
            @"D:\repo\file.txt",
            text,
            out var commandId,
            out var argsJson,
            out var error);

        Assert.True(ok, error);
        Assert.Equal(IdeCommands.ApplyEdit, commandId);
        Assert.Contains(@"""start_line"":2", argsJson, StringComparison.Ordinal);
        Assert.Contains(@"""start_column"":1", argsJson, StringComparison.Ordinal);
        Assert.Contains(@"""end_line"":4", argsJson, StringComparison.Ordinal);
        Assert.Contains(@"""end_column"":1", argsJson, StringComparison.Ordinal);
        Assert.Contains(@"""new_text"":""""", argsJson, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParseLineRangeTail_Canonicalizes_ReversedBounds_ToSameRange()
    {
        Assert.True(ParametricIntentMelody.TryParseLineRangeTail("els:7:3", out var reversed));
        Assert.True(ParametricIntentMelody.TryParseLineRangeTail("els:3:7", out var forward));
        Assert.NotNull(reversed);
        Assert.NotNull(forward);
        Assert.Equal(forward!.Lines, reversed!.Lines);
        Assert.Equal(3, reversed.Lines.Start.Value);
        Assert.Equal(7, reversed.Lines.End.Value);
    }

    [Theory]
    [InlineData("els;7")]
    [InlineData("els:7")]
    [InlineData("els 7")]
    [InlineData("eld;7")]
    [InlineData("eld:7")]
    [InlineData("eld 7")]
    public void TryParseLineRangeTail_SingleLineNumber_IsInclusiveOneLineRange(string tail)
    {
        Assert.True(ParametricIntentMelody.TryParseLineRangeTail(tail, out var parsed), tail);
        Assert.NotNull(parsed);
        Assert.Equal(7, parsed!.Lines.Start.Value);
        Assert.Equal(7, parsed.Lines.End.Value);
    }

    [Theory]
    [InlineData("esc:", true, "")]
    [InlineData("esc:[m:run]", true, "[m:run]")]
    [InlineData("erc:[f:foo.cs; m:bar]", true, "[f:foo.cs; m:bar]")]
    [InlineData("er", false, null)]
    public void TryParseBracketCodeRefMelodyTail_PrefixAndPayload(string tail, bool expectedOk, string? expectedPayload)
    {
        var ok = ParametricIntentMelody.TryParseBracketCodeRefMelodyTail(tail, out var slugOut, out var payload);
        Assert.Equal(expectedOk, ok);
        Assert.Equal(expectedPayload, ok ? payload : null);
        if (ok)
        {
            var expectedSlug = tail.StartsWith("erc", StringComparison.Ordinal) ? "erc" : "esc";
            Assert.Equal(expectedSlug, slugOut);
        }
    }

    [Fact]
    public void TryResolveParametricExecution_BracketSelect_BuildsEditorSelectCodeArgs()
    {
        var ok = ParametricIntentMelody.TryResolveParametricExecution(
            "esc:[M:Run]",
            @"D:\repo\src\Foo.cs",
            "",
            out var commandId,
            out var argsJson,
            out _);

        Assert.True(ok);
        Assert.Equal(IdeCommands.EditorSelectCode, commandId);
        Assert.Contains(@"""code_ref"":""[M:Run]""", argsJson, StringComparison.Ordinal);
        Assert.Contains("Foo.cs", argsJson!, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("e", true)]
    [InlineData("es", true)]
    [InlineData("esc", true)]
    [InlineData("esc:", true)]
    [InlineData("esc:[", true)]
    [InlineData("erc:[m:run", true)]
    [InlineData("ex", false)]
    public void IsParametricChordTailPrefix_MatchesBracketMelodyTails(string tail, bool expected) =>
        Assert.Equal(expected, ParametricIntentMelody.IsParametricChordTailPrefix(tail));

    [Theory]
    [InlineData("w", true)]
    [InlineData("wa", true)]
    [InlineData("wai", true)]
    [InlineData("wai:", true)]
    [InlineData("wai:g", true)]
    [InlineData("wag", false)]
    [InlineData("el", true)]
    [InlineData("els", true)]
    [InlineData("els:", true)]
    [InlineData("els;", true)]
    [InlineData("els:5", true)]
    [InlineData("els;5", true)]
    [InlineData("els 7", true)]
    [InlineData("els:5:", true)]
    [InlineData("els;5;", true)]
    [InlineData("els:5:10", true)]
    [InlineData("els;5;10", true)]
    [InlineData("els:5:x", false)]
    public void IsParametricChordTailPrefix_MatchesChordBuildingTails(string tail, bool expected) =>
        Assert.Equal(expected, ParametricIntentMelody.IsParametricChordTailPrefix(tail));

    [Theory]
    [InlineData("wai", true)]
    [InlineData("els", true)]
    [InlineData("eld", true)]
    [InlineData("so", false)]
    [InlineData("gs", false)]
    public void IsParametricMelodyBaseAlias_RecognizesParametricRoots(string alias, bool expected) =>
        Assert.Equal(expected, ParametricIntentMelody.IsParametricMelodyBaseAlias(alias));

    [Fact]
    public void BuildAliasUsageHint_reads_palette_usage_hint_from_catalog()
    {
        var h = ParametricIntentMelody.BuildAliasUsageHint("els");
        Assert.Contains("els", h, StringComparison.Ordinal);
        Assert.Contains(";", h, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildAliasUsageCategory_resolves_palette_hint_slug_to_root()
    {
        Assert.Equal("Web AI Portal", ParametricIntentMelody.BuildAliasUsageCategory("wai-url"));
        Assert.Equal("Web AI Portal", ParametricIntentMelody.BuildAliasUsageCategory("wai"));
    }

    [Fact]
    public void BuildAliasUsageHint_unknown_slug_falls_back_to_template() =>
        Assert.Equal("c:zzz:<start>:<end>", ParametricIntentMelody.BuildAliasUsageHint("zzz"));

    private static ParametricIntentMelody.ParsedLineRange ParsedLineRange(string alias, string display, int start, int end)
    {
        Assert.True(LineNumber.TryCreate(start, out var s));
        Assert.True(LineNumber.TryCreate(end, out var e));
        Assert.True(LineRange.TryCreate(s, e, out var lines));
        return new ParametricIntentMelody.ParsedLineRange(alias, display, lines);
    }
}

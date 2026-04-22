using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ParametricIntentMelodyTests
{
    [Fact]
    public void TryParseLineRangeTail_Parses_EditorLineAliases()
    {
        Assert.True(ParametricIntentMelody.TryParseLineRangeTail("els:5:15", out var parsed));
        Assert.NotNull(parsed);
        Assert.Equal("els", parsed!.Alias);
        Assert.Equal(5, parsed.StartLine);
        Assert.Equal(15, parsed.EndLine);
    }

    [Fact]
    public void TryBuildExecutionArgs_ForSelect_UsesFullLineColumns()
    {
        var parsed = new ParametricIntentMelody.ParsedLineRange("els", "els:2:3", 2, 3);
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
        var parsed = new ParametricIntentMelody.ParsedLineRange("eld", "eld:2:3", 2, 3);
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
    public void TryBuildExecutionArgs_Rejects_InvertedRange()
    {
        var parsed = new ParametricIntentMelody.ParsedLineRange("els", "els:7:3", 7, 3);

        var ok = ParametricIntentMelody.TryBuildExecutionArgs(
            parsed,
            @"D:\repo\file.txt",
            "one\ntwo\nthree",
            out _,
            out _,
            out var error);

        Assert.False(ok);
        Assert.Contains("startLine", error, StringComparison.OrdinalIgnoreCase);
    }
}

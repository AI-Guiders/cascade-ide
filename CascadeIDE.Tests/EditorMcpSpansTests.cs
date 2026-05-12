using System.Text.Json;
using CascadeIDE.Models.Editor;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class EditorMcpSpansTests
{
    [Fact]
    public void EditorTextSpan_TryParse_AcceptsValidRectangularSpan()
    {
        var fp = CanonicalFilePath.Normalize(Path.Combine(Path.GetTempPath(), nameof(EditorMcpSpansTests), "sample.txt"));
        var args = Args(
            ("file_path", JsonSerializer.SerializeToElement(fp)),
            ("start_line", JsonSerializer.SerializeToElement(2)),
            ("start_column", JsonSerializer.SerializeToElement(1)),
            ("end_line", JsonSerializer.SerializeToElement(4)),
            ("end_column", JsonSerializer.SerializeToElement(10)));

        Assert.True(EditorTextSpan.TryParse(args, out var span, out var error), error);
        Assert.Equal(fp, span.File.Value, ignoreCase: true);
        Assert.Equal(2, span.StartLine.Value);
        Assert.Equal(1, span.StartColumn.Value);
        Assert.Equal(4, span.EndLine.Value);
        Assert.Equal(10, span.EndColumn.Value);
    }

    [Fact]
    public void EditorTextSpan_TryParse_RejectsZeroColumn()
    {
        var fp = @"D:\a\b\c.txt";
        var args = Args(
            ("file_path", JsonSerializer.SerializeToElement(fp)),
            ("start_line", JsonSerializer.SerializeToElement(1)),
            ("start_column", JsonSerializer.SerializeToElement(0)),
            ("end_line", JsonSerializer.SerializeToElement(1)),
            ("end_column", JsonSerializer.SerializeToElement(5)));

        Assert.False(EditorTextSpan.TryParse(args, out _, out var err));
        Assert.Contains("start_column", err, StringComparison.Ordinal);
    }

    [Fact]
    public void EditorTextSpan_TryParse_RejectsInvertedColumnsOnSameLine()
    {
        var fp = @"D:\a\b\c.txt";
        var args = Args(
            ("file_path", JsonSerializer.SerializeToElement(fp)),
            ("start_line", JsonSerializer.SerializeToElement(2)),
            ("start_column", JsonSerializer.SerializeToElement(8)),
            ("end_line", JsonSerializer.SerializeToElement(2)),
            ("end_column", JsonSerializer.SerializeToElement(3)));

        Assert.False(EditorTextSpan.TryParse(args, out _, out var err));
        Assert.Contains("end_column", err, StringComparison.Ordinal);
    }

    [Fact]
    public void EditorTextSpan_TryParse_RejectsEndLineBeforeStartLine()
    {
        var fp = @"D:\a\b\c.txt";
        var args = Args(
            ("file_path", JsonSerializer.SerializeToElement(fp)),
            ("start_line", JsonSerializer.SerializeToElement(5)),
            ("start_column", JsonSerializer.SerializeToElement(1)),
            ("end_line", JsonSerializer.SerializeToElement(2)),
            ("end_column", JsonSerializer.SerializeToElement(5)));

        Assert.False(EditorTextSpan.TryParse(args, out _, out var err));
        Assert.Contains("end_line не может быть меньше start_line", err, StringComparison.Ordinal);
    }

    [Fact]
    public void EditorContentLineRange_TryParse_DefaultsToSingleLineOne()
    {
        Assert.True(EditorContentLineRangeMcpArgs.TryParse(null, out var lines, out var err), err);
        Assert.Equal(1, lines.Start.Value);
        Assert.Equal(1, lines.End.Value);
    }

    [Fact]
    public void EditorContentLineRange_TryParse_RejectsInvertedRangeWhenExplicit()
    {
        var args = Args(
            ("start_line", JsonSerializer.SerializeToElement(8)),
            ("end_line", JsonSerializer.SerializeToElement(3)));
        Assert.False(EditorContentLineRangeMcpArgs.TryParse(args, out _, out var err));
        Assert.Contains("end_line не может быть меньше", err, StringComparison.Ordinal);
    }

    [Fact]
    public void EditorGoToPosition_TryParse_OptionalEndColumns()
    {
        var fp = @"D:\p\f.cs";
        var args = Args(
            ("file_path", JsonSerializer.SerializeToElement(fp)),
            ("line", JsonSerializer.SerializeToElement(2)),
            ("column", JsonSerializer.SerializeToElement(12)),
            ("end_column", JsonSerializer.SerializeToElement(18)));

        Assert.True(EditorGoToPositionMcpArgs.TryParse(args, out var doc, out var line, out var col, out var el, out var ec, out var err), err);
        Assert.Equal(CanonicalFilePath.Normalize(fp), doc.Value, ignoreCase: true);
        Assert.Equal(2, line.Value);
        Assert.Equal(12, col.Value);
        Assert.Null(el);
        Assert.NotNull(ec);
        Assert.Equal(18, ec!.Value.Value);
    }

    private static Dictionary<string, JsonElement> Args(params (string key, JsonElement el)[] pairs)
    {
        var d = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var (key, el) in pairs)
            d[key] = el;
        return d;
    }
}

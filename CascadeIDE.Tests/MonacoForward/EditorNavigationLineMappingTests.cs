using CascadeIDE.Features.Editor.Application.Monaco;
using Xunit;

namespace CascadeIDE.Tests.MonacoForward;

[Trait("Category", "MonacoForward")]
public sealed class EditorNavigationLineMappingTests
{
    [Fact]
    public void SelectionOffsetsFromLines_single_line_range()
    {
        var text = "abcdef";
        var (start, length) = EditorNavigationLineMapping.SelectionOffsetsFromLines(
            text, startLine: 1, startColumn: 2, endLine: 1, endColumn: 4);
        Assert.Equal(1, start);
        Assert.Equal(2, length);
    }

    [Fact]
    public void SelectionOffsetsFromLines_multiline_uses_end_of_line_overflow()
    {
        var text = "aa\nbb\ncc";
        var (start, length) = EditorNavigationLineMapping.SelectionOffsetsFromLines(
            text, startLine: 1, startColumn: 1, endLine: 2, endColumn: null);
        Assert.Equal(0, start);
        Assert.Equal(5, length);
    }

    [Fact]
    public void OffsetFromLineColumn_second_line_first_column()
    {
        var text = "a\nbc";
        Assert.Equal(2, EditorNavigationLineMapping.OffsetFromLineColumn(text, lineOneBased: 2, columnOneBased: 1));
    }
}

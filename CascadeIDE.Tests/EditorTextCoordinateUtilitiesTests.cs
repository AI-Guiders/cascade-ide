using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public class EditorTextCoordinateUtilitiesTests
{
    [Fact]
    public void LineColumnToOffset_FirstLineFirstColumn_ReturnsZero()
    {
        Assert.Equal(0, EditorTextCoordinateUtilities.LineColumnToOffset("abc", 1, 1));
    }

    [Fact]
    public void LineColumnToOffset_SecondLine_FirstColumn_AccountsForNewline()
    {
        var text = "a\nbc";
        Assert.Equal(2, EditorTextCoordinateUtilities.LineColumnToOffset(text, 2, 1));
    }

    [Fact]
    public void LineColumnToOffset_ColumnPastLineEnd_ClampsToEndPlusOne()
    {
        // line "ab" has length 2; column is clamped to lineLen+1 → offset after the line
        Assert.Equal(2, EditorTextCoordinateUtilities.LineColumnToOffset("ab", 1, 3));
        Assert.Equal(2, EditorTextCoordinateUtilities.LineColumnToOffset("ab", 1, 100));
    }

    [Fact]
    public void LineColumnToOffset_InvalidLineOrColumn_ReturnsMinusOne()
    {
        Assert.Equal(-1, EditorTextCoordinateUtilities.LineColumnToOffset("a", 0, 1));
        Assert.Equal(-1, EditorTextCoordinateUtilities.LineColumnToOffset("a", 1, 0));
        Assert.Equal(-1, EditorTextCoordinateUtilities.LineColumnToOffset("a", 2, 1));
    }

    [Fact]
    public void PathsReferToSameFile_SamePathDifferentCase_WhenFullPathWorks()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cascade_path_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var a = Path.Combine(dir, "file.txt");
            File.WriteAllText(a, "x");
            var b = Path.Combine(dir.ToUpperInvariant(), "FILE.TXT");
            Assert.True(EditorTextCoordinateUtilities.PathsReferToSameFile(a, b));
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void PathsReferToSameFile_DifferentPaths_ReturnsFalse()
    {
        Assert.False(EditorTextCoordinateUtilities.PathsReferToSameFile(
            Path.Combine("a", "x.txt"),
            Path.Combine("b", "x.txt")));
    }
}

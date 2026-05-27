using CascadeIDE.Features.WorkspaceNavigation.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceNavigationMapControlFlowCursorTests
{
    [Fact]
    public void ResolveControlFlowCursorForRefresh_uses_editor_caret_when_anchor_is_current_file()
    {
        const string source = """
class Demo
{
    void A()
    {
        B();
    }

    void B() { }
}
""";
        var offset = source.IndexOf("B();", StringComparison.Ordinal);
        var (line, column) = WorkspaceNavigationMapOrchestrator.ResolveControlFlowCursorForRefresh(
            navigationPath: @"D:\w\Demo.cs",
            currentPath: @"D:\w\Demo.cs",
            sourceText: source,
            caretOrSelectionOffset: offset);

        Assert.True(line > 1);
        Assert.True(column > 1);
    }

    [Fact]
    public void ResolveControlFlowCursorForRefresh_prefers_graph_navigate_line_over_caret()
    {
        const string source = """
class Demo
{
    void A()
    {
        B();
    }

    void B() { }
}
""";
        var caretInA = source.IndexOf("void A", StringComparison.Ordinal);
        const int lineInB = 9;
        var (line, column) = WorkspaceNavigationMapOrchestrator.ResolveControlFlowCursorForRefresh(
            navigationPath: @"D:\w\Demo.cs",
            currentPath: @"D:\w\Demo.cs",
            sourceText: source,
            caretOrSelectionOffset: caretInA,
            navigateToLine: lineInB,
            navigateToColumn: 1);

        Assert.Equal(lineInB, line);
        Assert.Equal(1, column);
    }

    [Fact]
    public void TryOffsetForLine_maps_one_based_line_to_offset()
    {
        const string source = "a\nbb\nccc";
        Assert.Equal(0, WorkspaceNavigationMapOrchestrator.TryOffsetForLine(source, 1));
        Assert.Equal(2, WorkspaceNavigationMapOrchestrator.TryOffsetForLine(source, 2));
        Assert.Equal(5, WorkspaceNavigationMapOrchestrator.TryOffsetForLine(source, 3));
    }

    [Fact]
    public void ResolveControlFlowCursorForRefresh_returns_null_when_navigation_path_not_current_editor()
    {
        const string source = """
class Demo
{
    void Entry() { Run(); }
    void Run() { }
}
""";
        var (line, column) = WorkspaceNavigationMapOrchestrator.ResolveControlFlowCursorForRefresh(
            navigationPath: @"D:\w\Program.cs",
            currentPath: @"D:\w\Other.cs",
            sourceText: source,
            caretOrSelectionOffset: 999);

        Assert.Null(line);
        Assert.Null(column);
    }
}

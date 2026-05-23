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
            anchorPath: @"D:\w\Demo.cs",
            currentPath: @"D:\w\Demo.cs",
            sourceText: source,
            caretOrSelectionOffset: offset);

        Assert.True(line > 1);
        Assert.True(column > 1);
    }

    [Fact]
    public void ResolveControlFlowCursorForRefresh_uses_first_method_when_anchor_differs_from_editor()
    {
        const string source = """
class Demo
{
    void Entry()
    {
        Run();
    }

    void Run() { }
}
""";
        var (line, column) = WorkspaceNavigationMapOrchestrator.ResolveControlFlowCursorForRefresh(
            anchorPath: @"D:\w\Program.cs",
            currentPath: @"D:\w\Other.cs",
            sourceText: source,
            caretOrSelectionOffset: 999);

        Assert.Equal(3, line);
        Assert.Equal(10, column);
    }
}

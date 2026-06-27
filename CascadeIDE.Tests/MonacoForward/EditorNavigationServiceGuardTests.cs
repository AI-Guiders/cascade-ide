using CascadeIDE.Features.Editor.Application.Monaco;
using Xunit;

namespace CascadeIDE.Tests.MonacoForward;

[Trait("Category", "MonacoForward")]
public sealed class EditorNavigationServiceGuardTests
{
    [Fact]
    public void TryNavigateReveal_empty_path_returns_false()
    {
        var vm = new ViewModels.MainWindowViewModel();
        var nav = new EditorNavigationService(vm);
        Assert.False(nav.TryNavigateReveal("", startLine: 1, endLine: 1, durationMs: null));
        Assert.False(nav.TryNavigateGoTo(null, line: 1, column: 1));
    }

    [Fact]
    public void TryNavigateGoTo_valid_path_returns_true_without_waiting()
    {
        var vm = new ViewModels.MainWindowViewModel();
        var nav = new EditorNavigationService(vm);
        Assert.True(nav.TryNavigateGoTo(@"D:\Fake\File.cs", line: 10, column: 3));
    }
}

using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MarkdownPreviewRoutingTests
{
    [AvaloniaFact]
    public void ShowMarkdownPreviewPageCommand_NavigatesToMarkdownPreviewPage()
    {
        var vm = new MainWindowViewModel
        {
            CurrentFilePath = "note.md",
            EditorText = "# Title"
        };

        vm.ShowMarkdownPreviewPageCommand.Execute(null);

        Assert.True(vm.IsMfdRegionExpanded);
        Assert.Equal(MfdShellPage.MarkdownPreview, vm.CurrentMfdShellPage);
        Assert.Equal("note.md", vm.MarkdownPreviewTool.Title);
    }

    [AvaloniaFact]
    public async Task IdeMcpAction_ShowEditorPreview_RoutesToMarkdownPreviewPage()
    {
        var vm = new MainWindowViewModel
        {
            CurrentFilePath = "note.md",
            EditorText = "# Title"
        };

        vm.IdeMcp.ShowEditorPreview();
        await Dispatcher.UIThread.InvokeAsync(() => { });

        Assert.Equal(MfdShellPage.MarkdownPreview, vm.CurrentMfdShellPage);
    }
}

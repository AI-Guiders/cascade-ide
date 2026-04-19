using Avalonia.Headless.XUnit;
using Avalonia.Controls;
using CascadeIDE.ViewModels;
using CascadeIDE.Views;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ViewLocatorTests
{
    [AvaloniaFact]
    public void Build_DockDocumentViewModel_ReturnsDockDocumentView()
    {
        var locator = new ViewLocator();
        var vm = new DockDocumentViewModel(new OpenDocumentViewModel("a.cs", "a.cs", "class A {}"));

        var control = locator.Build(vm);

        Assert.IsType<DockDocumentView>(control);
    }

    [AvaloniaFact]
    public void Build_MarkdownDockDocumentViewModel_DoesNotRequireInlinePreviewHost()
    {
        var locator = new ViewLocator();
        var vm = new DockDocumentViewModel(new OpenDocumentViewModel("note.md", "note.md", "# Title"));

        var control = Assert.IsType<DockDocumentView>(locator.Build(vm));

        Assert.Null(control.FindControl<ContentControl>("InlineMarkdownPreviewHost"));
    }
}

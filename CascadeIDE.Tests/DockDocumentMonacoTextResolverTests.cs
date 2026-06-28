using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class DockDocumentMonacoTextResolverTests
{
    [Fact]
    public void InactiveTab_UsesDocumentContent_NotSharedEditorText()
    {
        var doc = new OpenDocumentViewModel(@"C:\a\B.cs", "B.cs", "class B {}");
        var dock = new DockDocumentViewModel(doc);
        var vm = new MainWindowViewModel
        {
            CurrentFilePath = @"C:\a\A.cs",
            EditorText = "class A {}",
        };

        var text = DockDocumentMonacoTextResolver.Resolve(
            isActive: false,
            vm.CurrentFilePath,
            vm.EditorText,
            dock.Doc.FilePath,
            dock.Doc.Content);

        Assert.Equal("class B {}", text);
    }

    [Fact]
    public void ActiveTab_WithMatchingPath_UsesEditorText()
    {
        var path = @"C:\a\A.cs";
        var doc = new OpenDocumentViewModel(path, "A.cs", "stale");
        var dock = new DockDocumentViewModel(doc);
        var vm = new MainWindowViewModel
        {
            CurrentFilePath = path,
            EditorText = "live buffer",
        };

        var text = DockDocumentMonacoTextResolver.Resolve(
            isActive: true,
            vm.CurrentFilePath,
            vm.EditorText,
            dock.Doc.FilePath,
            dock.Doc.Content);

        Assert.Equal("live buffer", text);
    }

    [Fact]
    public void ActiveTab_PathMismatch_UsesDocumentContent()
    {
        var doc = new OpenDocumentViewModel(@"C:\a\B.cs", "B.cs", "class B {}");
        var dock = new DockDocumentViewModel(doc);
        var vm = new MainWindowViewModel
        {
            CurrentFilePath = @"C:\a\A.cs",
            EditorText = "class A {}",
        };

        var text = DockDocumentMonacoTextResolver.Resolve(
            isActive: true,
            vm.CurrentFilePath,
            vm.EditorText,
            dock.Doc.FilePath,
            dock.Doc.Content);

        Assert.Equal("class B {}", text);
    }
}

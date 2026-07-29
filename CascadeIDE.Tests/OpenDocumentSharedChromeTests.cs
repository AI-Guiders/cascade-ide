using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public class OpenDocumentSharedChromeTests
{
    [Fact]
    public void DisplayTitle_AppendsSharedSuffix_WhenCoPresent()
    {
        var doc = new OpenDocumentViewModel(@"C:\a\Foo.cs", "Foo.cs", "class Foo {}");
        Assert.Equal("Foo.cs", doc.DisplayTitle);

        doc.IsSharedWithAgent = true;
        Assert.Equal("Foo.cs" + OpenDocumentViewModel.SharedWithAgentSuffix, doc.DisplayTitle);

        doc.IsDirty = true;
        Assert.Equal("Foo.cs*" + OpenDocumentViewModel.SharedWithAgentSuffix, doc.DisplayTitle);

        doc.IsPinned = true;
        Assert.Equal("[P] Foo.cs*" + OpenDocumentViewModel.SharedWithAgentSuffix, doc.DisplayTitle);
    }
}

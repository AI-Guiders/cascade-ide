using System.Collections.ObjectModel;
using CascadeIDE.Features.Chat;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceFileSlashCompletionProviderTests
{
    [Fact]
    public void GetMatches_filters_by_prefix()
    {
        var src = SolutionItem.CreateFolder("src");
        src.Children.Add(SolutionItem.CreateFile("Foo.cs", @"D:\ws\src\Foo.cs"));
        src.Children.Add(SolutionItem.CreateFile("Bar.cs", @"D:\ws\src\Bar.cs"));
        var root = new ObservableCollection<SolutionItem> { src };

        var provider = new WorkspaceFileSlashCompletionProvider(
            () => null,
            () => root,
            () => @"D:\ws");

        var all = provider.GetMatches("", 30);
        Assert.True(all.Count >= 2);

        var filtered = provider.GetMatches("src/F", 30);
        Assert.Contains(filtered, m => m.InsertPath.Contains("Foo", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(filtered, m => m.InsertPath.Contains("Bar.cs", StringComparison.OrdinalIgnoreCase));
    }
}

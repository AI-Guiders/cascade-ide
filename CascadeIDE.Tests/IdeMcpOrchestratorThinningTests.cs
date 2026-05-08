using System.Collections.ObjectModel;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeMcpOrchestratorThinningTests
{
    [Fact]
    public void BuildGetOpenDocumentTextResponse_no_path_returns_error()
    {
        var json = IdeMcpEditorOrchestrator.BuildGetOpenDocumentTextResponse(null, null, [], null);
        Assert.Contains("no_path", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildGetOpenDocumentTextResponse_not_open_tab_returns_error()
    {
        var tabs = new[]
        {
            new IdeMcpEditorOrchestrator.OpenDocumentTabSnapshot(@"C:\proj\a.cs", "x", false),
        };
        var json = IdeMcpEditorOrchestrator.BuildGetOpenDocumentTextResponse(@"C:\proj\b.cs", null, tabs, null);
        Assert.Contains("not_open", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildGetOpenDocumentTextResponse_matches_tab_by_path()
    {
        var path = @"C:\proj\match.cs";
        var tabs = new[] { new IdeMcpEditorOrchestrator.OpenDocumentTabSnapshot(path, "body", true) };
        var json = IdeMcpEditorOrchestrator.BuildGetOpenDocumentTextResponse(path, null, tabs, null);
        Assert.Contains("\"body\"", json, StringComparison.Ordinal);
        Assert.Contains("is_dirty", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSolutionFilesJson_empty_collection_has_empty_arrays()
    {
        var json = IdeMcpBuildTestOrchestrator.BuildSolutionFilesJson(null, new ObservableCollection<SolutionItem>());
        Assert.Contains("\"file_entries\":[]", json, StringComparison.Ordinal);
        Assert.Contains("\"solution_tree\":[]", json, StringComparison.Ordinal);
    }
}

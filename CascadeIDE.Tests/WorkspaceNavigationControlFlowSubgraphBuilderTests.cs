using System.Text.Json;
using CascadeIDE.Services.Navigation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceNavigationControlFlowSubgraphBuilderTests
{
    [Fact]
    public void BuildJson_EmitsLoopAndMultiBranchEdgeKinds()
    {
        const string source = """
using System;
class Demo {
    void A(int x)
    {
        while (x > 0)
        {
            B();
            x--;
        }

        switch (x)
        {
            case 0:
                C();
                break;
            default:
                D();
                break;
        }

        E();
    }

    void B() { }
    void C() { }
    void D() { }
    void E() { }
}
""";

        var json = WorkspaceNavigationControlFlowSubgraphBuilder.BuildJson(
            filePath: @"D:\w\Demo.cs",
            sourceText: source,
            line: 5,
            column: 10,
            maxNodes: 32,
            maxEdges: 64);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("subgraph", root.GetProperty("mode").GetString());
        var edges = root.GetProperty("edges").EnumerateArray().ToList();
        Assert.Contains(edges, e => string.Equals(e.GetProperty("kind").GetString(), "LoopCall", StringComparison.Ordinal));
        Assert.Contains(edges, e => string.Equals(e.GetProperty("kind").GetString(), "MultiBranch", StringComparison.Ordinal));
    }
}

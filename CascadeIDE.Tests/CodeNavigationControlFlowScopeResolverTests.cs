using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Services.CodeNavigation;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationControlFlowScopeResolverTests
{
    [Fact]
    public void TryFindScope_InClassMethod_ReturnsMethodBody()
    {
        const string source = """
using System;

var x = 1;
if (x > 0) { }

class SmokeClient
{
    public void RequestPermissionAsync()
    {
        DoWork();
    }

    void DoWork() { }
}
""";

        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot()!;
        var offset = source.IndexOf("DoWork();", StringComparison.Ordinal);
        var (line, col) = WorkspaceNavigationMapOrchestrator.ComputeLineColumn(source, offset);

        var scope = CodeNavigationControlFlowScopeResolver.TryFindScope(root, tree, line, col);

        Assert.NotNull(scope);
        Assert.False(scope.IsTopLevel);
        Assert.Equal("RequestPermissionAsync", scope.ScopeLabel);
    }

    [Fact]
    public void TryFindScope_InTopLevel_ReturnsAllGlobals()
    {
        const string source = """
var x = 1;
Console.WriteLine(x);
""";

        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot()!;
        var scope = CodeNavigationControlFlowScopeResolver.TryFindScope(root, tree, line: 2, column: 5);

        Assert.NotNull(scope);
        Assert.True(scope.IsTopLevel);
        Assert.Equal(2, scope.TopLevelStatements.Count);
    }

    [Fact]
    public void TryFindScope_InLocalFunction_ReturnsFunctionBody()
    {
        const string source = """
var x = 1;

static void Helper()
{
    Console.WriteLine("hi");
}
""";

        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot()!;
        var offset = source.IndexOf("Console.WriteLine", StringComparison.Ordinal);
        var (line, col) = WorkspaceNavigationMapOrchestrator.ComputeLineColumn(source, offset);

        var scope = CodeNavigationControlFlowScopeResolver.TryFindScope(root, tree, line, col);

        Assert.NotNull(scope);
        Assert.False(scope.IsTopLevel);
        Assert.Equal("Helper", scope.ScopeLabel);
    }

    [Fact]
    public void BuildJson_AcpSmoke_InSmokeClientMethod_SmallerThanTopLevel()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "samples", "AcpSmokeDotnet", "Program.cs"));
        Assert.True(File.Exists(path), path);

        var source = File.ReadAllText(path);
        var methodOffset = source.IndexOf("RequestPermissionAsync", StringComparison.Ordinal);
        var (methodLine, methodCol) = WorkspaceNavigationMapOrchestrator.ComputeLineColumn(source, methodOffset);

        var topOffset = source.IndexOf("InitializeAsync", StringComparison.Ordinal);
        var (topLine, topCol) = WorkspaceNavigationMapOrchestrator.ComputeLineColumn(source, topOffset);

        var methodJson = CodeNavigationMethodIntentSubgraphBuilder.BuildJson(
            path, source, methodLine, methodCol, 48, 96);
        var topJson = CodeNavigationMethodIntentSubgraphBuilder.BuildJson(
            path, source, topLine, topCol, 48, 96);

        using var methodDoc = System.Text.Json.JsonDocument.Parse(methodJson);
        using var topDoc = System.Text.Json.JsonDocument.Parse(topJson);
        var methodNodes = methodDoc.RootElement.GetProperty("nodes").GetArrayLength();
        var topNodes = topDoc.RootElement.GetProperty("nodes").GetArrayLength();

        Assert.True(methodNodes < topNodes, $"method={methodNodes}, top-level={topNodes}");
        Assert.True(methodNodes <= 6, $"method scope should be compact, got {methodNodes}");
        Assert.True(topNodes >= 15, $"top-level should be large, got {topNodes}");
    }
}

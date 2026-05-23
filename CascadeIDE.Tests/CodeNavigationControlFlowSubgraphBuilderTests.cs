using System.Text.Json;
using CascadeIDE.Services.CodeNavigation;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationControlFlowSubgraphBuilderTests
{
    [Fact]
    public void TryResolveFirstMethodLineColumn_finds_first_method()
    {
        const string source = """
class Demo {
    void First() { }
    void Second() { }
}
""";
        var (line, col) = CodeNavigationControlFlowSubgraphBuilder.TryResolveFirstMethodLineColumn(source);
        Assert.NotNull(line);
        Assert.True(line > 0);
        Assert.Contains("First", source.Split('\n')[line!.Value - 1], StringComparison.Ordinal);
    }

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

        var json = CodeNavigationControlFlowSubgraphBuilder.BuildJson(
            filePath: @"D:\w\Demo.cs",
            sourceText: source,
            line: 5,
            column: 10,
            maxNodes: 32,
            maxEdges: 64);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("subgraph", root.GetProperty("mode").GetString());
        Assert.Equal("code_intent_code_navigation_map", root.GetProperty("graph_kind").GetString());
        var edges = root.GetProperty("edges").EnumerateArray().ToList();
        Assert.Contains(edges, e => string.Equals(e.GetProperty("kind").GetString(), "LoopCall", StringComparison.Ordinal));
        Assert.Contains(edges, e => string.Equals(e.GetProperty("kind").GetString(), "MultiBranch", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildJson_ForeachVariable_Deconstruction_EmitsLoopCall()
    {
        const string source = """
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
class Demo {
    void A((string Path, string Text)[] files)
    {
        var trees = new System.Collections.Generic.List<SyntaxTree>(files.Length);
        foreach (var (path, text) in files)
            trees.Add(CSharpSyntaxTree.ParseText(text, path: path));
    }
}
""";

        var json = CodeNavigationControlFlowSubgraphBuilder.BuildJson(
            filePath: @"D:\w\Demo.cs",
            sourceText: source,
            line: 7,
            column: 20,
            maxNodes: 32,
            maxEdges: 64);

        using var doc = JsonDocument.Parse(json);
        var edges = doc.RootElement.GetProperty("edges").EnumerateArray().ToList();
        Assert.Contains(edges, e => string.Equals(e.GetProperty("kind").GetString(), "LoopCall", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildJson_TernaryOperator_EmitsConditionalAndMerge()
    {
        const string source = """
class Demo {
    string A(bool ok)
    {
        var value = ok ? B() : C();
        D();
        return value;
    }

    string B() => "b";
    string C() => "c";
    void D() { }
}
""";

        var json = CodeNavigationControlFlowSubgraphBuilder.BuildJson(
            filePath: @"D:\w\Demo.cs",
            sourceText: source,
            line: 4,
            column: 20,
            maxNodes: 32,
            maxEdges: 64);

        using var doc = JsonDocument.Parse(json);
        var edges = doc.RootElement.GetProperty("edges").EnumerateArray().ToList();
        Assert.Contains(edges, e => string.Equals(e.GetProperty("kind").GetString(), "ConditionalCall", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildJson_WhenCursorOutsideMethod_DoesNotFallbackToFirstMethod()
    {
        const string source = """
using System;
class Demo {
    void A() { B(); }
    void B() { }
}
""";

        var json = CodeNavigationControlFlowSubgraphBuilder.BuildJson(
            filePath: @"D:\w\Demo.cs",
            sourceText: source,
            line: 1,
            column: 1,
            maxNodes: 32,
            maxEdges: 64);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var nodes = root.GetProperty("nodes").EnumerateArray().ToList();
        var edges = root.GetProperty("edges").EnumerateArray().ToList();

        Assert.Single(nodes);
        Assert.Empty(edges);
        Assert.Equal("no_method_at_cursor", nodes[0].GetProperty("rationale").GetString());
    }

    [Fact]
    public void BuildJson_GuardReturn_EmitsExitNodeAndMergeBackToMainPath()
    {
        const string source = """
class Demo {
    void A()
    {
        if (IsNot())
            return;

        DoWork();
    }

    bool IsNot() => true;
    void DoWork() { }
}
""";

        var json = CodeNavigationControlFlowSubgraphBuilder.BuildJson(
            filePath: @"D:\w\Demo.cs",
            sourceText: source,
            line: 4,
            column: 10,
            maxNodes: 32,
            maxEdges: 64);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var nodes = root.GetProperty("nodes").EnumerateArray().ToList();
        var edges = root.GetProperty("edges").EnumerateArray().ToList();

        Assert.Contains(nodes, n => string.Equals(n.GetProperty("kind").GetString(), "condition_step", StringComparison.Ordinal));
        Assert.Contains(nodes, n => string.Equals(n.GetProperty("kind").GetString(), "exit_step", StringComparison.Ordinal));
        Assert.Contains(edges, e => string.Equals(e.GetProperty("kind").GetString(), "ConditionalCall", StringComparison.Ordinal));
        Assert.Contains(edges, e => string.Equals(e.GetProperty("kind").GetString(), "Exit", StringComparison.Ordinal));

        var text = ControlFlowSubgraphTextPresenter.Render(json);
        Assert.Contains("(ConditionalCall) ?", text, StringComparison.Ordinal);
        Assert.Contains("(Exit) R", text, StringComparison.Ordinal);
        Assert.Contains("(Call) DoWork", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildJson_NestedInvocation_OrdersInnerBeforeOuter()
    {
        const string source = """
class Demo {
    void A()
    {
        Report(Create(GetLocation()));
    }

    object GetLocation() => new();
    object Create(object x) => x;
    void Report(object x) { }
}
""";

        var json = CodeNavigationControlFlowSubgraphBuilder.BuildJson(
            filePath: @"D:\w\Demo.cs",
            sourceText: source,
            line: 4,
            column: 10,
            maxNodes: 32,
            maxEdges: 64);

        using var doc = JsonDocument.Parse(json);
        var labels = doc.RootElement
            .GetProperty("nodes")
            .EnumerateArray()
            .Skip(1)
            .Select(n => n.GetProperty("label").GetString())
            .ToList();

        Assert.Equal(["GetLocation", "Create", "Report"], labels);
    }

    [Fact]
    public void BuildJson_AnalyzerGuardChain_ProducesExpectedControlFlowSignals()
    {
        const string source = """
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
class Demo {
    private static readonly DiagnosticDescriptor Rule = new("X001", "t", "m", "c", DiagnosticSeverity.Warning, true);

    private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not AssignmentExpressionSyntax assign || assign.Kind() != SyntaxKind.SimpleAssignmentExpression)
            return;

        if (!IsMainWindowViewModelFile(context.Node.SyntaxTree.FilePath))
            return;

        var model = context.SemanticModel;
        var symbol = model.GetSymbolInfo(assign.Left).Symbol;
        if (symbol is null)
            return;

        if (!IsCockpitIntentMember(symbol, out var displayName))
            return;

        if (IsAllowedSourcePath(context.Node.SyntaxTree.FilePath))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, assign.GetLocation(), displayName));
    }

    private static bool IsMainWindowViewModelFile(string path) => true;
    private static bool IsCockpitIntentMember(object symbol, out string displayName) { displayName = ""; return true; }
    private static bool IsAllowedSourcePath(string path) => false;
}
""";

        var json = CodeNavigationControlFlowSubgraphBuilder.BuildJson(
            filePath: @"D:\w\Demo.cs",
            sourceText: source,
            line: 10,
            column: 20,
            maxNodes: 64,
            maxEdges: 128);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var nodes = root.GetProperty("nodes").EnumerateArray().ToList();
        var edges = root.GetProperty("edges").EnumerateArray().ToList();

        Assert.True(nodes.Count(n => string.Equals(n.GetProperty("kind").GetString(), "condition_step", StringComparison.Ordinal)) >= 4);
        Assert.True(nodes.Count(n => string.Equals(n.GetProperty("kind").GetString(), "exit_step", StringComparison.Ordinal)) >= 4);
        Assert.Contains(edges, e => string.Equals(e.GetProperty("kind").GetString(), "ConditionalCall", StringComparison.Ordinal));
        Assert.Contains(edges, e => string.Equals(e.GetProperty("kind").GetString(), "Exit", StringComparison.Ordinal));

        var text = ControlFlowSubgraphTextPresenter.Render(json);
        Assert.Contains("(ConditionalCall) ?", text, StringComparison.Ordinal);
        Assert.Contains("ReportDiagnostic", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ToString", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetLocation", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildJson_TryCatch_EmitsProtectedHandlerAndExceptionFlowEdges()
    {
        const string source = """
using System;
class Demo {
    void A()
    {
        try
        {
            B();
        }
        catch (InvalidOperationException)
        {
            C();
        }
    }

    void B() { }
    void C() { }
}
""";

        var json = CodeNavigationControlFlowSubgraphBuilder.BuildJson(
            filePath: @"D:\w\Demo.cs",
            sourceText: source,
            line: 5,
            column: 10,
            maxNodes: 48,
            maxEdges: 96);

        using var doc = JsonDocument.Parse(json);
        var nodes = doc.RootElement.GetProperty("nodes").EnumerateArray().ToList();
        var edges = doc.RootElement.GetProperty("edges").EnumerateArray().ToList();

        Assert.Contains(nodes, n => string.Equals(n.GetProperty("kind").GetString(), "protected_step", StringComparison.Ordinal));
        Assert.Contains(nodes, n => string.Equals(n.GetProperty("kind").GetString(), "handler_step", StringComparison.Ordinal));
        Assert.Contains(edges, e => string.Equals(e.GetProperty("kind").GetString(), "ExceptionFlow", StringComparison.Ordinal));
    }
}

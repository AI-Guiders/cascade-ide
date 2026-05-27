using System.Text.Json;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Services.CodeNavigation;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationControlFlowSubgraphBuilderTests
{
    [Fact]
    public void BuildJson_TopLevelStatements_BuildsScopeGraph()
    {
        const string source = """
using System;

Console.WriteLine("hi");
if (args.Length > 0)
    Console.WriteLine(args[0]);
""";

        var json = CodeNavigationMethodIntentSubgraphBuilder.BuildJson(
            @"D:\w\Program.cs",
            source,
            line: 4,
            column: 8,
            maxNodes: 32,
            maxEdges: 64);

        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("error", out _));
        var nodes = doc.RootElement.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count >= 2);
        Assert.Contains(nodes, n => string.Equals(n.GetProperty("kind").GetString(), "condition_step", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildJson_AcpSmokeTopLevel_ReachesPastLegacyTwelveNodeCap()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "samples", "AcpSmokeDotnet", "Program.cs"));
        Assert.True(File.Exists(path), path);

        var source = File.ReadAllText(path);
        var json = CodeNavigationMethodIntentSubgraphBuilder.BuildJson(
            path,
            source,
            line: 40,
            column: 10,
            CodeNavigationContextBuilder.DefaultControlFlowSubgraphMaxNodes,
            CodeNavigationContextBuilder.DefaultControlFlowSubgraphMaxEdges);

        using var doc = JsonDocument.Parse(json);
        var nodes = doc.RootElement.GetProperty("nodes").EnumerateArray().ToList();
        Assert.True(nodes.Count > 12, $"expected >12 nodes, got {nodes.Count}");
        if (doc.RootElement.TryGetProperty("truncated_nodes", out var trunc))
            Assert.False(trunc.GetBoolean());
        Assert.True(
            nodes.Any(n => n.TryGetProperty("legend_text", out var leg)
                && leg.ValueKind == JsonValueKind.String
                && leg.GetString()!.Contains("InitializeAsync", StringComparison.Ordinal))
            || nodes.Any(n => n.TryGetProperty("line_start", out var ls)
                && ls.ValueKind == JsonValueKind.Number
                && ls.GetInt32() >= 43));
    }

    [Fact]
    public void BuildJson_WhenNodeCapReached_SetsTruncatedFlag()
    {
        const string source = """
class X {
  void M() {
    A(); B(); C(); D(); E(); F(); G(); H(); I(); J(); K(); L(); M2(); N(); O();
  }
  void A() { } void B() { } void C() { } void D() { } void E() { } void F() { }
  void G() { } void H() { } void I() { } void J() { } void K() { } void L() { }
  void M2() { } void N() { } void O() { }
}
""";
        var json = CodeNavigationMethodIntentSubgraphBuilder.BuildJson(
            @"D:\w\X.cs",
            source,
            line: 4,
            column: 8,
            maxNodes: 12,
            maxEdges: 64);

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("truncated_nodes").GetBoolean());
    }

    [Fact]
    public void BuildJson_IfInsideWhile_BranchEdgesAreConditionalCallNotLoopCall()
    {
        const string source = """
class X {
    void M(int x) {
        while (x > 0) {
            if (x > 1)
                return;
            x--;
        }
    }
}
""";
        var json = CodeNavigationMethodIntentSubgraphBuilder.BuildJson(
            @"D:\w\X.cs",
            source,
            line: 6,
            column: 16,
            maxNodes: 32,
            maxEdges: 64);

        using var doc = JsonDocument.Parse(json);
        var edges = doc.RootElement.GetProperty("edges").EnumerateArray().ToList();
        var branchEdges = edges
            .Where(e =>
                e.TryGetProperty("edge_provenance", out var p)
                && (p.GetString() == CodeNavigationMapConditionBranchProvenance.True
                    || p.GetString() == CodeNavigationMapConditionBranchProvenance.False))
            .ToList();
        Assert.NotEmpty(branchEdges);
        Assert.All(
            branchEdges,
            e => Assert.Equal("ConditionalCall", e.GetProperty("kind").GetString()));
        Assert.Contains(
            edges,
            e => string.Equals(e.GetProperty("kind").GetString(), "LoopCall", StringComparison.Ordinal));
        Assert.Contains(
            edges,
            e => string.Equals(e.GetProperty("kind").GetString(), "LoopBack", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildJson_IfStatement_TagsTrueAndFalseBranchEdges()
    {
        const string source = """
class X {
    void M(int x) {
        if (x > 0)
            A();
        else
            B();
    }
    void A() { }
    void B() { }
}
""";
        var json = CodeNavigationMethodIntentSubgraphBuilder.BuildJson(
            @"D:\w\X.cs",
            source,
            line: 4,
            column: 12,
            maxNodes: 32,
            maxEdges: 64);

        using var doc = JsonDocument.Parse(json);
        var edges = doc.RootElement.GetProperty("edges").EnumerateArray().ToList();
        Assert.Contains(
            edges,
            e => e.TryGetProperty("edge_provenance", out var p)
                && p.GetString() == "cf_branch_true");
        Assert.Contains(
            edges,
            e => e.TryGetProperty("edge_provenance", out var p)
                && p.GetString() == "cf_branch_false");
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
        Assert.Contains(edges, e => string.Equals(e.GetProperty("kind").GetString(), "LoopBack", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildJson_WhileTrueWithNestedIf_TargetsInnerIfLegendWithTryParse()
    {
        const string source = """
using System;

class Demo
{
    void M(Request request)
    {
        while (true)
        {
            Console.WriteLine("?");
            var answer = Console.ReadLine()?.Trim() ?? "";

            if (int.TryParse(answer, out _))
                return;

            Console.WriteLine("!");
        }
    }

    sealed class Request { }
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
        var root = doc.RootElement;
        Assert.Equal("subgraph", root.GetProperty("mode").GetString());

        var nodes = root.GetProperty("nodes").EnumerateArray().ToList();

        Assert.Contains(nodes, node =>
            string.Equals(node.GetProperty("kind").GetString(), "condition_step", StringComparison.OrdinalIgnoreCase)
            && node.GetProperty("legend_text").GetString()?.Contains("TryParse", StringComparison.OrdinalIgnoreCase)
            == true);

        Assert.Contains(nodes, node =>
            string.Equals(node.GetProperty("kind").GetString(), "condition_step", StringComparison.OrdinalIgnoreCase)
            && string.Equals(node.GetProperty("legend_text").GetString(), "true", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(nodes, node =>
            string.Equals(node.GetProperty("kind").GetString(), "exit_step", StringComparison.OrdinalIgnoreCase));

        var loopGroupIds = nodes
            .Where(n =>
                n.TryGetProperty("loop_group", out var gp)
                && gp is { ValueKind: JsonValueKind.Number }
                && gp.TryGetInt32(out var gv)
                && gv > 0)
            .Select(n => n.GetProperty("loop_group").GetInt32())
            .Distinct()
            .ToList();
        Assert.NotEmpty(loopGroupIds);
        Assert.Single(loopGroupIds);
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
        Assert.Equal("no_control_flow_scope", root.GetProperty("error").GetString());
        Assert.False(root.TryGetProperty("nodes", out _));
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

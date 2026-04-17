#nullable enable
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeIDE.Services.Navigation;

/// <summary>Строит subgraph для Semantic Map на основе синтаксиса метода (control flow).</summary>
public static class WorkspaceNavigationControlFlowSubgraphBuilder
{
    public static string BuildJson(
        string? filePath,
        string? sourceText,
        int? line,
        int? column,
        int maxNodes,
        int maxEdges)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return """{"error":"no_file","message":"No current file."}""";

        string text;
        if (!string.IsNullOrWhiteSpace(sourceText))
        {
            text = sourceText!;
        }
        else if (File.Exists(filePath))
        {
            text = File.ReadAllText(filePath!);
        }
        else
        {
            return """{"error":"no_file","message":"File not found."}""";
        }

        var tree = CSharpSyntaxTree.ParseText(text);
        var root = tree.GetRoot();
        if (root is null)
            return """{"error":"parse_error","message":"Unable to parse syntax tree."}""";

        var method = FindTargetMethod(root, tree, line, column)
                     ?? root.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (method is null)
            return BuildEmptySubgraph(filePath!);

        var nodeCap = Math.Max(2, maxNodes <= 0 ? 32 : maxNodes);
        var edgeCap = Math.Max(1, maxEdges <= 0 ? 64 : maxEdges);

        var nodes = new List<object>(nodeCap);
        var edges = new List<object>(edgeCap);

        nodes.Add(new
        {
            id = "n0",
            path = filePath,
            kind = "anchor",
            label = Path.GetFileName(filePath),
            relative_path = "",
            rationale = $"method {method.Identifier.Text}"
        });

        var steps = method
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Take(nodeCap - 1)
            .ToList();

        var previousId = "n0";
        var previousWasConditional = false;
        var index = 1;
        foreach (var inv in steps)
        {
            var id = $"n{index}";
            var label = ExtractInvocationLabel(inv);
            var kind = DetectEdgeKind(inv);
            var rationale = $"call {label}";
            nodes.Add(new
            {
                id,
                path = filePath,
                kind = "call_step",
                label,
                relative_path = "",
                rationale
            });

            if (edges.Count < edgeCap)
            {
                var edgeKind = kind;
                if (previousWasConditional && !IsConditionalContext(inv))
                    edgeKind = "Merge";

                edges.Add(new
                {
                    from_id = previousId,
                    to_id = id,
                    kind = edgeKind,
                    related_kind = kind
                });
            }

            previousId = id;
            previousWasConditional = IsConditionalKind(kind);
            index++;
        }

        var payload = new
        {
            mode = "subgraph",
            anchor_path = filePath,
            nodes,
            edges
        };
        return JsonSerializer.Serialize(payload);
    }

    private static string BuildEmptySubgraph(string filePath)
    {
        var payload = new
        {
            mode = "subgraph",
            anchor_path = filePath,
            nodes = new[]
            {
                new
                {
                    id = "n0",
                    path = filePath,
                    kind = "anchor",
                    label = Path.GetFileName(filePath),
                    relative_path = "",
                    rationale = "no_methods"
                }
            },
            edges = Array.Empty<object>()
        };
        return JsonSerializer.Serialize(payload);
    }

    private static MethodDeclarationSyntax? FindTargetMethod(SyntaxNode root, SyntaxTree tree, int? line, int? column)
    {
        if (line is null || line <= 0)
            return null;

        var linePos = Math.Max(0, line.Value - 1);
        var colPos = Math.Max(0, (column ?? 1) - 1);
        var text = tree.GetText();
        if (linePos >= text.Lines.Count)
            return null;

        var pos = text.Lines[linePos].Start + Math.Min(colPos, text.Lines[linePos].Span.Length);
        var token = root.FindToken(pos);
        return token.Parent?.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
    }

    private static string DetectEdgeKind(InvocationExpressionSyntax invocation)
    {
        if (invocation.Ancestors().Any(a => a is ForStatementSyntax or ForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax))
            return "LoopCall";
        if (invocation.Ancestors().Any(a => a is SwitchStatementSyntax or SwitchExpressionSyntax))
            return "MultiBranch";
        if (IsConditionalContext(invocation))
            return "ConditionalCall";
        return "Call";
    }

    private static bool IsConditionalContext(SyntaxNode node) =>
        node.Ancestors().Any(a => a is IfStatementSyntax or ConditionalExpressionSyntax);

    private static bool IsConditionalKind(string kind) =>
        string.Equals(kind, "ConditionalCall", StringComparison.OrdinalIgnoreCase)
        || string.Equals(kind, "MultiBranch", StringComparison.OrdinalIgnoreCase);

    private static string ExtractInvocationLabel(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            IdentifierNameSyntax id => id.Identifier.Text,
            GenericNameSyntax generic => generic.Identifier.Text,
            _ => invocation.Expression.ToString()
        };
    }
}

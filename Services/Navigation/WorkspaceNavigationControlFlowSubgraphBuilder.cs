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

        var method = FindTargetMethod(root, tree, line, column);
        if (method is null)
            return BuildEmptySubgraph(filePath!, line is > 0 ? "no_method_at_cursor" : "no_cursor_position");

        var nodeCap = Math.Max(2, maxNodes <= 0 ? 32 : maxNodes);
        var edgeCap = Math.Max(1, maxEdges <= 0 ? 64 : maxEdges);

        var graph = new ControlFlowGraphBuilder(filePath!, method.Identifier.Text, nodeCap, edgeCap);
        graph.Build(method);

        var payload = new
        {
            mode = "subgraph",
            anchor_path = filePath,
            nodes = graph.Nodes.Select(n => new
            {
                id = n.Id,
                path = n.Path,
                kind = n.Kind,
                label = n.Label,
                relative_path = n.RelativePath,
                rationale = n.Rationale,
                legend_index = n.LegendIndex,
                legend_text = n.LegendText
            }).ToList(),
            edges = graph.Edges.Select(e => new
            {
                from_id = e.FromId,
                to_id = e.ToId,
                kind = e.Kind,
                related_kind = e.RelatedKind
            }).ToList()
        };
        return JsonSerializer.Serialize(payload);
    }

    private static string BuildEmptySubgraph(string filePath, string rationale)
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
                    rationale
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

    private sealed class ControlFlowGraphBuilder
    {
        private readonly string _filePath;
        private readonly int _nodeCap;
        private readonly int _edgeCap;
        private int _nextId = 1;
        private int _legendSerial;

        public List<NodeRecord> Nodes { get; }
        public List<EdgeRecord> Edges { get; } = [];

        public ControlFlowGraphBuilder(string filePath, string methodName, int nodeCap, int edgeCap)
        {
            _filePath = filePath;
            _nodeCap = nodeCap;
            _edgeCap = edgeCap;
            Nodes =
            [
                new NodeRecord("n0", filePath, "anchor", Path.GetFileName(filePath), "", $"method {methodName}", null, null)
            ];
        }

        public void Build(MethodDeclarationSyntax method)
        {
            var continuation = new List<string> { "n0" };
            if (method.Body is not null)
            {
                _ = BuildStatementList(method.Body.Statements, continuation);
                return;
            }

            if (method.ExpressionBody is not null)
                _ = BuildExpressionInvocations(method.ExpressionBody.Expression, continuation);
        }

        private List<string> BuildStatementList(SyntaxList<StatementSyntax> statements, List<string> incoming)
        {
            var continuation = incoming;
            foreach (var statement in statements)
            {
                continuation = BuildStatement(statement, continuation);
                if (continuation.Count == 0)
                    break;
            }

            return continuation;
        }

        private List<string> BuildStatement(StatementSyntax statement, List<string> incoming)
        {
            if (incoming.Count == 0)
                return incoming;

            return statement switch
            {
                BlockSyntax block => BuildStatementList(block.Statements, incoming),
                IfStatementSyntax ifStatement => BuildIfStatement(ifStatement, incoming),
                ReturnStatementSyntax => BuildReturnStatement(incoming),
                _ => BuildExpressionInvocations(statement, incoming)
            };
        }

        private List<string> BuildIfStatement(IfStatementSyntax ifStatement, List<string> incoming)
        {
            var condLine = SanitizeLegendLine(ifStatement.Condition.ToString(), 200);
            var conditionId = AddNode("condition_step", "IF", "if condition", condLine);
            if (conditionId is null)
                return incoming;

            AddEdges(incoming, conditionId, "ConditionalCall", "ConditionalCall");
            var fromCondition = new List<string> { conditionId };

            var thenContinuation = BuildStatement(ifStatement.Statement, fromCondition);
            var elseContinuation = ifStatement.Else is null
                ? fromCondition
                : BuildStatement(ifStatement.Else.Statement, fromCondition);

            return thenContinuation
                .Concat(elseContinuation)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private List<string> BuildReturnStatement(List<string> incoming)
        {
            var returnId = AddNode("exit_step", "RET", "return", "return");
            if (returnId is null)
                return incoming;

            AddEdges(incoming, returnId, "Exit", "Exit");
            return [];
        }

        private List<string> BuildExpressionInvocations(SyntaxNode expressionOwner, List<string> incoming)
        {
            var invocations = expressionOwner
                .DescendantNodes(descendIntoChildren: _ => true)
                .Where(n => n is InvocationExpressionSyntax)
                .Select(n => (InvocationExpressionSyntax)n)
                .OrderBy(i => i.Span.End)
                .Where(i => !ShouldSkipInvocation(i))
                .ToList();
            if (invocations.Count == 0)
                return incoming;

            var continuation = incoming;
            foreach (var invocation in invocations)
            {
                var label = ExtractInvocationLabel(invocation);
                var legend = SanitizeLegendLine(invocation.ToString(), 200);
                var nodeId = AddNode("call_step", label, $"call {label}", legend);
                if (nodeId is null)
                    return continuation;

                var edgeKind = DetectEdgeKind(invocation);
                AddEdges(continuation, nodeId, edgeKind, edgeKind);
                continuation = [nodeId];
            }

            return continuation;
        }

        private string? AddNode(string kind, string label, string rationale, string? legendLine = null)
        {
            if (Nodes.Count >= _nodeCap)
                return null;

            var id = $"n{_nextId++}";
            var leg = string.IsNullOrWhiteSpace(legendLine)
                ? SanitizeLegendLine(rationale, 200)
                : SanitizeLegendLine(legendLine, 200);
            _legendSerial++;
            var idx = _legendSerial;
            Nodes.Add(new NodeRecord(id, _filePath, kind, label, "", rationale, idx, string.IsNullOrEmpty(leg) ? null : leg));
            return id;
        }

        private static string SanitizeLegendLine(string? text, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            var s = text.Replace('\r', ' ').Replace('\n', ' ');
            while (s.Contains("  ", StringComparison.Ordinal))
                s = s.Replace("  ", " ", StringComparison.Ordinal);
            s = s.Trim();
            if (s.Length <= maxLen)
                return s;
            return s[..(maxLen - 1)] + "…";
        }

        private void AddEdges(List<string> fromIds, string toId, string kind, string relatedKind)
        {
            if (fromIds.Count == 0)
                return;

            var edgeKind = fromIds.Count > 1 ? "Merge" : kind;
            foreach (var fromId in fromIds)
            {
                if (Edges.Count >= _edgeCap)
                    break;

                Edges.Add(new EdgeRecord(fromId, toId, edgeKind, relatedKind));
            }
        }
    }

    private static string DetectEdgeKind(InvocationExpressionSyntax invocation)
    {
        if (invocation.Ancestors().Any(a => a is ForStatementSyntax or ForEachStatementSyntax or ForEachVariableStatementSyntax or WhileStatementSyntax or DoStatementSyntax))
            return "LoopCall";
        if (invocation.Ancestors().Any(a => a is SwitchStatementSyntax or SwitchExpressionSyntax))
            return "MultiBranch";
        if (IsConditionalContext(invocation))
            return "ConditionalCall";
        return "Call";
    }

    private static bool IsConditionalContext(SyntaxNode node) =>
        node.Ancestors().Any(a => a is IfStatementSyntax or ConditionalExpressionSyntax);

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

    private static bool ShouldSkipInvocation(InvocationExpressionSyntax invocation)
    {
        var label = ExtractInvocationLabel(invocation);
        if (string.Equals(label, "ToString", StringComparison.Ordinal) && invocation.ArgumentList.Arguments.Count == 0)
            return true;
        if (string.Equals(label, "GetLocation", StringComparison.Ordinal) && IsInsideDiagnosticCreate(invocation))
            return true;
        if (string.Equals(label, "Create", StringComparison.Ordinal) && IsDiagnosticCreate(invocation))
            return true;
        return false;
    }

    private static bool IsInsideDiagnosticCreate(InvocationExpressionSyntax invocation)
    {
        var parentInvocation = invocation.Parent?.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
        return parentInvocation is not null && IsDiagnosticCreate(parentInvocation);
    }

    private static bool IsDiagnosticCreate(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax member)
            return false;
        if (!string.Equals(member.Name.Identifier.Text, "Create", StringComparison.Ordinal))
            return false;
        return string.Equals(member.Expression.ToString(), "Diagnostic", StringComparison.Ordinal);
    }

    private readonly record struct NodeRecord(
        string Id,
        string Path,
        string Kind,
        string Label,
        string RelativePath,
        string Rationale,
        int? LegendIndex,
        string? LegendText);

    private readonly record struct EdgeRecord(
        string FromId,
        string ToId,
        string Kind,
        string RelatedKind);
}

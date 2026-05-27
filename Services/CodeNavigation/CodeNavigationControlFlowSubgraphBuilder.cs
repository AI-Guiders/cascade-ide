#nullable enable
using System.Text.Json;
using CascadeIDE.Cockpit.Graph;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeIDE.Services.CodeNavigation;

/// <summary>
/// Строит subgraph control flow из синтаксиса метода (Roslyn). Домен — <b>навигация по коду</b>, не структура workspace/git;
/// wire-документ — <see cref="GraphDocument"/> (та же семантическая карта, что отображается в кокпите; см. ADR 0039, 0053).
/// </summary>
public static class CodeNavigationControlFlowSubgraphBuilder
{
    /// <summary>Первая декларация метода в тексте — якорь CF, когда курсор редактора не применим (fallback-файл).</summary>
    public static (int? line, int? column) TryResolveFirstMethodLineColumn(string? sourceText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
            return (null, null);

        var tree = CSharpSyntaxTree.ParseText(sourceText);
        var root = tree.GetRoot();
        if (root is null)
            return (null, null);

        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (method is null)
            return (null, null);

        var span = tree.GetLineSpan(method.Identifier.Span);
        return (span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1);
    }

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
            graph_kind = GraphKindWire.CodeIntent,
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
                legend_text = n.LegendText,
                line_start = n.LineStart,
                line_end = n.LineEnd,
                loop_group = n.LoopGroupId
            }).ToList(),
            edges = graph.Edges.Select(e => new
            {
                from_id = e.FromId,
                to_id = e.ToId,
                kind = e.Kind,
                related_kind = e.RelationKind
            }).ToList()
        };
        return JsonSerializer.Serialize(payload);
    }

    private static string BuildEmptySubgraph(string filePath, string rationale)
    {
        var payload = new
        {
            mode = "subgraph",
            graph_kind = GraphKindWire.CodeIntent,
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
        private readonly GraphDocumentBlueprint _graph;
        private SyntaxTree? _syntaxTree;
        private int _nextLoopGroupId = 1;
        private int? _currentLoopGroupId;

        public List<GraphBuildNode> Nodes => _graph.Nodes;

        public List<GraphBuildEdge> Edges => _graph.Edges;

        public ControlFlowGraphBuilder(string filePath, string methodName, int nodeCap, int edgeCap)
        {
            _filePath = filePath;
            _graph = new GraphDocumentBlueprint(
                filePath,
                nodeCap,
                edgeCap,
                Path.GetFileName(filePath),
                $"method {methodName}",
                GraphKind.CodeIntent);
        }

        public void Build(MethodDeclarationSyntax method)
        {
            _syntaxTree = method.SyntaxTree;
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
                TryStatementSyntax tryStatement => BuildTryStatement(tryStatement, incoming),
                ReturnStatementSyntax returnStatement => BuildReturnStatement(incoming, returnStatement),
                EmptyStatementSyntax => incoming,
                BreakStatementSyntax or ContinueStatementSyntax => incoming,
                LocalFunctionStatementSyntax => incoming,
                WhileStatementSyntax ws => BuildWhileStatement(ws, incoming),
                DoStatementSyntax ds => BuildDoWhileStatement(ds, incoming),
                ForStatementSyntax fs => BuildForStatement(fs, incoming),
                ForEachStatementSyntax fe =>
                    BuildForEachBranch(fe, fe.Statement, incoming),
                ForEachVariableStatementSyntax fev =>
                    BuildForEachBranch(fev, fev.Statement, incoming),
                ExpressionStatementSyntax es =>
                    BuildExpressionInvocations(es.Expression, incoming),
                LocalDeclarationStatementSyntax localDecl =>
                    BuildVariableDeclaration(localDecl.Declaration, incoming),
                LockStatementSyntax lk => BuildStatement(lk.Statement, incoming),
                UsingStatementSyntax us => BuildUsingStatement(us, incoming),
                UnsafeStatementSyntax u => BuildStatement(u.Block, incoming),
                CheckedStatementSyntax c => c.Block is not null
                    ? BuildStatement(c.Block, incoming)
                    : incoming,
                FixedStatementSyntax f => BuildStatement(f.Statement, incoming),
                LabeledStatementSyntax l => BuildStatement(l.Statement, incoming),
                _ => BuildExpressionInvocations(statement, incoming)
            };
        }

        private List<string> BuildTryStatement(TryStatementSyntax tryStatement, List<string> incoming)
        {
            if (incoming.Count == 0)
                return incoming;

            var hubId = AddNode("protected_step", "try", "try { … }", "try", tryStatement);
            if (hubId is null)
                return BuildStatementList(tryStatement.Block.Statements, incoming);

            AddEdges(incoming, hubId, "Sequential", "Sequential");
            var afterTry = BuildStatementList(tryStatement.Block.Statements, new List<string> { hubId });
            var merge = new List<string>(afterTry);

            foreach (var catchClause in tryStatement.Catches)
            {
                var legend = CatchLegend(catchClause);
                var catchId = AddNode("handler_step", "catch", "catch", legend, catchClause);
                if (catchId is null)
                    continue;

                AddEdges(new List<string> { hubId }, catchId, "ExceptionFlow", "ExceptionFlow");
                var catchOut = BuildStatementList(catchClause.Block.Statements, new List<string> { catchId });
                foreach (var id in catchOut)
                {
                    if (!merge.Exists(x => string.Equals(x, id, StringComparison.OrdinalIgnoreCase)))
                        merge.Add(id);
                }
            }

            if (tryStatement.Finally is not null)
                merge = BuildStatementList(tryStatement.Finally.Block.Statements, merge);

            return merge;
        }

        private static string CatchLegend(CatchClauseSyntax catchClause)
        {
            if (catchClause.Declaration is { } d)
                return GraphDocumentBlueprint.SanitizeLegendLine($"catch ({d})", 200);
            if (catchClause.Filter is { } f)
                return GraphDocumentBlueprint.SanitizeLegendLine($"catch when ({f.FilterExpression})", 200);
            return "catch";
        }

        private List<string> BuildIfStatement(IfStatementSyntax ifStatement, List<string> incoming)
        {
            var conditionId = AddConditionStep(ifStatement.Condition.ToString(), incoming, ifStatement.Condition);
            if (conditionId is null)
                return incoming;

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

        private List<string> BuildReturnStatement(List<string> incoming, ReturnStatementSyntax statement) =>
            AddReturnExit(incoming, statement);

        private List<string> BuildWhileStatement(WhileStatementSyntax ws, List<string> incoming)
        {
            if (incoming.Count == 0)
                return incoming;

            var saved = _currentLoopGroupId;
            _currentLoopGroupId = _nextLoopGroupId++;
            try
            {
                var condSource = (SyntaxNode?)ws.Condition ?? ws;
                var condId = AddConditionStep(ws.Condition?.ToString() ?? "true", incoming, condSource);
                if (condId is null)
                    return incoming;

                var bodyOut = BuildStatement(ws.Statement, new List<string> { condId });
                AddEdges(bodyOut, condId, "LoopBack", "LoopBack");
                return [condId];
            }
            finally
            {
                _currentLoopGroupId = saved;
            }
        }

        private List<string> BuildDoWhileStatement(DoStatementSyntax ds, List<string> incoming)
        {
            if (incoming.Count == 0)
                return incoming;

            var saved = _currentLoopGroupId;
            _currentLoopGroupId = _nextLoopGroupId++;
            try
            {
                var entryId = AddNode(
                    "loop_step",
                    "do",
                    "do { … } while",
                    GraphDocumentBlueprint.SanitizeLegendLine("do { … } while", 200),
                    ds);
                if (entryId is null)
                    return incoming;

                AddEdges(incoming, entryId, "Sequential", "Sequential");
                var bodyOut = BuildStatement(ds.Statement, new List<string> { entryId });
                var condSource = (SyntaxNode?)ds.Condition ?? ds;
                var condId = AddConditionStep(ds.Condition?.ToString() ?? "true", bodyOut, condSource);
                if (condId is null)
                    return incoming;

                AddEdges([condId], entryId, "LoopBack", "LoopBack");
                return [condId];
            }
            finally
            {
                _currentLoopGroupId = saved;
            }
        }

        private List<string> BuildForStatement(ForStatementSyntax fs, List<string> incoming)
        {
            if (incoming.Count == 0)
                return incoming;

            var saved = _currentLoopGroupId;
            _currentLoopGroupId = _nextLoopGroupId++;
            try
            {
                var initOut = incoming;
                if (fs.Declaration is not null)
                    initOut = BuildVariableDeclaration(fs.Declaration, initOut);

                foreach (var initExpr in fs.Initializers)
                    initOut = BuildExpressionInvocations(initExpr, initOut);

                var condSyntax = (SyntaxNode?)fs.Condition ?? fs;
                var condId = AddConditionStep(fs.Condition?.ToString() ?? "true", initOut, condSyntax);
                if (condId is null)
                    return incoming;

                var bodyTail = BuildStatement(fs.Statement, new List<string> { condId });
                var afterBody = bodyTail;
                foreach (var inc in fs.Incrementors)
                    afterBody = BuildExpressionInvocations(inc, afterBody);

                AddEdges(afterBody, condId, "LoopBack", "LoopBack");
                return [condId];
            }
            finally
            {
                _currentLoopGroupId = saved;
            }
        }

        private List<string> BuildForEachBranch(
            SyntaxNode wholeForEach,
            StatementSyntax body,
            List<string> incoming)
        {
            if (incoming.Count == 0)
                return incoming;

            var saved = _currentLoopGroupId;
            _currentLoopGroupId = _nextLoopGroupId++;
            try
            {
                var condLegend =
                    GraphDocumentBlueprint.SanitizeLegendLine(wholeForEach.ToString(), 200);
                var condId =
                    AddConditionStep(condLegend, incoming, wholeForEach);
                if (condId is null)
                    return incoming;

                var bodyOut = BuildStatement(body, new List<string> { condId });
                AddEdges(bodyOut, condId, "LoopBack", "LoopBack");
                return [condId];
            }
            finally
            {
                _currentLoopGroupId = saved;
            }
        }

        private List<string> BuildVariableDeclaration(VariableDeclarationSyntax decl, List<string> incoming)
        {
            var continuation = incoming;
            foreach (var v in decl.Variables)
            {
                if (v.Initializer?.Value is { } initializerExpr)
                    continuation = BuildExpressionInvocations(initializerExpr, continuation);
            }

            return continuation;
        }

        private List<string> BuildUsingStatement(UsingStatementSyntax us, List<string> incoming)
        {
            var continuation = incoming;
            if (us.Declaration is not null)
                continuation = BuildVariableDeclaration(us.Declaration, continuation);
            else if (us.Expression is not null)
                continuation = BuildExpressionInvocations(us.Expression, continuation);

            return BuildStatement(us.Statement, continuation);
        }

        /// <summary>Высокоуровневый шаг: узел условия (ромб) и рёбра от входящих потоков.</summary>
        private string? AddConditionStep(string conditionExpressionText, List<string> incoming, SyntaxNode? syntax = null)
        {
            var condLine = GraphDocumentBlueprint.SanitizeLegendLine(conditionExpressionText, 200);
            var conditionId = AddNode("condition_step", "IF", "if condition", condLine, syntax);
            if (conditionId is null)
                return null;
            AddEdges(incoming, conditionId, "ConditionalCall", "ConditionalCall");
            return conditionId;
        }

        /// <summary>Высокоуровневый шаг: return — выход из метода; продолжение потока пустое.</summary>
        private List<string> AddReturnExit(List<string> incoming, SyntaxNode? syntax)
        {
            var returnId = AddNode("exit_step", "RET", "return", "return", syntax);
            if (returnId is null)
                return incoming;

            AddEdges(incoming, returnId, "Exit", "Exit");
            return [];
        }

        private List<string> BuildExpressionInvocations(SyntaxNode expressionOwner, List<string> incoming)
        {
            var invocations = expressionOwner
                .DescendantNodesAndSelf(descendIntoChildren: _ => true)
                .Where(n => n is InvocationExpressionSyntax)
                .Select(n => (InvocationExpressionSyntax)n)
                .OrderBy(i => i.Span.End)
                .Where(i => !ShouldSkipInvocation(i))
                .ToList();
            if (invocations.Count == 0)
            {
                if (expressionOwner is ExpressionSyntax exprOnly)
                {
                    var legend = GraphDocumentBlueprint.SanitizeLegendLine(exprOnly.ToString(), 200);
                    var nodeId = AddNode("call_step", ExpressionShortLabel(exprOnly), "expr", legend, exprOnly);
                    if (nodeId is null)
                        return incoming;
                    var edgeKind = DetectEdgeKindForExpression(exprOnly);
                    AddEdges(incoming, nodeId, edgeKind, edgeKind);
                    return [nodeId];
                }

                return incoming;
            }

            var continuation = incoming;
            foreach (var invocation in invocations)
            {
                var label = ExtractInvocationLabel(invocation);
                var legend = GraphDocumentBlueprint.SanitizeLegendLine(invocation.ToString(), 200);
                var nodeId = AddNode("call_step", label, $"call {label}", legend, invocation);
                if (nodeId is null)
                    return continuation;

                var edgeKind = DetectEdgeKind(invocation);
                AddEdges(continuation, nodeId, edgeKind, edgeKind);
                continuation = [nodeId];
            }

            return continuation;
        }

        private string? AddNode(string kind, string label, string rationale, string? legendLine = null, SyntaxNode? syntax = null)
        {
            var (lineStart, lineEnd) = ResolveLineRange(syntax);
            return _graph.TryAddNode(
                kind,
                _filePath,
                label,
                "",
                rationale,
                legendLine,
                assignControlFlowLegendIndex: true,
                lineStart,
                lineEnd,
                _currentLoopGroupId);
        }

        private (int? lineStart, int? lineEnd) ResolveLineRange(SyntaxNode? syntax)
        {
            if (syntax is null || _syntaxTree is null)
                return (null, null);

            syntax = syntax switch
            {
                InvocationExpressionSyntax inv => inv.Expression switch
                {
                    MemberAccessExpressionSyntax member => member.Name,
                    IdentifierNameSyntax or GenericNameSyntax => inv.Expression,
                    _ => inv
                },
                _ => syntax
            };

            var span = _syntaxTree.GetLineSpan(syntax.Span);
            var start = span.StartLinePosition.Line + 1;
            var end = span.EndLinePosition.Line + 1;
            return (start, Math.Max(start, end));
        }

        private void AddEdges(List<string> fromIds, string toId, string kind, string relatedKind) =>
            _graph.AddEdges(fromIds, toId, kind, relatedKind);
    }

    private static string DetectEdgeKind(InvocationExpressionSyntax invocation) =>
        DetectEdgeKindForExpression(invocation);

    private static string DetectEdgeKindForExpression(ExpressionSyntax expr)
    {
        if (expr.Ancestors().Any(static a =>
                a is ForStatementSyntax
                or WhileStatementSyntax
                or DoStatementSyntax
                or CommonForEachStatementSyntax))
            return "LoopCall";
        if (expr.Ancestors().Any(static a => a is SwitchStatementSyntax or SwitchExpressionSyntax))
            return "MultiBranch";
        if (IsConditionalContext(expr))
            return "ConditionalCall";
        return "Call";
    }

    private static string ExpressionShortLabel(ExpressionSyntax expr) =>
        expr switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            InvocationExpressionSyntax inv => ExtractInvocationLabel(inv),
            PostfixUnaryExpressionSyntax p => p.OperatorToken.Text + "…",
            PrefixUnaryExpressionSyntax p => p.OperatorToken.Text + "…",
            AssignmentExpressionSyntax => "=",
            LiteralExpressionSyntax lit => lit.Token.ValueText.Length > 12
                ? lit.Token.ValueText[..12] + "…"
                : lit.Token.ValueText,
            _ => "expr"
        };

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

}

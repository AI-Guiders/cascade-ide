#nullable enable
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Models;
using CascadeIDE.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeIDE.Services.CodeNavigation;

/// <summary>
/// Subgraph «намерение метода»: крупное зерно (операторы, целиком заголовки циклов без literal/i++/цепочек выражений внутри них).
/// Сверху ADR <see href="../../../docs/adr/0053-semantic-map-control-flow-pfd.md">0053</see>.
/// Контракт JSON тот же, что у <see cref="CodeNavigationControlFlowSubgraphBuilder"/>.
/// </summary>
public static class CodeNavigationMethodIntentSubgraphBuilder
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
            text = sourceText!;
        else if (File.Exists(filePath))
            text = File.ReadAllText(filePath!);
        else
            return """{"error":"no_file","message":"File not found."}""";

        var tree = CSharpSyntaxTree.ParseText(text);
        var root = tree.GetRoot();
        if (root is null)
            return """{"error":"parse_error","message":"Unable to parse syntax tree."}""";

        var scope = CodeNavigationControlFlowScopeResolver.TryFindScope(root, tree, line, column);
        if (scope is null)
            return CodeNavigationControlFlowSubgraphBuilder.BuildNoScopeAtCursorJson(filePath!, line is > 0);

        var nodeCap = Math.Max(2, maxNodes <= 0 ? CodeNavigationContextBuilder.DefaultControlFlowSubgraphMaxNodes : maxNodes);
        var edgeCap = Math.Max(1, maxEdges <= 0 ? CodeNavigationContextBuilder.DefaultControlFlowSubgraphMaxEdges : maxEdges);

        var graph = new MethodIntentGraphBuilder(filePath!, scope.ScopeLabel, nodeCap, edgeCap);
        graph.Build(scope);

        return CodeNavigationControlFlowSubgraphBuilder.SerializeSubgraphPayload(filePath!, graph.Blueprint);
    }

    private sealed class MethodIntentGraphBuilder
    {
        private readonly string _filePath;
        private readonly GraphDocumentBlueprint _graph;
        private SyntaxTree? _syntaxTree;
        private int _nextLoopGroupId = 1;
        private int? _currentLoopGroupId;
        private string? _pendingBranchProvenance;

        public GraphDocumentBlueprint Blueprint => _graph;

        public List<GraphBuildNode> Nodes => _graph.Nodes;

        public List<GraphBuildEdge> Edges => _graph.Edges;

        public MethodIntentGraphBuilder(string filePath, string methodName, int nodeCap, int edgeCap)
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

        public void Build(CodeNavigationControlFlowScope scope)
        {
            _syntaxTree = scope.SyntaxTree;
            var continuation = new List<string> { "n0" };
            _ = BuildStatementEnumerable(CodeNavigationControlFlowScopeStatements.Enumerate(scope), continuation);
        }

        private List<string> BuildStatementEnumerable(
            IEnumerable<StatementSyntax> statements,
            List<string> incoming)
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
                WhileStatementSyntax ws => BuildWhileLoopSketch(ws, incoming),
                DoStatementSyntax ds => BuildDoWhileLoopSketch(ds, incoming),
                ForStatementSyntax fs => BuildForLoopSketch(fs, incoming),
                ForEachStatementSyntax fe => BuildForEachLoopSketch(fe, fe.Statement, incoming),
                ForEachVariableStatementSyntax fev => BuildForEachLoopSketch(fev, fev.Statement, incoming),
                ExpressionStatementSyntax es => BuildExpressionStatementSketch(es, incoming),
                LocalDeclarationStatementSyntax localDecl => BuildLocalDeclarationSketch(localDecl, incoming),
                LockStatementSyntax lk => BuildStatement(lk.Statement, incoming),
                UsingStatementSyntax us => BuildUsingStatement(us, incoming),
                UnsafeStatementSyntax u => BuildStatement(u.Block, incoming),
                CheckedStatementSyntax c => c.Block is not null
                    ? BuildStatement(c.Block, incoming)
                    : incoming,
                FixedStatementSyntax f => BuildStatement(f.Statement, incoming),
                LabeledStatementSyntax l => BuildStatement(l.Statement, incoming),
                _ => BuildGenericStatementSketch(statement, incoming)
            };
        }

        private List<string> BuildGenericStatementSketch(StatementSyntax statement, List<string> incoming)
        {
            var legend = GraphDocumentBlueprint.SanitizeLegendLine(statement.ToString(), 200);
            var id = AddNode("call_step", "stmt", "statement", legend, statement);
            if (id is null)
                return incoming;
            var kind = EdgeKindForStatement(statement);
            AddEdges(incoming, id, kind, kind);
            return [id];
        }

        private List<string> BuildExpressionStatementSketch(ExpressionStatementSyntax es, List<string> incoming)
        {
            var expr = es.Expression;
            var label = ExpressionShortLabel(expr);
            var legend = GraphDocumentBlueprint.SanitizeLegendLine(es.ToString(), 200);
            var id = AddNode("call_step", label, "expr", legend, es);
            if (id is null)
                return incoming;
            var kind = DetectEdgeKindForExpression(expr);
            AddEdges(incoming, id, kind, kind);
            return [id];
        }

        private List<string> BuildSingleExpressionStatement(ExpressionSyntax expr, List<string> incoming)
        {
            var label = ExpressionShortLabel(expr);
            var legend = GraphDocumentBlueprint.SanitizeLegendLine(expr.ToString(), 200);
            var id = AddNode("call_step", label, "expr", legend, expr);
            if (id is null)
                return incoming;
            var kind = DetectEdgeKindForExpression(expr);
            AddEdges(incoming, id, kind, kind);
            return [id];
        }

        private List<string> BuildLocalDeclarationSketch(LocalDeclarationStatementSyntax localDecl, List<string> incoming)
        {
            var legend = GraphDocumentBlueprint.SanitizeLegendLine(localDecl.ToString(), 200);
            var id = AddNode("call_step", "var", "local decl", legend, localDecl);
            if (id is null)
                return incoming;
            AddEdges(incoming, id, "Sequential", "Sequential");
            return [id];
        }

        private List<string> BuildUsingStatement(UsingStatementSyntax us, List<string> incoming)
        {
            var continuation = incoming;
            if (us.Declaration is not null)
            {
                var legend = GraphDocumentBlueprint.SanitizeLegendLine(
                    $"using ({us.Declaration})".Replace('\r', ' ').Replace('\n', ' '),
                    200);
                var id = AddNode("call_step", "using", "using", legend, us.Declaration);
                if (id is not null)
                {
                    AddEdges(continuation, id, "Sequential", "Sequential");
                    continuation = [id];
                }
            }
            else if (us.Expression is not null)
            {
                continuation = BuildExpressionStatementSketch(
                    SyntaxFactory.ExpressionStatement(us.Expression),
                    continuation);
            }

            return BuildStatement(us.Statement, continuation);
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

            _pendingBranchProvenance = CodeNavigationMapConditionBranchProvenance.True;
            var thenContinuation = BuildStatement(ifStatement.Statement, fromCondition);
            List<string> elseContinuation;
            if (ifStatement.Else is null)
            {
                elseContinuation = fromCondition;
            }
            else
            {
                _pendingBranchProvenance = CodeNavigationMapConditionBranchProvenance.False;
                elseContinuation = BuildStatement(ifStatement.Else.Statement, fromCondition);
            }

            _pendingBranchProvenance = null;

            return thenContinuation
                .Concat(elseContinuation)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private List<string> BuildReturnStatement(List<string> incoming, ReturnStatementSyntax statement) =>
            AddReturnExit(incoming, statement);

        private List<string> AddReturnExit(List<string> incoming, SyntaxNode? syntax)
        {
            var returnId = AddNode("exit_step", "RET", "return", "return", syntax);
            if (returnId is null)
                return incoming;

            AddEdges(incoming, returnId, "Exit", "Exit");
            return [];
        }

        private List<string> BuildForLoopSketch(ForStatementSyntax fs, List<string> incoming)
        {
            if (incoming.Count == 0)
                return incoming;

            var saved = _currentLoopGroupId;
            _currentLoopGroupId = _nextLoopGroupId++;
            try
            {
                var legend = GraphDocumentBlueprint.SanitizeLegendLine(fs.ToString(), 200);
                var hubId = AddNode("loop_step", "for", "for loop", legend, fs);
                if (hubId is null)
                    return incoming;

                AddEdges(incoming, hubId, "Sequential", "Sequential");
                var bodyOut = BuildStatement(fs.Statement, new List<string> { hubId });
                AddEdges(bodyOut, hubId, "LoopBack", "LoopBack");
                return [hubId];
            }
            finally
            {
                _currentLoopGroupId = saved;
            }
        }

        private List<string> BuildWhileLoopSketch(WhileStatementSyntax ws, List<string> incoming)
        {
            if (incoming.Count == 0)
                return incoming;

            var saved = _currentLoopGroupId;
            _currentLoopGroupId = _nextLoopGroupId++;
            try
            {
                var legend = GraphDocumentBlueprint.SanitizeLegendLine(ws.ToString(), 200);
                var hubId = AddNode("loop_step", "while", "while loop", legend, ws);
                if (hubId is null)
                    return incoming;

                AddEdges(incoming, hubId, "Sequential", "Sequential");
                var bodyOut = BuildStatement(ws.Statement, new List<string> { hubId });
                AddEdges(bodyOut, hubId, "LoopBack", "LoopBack");
                return [hubId];
            }
            finally
            {
                _currentLoopGroupId = saved;
            }
        }

        private List<string> BuildDoWhileLoopSketch(DoStatementSyntax ds, List<string> incoming)
        {
            if (incoming.Count == 0)
                return incoming;

            var saved = _currentLoopGroupId;
            _currentLoopGroupId = _nextLoopGroupId++;
            try
            {
                var legend = GraphDocumentBlueprint.SanitizeLegendLine(ds.ToString(), 200);
                var hubId = AddNode("loop_step", "do", "do-while loop", legend, ds);
                if (hubId is null)
                    return incoming;

                AddEdges(incoming, hubId, "Sequential", "Sequential");
                var bodyOut = BuildStatement(ds.Statement, new List<string> { hubId });
                AddEdges(bodyOut, hubId, "LoopBack", "LoopBack");
                return [hubId];
            }
            finally
            {
                _currentLoopGroupId = saved;
            }
        }

        private List<string> BuildForEachLoopSketch(
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
                var legend = GraphDocumentBlueprint.SanitizeLegendLine(wholeForEach.ToString(), 200);
                var hubId = AddNode("loop_step", "foreach", "foreach", legend, wholeForEach);
                if (hubId is null)
                    return incoming;

                AddEdges(incoming, hubId, "Sequential", "Sequential");
                var bodyOut = BuildStatement(body, new List<string> { hubId });
                AddEdges(bodyOut, hubId, "LoopBack", "LoopBack");
                return [hubId];
            }
            finally
            {
                _currentLoopGroupId = saved;
            }
        }

        private string? AddConditionStep(string conditionExpressionText, List<string> incoming, SyntaxNode? syntax = null)
        {
            var condLine = GraphDocumentBlueprint.SanitizeLegendLine(conditionExpressionText, 200);
            var conditionId = AddNode("condition_step", "IF", "if condition", condLine, syntax);
            if (conditionId is null)
                return null;
            AddEdges(incoming, conditionId, "ConditionalCall", "ConditionalCall");
            return conditionId;
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

        private void AddEdges(List<string> fromIds, string toId, string kind, string relationKind)
        {
            var provenance = _pendingBranchProvenance;
            if (provenance is not null)
            {
                kind = "ConditionalCall";
                relationKind = "ConditionalCall";
            }

            _graph.AddEdges(fromIds, toId, kind, relationKind, provenance);
            _pendingBranchProvenance = null;
        }

        private static string EdgeKindForStatement(StatementSyntax stmt) =>
            stmt.Ancestors().Any(static a => a is SwitchStatementSyntax or SwitchExpressionSyntax)
                ? "MultiBranch"
                :stmt.Ancestors().Any(static a =>
                    a is ForStatementSyntax or WhileStatementSyntax or DoStatementSyntax or CommonForEachStatementSyntax)
                    ? "LoopCall"
                    : "Call";

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

        private static string ExtractInvocationLabel(InvocationExpressionSyntax invocation) =>
            invocation.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
                IdentifierNameSyntax id => id.Identifier.Text,
                GenericNameSyntax generic => generic.Identifier.Text,
                _ => invocation.Expression.ToString()
            };
    }
}

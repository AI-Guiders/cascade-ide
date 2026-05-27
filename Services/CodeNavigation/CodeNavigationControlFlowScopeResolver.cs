#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeIDE.Services.CodeNavigation;

/// <summary>
/// Область control-flow: тело метода или top-level statements (C# 9+).
/// </summary>
internal sealed record CodeNavigationControlFlowScope(
    SyntaxTree SyntaxTree,
    SyntaxList<StatementSyntax> Statements,
    IReadOnlyList<GlobalStatementSyntax> TopLevelStatements,
    string ScopeLabel)
{
    public bool IsTopLevel => TopLevelStatements.Count > 0;
}

internal static class CodeNavigationControlFlowScopeStatements
{
    public static IEnumerable<StatementSyntax> Enumerate(CodeNavigationControlFlowScope scope)
    {
        if (scope.IsTopLevel)
        {
            foreach (var global in scope.TopLevelStatements)
            {
                var statement = UnwrapGlobalStatement(global);
                if (statement is not null)
                    yield return statement;
            }

            yield break;
        }

        foreach (var statement in scope.Statements)
            yield return statement;
    }

    private static StatementSyntax? UnwrapGlobalStatement(GlobalStatementSyntax global) =>
        global.ChildNodes().OfType<StatementSyntax>().FirstOrDefault();
}

internal static class CodeNavigationControlFlowScopeResolver
{
    public static bool TryGetTextPosition(SyntaxNode root, SyntaxTree tree, int? line, int? column, out int position)
    {
        position = 0;
        if (line is null or <= 0)
            return false;

        var linePos = Math.Max(0, line.Value - 1);
        var colPos = Math.Max(0, (column ?? 1) - 1);
        var text = tree.GetText();
        if (linePos >= text.Lines.Count)
            return false;

        position = text.Lines[linePos].Start + Math.Min(colPos, text.Lines[linePos].Span.Length);
        return true;
    }

    public static CodeNavigationControlFlowScope? TryFindScope(
        SyntaxNode root,
        SyntaxTree tree,
        int? line,
        int? column)
    {
        if (!TryGetTextPosition(root, tree, line, column, out var pos))
            return null;

        var token = root.FindToken(pos);
        var method = token.Parent?.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (method is not null)
            return ScopeFromMethod(method);

        var localFunction = token.Parent?.AncestorsAndSelf().OfType<LocalFunctionStatementSyntax>().FirstOrDefault();
        if (localFunction is not null)
            return ScopeFromLocalFunction(localFunction);

        if (root is CompilationUnitSyntax cu)
        {
            if (token.Parent?.AncestorsAndSelf().OfType<GlobalStatementSyntax>().Any() == true
                || cu.Members.OfType<GlobalStatementSyntax>().Any(g => g.Span.Contains(pos)))
            {
                return ScopeFromTopLevel(cu);
            }
        }

        return null;
    }

    private static CodeNavigationControlFlowScope? ScopeFromLocalFunction(LocalFunctionStatementSyntax localFunction)
    {
        if (localFunction.Body is null)
            return null;

        return new CodeNavigationControlFlowScope(
            localFunction.SyntaxTree,
            localFunction.Body.Statements,
            [],
            localFunction.Identifier.Text);
    }

    private static CodeNavigationControlFlowScope? ScopeFromMethod(MethodDeclarationSyntax method)
    {
        if (method.Body is not null)
        {
            return new CodeNavigationControlFlowScope(
                method.SyntaxTree,
                method.Body.Statements,
                [],
                method.Identifier.Text);
        }

        if (method.ExpressionBody is null)
            return null;

        var expr = method.ExpressionBody.Expression;
        var synthetic = SyntaxFactory.ExpressionStatement(expr);
        return new CodeNavigationControlFlowScope(
            method.SyntaxTree,
            SyntaxFactory.SingletonList<StatementSyntax>(synthetic),
            [],
            method.Identifier.Text);
    }

    private static CodeNavigationControlFlowScope? ScopeFromTopLevel(CompilationUnitSyntax cu)
    {
        var globals = cu.Members.OfType<GlobalStatementSyntax>().ToList();
        if (globals.Count == 0)
            return null;

        return new CodeNavigationControlFlowScope(
            cu.SyntaxTree,
            default,
            globals,
            "top-level");
    }
}

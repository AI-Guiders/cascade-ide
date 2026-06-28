using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services;

public sealed partial class CSharpLanguageService
{
    public sealed record ReferenceLocation(string FilePath, int Line, int Column);

    /// <summary>Find references in the current file (Roslyn fast path).</summary>
    public IReadOnlyList<ReferenceLocation> FindReferencesInFile(
        string filePath,
        string sourceText,
        int line,
        int column,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1 || column < 1)
            return [];

        try
        {
            var text = SourceText.From(sourceText);
            var model = GetOrCreateModel(filePath, text, ct);
            var lines = text.Lines;
            if (line > lines.Count)
                return [];

            var lineInfo = lines[line - 1];
            var position = lineInfo.Start + Math.Min(Math.Max(0, column - 1), lineInfo.Span.Length);
            var root = model.SyntaxTree.GetRoot(ct);
            var symbol = TryResolveSymbolAtPosition(model, root, position, ct);
            if (symbol is null)
                return [];

            var seen = new HashSet<(int line, int col)>();
            var list = new List<ReferenceLocation>();
            foreach (var node in root.DescendantNodes())
            {
                if (!TryGetBoundSymbol(model, node, ct, out var nodeSymbol))
                    continue;
                if (!SymbolEqualityComparer.Default.Equals(nodeSymbol, symbol))
                    continue;

                var span = GetReferenceSpan(node);
                if (span.Length == 0)
                    continue;

                var pos = text.Lines.GetLinePosition(span.Start);
                var key = (pos.Line, pos.Character);
                if (!seen.Add(key))
                    continue;

                list.Add(new ReferenceLocation(filePath, pos.Line + 1, pos.Character + 1));
            }

            list.Sort(static (a, b) =>
            {
                var lineCmp = a.Line.CompareTo(b.Line);
                return lineCmp != 0 ? lineCmp : a.Column.CompareTo(b.Column);
            });
            return list;
        }
        catch
        {
            return [];
        }
    }

    private static TextSpan GetReferenceSpan(SyntaxNode node) =>
        node switch
        {
            IdentifierNameSyntax id => id.Identifier.Span,
            GenericNameSyntax generic => generic.Identifier.Span,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Span,
            MemberAccessExpressionSyntax member => member.Name.Identifier.Span,
            TypeDeclarationSyntax typeDecl => typeDecl.Identifier.Span,
            EnumDeclarationSyntax enumDecl => enumDecl.Identifier.Span,
            DelegateDeclarationSyntax delegateDecl => delegateDecl.Identifier.Span,
            _ => default,
        };

    private static bool TryGetBoundSymbol(
        SemanticModel model,
        SyntaxNode node,
        CancellationToken ct,
        out ISymbol? symbol)
    {
        symbol = node switch
        {
            BaseTypeDeclarationSyntax or DelegateDeclarationSyntax
                => model.GetDeclaredSymbol(node, ct),
            _ => model.GetSymbolInfo(node, ct).Symbol ?? model.GetSymbolInfo(node, ct).CandidateSymbols.FirstOrDefault(),
        };
        return symbol is not null;
    }

    private static ISymbol? TryResolveSymbolAtPosition(
        SemanticModel model,
        SyntaxNode root,
        int position,
        CancellationToken ct)
    {
        var node = root.FindNode(TextSpan.FromBounds(position, position), getInnermostNodeForTie: true);
        for (var current = node; current is not null; current = current.Parent)
        {
            if (TryGetBoundSymbol(model, current, ct, out var symbol))
                return symbol;
        }

        return null;
    }
}

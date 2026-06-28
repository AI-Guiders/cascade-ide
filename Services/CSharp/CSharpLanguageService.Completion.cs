using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services;

public sealed partial class CSharpLanguageService
{
    /// <summary>Возвращает предложения автодополнения в позиции (1-based line, column). Выполнять в фоне.</summary>
    public IReadOnlyList<CompletionItem> GetCompletionItems(string filePath, string sourceText, int line, int column, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(filePath) || line < 1 || column < 1)
            return [];

        var text = SourceText.From(sourceText);
        var textHash = GetStableHash(text);
        var cacheKey = (filePath, textHash, line, column);
        if (_completionCache.TryGetValue(cacheKey, out var cached))
            return cached;

        try
        {
            var model = GetOrCreateModel(filePath, text, ct);
            var lines = text.Lines;
            if (line > lines.Count)
                return [];

            var lineInfo = lines[line - 1];
            var colIndex = column - 1;
            var position = lineInfo.Start + Math.Min(Math.Max(0, colIndex), lineInfo.Span.Length);

            var root = model.SyntaxTree.GetRoot(ct);
            var token = root.FindToken(position, findInsideTrivia: true);
            var prefix = GetCompletionPrefix(token, position);

            List<CompletionItem> list;
            if (TryIsNamespaceDeclarationNamePosition(root, model.SyntaxTree, position))
            {
                list = CollectNamespaceNameCompletions(model, root, position, prefix, ct);
            }
            else if (TryGetMemberAccessExpression(root, position, out var memberTarget))
            {
                list = CollectMemberCompletions(model, memberTarget, position, prefix, ct);
            }
            else
            {
                list = CollectScopeCompletions(model, position, prefix, ct);
            }

            TrimCaches(_completionCache);
            _completionCache[cacheKey] = list;
            return list;
        }
        catch
        {
            return [];
        }
    }

    private static string GetCompletionPrefix(SyntaxToken token, int position)
    {
        if (token.IsKind(SyntaxKind.IdentifierToken) && token.Span.Contains(position))
            return token.Text[..Math.Min(token.Text.Length, Math.Max(0, position - token.Span.Start))];
        return "";
    }

    private static bool MatchesPrefix(string name, string prefix) =>
        CSharpCompletionMatcher.Matches(name, prefix);

    private static int CompareCompletionNames(string nameA, string nameB, string prefix) =>
        CSharpCompletionMatcher.CompareByRelevance(nameA, nameB, prefix);

    private static bool TryGetMemberAccessExpression(SyntaxNode root, int position, out ExpressionSyntax expression)
    {
        expression = null!;
        var token = root.FindToken(position, findInsideTrivia: true);
        if (token.IsKind(SyntaxKind.DotToken) && token.Parent is MemberAccessExpressionSyntax dotAccess)
        {
            expression = dotAccess.Expression;
            return true;
        }

        var node = root.FindNode(TextSpan.FromBounds(position, position), getInnermostNodeForTie: true);
        while (node is not null)
        {
            if (node is MemberAccessExpressionSyntax memberAccess
                && memberAccess.OperatorToken.IsKind(SyntaxKind.DotToken)
                && memberAccess.OperatorToken.Span.End <= position)
            {
                expression = memberAccess.Expression;
                return true;
            }

            node = node.Parent;
        }

        return false;
    }

    private static bool TryIsNamespaceDeclarationNamePosition(SyntaxNode root, SyntaxTree tree, int position)
    {
        var node = root.FindNode(TextSpan.FromBounds(position, position), getInnermostNodeForTie: true);
        for (var current = node; current is not null; current = current.Parent)
        {
            if (current is not BaseNamespaceDeclarationSyntax nsDecl)
                continue;

            if (nsDecl is NamespaceDeclarationSyntax block
                && position >= block.OpenBraceToken.SpanStart)
                return false;

            return position >= nsDecl.NamespaceKeyword.Span.End;
        }

        return IsIncompleteNamespaceDeclarationLine(tree, position);
    }

    private static bool IsIncompleteNamespaceDeclarationLine(SyntaxTree tree, int position)
    {
        var line = tree.GetText().Lines.GetLineFromPosition(position);
        var offset = position - line.Start;
        if (offset < 0 || offset > line.Span.Length)
            return false;

        var beforeCursor = line.ToString()[..offset].TrimStart();
        if (!beforeCursor.StartsWith("namespace", StringComparison.Ordinal))
            return false;

        if (beforeCursor.Length == "namespace".Length)
            return true;

        if (beforeCursor.Length <= "namespace".Length || beforeCursor["namespace".Length] != ' ')
            return false;

        var namePart = beforeCursor["namespace".Length..].TrimStart();
        var terminator = namePart.IndexOfAny(['{', ';']);
        if (terminator >= 0)
            namePart = namePart[..terminator].TrimEnd();

        if (namePart.Length == 0)
            return true;

        foreach (var ch in namePart)
        {
            if (ch != '.' && ch != '_' && !char.IsLetterOrDigit(ch))
                return false;
        }

        return true;
    }

    private static List<CompletionItem> CollectNamespaceNameCompletions(
        SemanticModel model,
        SyntaxNode root,
        int position,
        string prefix,
        CancellationToken ct)
    {
        var list = new List<CompletionItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var searchRoot = ResolveNamespaceContainer(model, root, position, ct);

        foreach (var child in searchRoot.GetNamespaceMembers())
        {
            if (!MatchesPrefix(child.Name, prefix) || !seen.Add(child.Name))
                continue;

            list.Add(new CompletionItem(
                child.Name,
                child.Name,
                child.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                CSharpCompletionKind.Other));
        }

        list.Sort((a, b) => CompareCompletionNames(a.DisplayText, b.DisplayText, prefix));
        return list;
    }

    private static INamespaceSymbol ResolveNamespaceContainer(
        SemanticModel model,
        SyntaxNode root,
        int position,
        CancellationToken ct)
    {
        var token = root.FindToken(position, findInsideTrivia: true);
        if (token.IsKind(SyntaxKind.DotToken) && token.Parent is QualifiedNameSyntax dotParent)
            return ResolveNamespaceFromQualifiedLeft(model, dotParent.Left, position, ct)
                ?? model.Compilation.GlobalNamespace;

        var node = root.FindNode(TextSpan.FromBounds(position, position), getInnermostNodeForTie: true);
        for (var current = node; current is not null; current = current.Parent)
        {
            if (current is QualifiedNameSyntax qualified
                && position > qualified.DotToken.Span.End)
            {
                return ResolveNamespaceFromQualifiedLeft(model, qualified.Left, position, ct)
                    ?? model.Compilation.GlobalNamespace;
            }
        }

        return model.Compilation.GlobalNamespace;
    }

    private static INamespaceSymbol? ResolveNamespaceFromQualifiedLeft(
        SemanticModel model,
        NameSyntax left,
        int position,
        CancellationToken ct)
    {
        if (model.GetSymbolInfo(left, ct).Symbol is INamespaceSymbol ns)
            return ns;

        if (left is IdentifierNameSyntax id)
        {
            foreach (var symbol in model.LookupSymbols(position, name: id.Identifier.Text))
            {
                if (symbol is INamespaceSymbol lookupNs)
                    return lookupNs;
            }
        }

        return null;
    }

    private static List<CompletionItem> CollectMemberCompletions(
        SemanticModel model,
        ExpressionSyntax targetExpression,
        int position,
        string prefix,
        CancellationToken ct)
    {
        var list = new List<CompletionItem>();
        var typeInfo = model.GetTypeInfo(targetExpression, ct);
        var type = typeInfo.Type ?? typeInfo.ConvertedType;
        if (type is null || type.TypeKind == TypeKind.Error)
            return list;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in type.GetMembers().Where(m => m.CanBeReferencedByName && !m.IsImplicitlyDeclared))
        {
            if (!model.IsAccessible(position, member))
                continue;
            if (!seen.Add(member.Name))
                continue;
            if (!MatchesPrefix(member.Name, prefix))
                continue;

            var item = CreateCompletionItem(member);
            if (item is not null)
                list.Add(item);
        }

        list.Sort((a, b) => CompareCompletionNames(a.DisplayText, b.DisplayText, prefix));
        return list;
    }

    private static List<CompletionItem> CollectScopeCompletions(
        SemanticModel model,
        int position,
        string prefix,
        CancellationToken ct)
    {
        var list = new List<CompletionItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var symbol in model.LookupSymbols(position, name: null, includeReducedExtensionMethods: true))
        {
            if (!IsScopeCompletionSymbol(symbol))
                continue;
            if (!seen.Add(symbol.Name) || !MatchesPrefix(symbol.Name, prefix))
                continue;

            var item = CreateCompletionItem(symbol);
            if (item is not null)
                list.Add(item);
        }

        foreach (var typeSymbol in model.LookupNamespacesAndTypes(position, name: null))
        {
            if (!seen.Add(typeSymbol.Name) || !MatchesPrefix(typeSymbol.Name, prefix))
                continue;

            var item = CreateCompletionItem(typeSymbol);
            if (item is not null)
                list.Add(item);
        }

        foreach (var keyword in CSharpCompletionKeywords.All)
        {
            if (!MatchesPrefix(keyword, prefix) || !seen.Add(keyword))
                continue;
            list.Add(new CompletionItem(keyword, keyword, "keyword", CSharpCompletionKind.Keyword));
        }

        list.Sort((a, b) => CompareCompletionNames(a.DisplayText, b.DisplayText, prefix));
        return list;
    }

    private static CompletionItem? CreateCompletionItem(ISymbol member)
    {
        switch (member)
        {
            case IMethodSymbol method when method.MethodKind is MethodKind.Ordinary or MethodKind.LocalFunction:
            {
                var insert = method.Parameters.Length > 0 ? $"{method.Name}(" : method.Name;
                return new CompletionItem(method.Name, insert, method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), CSharpCompletionKind.Method);
            }
            case IPropertySymbol property:
                return new CompletionItem(property.Name, property.Name, property.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), CSharpCompletionKind.Property);
            case IFieldSymbol field when field.ContainingType?.TypeKind == TypeKind.Enum:
                return new CompletionItem(field.Name, field.Name, field.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), CSharpCompletionKind.EnumMember);
            case IFieldSymbol field:
                return new CompletionItem(field.Name, field.Name, field.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), CSharpCompletionKind.Field);
            case IEventSymbol ev:
                return new CompletionItem(ev.Name, ev.Name, ev.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), CSharpCompletionKind.Event);
            case INamedTypeSymbol typeSymbol:
                return new CompletionItem(typeSymbol.Name, typeSymbol.Name, typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), MapTypeKind(typeSymbol.TypeKind));
            case ILocalSymbol local:
                return new CompletionItem(local.Name, local.Name, local.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), CSharpCompletionKind.Variable);
            case IParameterSymbol parameter:
                return new CompletionItem(parameter.Name, parameter.Name, parameter.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), CSharpCompletionKind.Variable);
            case IRangeVariableSymbol range:
                return new CompletionItem(range.Name, range.Name, range.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), CSharpCompletionKind.Variable);
            default:
                return null;
        }
    }

    private static bool IsScopeCompletionSymbol(ISymbol symbol) =>
        symbol switch
        {
            ILocalSymbol or IParameterSymbol or IRangeVariableSymbol => true,
            IFieldSymbol or IPropertySymbol or IEventSymbol => true,
            IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction } => true,
            _ => false,
        };

    private static CSharpCompletionKind MapTypeKind(TypeKind kind) =>
        kind switch
        {
            TypeKind.Class => CSharpCompletionKind.Class,
            TypeKind.Interface => CSharpCompletionKind.Interface,
            TypeKind.Struct => CSharpCompletionKind.Struct,
            TypeKind.Delegate => CSharpCompletionKind.Delegate,
            TypeKind.Enum => CSharpCompletionKind.Enum,
            _ => CSharpCompletionKind.Other,
        };
}

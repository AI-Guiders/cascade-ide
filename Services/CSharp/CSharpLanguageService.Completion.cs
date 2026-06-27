using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services;

public sealed partial class CSharpLanguageService
{
    private static readonly string[] StatementKeywords =
    [
        "abstract", "async", "await", "base", "bool", "break", "byte", "case", "catch", "char", "class",
        "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
        "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "get", "goto", "if",
        "implicit", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
        "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly",
        "ref", "return", "sbyte", "sealed", "set", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
        "unsafe", "ushort", "using", "var", "virtual", "void", "volatile", "while",
    ];

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
            if (TryGetMemberAccessExpression(root, position, out var memberTarget))
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
        string.IsNullOrEmpty(prefix)
        || name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

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

        list.Sort(static (a, b) => string.Compare(a.DisplayText, b.DisplayText, StringComparison.OrdinalIgnoreCase));
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

        foreach (var keyword in StatementKeywords)
        {
            if (!MatchesPrefix(keyword, prefix) || !seen.Add(keyword))
                continue;
            list.Add(new CompletionItem(keyword, keyword, "keyword", CSharpCompletionKind.Keyword));
        }

        list.Sort(static (a, b) => string.Compare(a.DisplayText, b.DisplayText, StringComparison.OrdinalIgnoreCase));
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

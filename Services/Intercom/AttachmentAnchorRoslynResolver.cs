#nullable enable

using System.Text;
using CascadeIDE.Models.Editor;
using CascadeIDE.Models.Intercom;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services.Intercom;

/// <summary>Re-resolve <see cref="AttachmentAnchor"/> member / syntaxScope в файле получателя (ADR 0130 фаза 2).</summary>
public static class AttachmentAnchorRoslynResolver
{
    public static bool TryResolveLineRange(
        string absoluteFilePath,
        string? memberKey,
        AttachmentSyntaxScope? syntaxScope,
        out LineRange lines,
        out string detail) =>
        TryResolveLineRange(null, absoluteFilePath, memberKey, syntaxScope, out lines, out detail);

    public static bool TryResolveLineRange(
        IntercomAttachmentRoslynResolveSession? session,
        string absoluteFilePath,
        string? memberKey,
        AttachmentSyntaxScope? syntaxScope,
        out LineRange lines,
        out string detail)
    {
        lines = default;
        detail = "";

        if (!absoluteFilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            detail = "not_csharp";
            return false;
        }

        if (!TryGetOrCreateEntry(session, absoluteFilePath, out var entry, out detail))
            return false;

        var tree = entry.Tree;
        var root = tree.GetCompilationUnitRoot();
        if (root is null)
        {
            detail = "parse_error";
            return false;
        }

        var model = entry.Model;

        if (syntaxScope is not null)
        {
            var parentKey = syntaxScope.ParentMemberKey ?? memberKey;
            if (!tryResolveSyntaxScope(root, tree, model, syntaxScope, parentKey, out lines, out detail))
                return false;
            detail = string.IsNullOrEmpty(detail) ? "syntax_scope" : detail;
            return true;
        }

        if (string.IsNullOrWhiteSpace(memberKey))
        {
            detail = "no_member_or_scope";
            return false;
        }

        if (!tryResolveMember(root, tree, model, memberKey.Trim(), out lines, out detail))
            return false;

        detail = string.IsNullOrEmpty(detail) ? "member" : detail;
        return true;
    }

    internal static bool TryGetOrCreateEntry(
        IntercomAttachmentRoslynResolveSession? session,
        string absoluteFilePath,
        out IntercomAttachmentRoslynResolveSession.FileEntry entry,
        out string detail)
    {
        entry = null!;
        detail = "";

        if (session is not null
            && session.Entries.TryGetValue(absoluteFilePath, out var cached))
        {
            if (cached is not null)
            {
                entry = cached;
                return true;
            }

            detail = "parse_error";
            return false;
        }

        if (!File.Exists(absoluteFilePath))
        {
            detail = "file_not_found";
            session?.Entries.TryAdd(absoluteFilePath, null);
            return false;
        }

        string text;
        try
        {
            text = File.ReadAllText(absoluteFilePath);
        }
        catch (Exception ex)
        {
            detail = "read_error: " + ex.Message;
            session?.Entries.TryAdd(absoluteFilePath, null);
            return false;
        }

        var tree = CSharpSyntaxTree.ParseText(SourceText.From(text, Encoding.UTF8), path: absoluteFilePath);
        if (tree.GetCompilationUnitRoot() is null)
        {
            detail = "parse_error";
            session?.Entries.TryAdd(absoluteFilePath, null);
            return false;
        }

        var compilation = buildCompilation(absoluteFilePath, tree);
        entry = new IntercomAttachmentRoslynResolveSession.FileEntry
        {
            Text = text,
            Tree = tree,
            Model = compilation.GetSemanticModel(tree),
        };
        session?.Entries.TryAdd(absoluteFilePath, entry);
        return true;
    }

    internal static bool TryGetCachedText(
        IntercomAttachmentRoslynResolveSession? session,
        string absoluteFilePath,
        out string text)
    {
        text = "";
        if (session is null)
            return false;

        if (session.Entries.TryGetValue(absoluteFilePath, out var cached) && cached is not null)
        {
            text = cached.Text;
            return true;
        }

        return false;
    }

    private static bool tryResolveMember(
        CompilationUnitSyntax root,
        SyntaxTree tree,
        SemanticModel model,
        string memberKey,
        out LineRange lines,
        out string detail)
    {
        lines = default;
        detail = "";

        if (tryFindSymbolByDocumentationId(root, model, memberKey, out var symbol, out var declNode))
            return tryLineRangeFromNode(tree, declNode, symbol, out lines, out detail);

        var simpleName = memberKey.Contains('.') ? memberKey.Split('.')[^1] : memberKey;
        if (tryFindMemberBySimpleName(root, model, simpleName, out declNode, out symbol))
            return tryLineRangeFromNode(tree, declNode, symbol, out lines, out detail);

        detail = "member_not_found";
        return false;
    }

    private static bool tryResolveSyntaxScope(
        CompilationUnitSyntax root,
        SyntaxTree tree,
        SemanticModel model,
        AttachmentSyntaxScope scope,
        string? parentMemberKey,
        out LineRange lines,
        out string detail)
    {
        lines = default;
        detail = "";

        SyntaxNode? searchRoot = root;
        if (!string.IsNullOrWhiteSpace(parentMemberKey)
            && tryFindSymbolByDocumentationId(root, model, parentMemberKey, out _, out var parentNode))
        {
            searchRoot = parentNode;
        }
        else if (!string.IsNullOrWhiteSpace(parentMemberKey)
                 && tryFindMemberBySimpleName(root, model, parentMemberKey.Split('.')[^1], out var parentDecl, out _))
        {
            searchRoot = parentDecl;
        }

        var matches = collectScopeNodes(searchRoot, scope.Kind);
        if (matches.Count == 0)
        {
            detail = "scope_not_found";
            return false;
        }

        var index = Math.Clamp(scope.IndexInParent, 1, matches.Count);
        var node = matches[index - 1];
        return tryLineRangeFromSyntax(tree, node, out lines, out detail);
    }

    private static List<SyntaxNode> collectScopeNodes(SyntaxNode? root, string kind)
    {
        var list = new List<SyntaxNode>();
        if (root is null)
            return list;

        var normalized = kind.Trim().ToLowerInvariant();
        foreach (var node in root.DescendantNodes())
        {
            if (matchesScopeKind(node, normalized))
                list.Add(node);
        }

        return list;
    }

    private static bool matchesScopeKind(SyntaxNode node, string kind) =>
        kind switch
        {
            "for" => node is ForStatementSyntax,
            "foreach" => node is ForEachStatementSyntax,
            "if" => node is IfStatementSyntax,
            "while" => node is WhileStatementSyntax,
            "switch" => node is SwitchStatementSyntax,
            "try" => node is TryStatementSyntax,
            "lock" => node is LockStatementSyntax,
            "using" => node is UsingStatementSyntax,
            _ => false,
        };

    private static bool tryFindSymbolByDocumentationId(
        CompilationUnitSyntax root,
        SemanticModel model,
        string memberKey,
        out ISymbol symbol,
        out SyntaxNode? node)
    {
        symbol = null!;
        node = null;

        foreach (var candidate in root.DescendantNodesAndSelf())
        {
            ISymbol? sym = candidate switch
            {
                MethodDeclarationSyntax m => model.GetDeclaredSymbol(m),
                PropertyDeclarationSyntax p => model.GetDeclaredSymbol(p),
                FieldDeclarationSyntax f => model.GetDeclaredSymbol(f.Declaration.Variables.First()),
                VariableDeclaratorSyntax v => model.GetDeclaredSymbol(v),
                ConstructorDeclarationSyntax c => model.GetDeclaredSymbol(c),
                IndexerDeclarationSyntax i => model.GetDeclaredSymbol(i),
                EventDeclarationSyntax e => model.GetDeclaredSymbol(e),
                ClassDeclarationSyntax cl => model.GetDeclaredSymbol(cl),
                StructDeclarationSyntax st => model.GetDeclaredSymbol(st),
                InterfaceDeclarationSyntax it => model.GetDeclaredSymbol(it),
                RecordDeclarationSyntax r => model.GetDeclaredSymbol(r),
                _ => null,
            };

            if (sym is null)
                continue;

            var docId = sym.GetDocumentationCommentId();
            if (string.Equals(docId, memberKey, StringComparison.Ordinal))
            {
                symbol = sym;
                node = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool tryFindMemberBySimpleName(
        CompilationUnitSyntax root,
        SemanticModel model,
        string simpleName,
        out SyntaxNode? node,
        out ISymbol? symbol)
    {
        node = null;
        symbol = null;

        foreach (var candidate in root.DescendantNodes())
        {
            ISymbol? sym = candidate switch
            {
                MethodDeclarationSyntax m when string.Equals(m.Identifier.Text, simpleName, StringComparison.Ordinal) => model.GetDeclaredSymbol(m),
                PropertyDeclarationSyntax p when string.Equals(p.Identifier.Text, simpleName, StringComparison.Ordinal) => model.GetDeclaredSymbol(p),
                ConstructorDeclarationSyntax c when string.Equals(c.Identifier.Text, simpleName, StringComparison.Ordinal) => model.GetDeclaredSymbol(c),
                ClassDeclarationSyntax cl when string.Equals(cl.Identifier.Text, simpleName, StringComparison.Ordinal) => model.GetDeclaredSymbol(cl),
                _ => null,
            };

            if (sym is null)
                continue;

            symbol = sym;
            node = candidate;
            return true;
        }

        return false;
    }

    private static bool tryLineRangeFromNode(
        SyntaxTree tree,
        SyntaxNode? node,
        ISymbol? symbol,
        out LineRange lines,
        out string detail)
    {
        if (node is not null)
            return tryLineRangeFromSyntax(tree, node, out lines, out detail);

        if (symbol is not null)
        {
            var loc = symbol.Locations.FirstOrDefault(l => l.SourceTree == tree);
            if (loc is not null)
            {
                var syntax = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                if (syntax is not null)
                    return tryLineRangeFromSyntax(tree, syntax, out lines, out detail);
            }
        }

        lines = default;
        detail = "no_span";
        return false;
    }

    private static bool tryLineRangeFromSyntax(SyntaxTree tree, SyntaxNode node, out LineRange lines, out string detail)
    {
        lines = default;
        detail = "";

        var span = node.Span;
        if (span.IsEmpty)
        {
            detail = "empty_span";
            return false;
        }

        var startLine = RoslynLinePositionMapper.ToEditorLineNumber(tree.GetLineSpan(span).StartLinePosition);
        var endSpan = span.Length > 0 ? new TextSpan(Math.Max(span.Start, span.End - 1), 1) : span;
        var endLine = RoslynLinePositionMapper.ToEditorLineNumber(tree.GetLineSpan(endSpan).StartLinePosition);

        if (!LineRange.TryCreate(startLine, endLine, out lines))
        {
            detail = "invalid_line_range";
            return false;
        }

        return true;
    }

    private static CSharpCompilation buildCompilation(string filePath, SyntaxTree tree)
    {
        var refs = new List<MetadataReference>();
        void addRef(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;
            refs.Add(MetadataReference.CreateFromFile(path));
        }

        addRef(typeof(object).Assembly.Location);
        addRef(typeof(Enumerable).Assembly.Location);

        return CSharpCompilation.Create(
            "CascadeAttachmentResolve_" + Guid.NewGuid().ToString("N"),
            new[] { tree },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}

#nullable enable

using System.Text;
using System.Text.Json;
using CascadeIDE.Models.Intercom;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CascadeIDE.Services.Intercom;

/// <summary>H0b: innermost syntax scope @ caret → <see cref="AttachmentAnchor"/> (ADR 0128).</summary>
public static class AttachmentAnchorCaretScopeResolver
{
    public static bool TryResolveAtCaret(
        string? filePath,
        string? editorText,
        int? caretOffset,
        string? workspaceRoot,
        out AttachmentAnchor anchor,
        out string error)
    {
        anchor = new AttachmentAnchor();
        error = "";

        if (string.IsNullOrWhiteSpace(filePath) || !filePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            error = "Attach scope доступен для .cs в активном редакторе.";
            return false;
        }

        var text = editorText ?? "";
        var offset = Math.Clamp(caretOffset ?? 0, 0, text.Length);
        if (!AttachmentAnchorPaths.TryResolveAbsolute(
                AttachmentAnchorPaths.ToWorkspaceRelative(filePath, workspaceRoot) ?? filePath,
                workspaceRoot,
                out var absolute,
                out var pathErr))
        {
            error = pathErr;
            return false;
        }

        if (!string.Equals(absolute, filePath, StringComparison.OrdinalIgnoreCase)
            && File.Exists(filePath))
        {
            absolute = filePath;
        }

        if (!File.Exists(absolute))
        {
            error = "Файл не найден на диске.";
            return false;
        }

        string diskText;
        try
        {
            diskText = File.ReadAllText(absolute);
        }
        catch (Exception ex)
        {
            error = "Не удалось прочитать файл: " + ex.Message;
            return false;
        }

        var tree = CSharpSyntaxTree.ParseText(SourceText.From(diskText, Encoding.UTF8), path: absolute);
        var root = tree.GetCompilationUnitRoot();
        var pos = Math.Clamp(offset, 0, Math.Max(0, diskText.Length - 1));
        var node = root.FindToken(pos).Parent;
        if (node is null)
        {
            error = "Не найден синтаксический узел @ caret.";
            return false;
        }

        var scopeNode = findInnermostScope(node);
        if (scopeNode is null)
        {
            error = "Под кареткой нет for/if/while/switch — используй /attach selection.";
            return false;
        }

        var memberNode = scopeNode.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();
        var memberName = memberNode switch
        {
            MethodDeclarationSyntax m => m.Identifier.Text,
            PropertyDeclarationSyntax p => p.Identifier.Text,
            ConstructorDeclarationSyntax c => c.Identifier.Text,
            _ => null,
        };

        var kind = scopeKindName(scopeNode);
        if (kind is null)
        {
            error = "Неподдерживаемый синтаксический узел.";
            return false;
        }

        var memberBody = memberNode?.ChildNodes().FirstOrDefault(n => n is BlockSyntax or ArrowExpressionClauseSyntax);
        var searchRoot = memberBody ?? root;
        var matches = collectScopeNodes(searchRoot, kind);
        var index = matches.FindIndex(n => n == scopeNode) + 1;
        if (index < 1)
            index = 1;

        var rel = AttachmentAnchorPaths.ToWorkspaceRelative(absolute, workspaceRoot) ?? absolute.Replace('\\', '/');
        var syntaxScope = JsonSerializer.SerializeToElement(new Dictionary<string, object?>
        {
            ["kind"] = kind,
            ["indexInParent"] = index,
            ["parentMemberKey"] = memberName,
        });

        var label = memberName is not null
            ? $"{memberName} › {kind} ({index})"
            : $"{Path.GetFileName(rel)} › {kind} ({index})";

        anchor = new AttachmentAnchor
        {
            AttachmentShape = "syntax-scope",
            File = rel.Replace('\\', '/'),
            MemberKey = memberName,
            SyntaxScope = syntaxScope,
            DisplayLabel = label,
        };
        return true;
    }

    private static SyntaxNode? findInnermostScope(SyntaxNode node)
    {
        for (var cur = node; cur is not null; cur = cur.Parent)
        {
            if (matchesScopeKind(cur, "for")
                || matchesScopeKind(cur, "if")
                || matchesScopeKind(cur, "while")
                || matchesScopeKind(cur, "switch")
                || matchesScopeKind(cur, "foreach")
                || matchesScopeKind(cur, "try")
                || matchesScopeKind(cur, "lock")
                || matchesScopeKind(cur, "using"))
            {
                return cur;
            }
        }

        return null;
    }

    private static string? scopeKindName(SyntaxNode node) =>
        node switch
        {
            ForStatementSyntax => "for",
            ForEachStatementSyntax => "foreach",
            IfStatementSyntax => "if",
            WhileStatementSyntax => "while",
            SwitchStatementSyntax => "switch",
            TryStatementSyntax => "try",
            LockStatementSyntax => "lock",
            UsingStatementSyntax => "using",
            _ => null,
        };

    private static List<SyntaxNode> collectScopeNodes(SyntaxNode root, string kind)
    {
        var list = new List<SyntaxNode>();
        foreach (var node in root.DescendantNodes())
        {
            if (matchesScopeKind(node, kind))
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
}

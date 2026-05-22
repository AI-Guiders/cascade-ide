#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeIDE.ArchitectureAnalyzers;

internal static class ArchitectureForbiddenApiSyntax
{
    internal static readonly ImmutableHashSet<string> ForbiddenExternalTypeNames =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "File",
            "Directory",
            "FileInfo",
            "DirectoryInfo",
            "Process",
            "HttpClient");

    internal static string? GetForbiddenApiTypeName(ExpressionSyntax expression) =>
        expression switch
        {
            IdentifierNameSyntax id when ForbiddenExternalTypeNames.Contains(id.Identifier.ValueText)
                => id.Identifier.ValueText,
            MemberAccessExpressionSyntax ma when ForbiddenExternalTypeNames.Contains(ma.Name.Identifier.ValueText)
                => ma.Name.Identifier.ValueText,
            MemberAccessExpressionSyntax ma => GetForbiddenApiTypeName(ma.Expression),
            AliasQualifiedNameSyntax alias when ForbiddenExternalTypeNames.Contains(alias.Name.Identifier.ValueText)
                => alias.Name.Identifier.ValueText,
            QualifiedNameSyntax qualified when ForbiddenExternalTypeNames.Contains(qualified.Right.Identifier.ValueText)
                => qualified.Right.Identifier.ValueText,
            _ => null
        };

    internal static string? ExtractSimpleTypeName(TypeSyntax typeSyntax) =>
        typeSyntax switch
        {
            IdentifierNameSyntax i => i.Identifier.ValueText,
            QualifiedNameSyntax q => q.Right.Identifier.ValueText,
            GenericNameSyntax g => g.Identifier.ValueText,
            AliasQualifiedNameSyntax a => a.Name.Identifier.ValueText,
            _ => null
        };
}

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CascadeIDE.ArchitectureAnalyzers;

/// <summary>
/// ADR 0161 F5: forge REST client surface (<c>Features.Forge.Infrastructure</c>, <c>Features.Forge.Lens</c>)
/// импортируется только из vertical slice и явных spine hooks.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ForgeVerticalBoundaryAnalyzer : DiagnosticAnalyzer
{
    public const string ForbiddenForgeImportId = "CASCOPE043";

    private static readonly DiagnosticDescriptor ForbiddenForgeImportRule = new(
        ForbiddenForgeImportId,
        "Forge vertical imports only from allowlisted spine hooks",
        "Файл вне Features/Forge не должен импортировать '{0}' (ADR 0161); используйте публичный hook или перенесите код в vertical slice",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Forge REST/Lens client namespaces belong to Features/Forge except Chat overlay merge, MCP dispatch, CRS, bracket consumers.");

    private static readonly ImmutableHashSet<string> RestrictedNamespaces =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "CascadeIDE.Features.Forge.Infrastructure",
            "CascadeIDE.Features.Forge.Lens");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ForbiddenForgeImportRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
    }

    private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not UsingDirectiveSyntax u)
            return;

        var filePath = context.Node.SyntaxTree.FilePath;
        if (IsForgeVerticalPath(filePath) || IsAllowlistedConsumerPath(filePath))
            return;

        var ns = u.Name?.ToString() ?? "";
        if (!RestrictedNamespaces.Contains(ns))
            return;

        context.ReportDiagnostic(Diagnostic.Create(ForbiddenForgeImportRule, u.GetLocation(), ns));
    }

    private static bool IsForgeVerticalPath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        var n = Normalize(filePath);
        return n.Contains("/Features/Forge/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAllowlistedConsumerPath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        var n = Normalize(filePath);
        return n.Contains("/Features/Chat/", StringComparison.OrdinalIgnoreCase)
            || n.Contains("/Features/IdeMcp/", StringComparison.OrdinalIgnoreCase)
            || n.Contains("/Features/WorkspaceNavigation/", StringComparison.OrdinalIgnoreCase)
            || n.Contains("/ViewModels/", StringComparison.OrdinalIgnoreCase)
            || n.Contains("/Services/MarkdownPreview/", StringComparison.OrdinalIgnoreCase)
            || n.Contains("/CascadeIDE.Tests/", StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string filePath) => filePath.Replace('\\', '/');
}

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CascadeIDE.ArchitectureAnalyzers;

/// <summary>
/// ADR 0097 + ADR 0102:
/// <c>Cockpit/ComputingUnits/*</c> — слой вычисления снимков/DTO, без прямой добычи внешних данных и без UI-зависимостей.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CockpitComputeUnitBoundaryAnalyzer : DiagnosticAnalyzer
{
    public const string ForbiddenExternalAccessId = "CASCOPE020";
    public const string ForbiddenLayerDependencyId = "CASCOPE021";

    private static readonly ImmutableHashSet<string> ForbiddenExternalTypeNames =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "File",
            "Directory",
            "FileInfo",
            "DirectoryInfo",
            "Process",
            "HttpClient",
            "JsonDocument",
            "JsonSerializer");

    private static readonly DiagnosticDescriptor ForbiddenExternalAccessRule = new(
        ForbiddenExternalAccessId,
        "CCU не должен добывать внешние данные напрямую",
        "В Cockpit/ComputingUnits запрещён прямой доступ к внешнему источнику через '{0}' (ADR 0097 + ADR 0102 DAL)",
        "Architecture",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "CCU должен работать с подготовленными входными данными; fs/process/http/json parsing — в Data Acquisition Layer.");

    private static readonly DiagnosticDescriptor ForbiddenLayerDependencyRule = new(
        ForbiddenLayerDependencyId,
        "CCU не должен зависеть от UI-слоёв",
        "В Cockpit/ComputingUnits запрещён using '{0}' (CCU должен быть независим от ViewModels/Views/UI)",
        "Architecture",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Compute units не импортируют UI-представления и UI-фреймворки.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ForbiddenExternalAccessRule, ForbiddenLayerDependencyRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
    {
        if (!IsComputingUnitFile(context.Node.SyntaxTree.FilePath))
            return;
        if (context.Node is not UsingDirectiveSyntax u)
            return;

        var ns = u.Name?.ToString() ?? "";
        if (IsForbiddenUiNamespace(ns))
            context.ReportDiagnostic(Diagnostic.Create(ForbiddenLayerDependencyRule, u.GetLocation(), ns));
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (!IsComputingUnitFile(context.Node.SyntaxTree.FilePath))
            return;
        if (context.Node is not InvocationExpressionSyntax invocation)
            return;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        if (memberAccess.Expression is IdentifierNameSyntax id &&
            ForbiddenExternalTypeNames.Contains(id.Identifier.ValueText))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ForbiddenExternalAccessRule,
                memberAccess.GetLocation(),
                id.Identifier.ValueText));
        }
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        if (!IsComputingUnitFile(context.Node.SyntaxTree.FilePath))
            return;
        if (context.Node is not ObjectCreationExpressionSyntax creation)
            return;
        var typeName = ExtractSimpleTypeName(creation.Type);
        if (typeName is null || !ForbiddenExternalTypeNames.Contains(typeName))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            ForbiddenExternalAccessRule,
            creation.GetLocation(),
            typeName));
    }

    private static string? ExtractSimpleTypeName(TypeSyntax typeSyntax) =>
        typeSyntax switch
        {
            IdentifierNameSyntax i => i.Identifier.ValueText,
            QualifiedNameSyntax q => q.Right.Identifier.ValueText,
            GenericNameSyntax g => g.Identifier.ValueText,
            AliasQualifiedNameSyntax a => a.Name.Identifier.ValueText,
            _ => null
        };

    private static bool IsComputingUnitFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        var normalized = filePath.Replace('\\', '/');
        return normalized.Contains("/Cockpit/ComputingUnits/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsForbiddenUiNamespace(string namespaceName)
    {
        if (namespaceName.StartsWith("CascadeIDE.ViewModels", StringComparison.Ordinal))
            return true;
        if (namespaceName.StartsWith("CascadeIDE.Views", StringComparison.Ordinal))
            return true;
        if (namespaceName.StartsWith("CascadeIDE.Features.Ui", StringComparison.Ordinal))
            return true;
        if (namespaceName.StartsWith("Avalonia", StringComparison.Ordinal))
            return true;
        return false;
    }
}

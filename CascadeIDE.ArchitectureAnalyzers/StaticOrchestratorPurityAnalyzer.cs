using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CascadeIDE.ArchitectureAnalyzers;

/// <summary>
/// Guardrail for static application orchestrators:
/// static orchestrators in Features/*/Application should stay pure helpers
/// (no internal state, no direct external I/O APIs).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StaticOrchestratorPurityAnalyzer : DiagnosticAnalyzer
{
    public const string StatefulStaticOrchestratorId = "CASCOPE030";
    public const string ExternalIoInStaticOrchestratorId = "CASCOPE031";

    private static readonly ImmutableHashSet<string> ForbiddenExternalTypeNames =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "File",
            "Directory",
            "FileInfo",
            "DirectoryInfo",
            "Process",
            "HttpClient",
            "WebRequest",
            "WebClient");

    private static readonly DiagnosticDescriptor StatefulStaticOrchestratorRule = new(
        StatefulStaticOrchestratorId,
        "Статический orchestrator не должен хранить состояние",
        "Static orchestrator '{0}' содержит field '{1}'. Вынеси состояние/зависимости в instance service (Application layer).",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Static orchestrator in Application layer should remain stateless pure helper.");

    private static readonly DiagnosticDescriptor ExternalIoInStaticOrchestratorRule = new(
        ExternalIoInStaticOrchestratorId,
        "Статический orchestrator не должен выполнять внешний I/O напрямую",
        "Static orchestrator '{0}' выполняет внешний вызов через '{1}'. Вынеси I/O в DataAcquisition или instance service.",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Static orchestrator should not call direct fs/process/http APIs.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(StatefulStaticOrchestratorRule, ExternalIoInStaticOrchestratorRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeField(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not FieldDeclarationSyntax field)
            return;
        if (!TryGetTargetStaticOrchestrator(field, out var classDeclaration))
            return;

        foreach (var variable in field.Declaration.Variables)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                StatefulStaticOrchestratorRule,
                variable.GetLocation(),
                classDeclaration.Identifier.ValueText,
                variable.Identifier.ValueText));
        }
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
            return;
        if (!TryGetTargetStaticOrchestrator(invocation, out var classDeclaration))
            return;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;
        if (memberAccess.Expression is not IdentifierNameSyntax id)
            return;
        if (!ForbiddenExternalTypeNames.Contains(id.Identifier.ValueText))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            ExternalIoInStaticOrchestratorRule,
            memberAccess.GetLocation(),
            classDeclaration.Identifier.ValueText,
            id.Identifier.ValueText));
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ObjectCreationExpressionSyntax creation)
            return;
        if (!TryGetTargetStaticOrchestrator(creation, out var classDeclaration))
            return;
        var typeName = ExtractSimpleTypeName(creation.Type);
        if (typeName is null || !ForbiddenExternalTypeNames.Contains(typeName))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            ExternalIoInStaticOrchestratorRule,
            creation.GetLocation(),
            classDeclaration.Identifier.ValueText,
            typeName));
    }

    private static bool TryGetTargetStaticOrchestrator(SyntaxNode node, out ClassDeclarationSyntax classDeclaration)
    {
        classDeclaration = null!;
        var classNode = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        if (classNode is null)
            return false;
        if (!classNode.Modifiers.Any(SyntaxKind.StaticKeyword))
            return false;
        if (!classNode.Identifier.ValueText.EndsWith("Orchestrator", StringComparison.Ordinal))
            return false;
        if (!IsApplicationLayerFile(classNode.SyntaxTree.FilePath))
            return false;

        classDeclaration = classNode;
        return true;
    }

    private static bool IsApplicationLayerFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        var normalized = filePath.Replace('\\', '/');
        return normalized.Contains("/Features/", StringComparison.OrdinalIgnoreCase)
            && normalized.Contains("/Application/", StringComparison.OrdinalIgnoreCase);
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
}

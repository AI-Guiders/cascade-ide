using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CascadeIDE.ArchitectureAnalyzers;

/// <summary>
/// ADR 0055 guards for Skia instrument pipeline boundaries and stage flow.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SkiaPipelineArchitectureAnalyzer : DiagnosticAnalyzer
{
    public const string SkiaBoundaryId = "CASCOPE007";
    public const string SemanticMapStageFlowId = "CASCOPE008";
    public const string LayoutBypassId = "CASCOPE009";
    public const string SkiaViewDomainLeakId = "CASCOPE010";

    private static readonly DiagnosticDescriptor SkiaBoundaryRule = new(
        SkiaBoundaryId,
        "Skia instruments layer must stay UI-agnostic",
        "File under Services/SkiaInstruments must not import '{0}'. Keep UI dependencies in Views/Surface layer.",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ADR 0055 boundary: Skia instrument contracts do not reference Avalonia/ViewModels.");

    private static readonly DiagnosticDescriptor SemanticMapStageFlowRule = new(
        SemanticMapStageFlowId,
        "CodeNavigationMapCompositor must execute Intent -> Declutter -> Layout",
        "CodeNavigationMapCompositor.Compose(...) must call _intentStage.Resolve, _declutterStage.Apply and _layoutStage.Layout",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ADR 0055 canonical stage chain must be explicit in CodeNavigationMapCompositor.");

    private static readonly DiagnosticDescriptor LayoutBypassRule = new(
        LayoutBypassId,
        "Layout engines must be used only in CodeNavigationMapLayoutStage",
        "Direct usage of '{0}' is allowed only inside CodeNavigationMapLayoutStage to avoid pipeline bypass",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ADR 0055: layout selection happens in Layout stage, not in VM/compositor callers.");

    private static readonly DiagnosticDescriptor SkiaViewDomainLeakRule = new(
        SkiaViewDomainLeakId,
        "Skia views must not depend on SemanticMap stage-domain APIs",
        "View under Views/*Skia* must not reference '{0}'. Keep stage-domain logic in Services/Navigation.",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ADR 0055 boundary: Views render scene and do not depend on pipeline stage-domain contracts.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(SkiaBoundaryRule, SemanticMapStageFlowRule, LayoutBypassRule, SkiaViewDomainLeakRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
        context.RegisterSyntaxNodeAction(AnalyzeIdentifier, SyntaxKind.IdentifierName);
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not UsingDirectiveSyntax u)
            return;
        var path = context.Node.SyntaxTree.FilePath?.Replace('\\', '/') ?? "";

        if (!path.Contains("/Services/SkiaInstruments/", StringComparison.OrdinalIgnoreCase))
            return;

        var name = u.Name?.ToString() ?? "";
        if (name.StartsWith("Avalonia", StringComparison.Ordinal)
            || name.StartsWith("CascadeIDE.ViewModels", StringComparison.Ordinal))
        {
            context.ReportDiagnostic(Diagnostic.Create(SkiaBoundaryRule, u.GetLocation(), name));
        }
    }

    private static void AnalyzeIdentifier(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not IdentifierNameSyntax id)
            return;

        var path = context.Node.SyntaxTree.FilePath?.Replace('\\', '/') ?? "";
        if (!path.Contains("/Views/", StringComparison.OrdinalIgnoreCase)
            || !path.Contains("Skia", StringComparison.OrdinalIgnoreCase))
            return;

        var name = id.Identifier.ValueText;
        if (name is "ICodeNavigationMapIntentStage"
            or "ICodeNavigationMapDeclutterStage"
            or "ICodeNavigationMapLayoutStage"
            or "CodeNavigationMapPipelineState"
            or "CodeNavigationMapPipelineContext")
        {
            context.ReportDiagnostic(Diagnostic.Create(SkiaViewDomainLeakRule, id.GetLocation(), name));
        }
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MethodDeclarationSyntax method)
            return;
        if (!string.Equals(method.Identifier.ValueText, "Compose", StringComparison.Ordinal))
            return;

        var type = method.Parent as TypeDeclarationSyntax;
        if (type is null || !string.Equals(type.Identifier.ValueText, "CodeNavigationMapCompositor", StringComparison.Ordinal))
            return;

        var hasIntent = method.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
            .Any(m => m.Expression is IdentifierNameSyntax left
                      && left.Identifier.ValueText == "_intentStage"
                      && m.Name.Identifier.ValueText == "Resolve");
        var hasDeclutter = method.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
            .Any(m => m.Expression is IdentifierNameSyntax left
                      && left.Identifier.ValueText == "_declutterStage"
                      && m.Name.Identifier.ValueText == "Apply");
        var hasLayout = method.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
            .Any(m => m.Expression is IdentifierNameSyntax left
                      && left.Identifier.ValueText == "_layoutStage"
                      && m.Name.Identifier.ValueText == "Layout");

        if (!hasIntent || !hasDeclutter || !hasLayout)
            context.ReportDiagnostic(Diagnostic.Create(SemanticMapStageFlowRule, method.Identifier.GetLocation()));
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ObjectCreationExpressionSyntax create)
            return;
        var typeName = create.Type.ToString();
        if (!string.Equals(typeName, "CodeNavigationMapStarGraphLayoutEngine", StringComparison.Ordinal)
            && !string.Equals(typeName, "CodeNavigationMapControlFlowGraphLayoutEngine", StringComparison.Ordinal))
            return;

        var enclosingType = create.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText ?? "";
        if (string.Equals(enclosingType, "CodeNavigationMapLayoutStage", StringComparison.Ordinal))
            return;

        context.ReportDiagnostic(Diagnostic.Create(LayoutBypassRule, create.GetLocation(), typeName));
    }
}

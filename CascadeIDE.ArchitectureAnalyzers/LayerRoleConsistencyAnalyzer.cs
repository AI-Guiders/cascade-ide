#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CascadeIDE.ArchitectureAnalyzers;

/// <summary>
/// ADR 0097 / 0006: согласованность ролей CU (<see cref="ComputingUnitAttribute"/>),
/// orchestrator (<see cref="ApplicationOrchestratorAttribute"/>),
/// projection (<see cref="PresentationProjectionAttribute"/>).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LayerRoleConsistencyAnalyzer : DiagnosticAnalyzer
{
    public const string MissingApplicationOrchestratorAttributeId = "CASCOPE032";
    public const string MissingComputingUnitAttributeId = "CASCOPE033";
    public const string MissingPresentationProjectionSuffixAttributeId = "CASCOPE034";
    public const string ApplicationOrchestratorNameMismatchId = "CASCOPE035";
    public const string ComputingUnitOnOrchestratorNameId = "CASCOPE036";
    public const string ApplicationOrchestratorInComputingUnitPathId = "CASCOPE037";
    public const string StaticHelperSuggestProjectionOrCuId = "CASCOPE038";
    public const string OrchestratorLooksLikeProjectionId = "CASCOPE039";
    public const string PresentationProjectionForbiddenIoId = "CASCOPE040";
    public const string ViewModelsProjectionSuggestMoveId = "CASCOPE041";
    public const string MissingShortProjectionSuffixAttributeId = "CASCOPE042";

    private const string ApplicationOrchestratorAttributeMetadata =
        "CascadeIDE.Contracts.ApplicationOrchestratorAttribute";

    private const string ComputingUnitAttributeMetadata =
        "CascadeIDE.Contracts.ComputingUnitAttribute";

    private const string PresentationProjectionAttributeMetadata =
        "CascadeIDE.Contracts.PresentationProjectionAttribute";

    private const string CockpitComputeUnitMetadata =
        "CascadeIDE.Cockpit.ComputingUnits.ICockpitComputeUnit";

    private static readonly DiagnosticDescriptor MissingApplicationOrchestratorAttributeRule = Create(
        MissingApplicationOrchestratorAttributeId,
        DiagnosticSeverity.Info,
        "Orchestrator: добавьте [ApplicationOrchestrator]",
        "Тип '{0}' в Features/Application с суффиксом Orchestrator — пометьте [ApplicationOrchestrator] (ADR 0006)",
        "Координаторы сценария явно маркируются для ревью и анализаторов.");

    private static readonly DiagnosticDescriptor MissingComputingUnitAttributeRule = Create(
        MissingComputingUnitAttributeId,
        DiagnosticSeverity.Info,
        "CCU: добавьте [ComputingUnit]",
        "Тип '{0}' реализует ICockpitComputeUnit — пометьте [ComputingUnit] (ADR 0097)",
        "Вычислительные блоки кабины явно маркируются.");

    private static readonly DiagnosticDescriptor MissingPresentationProjectionSuffixAttributeRule = Create(
        MissingPresentationProjectionSuffixAttributeId,
        DiagnosticSeverity.Info,
        "Projection: добавьте [PresentationProjection]",
        "Тип '{0}' (*PresentationProjection) — пометьте [PresentationProjection] или уточните имя/слой",
        "Проекции presentation-слоя отделены от orchestrator и CCU.");

    private static readonly DiagnosticDescriptor ApplicationOrchestratorNameMismatchRule = Create(
        ApplicationOrchestratorNameMismatchId,
        DiagnosticSeverity.Warning,
        "Orchestrator: имя должно оканчиваться на Orchestrator",
        "Тип '{0}' помечен [ApplicationOrchestrator], но имя не оканчивается на Orchestrator",
        "Атрибут и суффикс имени согласованы.");

    private static readonly DiagnosticDescriptor ComputingUnitOnOrchestratorNameRule = Create(
        ComputingUnitOnOrchestratorNameId,
        DiagnosticSeverity.Error,
        "Нельзя [ComputingUnit] на тип Orchestrator",
        "Тип '{0}': [ComputingUnit] несовместим с суффиксом Orchestrator — используйте [ApplicationOrchestrator]",
        "Смешение ролей CU и orchestrator.");

    private static readonly DiagnosticDescriptor ApplicationOrchestratorInComputingUnitPathRule = Create(
        ApplicationOrchestratorInComputingUnitPathId,
        DiagnosticSeverity.Error,
        "Нельзя [ApplicationOrchestrator] в Cockpit/ComputingUnits",
        "Тип '{0}' в Cockpit/ComputingUnits не должен иметь [ApplicationOrchestrator] — используйте [ComputingUnit] и ICockpitComputeUnit",
        "Orchestrator живёт в Features/Application.");

    private static readonly DiagnosticDescriptor StaticHelperSuggestProjectionOrCuRule = Create(
        StaticHelperSuggestProjectionOrCuId,
        DiagnosticSeverity.Info,
        "Статический helper: похож на projection/CU",
        "Статический тип '{0}' в Features/Application без суффикса Orchestrator/Projection — рассмотрите *Projection, *Orchestrator или перенос в Cockpit/ComputingUnits",
        "Эвристика для выделения ролей.");

    private static readonly DiagnosticDescriptor OrchestratorLooksLikeProjectionRule = Create(
        OrchestratorLooksLikeProjectionId,
        DiagnosticSeverity.Warning,
        "Orchestrator похож на presentation projection",
        "Тип '{0}' (*Orchestrator) без async/Task и без координации — рассмотрите *PresentationProjection (MCP-фасады с [ApplicationOrchestrator] и делегированием в *Service не предупреждаются)",
        "Эвристика: чистое форматирование снимка без сценария.");

    private static readonly DiagnosticDescriptor PresentationProjectionForbiddenIoRule = Create(
        PresentationProjectionForbiddenIoId,
        DiagnosticSeverity.Warning,
        "Projection не должен выполнять внешний I/O",
        "Presentation projection '{0}' вызывает внешний API '{1}' — вынесите I/O в DAL/orchestrator",
        "Проекции только преобразуют уже подготовленные данные.");

    private static readonly DiagnosticDescriptor ViewModelsProjectionSuggestMoveRule = Create(
        ViewModelsProjectionSuggestMoveId,
        DiagnosticSeverity.Info,
        "Projection в ViewModels",
        "Тип '{0}' в ViewModels — рассмотрите перенос в Features/*/Application/*Projection",
        "Биндинг остаётся в VM, форматирование — в projection.");

    private static readonly DiagnosticDescriptor MissingShortProjectionSuffixAttributeRule = Create(
        MissingShortProjectionSuffixAttributeId,
        DiagnosticSeverity.Info,
        "Projection: добавьте [PresentationProjection]",
        "Тип '{0}' (*Projection) в Features/Application — пометьте [PresentationProjection] (или [ComputingUnit] для I/O-исключений)",
        "Проекции presentation-слоя явно маркируются.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            MissingApplicationOrchestratorAttributeRule,
            MissingComputingUnitAttributeRule,
            MissingPresentationProjectionSuffixAttributeRule,
            ApplicationOrchestratorNameMismatchRule,
            ComputingUnitOnOrchestratorNameRule,
            ApplicationOrchestratorInComputingUnitPathRule,
            StaticHelperSuggestProjectionOrCuRule,
            OrchestratorLooksLikeProjectionRule,
            PresentationProjectionForbiddenIoRule,
            ViewModelsProjectionSuggestMoveRule,
            MissingShortProjectionSuffixAttributeRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol type)
            return;
        if (type.TypeKind is not TypeKind.Class and not TypeKind.Struct)
            return;
        if (type.IsImplicitlyDeclared || type.Name.Contains('<', StringComparison.Ordinal))
            return;

        var path = type.Locations.FirstOrDefault()?.SourceTree?.FilePath;
        var name = type.Name;
        var attrs = type.GetAttributes();

        var hasOrchestratorAttr = HasAttribute(attrs, ApplicationOrchestratorAttributeMetadata);
        var hasCuAttr = HasAttribute(attrs, ComputingUnitAttributeMetadata);
        var hasProjectionAttr = HasAttribute(attrs, PresentationProjectionAttributeMetadata);
        var hasProjectionMark = hasProjectionAttr || hasCuAttr;

        if (name.EndsWith("Orchestrator", StringComparison.Ordinal) && hasCuAttr)
        {
            Report(context, ComputingUnitOnOrchestratorNameRule, type, name);
        }

        if (ArchitectureLayerPaths.IsComputingUnitFilePath(path))
        {
            if (hasOrchestratorAttr)
                Report(context, ApplicationOrchestratorInComputingUnitPathRule, type, name);

            if (ImplementsCockpitComputeUnit(type, context.Compilation) && !hasCuAttr)
                Report(context, MissingComputingUnitAttributeRule, type, name);
        }

        if (ArchitectureLayerPaths.IsFeaturesApplicationPath(path))
        {
            if (name.EndsWith("Orchestrator", StringComparison.Ordinal))
            {
                if (!hasOrchestratorAttr)
                    Report(context, MissingApplicationOrchestratorAttributeRule, type, name);

                if (type.IsStatic
                    && LooksLikePureProjection(type)
                    && !ShouldSuppressOrchestratorLooksLikeProjection(type, hasOrchestratorAttr))
                    Report(context, OrchestratorLooksLikeProjectionRule, type, name);
            }

            if (name.EndsWith("PresentationProjection", StringComparison.Ordinal)
                && !hasProjectionMark)
            {
                Report(context, MissingPresentationProjectionSuffixAttributeRule, type, name);
            }

            if (name.EndsWith("Projection", StringComparison.Ordinal)
                && !name.EndsWith("PresentationProjection", StringComparison.Ordinal)
                && !hasProjectionMark)
            {
                Report(context, MissingShortProjectionSuffixAttributeRule, type, name);
            }

            if (hasOrchestratorAttr && !name.EndsWith("Orchestrator", StringComparison.Ordinal))
                Report(context, ApplicationOrchestratorNameMismatchRule, type, name);

            if (type.IsStatic
                && !hasProjectionMark
                && !hasOrchestratorAttr
                && !name.EndsWith("Orchestrator", StringComparison.Ordinal)
                && !name.EndsWith("Projection", StringComparison.Ordinal)
                && !HasInstanceFields(type)
                && AllMembersStatic(type))
            {
                Report(context, StaticHelperSuggestProjectionOrCuRule, type, name);
            }
        }

        if (ArchitectureLayerPaths.IsViewModelsPath(path)
            && name.EndsWith("Projection", StringComparison.Ordinal)
            && type.IsStatic)
        {
            Report(context, ViewModelsProjectionSuggestMoveRule, type, name);
        }
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (!TryGetPresentationProjectionType(context, out var type))
            return;
        if (context.Node is not InvocationExpressionSyntax invocation)
            return;
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;
        var apiType = ArchitectureForbiddenApiSyntax.GetForbiddenApiTypeName(memberAccess.Expression);
        if (apiType is null)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            PresentationProjectionForbiddenIoRule,
            memberAccess.GetLocation(),
            type.Name,
            apiType));
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        if (!TryGetPresentationProjectionType(context, out var type))
            return;
        if (context.Node is not ObjectCreationExpressionSyntax creation)
            return;
        var typeName = ArchitectureForbiddenApiSyntax.ExtractSimpleTypeName(creation.Type);
        if (typeName is null || !ArchitectureForbiddenApiSyntax.ForbiddenExternalTypeNames.Contains(typeName))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            PresentationProjectionForbiddenIoRule,
            creation.GetLocation(),
            type.Name,
            typeName));
    }

    private static bool TryGetPresentationProjectionType(
        SyntaxNodeAnalysisContext context,
        out INamedTypeSymbol type)
    {
        type = null!;
        var path = context.Node.SyntaxTree.FilePath;
        if (!ArchitectureLayerPaths.IsFeaturesApplicationPath(path))
            return false;

        var model = context.SemanticModel;
        var typeDecl = context.Node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (typeDecl is null)
            return false;

        var symbol = model.GetDeclaredSymbol(typeDecl);
        if (symbol is not INamedTypeSymbol named)
            return false;

        if (!IsPresentationProjectionRole(named))
            return false;

        type = named;
        return true;
    }

    private static bool IsPresentationProjectionRole(INamedTypeSymbol type) =>
        HasAttribute(type.GetAttributes(), PresentationProjectionAttributeMetadata);

    private static bool ShouldSuppressOrchestratorLooksLikeProjection(
        INamedTypeSymbol type,
        bool hasOrchestratorAttr)
    {
        if (!hasOrchestratorAttr)
            return false;

        return DelegatesToServiceLikeParameters(type)
            || HasApplicationDomainInputParameters(type);
    }

    private static bool DelegatesToServiceLikeParameters(INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers())
        {
            if (member is not IMethodSymbol method || method.MethodKind != MethodKind.Ordinary)
                continue;
            if (method.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Internal)
                continue;

            foreach (var parameter in method.Parameters)
            {
                if (!ParameterTypeLooksLikeInfrastructure(parameter.Type))
                    continue;
                if (MethodBodyInvokesParameter(method, parameter.Name))
                    return true;
            }
        }

        return false;
    }

    private static bool HasApplicationDomainInputParameters(INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers())
        {
            if (member is not IMethodSymbol method || method.MethodKind != MethodKind.Ordinary)
                continue;
            if (method.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Internal)
                continue;

            foreach (var parameter in method.Parameters)
            {
                var display = parameter.Type.ToDisplayString();
                if (display.Contains("JsonElement", StringComparison.Ordinal)
                    || display.Contains("JsonDocument", StringComparison.Ordinal)
                    || display.Contains("GraphDocument", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ParameterTypeLooksLikeInfrastructure(ITypeSymbol type)
    {
        var name = type.Name;
        return name.EndsWith("Service", StringComparison.Ordinal)
            || name.EndsWith("Runner", StringComparison.Ordinal)
            || name.EndsWith("Session", StringComparison.Ordinal)
            || name.EndsWith("Executor", StringComparison.Ordinal)
            || name.EndsWith("Scheduler", StringComparison.Ordinal);
    }

    private static bool MethodBodyInvokesParameter(IMethodSymbol method, string parameterName)
    {
        foreach (var syntaxRef in method.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is not MethodDeclarationSyntax syntax)
                continue;
            foreach (var node in syntax.DescendantNodes())
            {
                if (node is not InvocationExpressionSyntax invocation)
                    continue;
                if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                    continue;
                if (memberAccess.Expression is IdentifierNameSyntax id
                    && id.Identifier.ValueText == parameterName)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool LooksLikePureProjection(INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers())
        {
            if (member is not IMethodSymbol method || method.MethodKind != MethodKind.Ordinary)
                continue;
            if (method.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Internal)
                continue;
            if (method.IsAsync || InvolvesTask(method.ReturnType))
                return false;
            if (MethodLooksLikeCoordination(method))
                return false;
        }

        return type.GetMembers().Any(m => m is IMethodSymbol { MethodKind: MethodKind.Ordinary });
    }

    private static bool MethodLooksLikeCoordination(IMethodSymbol method)
    {
        foreach (var syntaxRef in method.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is not MethodDeclarationSyntax syntax)
                continue;
            foreach (var node in syntax.DescendantNodes())
            {
                if (node is not InvocationExpressionSyntax invocation)
                    continue;
                var text = invocation.Expression.ToString();
                if (text.Contains("UiScheduler", StringComparison.Ordinal)
                    || text.Contains("IUiScheduler", StringComparison.Ordinal)
                    || text.Contains("IDataBus", StringComparison.Ordinal)
                    || text.Contains(".Publish", StringComparison.Ordinal)
                    || text.Contains(".Subscribe", StringComparison.Ordinal)
                    || text.Contains("InvokeAsync", StringComparison.Ordinal)
                    || text.Contains(".Post(", StringComparison.Ordinal)
                    || text.Contains("JsonSerializer", StringComparison.Ordinal)
                    || text.Contains("JsonElement", StringComparison.Ordinal)
                    || text.Contains("CancellationToken", StringComparison.Ordinal)
                    || text.Contains("Orchestrator.", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool InvolvesTask(ITypeSymbol returnType)
    {
        if (returnType.SpecialType == SpecialType.System_IAsyncResult)
            return true;
        var n = returnType.ToDisplayString();
        return n.Contains("Task", StringComparison.Ordinal);
    }

    private static bool ImplementsCockpitComputeUnit(INamedTypeSymbol type, Compilation compilation)
    {
        var iface = compilation.GetTypeByMetadataName(CockpitComputeUnitMetadata);
        return iface is not null && type.AllInterfaces.Contains(iface, SymbolEqualityComparer.Default);
    }

    private static bool HasInstanceFields(INamedTypeSymbol type) =>
        type.GetMembers().Any(m => m is IFieldSymbol { IsStatic: false });

    private static bool AllMembersStatic(INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers())
        {
            if (member is IFieldSymbol or IMethodSymbol or IPropertySymbol)
            {
                if (!member.IsStatic)
                    return false;
            }
        }

        return true;
    }

    private static bool HasAttribute(ImmutableArray<AttributeData> attrs, string metadataName) =>
        attrs.Any(a => a.AttributeClass?.ToDisplayString() == metadataName);

    private static void Report(
        SymbolAnalysisContext context,
        DiagnosticDescriptor rule,
        INamedTypeSymbol type,
        string name)
    {
        var location = type.Locations.FirstOrDefault() ?? Location.None;
        context.ReportDiagnostic(Diagnostic.Create(rule, location, name));
    }

    private static DiagnosticDescriptor Create(
        string id,
        DiagnosticSeverity severity,
        string title,
        string messageFormat,
        string description) =>
        new(
            id,
            title,
            messageFormat,
            "Architecture",
            severity,
            isEnabledByDefault: true,
            description: description);
}

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CascadeIDE.ArchitectureAnalyzers;

/// <summary>
/// ADR 0079: <c>IdeDisplay/</c> — intent/композитор/снимок без Avalonia, без кабины и без хрома <c>UiChrome</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IdeDisplayLayerArchitectureAnalyzer : DiagnosticAnalyzer
{
    public const string CockpitNamespaceId = "CASCOPE013";
    public const string AvaloniaNamespaceId = "CASCOPE014";
    public const string FeaturesUiChromeId = "CASCOPE015";

    private static readonly DiagnosticDescriptor CockpitRule = new(
        CockpitNamespaceId,
        "IdeDisplay не должен ссылаться на CascadeIDE.Cockpit",
        "В IdeDisplay (ADR 0079) запрещён импорт из кабины (найдено: {0})",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Композиторы и снимки оверлеев IDE не зависят от канала/CDS/примитивов кабины.");

    private static readonly DiagnosticDescriptor AvaloniaRule = new(
        AvaloniaNamespaceId,
        "IdeDisplay не должен ссылаться на Avalonia",
        "В IdeDisplay (ADR 0079) запрещены типы Avalonia UI (найдено: {0})",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Снимок и композитор IDS остаются переносимыми и тестируемыми без UI-фреймворка.");

    private static readonly DiagnosticDescriptor FeaturesUiChromeRule = new(
        FeaturesUiChromeId,
        "IdeDisplay не должен импортировать CascadeIDE.Features.UiChrome",
        "В IdeDisplay запрещён using CascadeIDE.Features.UiChrome (ADR 0079, хром вне IDS)",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Семантика оверлея не смешивается с контролами зон IDE.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(CockpitRule, AvaloniaRule, FeaturesUiChromeRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedTypeSymbol, SymbolKind.NamedType);
        context.RegisterSyntaxNodeAction(AnalyzeUsingDirective, SyntaxKind.UsingDirective);
    }

    private static void AnalyzeUsingDirective(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not UsingDirectiveSyntax u)
            return;
        if (!IsIdeDisplayFilePath(context.Node.SyntaxTree.FilePath))
            return;

        var name = u.Name?.ToString() ?? "";
        if (name.StartsWith("CascadeIDE.Cockpit", StringComparison.Ordinal) || name == "CascadeIDE.Cockpit")
        {
            context.ReportDiagnostic(Diagnostic.Create(CockpitRule, u.GetLocation(), name));
            return;
        }

        if (name.StartsWith("Avalonia", StringComparison.Ordinal) || name == "Avalonia")
        {
            context.ReportDiagnostic(Diagnostic.Create(AvaloniaRule, u.GetLocation(), name));
            return;
        }

        if (name.StartsWith("CascadeIDE.Features.UiChrome", StringComparison.Ordinal)
            || name == "CascadeIDE.Features.UiChrome")
        {
            context.ReportDiagnostic(Diagnostic.Create(FeaturesUiChromeRule, u.GetLocation()));
        }
    }

    private static void AnalyzeNamedTypeSymbol(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol named)
            return;
        if (named.TypeKind == TypeKind.Enum)
            return;
        if (!IsIdeDisplayNamespace(named.ContainingNamespace))
            return;

        foreach (var iface in named.AllInterfaces)
            CheckType(context, named, iface, named.DeclaringSyntaxReferences);

        if (named.BaseType is { SpecialType: not SpecialType.System_Object })
            CheckType(context, named, named.BaseType, named.DeclaringSyntaxReferences);

        foreach (var member in named.GetMembers())
        {
            switch (member)
            {
                case IFieldSymbol f when !f.IsImplicitlyDeclared:
                    CheckType(context, named, f.Type, member.DeclaringSyntaxReferences);
                    break;
                case IPropertySymbol p when !p.IsImplicitlyDeclared:
                    CheckType(context, named, p.Type, member.DeclaringSyntaxReferences);
                    break;
                case IMethodSymbol m when m is { MethodKind: MethodKind.Ordinary, IsImplicitlyDeclared: false }:
                    CheckType(context, named, m.ReturnType, member.DeclaringSyntaxReferences);
                    foreach (var par in m.Parameters)
                        CheckType(context, named, par.Type, member.DeclaringSyntaxReferences);
                    break;
            }
        }
    }

    private static void CheckType(
        SymbolAnalysisContext context,
        INamedTypeSymbol declaring,
        ITypeSymbol type,
        ImmutableArray<SyntaxReference> fallbackLocations)
    {
        VisitTypesForAvaloniaBan(type, context, declaring, fallbackLocations);
    }

    private static void VisitTypesForAvaloniaBan(
        ITypeSymbol type,
        SymbolAnalysisContext context,
        INamedTypeSymbol declaring,
        ImmutableArray<SyntaxReference> fallbackLocations)
    {
        switch (type)
        {
            case IArrayTypeSymbol arr:
                VisitTypesForAvaloniaBan(arr.ElementType, context, declaring, fallbackLocations);
                return;
            case ITypeParameterSymbol:
                return;
            case INamedTypeSymbol named:
                if (named.IsGenericType)
                {
                    foreach (var arg in named.TypeArguments)
                        VisitTypesForAvaloniaBan(arg, context, declaring, fallbackLocations);
                }

                if (IsCockpitFamily(named))
                {
                    Report(context, CockpitRule, fallbackLocations, named.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                    return;
                }

                if (IsFeaturesUiChromeFamily(named))
                {
                    Report(context, FeaturesUiChromeRule, fallbackLocations);
                    return;
                }

                if (IsAvaloniaUiFamily(named))
                {
                    Report(
                        context,
                        AvaloniaRule,
                        fallbackLocations,
                        named.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
                }

                return;
        }
    }

    private static void Report(
        SymbolAnalysisContext context,
        DiagnosticDescriptor rule,
        ImmutableArray<SyntaxReference> fallbackLocations,
        string? formatArg = null)
    {
        var location = PickLocation(fallbackLocations);
        if (location is null)
            return;

        var diagnostic = formatArg is null
            ? Diagnostic.Create(rule, location)
            : Diagnostic.Create(rule, location, formatArg);
        context.ReportDiagnostic(diagnostic);
    }

    private static Location? PickLocation(ImmutableArray<SyntaxReference> refs)
    {
        foreach (var r in refs)
        {
            var syntax = r.GetSyntax();
            if (syntax is null)
                continue;
            var loc = syntax.GetLocation();
            if (loc.IsInSource)
                return loc;
        }

        return null;
    }

    private static bool IsIdeDisplayNamespace(INamespaceSymbol? ns)
    {
        if (ns is null)
            return false;
        var display = ns.ToDisplayString();
        return display.StartsWith("CascadeIDE.IdeDisplay", StringComparison.Ordinal);
    }

    private static bool IsIdeDisplayFilePath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;
        var n = filePath.Replace('\\', '/');
        return n.Contains("/IdeDisplay/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAvaloniaUiFamily(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named)
            return false;

        for (var ns = type.ContainingNamespace; ns != null && !ns.IsGlobalNamespace; ns = ns.ContainingNamespace)
        {
            if (ns.Name == "Avalonia")
                return true;
        }

        var asm = named.ContainingAssembly?.Name ?? "";
        if (asm.Equals("Avalonia", StringComparison.OrdinalIgnoreCase) || asm.StartsWith("Avalonia.", StringComparison.OrdinalIgnoreCase))
            return true;
        if (asm.StartsWith("Dock.Avalonia", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static bool IsCockpitFamily(INamedTypeSymbol named)
    {
        var display = named.ContainingNamespace?.ToDisplayString() ?? "";
        return display.StartsWith("CascadeIDE.Cockpit", StringComparison.Ordinal);
    }

    private static bool IsFeaturesUiChromeFamily(INamedTypeSymbol named)
    {
        var display = named.ContainingNamespace?.ToDisplayString() ?? "";
        return display.StartsWith("CascadeIDE.Features.UiChrome", StringComparison.Ordinal);
    }
}

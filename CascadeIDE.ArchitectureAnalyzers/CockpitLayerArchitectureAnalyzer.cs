using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CascadeIDE.ArchitectureAnalyzers;

/// <summary>
/// ADR 0036: слои <c>Cockpit/Channels</c>, <c>Cockpit/Cds</c>, <c>Cockpit/Composition</c> не тянут Avalonia и не тянут <c>Features.UiChrome</c> для семантики зон;
/// поверхность остаётся в <c>Cockpit/Surface</c> и <c>Views</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CockpitLayerArchitectureAnalyzer : DiagnosticAnalyzer
{
    public const string AvaloniaNamespaceId = "CASCOPE001";
    public const string FeaturesUiChromeId = "CASCOPE002";

    private static readonly DiagnosticDescriptor AvaloniaRule = new(
        AvaloniaNamespaceId,
        "Cockpit слой не должен ссылаться на Avalonia",
        "Слой '{0}' (ADR 0036) не должен ссылаться на типы Avalonia UI; используйте Cockpit/Surface или Views (найдено: {1})",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Канал, CDS и композитор не импортируют знание о рендеринге Avalonia.");

    private static readonly DiagnosticDescriptor FeaturesUiChromeRule = new(
        FeaturesUiChromeId,
        "Cockpit слой не должен импортировать CascadeIDE.Features.UiChrome",
        "Слой '{0}' не должен использовать using CascadeIDE.Features.UiChrome; семантика зон для снимка дерева — через границу Cockpit/Surface (ADR 0036)",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Импорт Features.UiChrome в этих слоях смешивает канал/CDS/композитор с контролами зон.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(AvaloniaRule, FeaturesUiChromeRule);

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
        if (!TryGetRestrictedLayerFromPath(context.Node.SyntaxTree.FilePath, out var layer))
            return;

        var name = u.Name?.ToString() ?? "";
        if (name.StartsWith("Avalonia", StringComparison.Ordinal) || name == "Avalonia")
        {
            context.ReportDiagnostic(Diagnostic.Create(AvaloniaRule, u.GetLocation(), layer, name));
            return;
        }

        if (name.StartsWith("CascadeIDE.Features.UiChrome", StringComparison.Ordinal))
            context.ReportDiagnostic(Diagnostic.Create(FeaturesUiChromeRule, u.GetLocation(), layer));
    }

    private static void AnalyzeNamedTypeSymbol(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol named)
            return;
        if (named.TypeKind == TypeKind.Enum)
            return;
        if (!IsRestrictedNamespace(named.ContainingNamespace))
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
        ImmutableArray<Microsoft.CodeAnalysis.SyntaxReference> fallbackLocations)
    {
        VisitTypesForAvaloniaBan(type, context, declaring, fallbackLocations);
    }

    private static void VisitTypesForAvaloniaBan(
        ITypeSymbol type,
        SymbolAnalysisContext context,
        INamedTypeSymbol declaring,
        ImmutableArray<Microsoft.CodeAnalysis.SyntaxReference> fallbackLocations)
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

                if (IsAvaloniaUiFamily(named))
                {
                    var location = PickLocation(fallbackLocations);
                    if (location is not null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            AvaloniaRule,
                            location,
                            NamespaceLayer(declaring),
                            named.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                    }
                }

                return;
        }
    }

    private static Location? PickLocation(ImmutableArray<Microsoft.CodeAnalysis.SyntaxReference> refs)
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

    private static bool IsRestrictedNamespace(INamespaceSymbol? ns)
    {
        if (ns is null)
            return false;
        var display = ns.ToDisplayString();
        return display.StartsWith("CascadeIDE.Cockpit.Channels", StringComparison.Ordinal)
            || display.StartsWith("CascadeIDE.Cockpit.Cds", StringComparison.Ordinal)
            || display.StartsWith("CascadeIDE.Cockpit.Composition", StringComparison.Ordinal);
    }

    private static string NamespaceLayer(INamedTypeSymbol named)
    {
        var d = named.ContainingNamespace?.ToDisplayString() ?? "";
        if (d.StartsWith("CascadeIDE.Cockpit.Channels", StringComparison.Ordinal))
            return "Channels";
        if (d.StartsWith("CascadeIDE.Cockpit.Cds", StringComparison.Ordinal))
            return "Cds";
        if (d.StartsWith("CascadeIDE.Cockpit.Composition", StringComparison.Ordinal))
            return "Composition";
        return "Cockpit";
    }

    private static bool TryGetRestrictedLayerFromPath(string? filePath, out string layer)
    {
        layer = "";
        if (string.IsNullOrEmpty(filePath))
            return false;

        var normalized = filePath!.Replace('\\', '/');
        if (normalized.Contains("/Cockpit/Channels/", StringComparison.OrdinalIgnoreCase))
        {
            layer = "Channels";
            return true;
        }

        if (normalized.Contains("/Cockpit/Cds/", StringComparison.OrdinalIgnoreCase))
        {
            layer = "Cds";
            return true;
        }

        if (normalized.Contains("/Cockpit/Composition/", StringComparison.OrdinalIgnoreCase))
        {
            layer = "Composition";
            return true;
        }

        return false;
    }
}

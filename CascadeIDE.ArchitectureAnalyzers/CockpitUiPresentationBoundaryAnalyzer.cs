using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CascadeIDE.ArchitectureAnalyzers;

/// <summary>
/// ADR 0066: <c>Features/UiChrome</c> (presentation shell) не импортирует отрисовку/палитру кабины
/// (<c>Cockpit/PrimitivesKit</c>); <c>Cockpit/PrimitivesKit</c> не импортирует <c>Features.UiChrome</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CockpitUiPresentationBoundaryAnalyzer : DiagnosticAnalyzer
{
    public const string UiChromeMustNotUsePrimitivesKitId = "CASCOPE011";
    public const string PrimitivesKitMustNotUseUiChromeId = "CASCOPE012";

    private static readonly DiagnosticDescriptor UiChromeRule = new(
        UiChromeMustNotUsePrimitivesKitId,
        "UiChrome не должен ссылаться на Cockpit.PrimitivesKit",
        "Features/UiChrome (ADR 0066) не должен импортировать CascadeIDE.Cockpit.PrimitivesKit; оверлеи и хром — отдельно от отрисовки приборов (найдено: {0})",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Смешение presentation IDE и библиотеки отрисовки deck/кабины; используйте тему/CascadeTheme или вынесите общее в нейтральный слой.");

    private static readonly DiagnosticDescriptor PrimitivesKitRule = new(
        PrimitivesKitMustNotUseUiChromeId,
        "PrimitivesKit не должен ссылаться на Features.UiChrome",
        "Cockpit/PrimitivesKit (ADR 0066) не должен импортировать CascadeIDE.Features.UiChrome; граница Surface/Views для зон — отдельно (см. ADR 0036 CASCOPE002 в других слоях Cockpit).",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Отрисовка приборов не зависит от контролов зон и хрома IDE.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(UiChromeRule, PrimitivesKitRule);

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

        var path = context.Node.SyntaxTree.FilePath;
        var name = u.Name?.ToString() ?? "";

        if (IsFeaturesUiChromePath(path))
        {
            if (name.StartsWith("CascadeIDE.Cockpit.PrimitivesKit", StringComparison.Ordinal)
                || name == "CascadeIDE.Cockpit.PrimitivesKit")
            {
                context.ReportDiagnostic(Diagnostic.Create(UiChromeRule, u.GetLocation(), name));
            }

            return;
        }

        if (IsCockpitPrimitivesKitPath(path))
        {
            if (name.StartsWith("CascadeIDE.Features.UiChrome", StringComparison.Ordinal)
                || name == "CascadeIDE.Features.UiChrome")
            {
                context.ReportDiagnostic(Diagnostic.Create(PrimitivesKitRule, u.GetLocation()));
            }
        }
    }

    private static bool IsFeaturesUiChromePath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;
        var n = filePath.Replace('\\', '/');
        return n.Contains("/Features/UiChrome/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCockpitPrimitivesKitPath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;
        var n = filePath.Replace('\\', '/');
        return n.Contains("/Cockpit/PrimitivesKit/", StringComparison.OrdinalIgnoreCase);
    }
}

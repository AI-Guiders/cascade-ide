using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CascadeIDE.ArchitectureAnalyzers;

/// <summary>
/// ADR 0079: кабина (<c>Cockpit/</c>) не импортирует контур <c>CascadeIDE.IdeDisplay</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CockpitIdeDisplayBoundaryAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CASCOPE016";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Cockpit не должен ссылаться на CascadeIDE.IdeDisplay",
        "В Cockpit (ADR 0079) запрещён импорт CascadeIDE.IdeDisplay — CDS и IDS разделены",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Семантика кабины не зависит от композиторов/снимков IDE-оверлеев.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
        if (!IsCockpitFilePath(context.Node.SyntaxTree.FilePath))
            return;

        var name = u.Name?.ToString() ?? "";
        if (name.StartsWith("CascadeIDE.IdeDisplay", StringComparison.Ordinal) || name == "CascadeIDE.IdeDisplay")
            context.ReportDiagnostic(Diagnostic.Create(Rule, u.GetLocation()));
    }

    private static bool IsCockpitFilePath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;
        var n = filePath.Replace('\\', '/');
        return n.Contains("/Cockpit/", StringComparison.OrdinalIgnoreCase);
    }
}

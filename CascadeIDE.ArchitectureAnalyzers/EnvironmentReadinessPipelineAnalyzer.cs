using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CascadeIDE.ArchitectureAnalyzers;

/// <summary>
/// Enforces Environment Readiness canonical pipeline:
/// channel Build(context) -> surface compositor Compose(...).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EnvironmentReadinessPipelineAnalyzer : DiagnosticAnalyzer
{
    public const string LegacySnapshotBuilderId = "CASCOPE006";

    private static readonly DiagnosticDescriptor LegacySnapshotBuilderRule = new(
        LegacySnapshotBuilderId,
        "Environment Readiness: direct snapshot builder usage is forbidden in MainWindowViewModel",
        "Use IEnvironmentReadinessChannel.Build(...) and IEnvironmentReadinessSurfaceCompositor.Compose(...) instead of EnvironmentReadinessSnapshotBuilder in MainWindowViewModel pipeline.",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Environment Readiness VM pipeline must go through channel and surface compositor.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(LegacySnapshotBuilderRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeIdentifier, SyntaxKind.IdentifierName);
    }

    private static void AnalyzeIdentifier(SyntaxNodeAnalysisContext context)
    {
        if (!IsMainWindowViewModelFile(context.Node.SyntaxTree.FilePath))
            return;
        if (context.Node is not IdentifierNameSyntax id)
            return;
        if (!string.Equals(id.Identifier.ValueText, "EnvironmentReadinessSnapshotBuilder", StringComparison.Ordinal))
            return;

        context.ReportDiagnostic(Diagnostic.Create(LegacySnapshotBuilderRule, id.GetLocation()));
    }

    private static bool IsMainWindowViewModelFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        var normalized = filePath.Replace('\\', '/');
        return normalized.Contains("/ViewModels/MainWindowViewModel", StringComparison.OrdinalIgnoreCase);
    }
}

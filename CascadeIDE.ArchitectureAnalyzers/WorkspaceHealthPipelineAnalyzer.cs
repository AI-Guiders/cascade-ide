using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CascadeIDE.ArchitectureAnalyzers;

/// <summary>
/// Enforces Workspace Health canonical pipeline:
/// channel Build(context) -> surface compositor Compose(...).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WorkspaceHealthPipelineAnalyzer : DiagnosticAnalyzer
{
    public const string LegacyGetSnapshotId = "CASCOPE004";
    public const string LegacySegmentBuilderId = "CASCOPE005";

    private static readonly DiagnosticDescriptor LegacyGetSnapshotRule = new(
        LegacyGetSnapshotId,
        "Workspace Health: legacy GetSnapshot path is forbidden",
        "Use Workspace Health channel Build(context) instead of legacy GetSnapshot() in MainWindowViewModel pipeline.",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Legacy snapshot entrypoint was removed; use IWorkspaceHealthChannel.Build(...).");

    private static readonly DiagnosticDescriptor LegacySegmentBuilderRule = new(
        LegacySegmentBuilderId,
        "Workspace Health: legacy SegmentBuilder usage is forbidden",
        "Use WorkspaceHealthSurfaceCompositor.Compose(...) instead of WorkspaceHealthSegmentBuilder.",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Legacy WorkspaceHealthSegmentBuilder was removed; compose via surface compositor.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(LegacyGetSnapshotRule, LegacySegmentBuilderRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeIdentifier, SyntaxKind.IdentifierName);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
            return;
        if (!IsMainWindowViewModelFile(context.Node.SyntaxTree.FilePath))
            return;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;
        if (!string.Equals(memberAccess.Name.Identifier.ValueText, "GetSnapshot", StringComparison.Ordinal))
            return;

        context.ReportDiagnostic(Diagnostic.Create(LegacyGetSnapshotRule, memberAccess.Name.GetLocation()));
    }

    private static void AnalyzeIdentifier(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not IdentifierNameSyntax id)
            return;
        if (!string.Equals(id.Identifier.ValueText, "WorkspaceHealthSegmentBuilder", StringComparison.Ordinal))
            return;

        context.ReportDiagnostic(Diagnostic.Create(LegacySegmentBuilderRule, id.GetLocation()));
    }

    private static bool IsMainWindowViewModelFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
        var normalized = filePath.Replace('\\', '/');
        return normalized.Contains("/ViewModels/MainWindowViewModel", StringComparison.OrdinalIgnoreCase);
    }
}

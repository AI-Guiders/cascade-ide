using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CascadeIDE.ArchitectureAnalyzers;

/// <summary>
/// ADR 0046: прямые присваивания intent-флагам кабины (P/M колонки главного окна) только в согласованных точках,
/// чтобы политика CDS и VM не расходились незаметно.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CockpitIntentPropertyAssignmentAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CASCOPE003";

    private const string MainVmMetadataName = "CascadeIDE.ViewModels.MainWindowViewModel";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Intent раскладки кабины: присваивание вне белого списка",
        "Прямое присваивание '{0}' вне разрешённых файлов; используйте ApplySolutionExplorerVisible / ApplyChatPanelExpanded, relay-команды или слой UI-режима (см. ADR 0046, CockpitPresentationLayoutPolicy).",
        "Architecture",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Свойства видимости P/M — точка согласования с пресетом presentation; новые присваивания только через белый список или расширение анализатора.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
    }

    private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not AssignmentExpressionSyntax assign || assign.Kind() != SyntaxKind.SimpleAssignmentExpression)
            return;

        if (!IsMainWindowViewModelFile(context.Node.SyntaxTree.FilePath))
            return;

        var model = context.SemanticModel;
        var symbol = model.GetSymbolInfo(assign.Left).Symbol;
        if (symbol is null)
            return;

        if (!IsCockpitIntentMember(symbol, out var displayName))
            return;

        if (IsAllowedSourcePath(context.Node.SyntaxTree.FilePath))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, assign.GetLocation(), displayName));
    }

    private static bool IsMainWindowViewModelFile(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;
        var n = path!.Replace('\\', '/');
        return n.IndexOf("/ViewModels/MainWindowViewModel", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsCockpitIntentMember(ISymbol symbol, out string displayName)
    {
        displayName = symbol.Name;
        if (symbol is IPropertySymbol or IFieldSymbol)
        {
            if (!IsMainWindowViewModel(symbol.ContainingType))
            {
                displayName = "";
                return false;
            }

            return symbol.Name switch
            {
                "IsSolutionExplorerVisible" or "IsChatPanelExpanded" => true,
                "_isSolutionExplorerVisible" or "_isChatPanelExpanded" => true,
                _ => false,
            };
        }

        displayName = "";
        return false;
    }

    private static bool IsMainWindowViewModel(INamedTypeSymbol? type)
    {
        if (type is null)
            return false;
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == $"global::{MainVmMetadataName}";
    }

    private static bool IsAllowedSourcePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var p = path!.Replace('\\', '/');

        // Тесты и сэмплы могут имитировать VM или поднимать фиктивные присваивания.
        if (p.IndexOf("/CascadeIDE.Tests/", StringComparison.OrdinalIgnoreCase) >= 0
            || p.IndexOf("/samples/", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        static bool EndsWithFile(string full, string fileName) =>
            full.EndsWith("/" + fileName, StringComparison.OrdinalIgnoreCase);

        return EndsWithFile(p, "MainWindowViewModel.PresentationLayoutAuthority.cs")
            || EndsWithFile(p, "MainWindowViewModel.RelayCommands.cs")
            || EndsWithFile(p, "MainWindowViewModel.cs")
            || EndsWithFile(p, "MainWindowViewModel.ShellState.cs")
            || EndsWithFile(p, "MainWindowViewModel.UiGitWorkspace.cs");
    }
}

#nullable enable
using System.Collections.ObjectModel;
using System.Text.Json;
using CascadeIDE.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeIDE.Services;

/// <summary>
/// MCP get_code_metrics: разрешение списка .cs файлов по scope и расчёт LOC / cyclomatic по Roslyn.
/// </summary>
public static class McpCodeMetrics
{
    public static IReadOnlyList<string> ResolveMetricFilePaths(
        string? scope,
        string? path,
        string? currentFilePath,
        ObservableCollection<SolutionItem>? solutionRoots)
    {
        var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "current_file" : scope.Trim().ToLowerInvariant();
        return normalizedScope switch
        {
            "file" => ResolveFilesFromPath(path),
            "path" => ResolveFilesFromPath(path),
            "solution" => solutionRoots is null
                ? []
                : McpSolutionTree.CollectFileEntries(solutionRoots)
                    .Select(e => e.FullPath)
                    .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && File.Exists(p))
                    .ToList(),
            _ => ResolveFilesFromPath(path ?? currentFilePath)
        };
    }

    private static List<string> ResolveFilesFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return [];

        try
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
                return fullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ? [fullPath] : [];
            if (!Directory.Exists(fullPath))
                return [];
            return Directory.EnumerateFiles(fullPath, "*.cs", SearchOption.AllDirectories).ToList();
        }
        catch
        {
            return [];
        }
    }

    public static async Task<string> ComputeMetricsJsonAsync(string? scope, IReadOnlyList<string> files, CancellationToken cancellationToken = default)
    {
        if (files.Count == 0)
            return JsonSerializer.Serialize(new { success = false, error = "No C# files resolved for metrics." });

        var perFile = new List<object>();
        var topMethods = new List<(string File, string Method, int Line, int Complexity)>();
        int totalLoc = 0, totalClasses = 0, totalMethods = 0, complexityTotal = 0, complexityMax = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string text;
            try
            {
                text = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                continue;
            }

            var tree = CSharpSyntaxTree.ParseText(text, cancellationToken: cancellationToken);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var methods = root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>().ToList();
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Count();
            var fileLoc = text.Split('\n').Count(static l => !string.IsNullOrWhiteSpace(l));

            int fileComplexity = 0;
            int fileMaxMethodComplexity = 0;
            foreach (var method in methods)
            {
                var complexity = CalculateCyclomaticComplexity(method);
                fileComplexity += complexity;
                if (complexity > fileMaxMethodComplexity)
                    fileMaxMethodComplexity = complexity;

                if (complexity >= 10)
                {
                    var methodName = method switch
                    {
                        MethodDeclarationSyntax m => m.Identifier.Text,
                        ConstructorDeclarationSyntax c => c.Identifier.Text,
                        DestructorDeclarationSyntax d => d.Identifier.Text,
                        _ => "method"
                    };
                    var line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                    topMethods.Add((file, methodName, line, complexity));
                }
            }

            totalLoc += fileLoc;
            totalClasses += classes;
            totalMethods += methods.Count;
            complexityTotal += fileComplexity;
            complexityMax = Math.Max(complexityMax, fileMaxMethodComplexity);

            perFile.Add(new
            {
                file,
                loc = fileLoc,
                class_count = classes,
                method_count = methods.Count,
                cyclomatic_total = fileComplexity,
                cyclomatic_max_method = fileMaxMethodComplexity
            });
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            scope = string.IsNullOrWhiteSpace(scope) ? "current_file" : scope,
            file_count = perFile.Count,
            totals = new
            {
                loc = totalLoc,
                class_count = totalClasses,
                method_count = totalMethods,
                cyclomatic_total = complexityTotal,
                cyclomatic_max_method = complexityMax
            },
            files = perFile,
            hot_methods = topMethods
                .OrderByDescending(x => x.Complexity)
                .Take(20)
                .Select(x => new { file = x.File, method = x.Method, line = x.Line, complexity = x.Complexity })
        });
    }

    private static int CalculateCyclomaticComplexity(SyntaxNode node)
    {
        var complexity = 1;
        foreach (var child in node.DescendantNodes())
        {
            switch (child)
            {
                case IfStatementSyntax:
                case ForStatementSyntax:
                case ForEachStatementSyntax:
                case WhileStatementSyntax:
                case DoStatementSyntax:
                case CaseSwitchLabelSyntax:
                case CatchClauseSyntax:
                case ConditionalExpressionSyntax:
                    complexity++;
                    break;
                case BinaryExpressionSyntax b when b.IsKind(SyntaxKind.LogicalAndExpression) || b.IsKind(SyntaxKind.LogicalOrExpression):
                    complexity++;
                    break;
            }
        }
        return complexity;
    }
}

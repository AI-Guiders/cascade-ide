using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Avalonia.Threading;
using CascadeIDE.Features.Instrumentation;
using CascadeIDE.Models;
using CascadeIDE.Services;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    string Services.IIdeMcpActions.GetSolutionInfo()
    {
        var path = Workspace.SolutionPath ?? "";
        var current = CurrentFilePath ?? "";
        var projects = CollectProjectPaths(Workspace.SolutionRoots).ToList();
        var selected = Workspace.SelectedSolutionItem?.FullPath ?? "";
        return JsonSerializer.Serialize(new { solution_path = path, current_file_path = current, project_paths = projects, selected_solution_path = selected });
    }

    string Services.IIdeMcpActions.GetBuildOutput()
    {
        var (bg, fg) = Services.UiThemeSnapshot.GetBuildOutputTheme();
        return JsonSerializer.Serialize(new { text = BuildOutputPanel.BuildOutput ?? "", theme = new { background = bg, foreground = fg } });
    }

    async Task<string> Services.IIdeMcpActions.GetWorkspaceStateAsync()
    {
        var diagnosticsJson = await ((Services.IIdeMcpActions)this).GetCurrentFileDiagnosticsAsync().ConfigureAwait(false);
        JsonElement diagnostics;
        try { diagnostics = JsonSerializer.Deserialize<JsonElement>(diagnosticsJson); }
        catch { diagnostics = JsonSerializer.SerializeToElement(Array.Empty<object>()); }

        // MCP вызывает с пула потоков; после ConfigureAwait(false) чтение VM/панелей только на UI.
        return await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var buildText = BuildOutputPanel.BuildOutput ?? "";
            if (buildText.Length > 2000)
                buildText = buildText[..2000] + "\n... (output truncated)";

            var state = new
            {
                solution_path = Workspace.SolutionPath,
                current_file_path = CurrentFilePath,
                selected_solution_path = Workspace.SelectedSolutionItem?.FullPath,
                editor = new
                {
                    content_length = (EditorText ?? "").Length,
                    selection_start = EditorSelectionStart,
                    selection_length = EditorSelectionLength
                },
                breakpoints = new
                {
                    current_file = AllBreakpointLinesInCurrentFile,
                    debugger_count = _debuggerBreakpoints.Count
                },
                debug = new
                {
                    position_file = DebugPositionFile,
                    position_line = DebugPositionLine,
                    stack_count = InstrumentationPanel.DebugStackFrames.Count,
                    variables_count = InstrumentationPanel.DebugVariables.Count
                },
                build = new
                {
                    is_visible = IsBuildOutputVisible,
                    output_preview = buildText,
                    binlog_path = _lastBuildBinlogPath
                },
                terminal = new { is_visible = IsTerminalVisible },
                ui_mode = UiMode,
                panels = new
                {
                    solution_explorer = IsSolutionExplorerVisible,
                    build_output = IsBuildOutputVisible,
                    chat_expanded = IsChatPanelExpanded,
                    git = IsGitPanelVisible,
                    instrumentation_dock = IsInstrumentationDockVisible
                },
                safety_level = SafetyLevel,
                editor_group_count = EditorGroupCount,
                agent_trace_step_count = InstrumentationPanel.AgentTraceSteps.Count,
                is_autonomous_running = Autonomous.IsAutonomousRunning,
                diagnostics
            };
            return JsonSerializer.Serialize(state);
        });
    }

    async Task<string> Services.IIdeMcpActions.GetCodeMetricsAsync(string? scope, string? path)
    {
        var files = await Dispatcher.UIThread.InvokeAsync(() =>
            ResolveMetricFiles(scope, path).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        if (files.Count == 0)
            return JsonSerializer.Serialize(new { success = false, error = "No C# files resolved for metrics." });

        var perFile = new List<object>();
        var topMethods = new List<(string File, string Method, int Line, int Complexity)>();
        int totalLoc = 0, totalClasses = 0, totalMethods = 0, complexityTotal = 0, complexityMax = 0;

        foreach (var file in files)
        {
            string text;
            try { text = await File.ReadAllTextAsync(file).ConfigureAwait(false); }
            catch { continue; }

            var tree = CSharpSyntaxTree.ParseText(text);
            var root = await tree.GetRootAsync().ConfigureAwait(false);
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

    private IReadOnlyList<string> ResolveMetricFiles(string? scope, string? path)
    {
        var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "current_file" : scope.Trim().ToLowerInvariant();
        return normalizedScope switch
        {
            "file" => ResolveFilesFromPath(path),
            "path" => ResolveFilesFromPath(path),
            "solution" => CollectFileEntries(Workspace.SolutionRoots)
                .Select(e => e.FullPath)
                .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && File.Exists(p))
                .ToList(),
            _ => ResolveFilesFromPath(path ?? CurrentFilePath)
        };
    }

    private static IReadOnlyList<string> ResolveFilesFromPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Array.Empty<string>();

        try
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
                return fullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ? [fullPath] : Array.Empty<string>();
            if (!Directory.Exists(fullPath))
                return Array.Empty<string>();
            return Directory.EnumerateFiles(fullPath, "*.cs", SearchOption.AllDirectories).ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
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

    private static IEnumerable<string> CollectProjectPaths(ObservableCollection<SolutionItem> roots)
    {
        foreach (var item in roots)
        {
            if (item.FullPath is { } p && (p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)))
                yield return p;
            foreach (var child in CollectProjectPaths(item.Children))
                yield return child;
        }
    }

    private static IEnumerable<(string Title, string FullPath)> CollectFileEntries(ObservableCollection<SolutionItem> roots)
    {
        foreach (var item in roots)
        {
            if (item.FullPath is { } p && !p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && !p.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) && !p.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
                yield return (item.Title, p);
            foreach (var child in CollectFileEntries(item.Children))
                yield return child;
        }
    }

    private static string? GetRelativePath(string? solutionPath, string? fullPath)
    {
        if (string.IsNullOrEmpty(solutionPath) || string.IsNullOrEmpty(fullPath))
            return null;
        var solutionDir = Path.GetDirectoryName(solutionPath);
        if (string.IsNullOrEmpty(solutionDir))
            return null;
        try
        {
            return Path.GetRelativePath(solutionDir, fullPath);
        }
        catch
        {
            return null;
        }
    }

    private static object BuildSolutionTreeNode(SolutionItem item, string? solutionPath)
    {
        var relative = GetRelativePath(solutionPath, item.FullPath);
        var path = item.FullPath;
        var title = item.Title;
        if (item.Children.Count == 0)
            return new { title, path, relative_path = relative };
        var children = item.Children.Select(c => BuildSolutionTreeNode(c, solutionPath)).ToList();
        return new { title, path, relative_path = relative, children };
    }
}

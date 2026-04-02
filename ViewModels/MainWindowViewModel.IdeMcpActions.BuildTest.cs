using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using Avalonia.Threading;
using DotNetBuildTestParsers;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    /// <summary>Диагностики открытого .cs файла (ошибки и предупреждения Roslyn). JSON: массив { id, message, severity, line, column }. Для не-C# или при отсутствии файла — [].</summary>
    async Task<string> Services.IIdeMcpActions.GetCurrentFileDiagnosticsAsync()
    {
        var (path, text) = await Dispatcher.UIThread.InvokeAsync(() => (CurrentFilePath ?? "", EditorText ?? "")).GetTask();
        return await Task.Run(() => _contextMinimizer.GetDiagnosticsJson(path, text)).ConfigureAwait(false);
    }

    /// <summary>Список файлов и дерево решения. file_entries — плоский список с path, title, relative_path. solution_tree — иерархия (solution → projects → folders → files). Выполняется в UI-потоке.</summary>
    Task<string> Services.IIdeMcpActions.GetSolutionFilesAsync() =>
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var solutionPath = Workspace.SolutionPath;
            var entries = CollectFileEntries(Workspace.SolutionRoots).Select(e => new
            {
                path = e.FullPath,
                title = e.Title,
                relative_path = GetRelativePath(solutionPath, e.FullPath)
            }).ToList();
            var tree = Workspace.SolutionRoots.Select(r => BuildSolutionTreeNode(r, solutionPath)).ToList();
            return JsonSerializer.Serialize(new { file_entries = entries, solution_tree = tree });
        }).GetTask();

    async Task<string> Services.IIdeMcpActions.BuildAsync()
    {
        var path = await Dispatcher.UIThread.InvokeAsync(() => Workspace.SolutionPath ?? "");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            var msg = "No solution loaded or file not found.";
            Dispatcher.UIThread.Post(() => { BuildOutputPanel.Set(msg + "\r\n"); IsBuildOutputVisible = true; });
            return msg;
        }
        try
        {
            var artifactsDir = Path.Combine(Path.GetDirectoryName(path) ?? "", ".cascade-ide", "build-artifacts");
            Directory.CreateDirectory(artifactsDir);
            var binlogPath = Path.Combine(artifactsDir, $"build-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.binlog");
            var workDir = Path.GetDirectoryName(path) ?? "";
            var (success, exitCode, output) = await _dotnetRunner
                .RunAsync(["build", path, $"-bl:{binlogPath}"], workDir)
                .ConfigureAwait(false);

            var outStr = output;
            if (!success && exitCode != 0)
                outStr += $"\r\nExit code: {exitCode}";
            var pathCopy = path;
            Dispatcher.UIThread.Post(() =>
            {
                BuildOutputPanel.Set($"Сборка: {pathCopy}\r\n{outStr}");
                IsBuildOutputVisible = true;
                _lastBuildBinlogPath = binlogPath;
            });
            return outStr;
        }
        catch (Exception ex)
        {
            var msg = "Error: " + ex.Message;
            Dispatcher.UIThread.Post(() => { BuildOutputPanel.Set(msg + "\r\n"); IsBuildOutputVisible = true; });
            return msg;
        }
    }

    async Task<string> Services.IIdeMcpActions.BuildStructuredAsync()
    {
        var raw = await ((Services.IIdeMcpActions)this).BuildAsync().ConfigureAwait(false);
        var parsed = BuildOutputParser.Parse(raw);
        const int maxRawChars = 4000;
        var rawTruncated = raw.Length > maxRawChars ? raw[..maxRawChars] + "\n... (output truncated)" : raw;
        var result = new
        {
            success = parsed.Success,
            exit_code = parsed.ExitCode,
            errors = parsed.Errors.Select(e => new { e.File, e.Line, e.Column, e.Code, e.Message }).ToList(),
            warnings = parsed.Warnings.Select(w => new { w.File, w.Line, w.Column, w.Code, w.Message }).ToList(),
            binlog_path = _lastBuildBinlogPath,
            raw_output = rawTruncated
        };
        return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });
    }

    async Task<string> Services.IIdeMcpActions.RunTestsAsync()
    {
        return await RunTestsInternalAsync(filterExpression: null, mode: "all").ConfigureAwait(false);
    }

    async Task<string> Services.IIdeMcpActions.RunAffectedTestsAsync(IReadOnlyList<string>? changedPaths)
    {
        var tokens = BuildAffectedTestTokens(changedPaths);
        if (tokens.Count == 0)
            return await RunTestsInternalAsync(filterExpression: null, mode: "fallback_all").ConfigureAwait(false);

        var filter = string.Join('|', tokens.Select(t => $"FullyQualifiedName~{t}"));
        return await RunTestsInternalAsync(filter, mode: "affected", tokens).ConfigureAwait(false);
    }

    private async Task<string> RunTestsInternalAsync(string? filterExpression, string mode, IReadOnlyList<string>? tokens = null)
    {
        var path = await Dispatcher.UIThread.InvokeAsync(() => Workspace.SolutionPath ?? "");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return JsonSerializer.Serialize(new { success = false, error = "No solution loaded or file not found.", mode });

        try
        {
            var resultsDir = Path.Combine(Path.GetDirectoryName(path) ?? "", ".cascade-ide", "test-artifacts");
            Directory.CreateDirectory(resultsDir);
            var trxFileName = $"tests-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.trx";
            var trxPath = Path.Combine(resultsDir, trxFileName);

            var workDir = Path.GetDirectoryName(path) ?? "";
            var args = new List<string>
            {
                "test",
                path,
                "--logger",
                "console;verbosity=detailed",
                "--logger",
                $"trx;LogFileName={trxFileName}",
                "--results-directory",
                resultsDir
            };
            if (!string.IsNullOrWhiteSpace(filterExpression))
            {
                args.Add("--filter");
                args.Add(filterExpression);
            }

            var (success, exitCode, output) = await _dotnetRunner.RunAsync(args, workDir).ConfigureAwait(false);
            var outStr = output;
            if (!success && exitCode != 0)
                outStr += $"\nExit code: {exitCode}";
            var parsed = File.Exists(trxPath)
                ? ParseTrx(trxPath) ?? TestOutputParser.Parse(outStr)
                : TestOutputParser.Parse(outStr);

            Dispatcher.UIThread.Post(() =>
            {
                LastTestSummary = $"{parsed.Passed}/{parsed.Total} passed, {parsed.Failed} failed";
                ImpactedTestsBadge = parsed.Failed;
                const int maxLogChars = 120_000;
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var block = $"=== {stamp} ===\n{LastTestSummary}\n\n{outStr}\n\n";
                var combined = InstrumentationPanel.TestResultsOutput + block;
                if (combined.Length > maxLogChars)
                    combined = combined[^maxLogChars..];
                InstrumentationPanel.TestResultsOutput = combined;
                if (ShowInstrumentationTabs)
                    BottomPanelTabIndex = 5;
            });
            var result = new
            {
                success = parsed.Success,
                total = parsed.Total,
                passed = parsed.Passed,
                failed = parsed.Failed,
                skipped = parsed.Skipped,
                failed_tests = parsed.FailedTests.Select(t => new { t.Name, t.Message, duration_ms = t.DurationMs }).ToList(),
                mode,
                filter = filterExpression,
                tokens,
                trx_path = File.Exists(trxPath) ? trxPath : null
            };
            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = false });
        }
        catch (Exception ex)
        {
            Dispatcher.UIThread.Post(() =>
            {
                const int maxLogChars = 120_000;
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var block = $"=== {stamp} (ошибка) ===\n{ex.Message}\n\n";
                var combined = InstrumentationPanel.TestResultsOutput + block;
                if (combined.Length > maxLogChars)
                    combined = combined[^maxLogChars..];
                InstrumentationPanel.TestResultsOutput = combined;
            });
            return JsonSerializer.Serialize(new { success = false, error = ex.Message, mode, filter = filterExpression });
        }
    }

    private static TestParseResult? ParseTrx(string trxPath)
    {
        try
        {
            var doc = XDocument.Load(trxPath);
            var root = doc.Root;
            if (root is null)
                return null;

            XNamespace ns = root.Name.Namespace;
            var counters = doc.Descendants(ns + "Counters").FirstOrDefault();
            int total = ParseInt(counters?.Attribute("total")?.Value);
            int passed = ParseInt(counters?.Attribute("passed")?.Value);
            int failed = ParseInt(counters?.Attribute("failed")?.Value);
            int skipped = ParseInt(counters?.Attribute("notExecuted")?.Value);

            var failedTests = new List<TestResultItem>();
            foreach (var unitTestResult in doc.Descendants(ns + "UnitTestResult"))
            {
                var outcome = unitTestResult.Attribute("outcome")?.Value;
                if (!string.Equals(outcome, "Failed", StringComparison.OrdinalIgnoreCase))
                    continue;

                var name = unitTestResult.Attribute("testName")?.Value ?? "";
                var duration = ParseDurationMs(unitTestResult.Attribute("duration")?.Value);
                var message = unitTestResult
                    .Descendants(ns + "Message")
                    .Select(m => m.Value)
                    .FirstOrDefault() ?? "";
                failedTests.Add(new TestResultItem(name, Passed: false, Message: message, DurationMs: duration));
            }

            return new TestParseResult(
                Total: total,
                Passed: passed,
                Failed: failed,
                Skipped: skipped,
                FailedTests: failedTests);
        }
        catch
        {
            return null;
        }

        static int ParseInt(string? raw) => int.TryParse(raw, out var value) ? value : 0;
        static int? ParseDurationMs(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            return TimeSpan.TryParse(raw, out var ts) ? (int)ts.TotalMilliseconds : null;
        }
    }

    private static IReadOnlyList<string> BuildAffectedTestTokens(IReadOnlyList<string>? changedPaths)
    {
        if (changedPaths is null || changedPaths.Count == 0)
            return Array.Empty<string>();

        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawPath in changedPaths)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                continue;

            var fileName = Path.GetFileNameWithoutExtension(rawPath);
            if (string.IsNullOrWhiteSpace(fileName))
                continue;

            // Prefer explicit test-like names; this keeps filter broad enough but still targeted.
            if (fileName.Contains("test", StringComparison.OrdinalIgnoreCase))
            {
                tokens.Add(fileName);
                continue;
            }

            tokens.Add(fileName + "Test");
            tokens.Add(fileName + "Tests");
        }
        return tokens.Take(24).ToList();
    }

    async Task<string> Services.IIdeMcpActions.RunCodeCleanupAsync(string? includePath)
    {
        var path = await Dispatcher.UIThread.InvokeAsync(() => Workspace.SolutionPath ?? "");
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return JsonSerializer.Serialize(new { success = false, error = "No solution loaded or file not found." });

        try
        {
            var workDir = Path.GetDirectoryName(path) ?? "";
            var args = new List<string>
            {
                "format",
                path,
                "--no-restore",
                "--verbosity",
                "minimal"
            };

            if (!string.IsNullOrWhiteSpace(includePath))
            {
                string includeArg;
                try
                {
                    includeArg = Path.GetFullPath(includePath);
                }
                catch
                {
                    includeArg = includePath;
                }
                args.Add("--include");
                args.Add(includeArg);
            }

            var (success, exitCode, outStr) = await _dotnetRunner.RunAsync(args, workDir).ConfigureAwait(false);
            const int maxRawChars = 4000;
            var rawTruncated = outStr.Length > maxRawChars ? outStr[..maxRawChars] + "\n... (output truncated)" : outStr;

            var pathCopy = path;
            Dispatcher.UIThread.Post(() =>
            {
                BuildOutputPanel.Set($"Code cleanup: {pathCopy}\r\n{outStr}");
                IsBuildOutputVisible = true;
            });

            return JsonSerializer.Serialize(new
            {
                success,
                exit_code = exitCode,
                raw_output = rawTruncated
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }
}

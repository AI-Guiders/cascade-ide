using System.Text.Json;
using System.Xml.Linq;
using DotNetBuildTestParsers;

namespace CascadeIDE.Services;

/// <summary>
/// Доменная логика MCP-команд «dotnet build / test / format» без привязки к UI.
/// MainWindowViewModel остаётся оркестратором: путь решения и обновление панелей — на стороне VM.
/// </summary>
public sealed class McpDotnetBuildTestService(IDotnetCommandRunner dotnetRunner)
{
    private static readonly JsonSerializerOptions JsonCompact = new() { WriteIndented = false };

    /// <summary><c>dotnet build</c> + binlog: поток в панель «Сборка» батчами (ADR 0094), в возврате — полный сырой лог (парсер, MCP).</summary>
    /// <param name="appendBatchedToLog">Снято с канала и сбатчено (~8K) в панель; с фона безопасно вызывать <c>BuildOutputPanel.Append</c>.</param>
    public async Task<(string Output, bool Success, int ExitCode, string BinlogPath)> BuildWithBinlogAsync(
        string solutionPath,
        Action<string> appendBatchedToLog,
        CancellationToken cancellationToken = default)
    {
        var artifactsDir = Path.Combine(Path.GetDirectoryName(solutionPath) ?? "", ".cascade-ide", "build-artifacts");
        Directory.CreateDirectory(artifactsDir);
        var binlogPath = Path.Combine(artifactsDir, $"build-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.binlog");
        var workDir = Path.GetDirectoryName(solutionPath) ?? "";
        var acc = new OutputAccumulator(DotnetCommandRunner.MaxOutputChars);
        var channel = BuildLogIngestion.CreateBuildLogChannel();
        var drain = BuildLogIngestion.DrainToAppendAsync(
            channel.Reader,
            appendBatchedToLog,
            8192,
            onEachDequeuedChunk: c => acc.Append(c.AsSpan()),
            cancellationToken);
        var run = dotnetRunner.RunWithChunkWriterAsync(
            ["build", solutionPath, $"-bl:{binlogPath}"],
            workDir,
            channel.Writer,
            cancellationToken);
        await Task.WhenAll(drain, run).ConfigureAwait(false);
        var (success, exitCode) = await run.ConfigureAwait(false);

        if (!success && exitCode != 0)
        {
            var suffix = $"\r\nExit code: {exitCode}";
            acc.Append(suffix.AsSpan());
            appendBatchedToLog(suffix);
        }

        return (acc.ToStringAndTrim(), success, exitCode, binlogPath);
    }

    public static string SerializeStructuredBuild(string rawOutput, string? binlogPath)
    {
        var parsed = BuildOutputParser.Parse(rawOutput);
        const int maxRawChars = 4000;
        var rawTruncated = rawOutput.Length > maxRawChars ? rawOutput[..maxRawChars] + "\n... (output truncated)" : rawOutput;
        var result = new
        {
            success = parsed.Success,
            exit_code = parsed.ExitCode,
            errors = parsed.Errors.Select(e => new { e.File, e.Line, e.Column, e.Code, e.Message }).ToList(),
            warnings = parsed.Warnings.Select(w => new { w.File, w.Line, w.Column, w.Code, w.Message }).ToList(),
            binlog_path = binlogPath,
            raw_output = rawTruncated
        };
        return JsonSerializer.Serialize(result, JsonCompact);
    }

    public sealed record TestRunOutcome(
        string JsonPayload,
        TestParseResult Parsed,
        string ConsoleOutput,
        string? TrxPath,
        string? FilterExpression,
        string Mode,
        IReadOnlyList<string>? Tokens);

    public async Task<TestRunOutcome> RunTestsAsync(
        string solutionPath,
        string? filterExpression,
        string mode,
        IReadOnlyList<string>? tokens = null,
        CancellationToken cancellationToken = default)
    {
        var resultsDir = Path.Combine(Path.GetDirectoryName(solutionPath) ?? "", ".cascade-ide", "test-artifacts");
        Directory.CreateDirectory(resultsDir);
        var trxFileName = $"tests-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.trx";
        var trxPath = Path.Combine(resultsDir, trxFileName);
        var workDir = Path.GetDirectoryName(solutionPath) ?? "";
        var args = new List<string>
        {
            "test",
            solutionPath,
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

        var (success, exitCode, output) = await dotnetRunner.RunAsync(args, workDir, cancellationToken).ConfigureAwait(false);
        var outStr = output;
        if (!success && exitCode != 0)
            outStr += $"\nExit code: {exitCode}";
        var parsed = File.Exists(trxPath)
            ? TryParseTrx(trxPath) ?? TestOutputParser.Parse(outStr)
            : TestOutputParser.Parse(outStr);

        var payload = new
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
        var json = JsonSerializer.Serialize(payload, JsonCompact);
        return new TestRunOutcome(json, parsed, outStr, File.Exists(trxPath) ? trxPath : null, filterExpression, mode, tokens);
    }

    public static string SerializeTestRunFailure(string message, string mode, string? filterExpression) =>
        JsonSerializer.Serialize(new { success = false, error = message, mode, filter = filterExpression });

    /// <summary><c>dotnet format</c> на решение: поток в панель батчами, полный вывод в <c>RawOutput</c> (JSON MCP).</summary>
    /// <param name="appendBatchedToLog">Батч в панель «Сборка»; с фона безопасен.</param>
    public async Task<(bool Success, int ExitCode, string RawOutput)> RunCodeCleanupAsync(
        string solutionPath,
        string? includePath,
        Action<string> appendBatchedToLog,
        CancellationToken cancellationToken = default)
    {
        var workDir = Path.GetDirectoryName(solutionPath) ?? "";
        var args = new List<string>
        {
            "format",
            solutionPath,
            "--no-restore",
            "--verbosity",
            "minimal"
        };

        if (!string.IsNullOrWhiteSpace(includePath))
        {
            string includeArg;
            try
            {
                includeArg = CanonicalFilePath.Normalize(includePath);
            }
            catch
            {
                includeArg = includePath;
            }
            args.Add("--include");
            args.Add(includeArg);
        }

        var acc = new OutputAccumulator(DotnetCommandRunner.MaxOutputChars);
        var channel = BuildLogIngestion.CreateBuildLogChannel();
        var drain = BuildLogIngestion.DrainToAppendAsync(
            channel.Reader,
            appendBatchedToLog,
            8192,
            onEachDequeuedChunk: c => acc.Append(c.AsSpan()),
            cancellationToken);
        var run = dotnetRunner.RunWithChunkWriterAsync(args, workDir, channel.Writer, cancellationToken);
        await Task.WhenAll(drain, run).ConfigureAwait(false);
        var (success, exitCode) = await run.ConfigureAwait(false);
        return (success, exitCode, acc.ToStringAndTrim());
    }

    public static TestParseResult? TryParseTrx(string trxPath)
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

    public static IReadOnlyList<string> BuildAffectedTestTokens(IReadOnlyList<string>? changedPaths)
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
}

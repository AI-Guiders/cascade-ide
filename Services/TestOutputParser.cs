using System.Text.RegularExpressions;

namespace CascadeIDE.Services;

/// <summary>Результат одного теста: имя и при падении — сообщение и опционально время.</summary>
public sealed record TestResultItem(string Name, bool Passed, string? Message = null, int? DurationMs = null);

/// <summary>Результат разбора вывода dotnet test (console logger).</summary>
public sealed record TestParseResult(
    int Total,
    int Passed,
    int Failed,
    int Skipped,
    IReadOnlyList<TestResultItem> FailedTests)
{
    public bool Success => Failed == 0;
}

public static class TestOutputParser
{
    // Passed TestName [123 ms]  или  Failed TestName [123 ms]  или  Skipped TestName
    private static readonly Regex ResultLineRegex = new(
        @"^\s*(Passed|Failed|Skipped)\s+(.+?)(?:\s+\[(\d+)\s*ms\])?\s*$",
        RegexOptions.Compiled);

    // Error Message: ...  или  Stack Trace: ...
    private static readonly Regex MessageLineRegex = new(
        @"^\s*(?:Error Message|Message|Stack Trace):\s*(.*)$",
        RegexOptions.Compiled);

    /// <summary>Разбирает вывод dotnet test (logger console;verbosity=normal или detailed).</summary>
    public static TestParseResult Parse(string output)
    {
        if (string.IsNullOrEmpty(output))
            return new TestParseResult(0, 0, 0, 0, []);

        var passed = 0;
        var failed = 0;
        var skipped = 0;
        var failedTests = new List<TestResultItem>();
        var lines = output.Split('\n');
        string? lastFailedName = null;
        int? lastFailedDuration = null;
        var messageLines = new List<string>();

        void FlushFailed()
        {
            if (lastFailedName is null) return;
            var msg = messageLines.Count > 0 ? string.Join(" ", messageLines).Trim() : null;
            if (string.IsNullOrWhiteSpace(msg)) msg = null;
            failedTests.Add(new TestResultItem(lastFailedName, false, msg, lastFailedDuration));
            lastFailedName = null;
            lastFailedDuration = null;
            messageLines.Clear();
        }

        foreach (var line in lines)
        {
            var m = ResultLineRegex.Match(line.Trim());
            if (m.Success)
            {
                FlushFailed();
                var outcome = m.Groups[1].Value;
                var name = m.Groups[2].Value.Trim();
                var duration = m.Groups[3].Success && m.Groups[3].Value.Length > 0 ? int.Parse(m.Groups[3].ValueSpan) : (int?)null;

                switch (outcome)
                {
                    case "Passed":
                        passed++;
                        break;
                    case "Failed":
                        failed++;
                        lastFailedName = name;
                        lastFailedDuration = duration;
                        break;
                    case "Skipped":
                        skipped++;
                        break;
                }
                continue;
            }

            var msgMatch = MessageLineRegex.Match(line);
            if (msgMatch.Success && lastFailedName is not null)
                messageLines.Add(msgMatch.Groups[1].Value.Trim());
        }

        FlushFailed();

        // Итоговые строки вида "Passed!  - Failed: 0, Passed: 5, Skipped: 0"
        var summaryMatch = Regex.Match(output, @"(?:Failed|Passed)!\s*-\s*Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+)", RegexOptions.IgnoreCase);
        if (summaryMatch.Success)
        {
            failed = int.Parse(summaryMatch.Groups[1].ValueSpan);
            passed = int.Parse(summaryMatch.Groups[2].ValueSpan);
            skipped = int.Parse(summaryMatch.Groups[3].ValueSpan);
        }
        else
        {
            var total = passed + failed + skipped;
            if (total == 0 && failedTests.Count > 0)
                failed = failedTests.Count;
        }

        var totalCount = passed + failed + skipped;
        return new TestParseResult(totalCount, passed, failed, skipped, failedTests);
    }
}

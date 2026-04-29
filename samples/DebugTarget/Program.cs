namespace DebugTarget;

/// <summary>Небольшая «живая» цель для отладки из Cascade IDE (не запускать саму IDE как debuggee).</summary>
internal static class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("DebugTarget — тестовая цель отладки Cascade IDE.");

        var input = args.Length > 0 ? args : ["7", "3", "-2", "oops", "11", "0"];
        var numbers = ParseInput(input);
        Console.WriteLine($"parsed={string.Join(", ", numbers)}");

        var report = await BuildReportAsync(numbers);
        Console.WriteLine($"sum={report.Sum}; even={report.EvenCount}; risky={report.RiskyResult}");

        foreach (var line in report.Trace)
            Console.WriteLine(line);
    }

    private static List<int> ParseInput(string[] args)
    {
        var numbers = new List<int>(args.Length);
        foreach (var raw in args)
        {
            if (int.TryParse(raw, out var n))
            {
                numbers.Add(n);
                continue;
            }

            // Intentional branch for debugger conditions.
            if (raw.Length > 3)
                numbers.Add(raw.Length);
        }

        return numbers;
    }

    private static async Task<Report> BuildReportAsync(IReadOnlyList<int> numbers)
    {
        var trace = new List<string>();
        var sum = 0;
        var evenCount = 0;

        for (var i = 0; i < numbers.Count; i++)
        {
            var current = numbers[i];
            var weighted = current * (i + 1);
            sum += weighted;

            if ((current & 1) == 0)
                evenCount++;

            trace.Add($"i={i}; n={current}; weighted={weighted}; sum={sum}");
            await Task.Delay(20);
        }

        var risky = TryComputeRisky(numbers, trace);
        return new Report(sum, evenCount, risky, trace);
    }

    private static int TryComputeRisky(IReadOnlyList<int> numbers, List<string> trace)
    {
        try
        {
            var firstNegative = numbers.FirstOrDefault(static n => n < 0);
            if (firstNegative == 0)
                throw new InvalidOperationException("No negative number found.");

            var divisor = Math.Abs(firstNegative) - 2;
            var source = numbers.Max();
            var result = checked(source / divisor);
            trace.Add($"risky: source={source}; divisor={divisor}; result={result}");
            return result;
        }
        catch (Exception ex)
        {
            trace.Add($"risky failed: {ex.GetType().Name}; msg={ex.Message}");
            return -1;
        }
    }

    private sealed record Report(int Sum, int EvenCount, int RiskyResult, IReadOnlyList<string> Trace);
}

using SniperCalc;

Console.WriteLine("SniperCalc — REPL (+, -, *, /, %). Empty line to quit.");

while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line))
        break;

    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 3 || !double.TryParse(parts[0], out var left) || !double.TryParse(parts[2], out var right))
    {
        Console.WriteLine("Format: <number> <op> <number>  e.g. 2 + 3");
        continue;
    }

    try
    {
        var result = Calculator.Evaluate(left, parts[1], right);
        Console.WriteLine($"= {result}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}


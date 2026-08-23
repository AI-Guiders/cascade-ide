namespace SniperCalc;

public static class Calculator
{
    public static double Evaluate(double left, string op, double right) => op switch
    {
        "+" => left + right,
        "-" => left - right,
        "*" => left * right,
        "/" => right == 0 ? throw new DivideByZeroException() : left / right,
        "%" => right == 0 ? throw new DivideByZeroException() : left % right,
        _ => throw new ArgumentException($"Unknown operator: {op}")
    };
}

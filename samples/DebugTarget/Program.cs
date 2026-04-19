namespace DebugTarget;

/// <summary>Минимальная консоль для отладки из Cascade IDE (не запускать саму IDE как debuggee).</summary>
internal static class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("DebugTarget — тестовая цель отладки Cascade IDE.");
        Greet(args.Length);
    }

    private static void Greet(int argc)
    {
        Console.WriteLine($"argc={argc}");
    }
}

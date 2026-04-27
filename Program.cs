using Avalonia;
using CascadeIDE.Services;
using CascadeIDE.Services.AgentContract;

namespace CascadeIDE;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        InlayLogPaths.EnsureInlayLogEnvironment();
        if (args.Length > 0 && string.Equals(args[0], "--agent-contract", StringComparison.OrdinalIgnoreCase))
        {
            var tail = args.AsSpan(1);
            var argv = tail.Length == 0 ? Array.Empty<string>() : tail.ToArray();
            Environment.Exit(AgentContractRunner.Run(argv));
            return;
        }

        App.RunMcpStdio = args.Contains("--mcp-stdio");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

using Avalonia;
using Avalonia.Win32;
using CascadeIDE.Features.MagicLink;
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

        var magicLinkUri = CideMagicLinkArgExtractor.FromArgs(args);
        if (!CideMagicLinkSingleInstance.TryAcquirePrimary(out var primaryMutex))
        {
            if (!string.IsNullOrWhiteSpace(magicLinkUri)
                && CideMagicLinkSingleInstance.TryForwardToPrimary(magicLinkUri))
            {
                return;
            }
        }
        else
        {
            App.MagicLinkPrimaryMutex = primaryMutex;
            if (!string.IsNullOrWhiteSpace(magicLinkUri))
                App.PendingMagicLinkUri = magicLinkUri;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

        // Диагностика «прозрачных» окон: set CASCADE_RENDER_SOFTWARE=1 (Win32 only).
        if (OperatingSystem.IsWindows()
            && string.Equals(Environment.GetEnvironmentVariable("CASCADE_RENDER_SOFTWARE"), "1", StringComparison.Ordinal))
        {
            builder = builder.With(new Win32PlatformOptions
            {
                RenderingMode = [Win32RenderingMode.Software]
            });
        }

        return builder;
    }
}

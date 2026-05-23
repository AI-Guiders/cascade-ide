using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Lang;
using CascadeIDE.ViewModels;
using CascadeIDE.Views;
using ModelContextProtocol.Server;

namespace CascadeIDE;

public partial class App : Application
{
    /// <summary>Запуск с MCP-сервером на stdio (агент/Cursor подключается к IDE по stdin/stdout).</summary>
    public static bool RunMcpStdio { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        UiCulture.ApplyFromSettingsOrSystem();
        UiModeCatalog.Initialize();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainWindowViewModel();
            vm.IsMcpServerMode = RunMcpStdio;
            desktop.MainWindow = new MainWindow { DataContext = vm };
            if (RunMcpStdio)
                _ = RunMcpServerAsync(vm);
            _ = vm.RefreshOllamaAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task RunMcpServerAsync(MainWindowViewModel vm)
    {
        try
        {
            var options = Services.IdeMcpServer.BuildOptions(vm.IdeMcp);
            await using var server = McpServer.Create(new StdioServerTransport("CascadeIDE"), options);
            await server.RunAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MCP server error: {ex.Message}");
        }
    }

}

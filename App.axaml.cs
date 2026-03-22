using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            var vm = new MainWindowViewModel();
            vm.IsMcpServerMode = RunMcpStdio;
            desktop.MainWindow = new MainWindow { DataContext = vm };
            if (RunMcpStdio && Services.SettingsService.Load().IdeMcpServerEnabled)
                _ = RunMcpServerAsync(vm);
            _ = vm.RefreshOllamaAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task RunMcpServerAsync(MainWindowViewModel vm)
    {
        try
        {
            var options = Services.IdeMcpServer.BuildOptions(vm);
            await using var server = McpServer.Create(new StdioServerTransport("CascadeIDE"), options);
            await server.RunAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MCP server error: {ex.Message}");
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
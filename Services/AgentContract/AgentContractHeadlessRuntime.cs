using System.Text.Json;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services.AgentContract;

/// <summary>
/// Headless Avalonia + <see cref="MainWindowViewModel"/> для <see cref="AgentContractRunner"/> (ADR 0052).
/// Без этого <see cref="IUiScheduler.InvokeAsync"/> не завершается.
/// </summary>
internal static class AgentContractHeadlessRuntime
{
    private static readonly object Gate = new();
    private static bool _initialized;

    /// <summary>Короткая сводка решения — тот же JSON, что MCP <c>ide_get_solution_info</c> / <see cref="IIdeMcpActions.GetSolutionInfo"/>.</summary>
    public static string GetSolutionInfoJson()
    {
        EnsureInitialized();
        return Dispatcher.UIThread.Invoke(() =>
        {
            var vm = new MainWindowViewModel();
            return ((IIdeMcpActions)vm).GetSolutionInfo();
        });
    }

    /// <summary>CDS JSON — тот же объект, что <c>cockpit_surface</c> в <see cref="IIdeMcpActions.GetIdeStateAsync"/>.</summary>
    public static string GetCockpitSurfaceJson()
    {
        EnsureInitialized();
        return Dispatcher.UIThread.Invoke(() =>
        {
            var vm = new MainWindowViewModel();
            return JsonSerializer.Serialize(vm.BuildCockpitSurfaceSnapshot());
        });
    }

    /// <summary>Полная сводка — тот же JSON, что MCP <c>ide_get_ide_state</c>.</summary>
    /// <remarks>
    /// Внутри — вложенные <c>UiScheduler.InvokeAsync</c>; без прокрутки очереди (<see cref="Dispatcher.RunJobs"/>) задача не завершится.
    /// </remarks>
    public static string GetIdeStateJson()
    {
        EnsureInitialized();
        return Dispatcher.UIThread.Invoke(() =>
        {
            var vm = new MainWindowViewModel();
            IIdeMcpActions mcp = vm;
            var task = mcp.GetIdeStateAsync();
            for (var n = 0; !task.IsCompleted; n++)
            {
                if (n > 500_000)
                    throw new InvalidOperationException("get_ide_state: dispatcher did not drain (possible deadlock).");
                Dispatcher.UIThread.RunJobs();
            }

            return task.GetAwaiter().GetResult();
        });
    }

    private static void EnsureInitialized()
    {
        lock (Gate)
        {
            if (_initialized)
                return;

            // Уже поднято (например Avalonia.Headless.XUnit в тестах) — не создаём второй Application.
            if (Application.Current is not null)
            {
                _initialized = true;
                return;
            }

            AppBuilder.Configure<AgentContractHeadlessApplication>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();
            _initialized = true;
        }
    }

    private sealed class AgentContractHeadlessApplication : Application
    {
        public override void Initialize()
        {
            Styles.Add(new FluentTheme());
        }
    }
}

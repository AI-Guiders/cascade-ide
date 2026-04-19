using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;

[assembly: AvaloniaTestApplication(typeof(CascadeIDE.Tests.AvaloniaTestAppBuilder))]

namespace CascadeIDE.Tests;

/// <summary>Сборка headless-платформы для <see cref="AvaloniaFact"/> / диспетчера UI в тестах.</summary>
public static class AvaloniaTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<HeadlessTestApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

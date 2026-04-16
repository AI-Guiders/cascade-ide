using Avalonia;
using Avalonia.Themes.Fluent;

namespace CascadeIDE.Tests;

/// <summary>Минимальное приложение для headless-тестов (без главного окна и без полного <c>App.axaml</c> IDE).</summary>
public sealed class HeadlessTestApplication : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
    }
}

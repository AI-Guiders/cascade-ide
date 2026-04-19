using Avalonia;
using Avalonia.Themes.Fluent;
using CascadeIDE.Lang;
using CascadeIDE.Views;

namespace CascadeIDE.Tests;

/// <summary>Минимальное приложение для headless-тестов (без главного окна и без полного <c>App.axaml</c> IDE).</summary>
public sealed class HeadlessTestApplication : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Resources["Loc"] = new LocViewModel();
        Resources["UiModeFamilyEq"] = new UiModeFamilyEqualsConverter();
        Resources["UiModeFamilyNe"] = new UiModeFamilyNotEqualsConverter();
    }
}

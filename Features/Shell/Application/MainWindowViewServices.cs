#nullable enable

using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using CascadeIDE.Views;

namespace CascadeIDE.Features.Shell.Application;

/// <summary>Точка входа View → Services для главного окна (не вызывать Services из code-behind напрямую).</summary>
public static class MainWindowViewServices
{
    public static void ApplyHotkeys(MainWindow window, MainWindowViewModel vm) =>
        MainWindowHotkeyService.ApplyAll(window, vm);

    public static string GetColorsUnderCursorJson(MainWindow window) =>
        UiColorsUnderCursor.GetJson(window);

    public static string GetControlAppearanceJson(MainWindow window, string name) =>
        UiControlAppearance.GetJson(window, name);

    public static string ApplyControlLayout(MainWindow window, string name, string json) =>
        UiControlLayoutApply.Apply(window, name, json);
}

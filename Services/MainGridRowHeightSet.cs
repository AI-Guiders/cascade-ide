using Avalonia.Controls;

namespace CascadeIDE.Services;

/// <summary>
/// Высоты строк 3–5 главного <c>MainGrid</c> (рабочая область, горизонтальный сплиттер, низ).
/// Одна точка правды для соотношения <c>2* / 6px / *</c> и для случая «низ скрыт» (<c>0 px</c> вместо star-строки).
/// </summary>
public readonly record struct MainGridRowHeightSet(
    GridLength Workspace,
    GridLength BottomSplitter,
    GridLength BottomPanel)
{
    /// <param name="showWorkspaceBottomChrome">Телеметрия и/или нижняя панель (см. <c>ShowWorkspaceBottomChrome</c> в VM).</param>
    public static MainGridRowHeightSet ForWorkspaceBottomChrome(bool showWorkspaceBottomChrome) =>
        showWorkspaceBottomChrome
            ? new MainGridRowHeightSet(
                new GridLength(2, GridUnitType.Star),
                new GridLength(6, GridUnitType.Pixel),
                new GridLength(1, GridUnitType.Star))
            : new MainGridRowHeightSet(
                new GridLength(1, GridUnitType.Star),
                new GridLength(0, GridUnitType.Pixel),
                new GridLength(0, GridUnitType.Pixel));
}

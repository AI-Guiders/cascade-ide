namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Числовые константы рабочей области: MainGrid, нижняя зона, колонка редактора, ширины региона Mfd по режимам.
/// Единая точка вместо разрозненных литералов в VM и code-behind.
/// </summary>
public static class UiWorkspaceLayoutDimensions
{
    public const int PfdRegionDefaultWidthPixels = 220;

    /// <summary>Сплиттеры между колонками MainGrid (дерево | редактор | Mfd).</summary>
    public const double MainGridColumnSplitterWidthPixels = 4;

    /// <summary>Минимальная высота строки нижней панели (терминал) и строки вывода сборки в колонке редактора.</summary>
    public const int BottomPanelMinRowPixels = 80;

    /// <summary>Свёрнутый регион Mfd в main grid: 0 px.</summary>
    public const int MfdRegionCollapsedWidthPixels = 0;

    public const int MfdRegionExpandedDefaultWidthPixels = 340;
    public const int MfdRegionExpandedPowerWidthPixels = 420;
    public const int MfdRegionExpandedAgentChatWidthPixels = 520;
}

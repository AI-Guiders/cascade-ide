namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Числовые константы рабочей области: MainGrid, нижняя зона, колонка редактора, ширины чата по режимам.
/// Единая точка вместо разрозненных литералов в VM и code-behind.
/// </summary>
public static class UiWorkspaceLayoutDimensions
{
    public const int SolutionExplorerDefaultWidthPixels = 220;

    /// <summary>Сплиттеры между колонками MainGrid (дерево | редактор | чат).</summary>
    public const double MainGridColumnSplitterWidthPixels = 4;

    /// <summary>Минимальная высота строки нижней панели (терминал) и строки вывода сборки в колонке редактора.</summary>
    public const int BottomPanelMinRowPixels = 80;

    /// <summary>Свёрнутый чат: колонка 0 px.</summary>
    public const int ChatPanelCollapsedWidthPixels = 0;

    public const int ChatPanelExpandedDefaultWidthPixels = 340;
    public const int ChatPanelExpandedPowerWidthPixels = 420;
    public const int ChatPanelExpandedAgentChatWidthPixels = 520;
}

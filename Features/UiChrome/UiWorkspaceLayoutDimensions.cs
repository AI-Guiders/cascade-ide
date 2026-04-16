namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Числовые константы рабочей области: MainGrid, нижняя зона, колонка редактора, ширины региона Mfd по режимам.
/// Единая точка вместо разрозненных литералов в VM и code-behind.
/// </summary>
public static class UiWorkspaceLayoutDimensions
{
    /// <summary>
    /// Индексы колонок <c>MainWindow.MainGrid</c> (док-панель: PFD | сплиттер | Forward | сплиттер | MFD).
    /// Согласовано с <c>MainWindow.axaml</c> и сборщиком строки колонок presentation (5 колонок).
    /// </summary>
    public static class MainWindowMainGridColumns
    {
        public const int PfdRegion = 0;
        public const int PfdSplitter = 1;
        public const int ForwardRegion = 2;
        public const int MfdSplitter = 3;
        public const int MfdRegion = 4;

        /// <summary>Число колонок в дефолтной строке <c>ColumnDefinitions</c> главного окна.</summary>
        public const int Count = MfdRegion + 1;
    }

    /// <summary>Колонки <c>EditorContentGrid</c> во вкладке документа (редактор | превью Markdown).</summary>
    public static class EditorContentGridColumns
    {
        public const int Editor = 0;
        public const int MarkdownPreview = 1;
    }

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

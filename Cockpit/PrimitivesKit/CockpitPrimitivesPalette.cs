#nullable enable

using Avalonia.Media;
using CascadeIDE.Cockpit.Channels.Eicas;
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.PrimitivesKit;

/// <summary>
/// Единый слой ARGB для примитивов кабины (визуальный язык, ADR 0064).
/// Правки палитры — здесь; отрисовка и конвертеры только ссылаются на эти значения.
/// </summary>
public static class CockpitPrimitivesPalette
{
    /// <summary>
    /// Канал EICAS / CAS: три уровня W/C/A (ADR 0021 §5). Канон для текста полосы, бейджей и сопоставимых уровней ламп.
    /// Значения совпадают с прежними hex в <c>EicasSeverityToBrushConverter</c>.
    /// Соответствие бытовым словам (Error / Warning / Information) и <see cref="AnnunciatorLampLevel"/> — таблица в ADR 0021 §5.
    /// </summary>
    public static class Eicas
    {
        public static readonly Color Warning = Color.Parse("#C02828");
        public static readonly Color Caution = Color.Parse("#B8860B");
        public static readonly Color Advisory = Color.Parse("#1565C0");

        public static Color Foreground(EicasSeverity severity) =>
            severity switch
            {
                EicasSeverity.Warning => Warning,
                EicasSeverity.Caution => Caution,
                _ => Advisory,
            };
    }

    /// <summary>Annunciator / Korry: корпус, линзы, акценты списка. Маппинг уровней — ADR 0021 §5, таблица.</summary>
    public static class Annunciator
    {
        public static readonly Color BezelOuter = Color.Parse("#3A3A3A");
        public static readonly Color BezelInner = Color.Parse("#1E1E1E");
        public static readonly Color Housing = Color.Parse("#080808");
        public static readonly Color PanelBackground = Color.Parse("#0C0C0C");
        public static readonly Color BevelHighlight = Color.Parse("#4A4A4A");
        public static readonly Color BevelShadow = Color.Parse("#0A0A0A");

        public static readonly Color OffLensTop = Color.Parse("#101010");
        public static readonly Color OffLensBottom = Color.Parse("#1C1C1C");
        public static readonly Color OffLensBorder = Color.Parse("#050505");
        public static readonly Color OffLabelFill = Color.Parse("#5C5C5C");
        public static readonly Color OutlinedTextStrokeDim = Color.Parse("#121212");
        public static readonly Color OutlinedTextStrokeLit = Color.Parse("#0A0A0A");

        /// <summary>Подсвеченная линза (не <see cref="AnnunciatorLampLevel.Ok"/>).</summary>
        public static readonly Color LitLensCaution = Eicas.Caution;
        public static readonly Color LitLensAdvisory = Eicas.Advisory;
        public static readonly Color LitLensCritical = Eicas.Warning;
        public static readonly Color LitLensUnknown = Color.Parse("#888888");

        /// <summary>Акцент строки таблицы / списка по уровню (иконка, заголовок; Ok = «норма», не линза).</summary>
        public static readonly Color RowAccentOk = Color.Parse("#2D8A5A");
        public static readonly Color RowAccentCaution = Eicas.Caution;
        public static readonly Color RowAccentAdvisory = Eicas.Advisory;
        public static readonly Color RowAccentCritical = Eicas.Warning;
        public static readonly Color RowAccentUnknown = Color.Parse("#888888");

        public static readonly Color TooltipDetailForeground = Color.Parse("#D0D0D0");

        /// <summary>CascadeChord toolbar: лампа «CMD + полоса» в armed (ADR 0060/0064), не EICAS W/C/A.</summary>
        public static readonly Color CommandArmedLabelAmber = Color.Parse("#FFB000");

        /// <summary>Нижняя полоса активности (armed) — зелёный индикатор «вкл».</summary>
        public static readonly Color CommandArmedStripGreen = Color.Parse("#00E676");

        public static Color LitLens(AnnunciatorLampLevel level) =>
            level switch
            {
                AnnunciatorLampLevel.Caution => LitLensCaution,
                AnnunciatorLampLevel.Advisory => LitLensAdvisory,
                AnnunciatorLampLevel.Critical => LitLensCritical,
                AnnunciatorLampLevel.Ok => throw new InvalidOperationException("Ok is drawn as off; no lit fill color."),
                _ => LitLensUnknown,
            };

        public static Color RowAccent(AnnunciatorLampLevel level) =>
            level switch
            {
                AnnunciatorLampLevel.Ok => RowAccentOk,
                AnnunciatorLampLevel.Caution => RowAccentCaution,
                AnnunciatorLampLevel.Advisory => RowAccentAdvisory,
                AnnunciatorLampLevel.Critical => RowAccentCritical,
                _ => RowAccentUnknown,
            };
    }

    /// <summary>Мини-карта навигации по коду (control flow / звезда) — узлы, рёбра, подписи; в UI — «Карта намерений».</summary>
    public static class CodeNavigationMap
    {
        public static readonly Color AnchorFill = Color.Parse("#7CC9FF");
        public static readonly Color ConditionFill = Color.FromArgb(240, 255, 210, 120);
        public static readonly Color ExitFill = Color.FromArgb(230, 165, 175, 190);
        public static readonly Color CallFill = Color.Parse("#9B8CFF");
        /// <summary>Обработчик исключений (catch) / «защищённый» участок (ADR 0053).</summary>
        public static readonly Color HandlerFill = Color.FromArgb(235, 70, 175, 155);
        public static readonly Color SideLabel = Color.FromArgb(220, 225, 235, 255);
        public static readonly Color BaseEdge = Color.FromArgb(180, 140, 140, 160);
        public static readonly Color ConditionalEdge = Color.FromArgb(220, 255, 210, 120);
        public static readonly Color MultiBranchEdge = Color.FromArgb(200, 110, 195, 255);
        public static readonly Color LoopEdge = Color.FromArgb(235, 120, 230, 255);
        public static readonly Color HighlightedEdge = Color.FromArgb(245, 255, 255, 190);
        public static readonly Color HighlightedLoopEdge = Color.FromArgb(250, 255, 255, 200);
        public static readonly Color HighlightedNode = Color.FromArgb(230, 255, 255, 200);
        public static readonly Color NodeStroke = Color.Parse("#22000000");
    }

    /// <summary>
    /// Мини-карта в режиме «связанные файлы» — отдельный акцент от CFG (тот же примитивный рендер, другая палитра; ADR 0067).
    /// </summary>
    public static class CodeNavigationMapWorkspace
    {
        public static readonly Color AnchorFill = Color.Parse("#4A9FD8");
        public static readonly Color ConditionFill = Color.FromArgb(235, 230, 195, 130);
        public static readonly Color ExitFill = Color.FromArgb(225, 150, 165, 185);
        public static readonly Color PeerFill = Color.FromArgb(235, 130, 175, 210);
        public static readonly Color HandlerFill = Color.FromArgb(230, 55, 165, 145);
        public static readonly Color SideLabel = Color.FromArgb(215, 210, 225, 240);
        public static readonly Color BaseEdge = Color.FromArgb(175, 100, 140, 175);
        public static readonly Color ConditionalEdge = Color.FromArgb(215, 220, 195, 130);
        public static readonly Color MultiBranchEdge = Color.FromArgb(195, 95, 175, 220);
        public static readonly Color LoopEdge = Color.FromArgb(225, 110, 200, 235);
        public static readonly Color HighlightedEdge = Color.FromArgb(240, 255, 250, 185);
        public static readonly Color HighlightedLoopEdge = Color.FromArgb(245, 255, 252, 195);
        public static readonly Color HighlightedNode = Color.FromArgb(225, 255, 252, 200);
        public static readonly Color NodeStroke = Color.Parse("#22000000");
    }
}

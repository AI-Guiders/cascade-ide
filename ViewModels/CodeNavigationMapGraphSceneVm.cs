#nullable enable
using Avalonia;
using CascadeIDE.Models;

namespace CascadeIDE.ViewModels;

/// <summary>Какой доменный граф рисуем на мини-карте: CFG vs связанные файлы (ADR 0067).</summary>
public enum CodeNavigationMapGraphPresentationKind
{
    /// <summary>Control flow / CFG (полётный план по коду).</summary>
    CodeControlFlow = 0,

    /// <summary>Якорь и связанные файлы проекта (звезда / workspace).</summary>
    WorkspaceRelatedFiles = 1
}

/// <summary>Как смонтирован блок легенды (номера шагов / ключи фигур) относительно графа control-flow — на уровне блока, а не пиксельной «подтяжки».</summary>
public enum CodeNavigationMapLegendBlockPlacement
{
    /// <summary>Колонка справа от графа, текст — в оставшейся ширине (классический split).</summary>
    BesideGraph = 0,

    /// <summary>Блок под графом, с выравниванием слева на всю ширину вьюпорта.</summary>
    BelowGraph = 1
}

/// <summary>Сцена мини-карты навигации по коду (узлы с центром в логических пикселях контрола).</summary>
public sealed class CodeNavigationMapGraphSceneVm
{
    public required IReadOnlyList<CodeNavigationMapGraphNodeLayout> Nodes { get; init; }
    public required IReadOnlyList<CodeNavigationMapGraphEdgeLayout> Edges { get; init; }

    /// <summary>Визуальный язык сцены; Render — <see cref="CascadeIDE.Views.SkiaKit.Graph.SkiaGraphVisualTheme.ForPresentation"/>.</summary>
    public CodeNavigationMapGraphPresentationKind Presentation { get; init; } = CodeNavigationMapGraphPresentationKind.CodeControlFlow;
    public IReadOnlyList<CodeNavigationMapLegendEntry> Legend { get; init; } = [];
    /// <summary>Резервировать колонку под легенду (номера шагов и/или обозначения фигур).</summary>
    public bool UseLegendColumn { get; init; }
    /// <summary>Показать в легенде расшифровку: ромб — условие.</summary>
    public bool ShowLegendConditionKey { get; init; }
    /// <summary>Показать в легенде расшифровку: круг со стрелкой — return.</summary>
    public bool ShowLegendReturnKey { get; init; }
    /// <summary>Показать в легенде расшифровку: обработчик исключений (catch) / ребро ExceptionFlow.</summary>
    public bool ShowLegendExceptionFlowKey { get; init; }
    /// <summary>Показать в легенде стили рёбер: сплошная / пунктир (условие, multibranch, loop).</summary>
    public bool ShowLegendEdgeStyleKey { get; init; }
    /// <summary>Левая граница колонки легенды (X); если легенды нет — равна ширине области (не рисуем). При <see cref="LegendPlacement"/> = <see cref="CodeNavigationMapLegendBlockPlacement.BelowGraph"/> — левый отступ текста (как у графа).</summary>
    public double LegendColumnLeft { get; init; } = double.PositiveInfinity;

    /// <summary>Кладка легенды: рядом с графом или снизу (control-flow; для звёзд не используется).</summary>
    public CodeNavigationMapLegendBlockPlacement LegendPlacement { get; init; } = CodeNavigationMapLegendBlockPlacement.BesideGraph;

    /// <summary>
    /// Y начала блока легенды при <see cref="LegendPlacement"/> = <see cref="CodeNavigationMapLegendBlockPlacement.BelowGraph"/>;
    /// иначе не используется.
    /// </summary>
    public double LegendBlockTopY { get; init; }
    public IReadOnlySet<string> HighlightedNodeIds { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> HighlightedEdgeKeys { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Номера шагов на узлах (без колонки легенды).</summary>
    public bool ShowNodeLegendGlyphs { get; init; }

    /// <summary><c>radial</c> | <c>top_down</c> | <c>bottom_up</c> (related-files).</summary>
    public string RelatedFilesLayout { get; init; } = CodeNavigationMapRelatedGraphLayoutKind.Radial;

    /// <summary>
    /// Размер шрифта боковых подписей узлов (call_step), согласованный с укладкой; null — <see cref="CascadeIDE.Cockpit.Graph.Layout.GraphRenderInvariants.MinSideLabelFontSize"/> при отрисовке.
    /// </summary>
    public double? SideLabelFontSizePx { get; init; }

    /// <summary>Ширина viewport при укладке (для hit-test при расхождении с <c>Bounds</c>).</summary>
    public double LayoutViewportWidth { get; init; }

    /// <summary>Высота viewport при укладке (для hit-test при расхождении с <c>Bounds</c>).</summary>
    public double LayoutViewportHeight { get; init; }

    public bool IsEmpty => Nodes.Count == 0;
}

/// <summary>Строка легенды control flow: номер ↔ одна строка кода/предиката.</summary>
public sealed class CodeNavigationMapLegendEntry
{
    public int Index { get; init; }
    public required string Text { get; init; }
}

public enum CodeNavigationMapNodeShape
{
    /// <summary>Обычный шаг (круг).</summary>
    Circle,

    /// <summary>Условие / ветвление в CFG; на карте — ромб (не имя геометрии в API).</summary>
    Condition
}

public sealed class CodeNavigationMapGraphNodeLayout
{
    public required string Id { get; init; }
    public required string Kind { get; init; }
    public required string FullPath { get; init; }
    public required string Label { get; init; }
    public required Point Center { get; init; }
    public required double Radius { get; init; }
    public required bool IsAnchor { get; init; }
    public CodeNavigationMapNodeShape Shape { get; init; } = CodeNavigationMapNodeShape.Circle;
    public int? LegendIndex { get; init; }
    public string? LegendLine { get; init; }
    public int? LineStart { get; init; }
    public int? LineEnd { get; init; }
}

public sealed class CodeNavigationMapGraphEdgeLayout
{
    public required string FromNodeId { get; init; }
    public required string ToNodeId { get; init; }
    public required Point From { get; init; }
    public required Point To { get; init; }
    public required double ToRadius { get; init; }
    public string? Kind { get; init; }
    public string? RelatedKind { get; init; }

    public string Key => $"{FromNodeId}->{ToNodeId}";
}

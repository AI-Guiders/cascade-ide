#nullable enable
using Avalonia;

namespace CascadeIDE.ViewModels;

/// <summary>Какой доменный граф рисуем на мини-карте: CFG vs связанные файлы (ADR 0067).</summary>
public enum SemanticMapGraphPresentationKind
{
    /// <summary>Control flow / CFG (полётный план по коду).</summary>
    CodeControlFlow = 0,

    /// <summary>Якорь и связанные файлы проекта (звезда / workspace).</summary>
    WorkspaceRelatedFiles = 1
}

/// <summary>Сцена мини-карты Semantic Map (узлы с центром в логических пикселях контрола).</summary>
public sealed class SemanticMapGraphSceneVm
{
    public required IReadOnlyList<SemanticMapGraphNodeLayout> Nodes { get; init; }
    public required IReadOnlyList<SemanticMapGraphEdgeLayout> Edges { get; init; }

    /// <summary>Визуальный язык сцены; <see cref="CascadeIDE.Cockpit.PrimitivesKit.SemanticMapVisualTheme.ForPresentation"/>.</summary>
    public SemanticMapGraphPresentationKind Presentation { get; init; } = SemanticMapGraphPresentationKind.CodeControlFlow;
    public IReadOnlyList<SemanticMapLegendEntry> Legend { get; init; } = [];
    /// <summary>Резервировать колонку под легенду (номера шагов и/или обозначения фигур).</summary>
    public bool UseLegendColumn { get; init; }
    /// <summary>Показать в легенде расшифровку: ромб — условие.</summary>
    public bool ShowLegendConditionKey { get; init; }
    /// <summary>Показать в легенде расшифровку: круг со стрелкой — return.</summary>
    public bool ShowLegendReturnKey { get; init; }
    /// <summary>Левая граница колонки легенды (X); если легенды нет — равна ширине области (не рисуем).</summary>
    public double LegendColumnLeft { get; init; } = double.PositiveInfinity;
    public IReadOnlySet<string> HighlightedNodeIds { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> HighlightedEdgeKeys { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool IsEmpty => Nodes.Count == 0;

    /// <summary>Подмена только <see cref="Presentation"/> (после укладки графа из wire <c>graph_kind</c>).</summary>
    public static SemanticMapGraphSceneVm WithPresentationKind(SemanticMapGraphSceneVm scene, SemanticMapGraphPresentationKind presentation)
    {
        if (scene.Presentation == presentation)
            return scene;
        return new SemanticMapGraphSceneVm
        {
            Nodes = scene.Nodes,
            Edges = scene.Edges,
            Presentation = presentation,
            Legend = scene.Legend,
            UseLegendColumn = scene.UseLegendColumn,
            ShowLegendConditionKey = scene.ShowLegendConditionKey,
            ShowLegendReturnKey = scene.ShowLegendReturnKey,
            LegendColumnLeft = scene.LegendColumnLeft,
            HighlightedNodeIds = scene.HighlightedNodeIds,
            HighlightedEdgeKeys = scene.HighlightedEdgeKeys
        };
    }
}

/// <summary>Строка легенды control flow: номер ↔ одна строка кода/предиката.</summary>
public sealed class SemanticMapLegendEntry
{
    public int Index { get; init; }
    public required string Text { get; init; }
}

public enum SemanticMapNodeShape
{
    /// <summary>Обычный шаг (круг).</summary>
    Circle,

    /// <summary>Условие / ветвление в CFG; на карте — ромб (не имя геометрии в API).</summary>
    Condition
}

public sealed class SemanticMapGraphNodeLayout
{
    public required string Id { get; init; }
    public required string Kind { get; init; }
    public required string FullPath { get; init; }
    public required string Label { get; init; }
    public required Point Center { get; init; }
    public required double Radius { get; init; }
    public required bool IsAnchor { get; init; }
    public SemanticMapNodeShape Shape { get; init; } = SemanticMapNodeShape.Circle;
    public int? LegendIndex { get; init; }
    public string? LegendLine { get; init; }
}

public sealed class SemanticMapGraphEdgeLayout
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

#nullable enable
using Avalonia;

namespace CascadeIDE.ViewModels;

/// <summary>Сцена мини-карты Semantic Map (узлы с центром в логических пикселях контрола).</summary>
public sealed class SemanticMapGraphSceneVm
{
    public required IReadOnlyList<SemanticMapGraphNodeLayout> Nodes { get; init; }
    public required IReadOnlyList<SemanticMapGraphEdgeLayout> Edges { get; init; }

    public bool IsEmpty => Nodes.Count == 0;
}

public sealed class SemanticMapGraphNodeLayout
{
    public required string Id { get; init; }
    public required string FullPath { get; init; }
    public required string Label { get; init; }
    public required Point Center { get; init; }
    public required double Radius { get; init; }
    public required bool IsAnchor { get; init; }
}

public sealed class SemanticMapGraphEdgeLayout
{
    public required Point From { get; init; }
    public required Point To { get; init; }
}

#nullable enable
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services.Navigation;

/// <summary>Укладка узлов/рёбер Semantic Map для мини-карты (ADR 0039 §4; v1 — звезда от якоря).</summary>
public interface ISemanticMapSubgraphLayoutEngine
{
    /// <summary>Строит сцену для отрисовки в заданном прямоугольнике (логические пиксели).</summary>
    SemanticMapGraphSceneVm Layout(SemanticMapSubgraphDocument doc, double width, double height);
}

#nullable enable
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services.Navigation;

/// <summary>Укладка узлов/рёбер карты кода для мини-карты (ADR 0039 §4; v1 — звезда от якоря).</summary>
public interface ICodeNavigationMapSubgraphLayoutEngine
{
    /// <summary>Строит сцену для отрисовки в заданном прямоугольнике (логические пиксели).</summary>
    CodeNavigationMapGraphSceneVm Layout(CodeNavigationMapSubgraphDocument doc, double width, double height);
}

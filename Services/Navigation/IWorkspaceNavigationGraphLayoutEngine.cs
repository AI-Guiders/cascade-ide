#nullable enable
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services.Navigation;

/// <summary>Укладка узлов/рёбер Semantic Map для мини-карты (ADR 0039 §4; v1 — звезда от якоря).</summary>
public interface IWorkspaceNavigationGraphLayoutEngine
{
    /// <summary>Строит сцену для отрисовки в заданном прямоугольнике (логические пиксели).</summary>
    SemanticMapGraphSceneVm Layout(WorkspaceNavigationSubgraphDocument doc, double width, double height);
}

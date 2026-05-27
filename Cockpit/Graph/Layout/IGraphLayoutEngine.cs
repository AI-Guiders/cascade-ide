#nullable enable
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Graph.Layout;

/// <summary>Укладка узлов/рёбер graph-backed surface (ADR 0067, 0115).</summary>
public interface IGraphLayoutEngine
{
    GraphLayoutScene Layout(
        GraphDocument doc,
        double width,
        double height,
        CodeNavigationMapDetailLevel detailLevel = CodeNavigationMapDetailLevel.Normal,
        GraphControlFlowMainAxis? controlFlowMainAxisOverride = null);
}
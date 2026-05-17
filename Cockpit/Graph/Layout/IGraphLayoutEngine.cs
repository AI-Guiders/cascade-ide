#nullable enable

namespace CascadeIDE.Cockpit.Graph.Layout;

/// <summary>Укладка узлов/рёбер graph-backed surface (ADR 0067, 0115).</summary>
public interface IGraphLayoutEngine
{
    GraphLayoutScene Layout(GraphDocument doc, double width, double height);
}

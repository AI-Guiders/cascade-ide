#nullable enable

namespace CascadeIDE.Cockpit.Graph.Layout;

/// <summary>Направление основного потока control-flow на карте (глубина BFS от якоря).</summary>
public enum GraphControlFlowMainAxis
{
    /// <summary>Глубина сверху вниз, ветки влево-вправо.</summary>
    Vertical = 0,

    /// <summary>Глубина слева направо, ветки вверх-вниз.</summary>
    Horizontal = 1
}

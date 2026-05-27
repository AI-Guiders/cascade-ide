#nullable enable

namespace CascadeIDE.Cockpit.Graph.Layout;

/// <summary>Подсказки укладчику для адаптации к плотности графа (ADR 0053 declutter).</summary>
/// <param name="IsDense">Компактнее подписи и немного меньше «чернило» узлов при перегруженном CFG.</param>
public readonly record struct GraphLayoutEngineOptions(bool IsDense = false);

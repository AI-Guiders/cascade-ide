#nullable enable
using Avalonia;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services.Navigation;

/// <summary>Стабильные id внутренних блоков прибора карты намерений (ADR 0088 §2.4) — не путать с <c>cell_id</c> deck региона.</summary>
public static class CodeNavigationMapWorkspaceInstrumentBlockIds
{
    public const string Graph = "code_navigation.workspace_instrument.block.graph";
    public const string Legend = "code_navigation.workspace_instrument.block.legend";
}

/// <summary>Вид внутреннего блока кабинного прибора карты намерений.</summary>
public enum CodeNavigationMapWorkspaceInstrumentBlockKind
{
    /// <summary>Подграф (узлы, рёбра) в зарезервированной области графа.</summary>
    Graph = 0,

    /// <summary>Легенда (индексы шагов, ключи фигур, стили рёбер).</summary>
    Legend = 1
}

/// <summary>Размещение одного внутреннего блока в системе координат мини-карты (0,0 — левый верх вьюпорта).</summary>
public sealed record CodeNavigationMapWorkspaceInstrumentBlockDescriptor(
    string Id,
    CodeNavigationMapWorkspaceInstrumentBlockKind Kind,
    Rect Bounds)
{
    public bool IsEmpty => Bounds.Width <= 0 || Bounds.Height <= 0;
}

/// <summary>
/// Композитор <b>внутренних</b> блоков прибора: по уже уложенной <see cref="CodeNavigationMapGraphSceneVm"/> и размеру вьюпорта
/// вычисляет осмысленные прямоугольники «граф / легенда» (тот же смысл, что разнесение в <see cref="Cockpit.PrimitivesKit.CodeNavigationMapSceneDrawing"/>).
/// </summary>
public static class CodeNavigationMapWorkspaceInstrumentBlockCompositor
{
    public static IReadOnlyList<CodeNavigationMapWorkspaceInstrumentBlockDescriptor> Compose(
        CodeNavigationMapGraphSceneVm scene,
        double viewportWidth,
        double viewportHeight)
    {
        if (scene.IsEmpty || viewportWidth <= 0 || viewportHeight <= 0)
            return Array.Empty<CodeNavigationMapWorkspaceInstrumentBlockDescriptor>();

        if (!scene.UseLegendColumn)
        {
            return
            [
                new CodeNavigationMapWorkspaceInstrumentBlockDescriptor(
                    CodeNavigationMapWorkspaceInstrumentBlockIds.Graph,
                    CodeNavigationMapWorkspaceInstrumentBlockKind.Graph,
                    new Rect(0, 0, viewportWidth, viewportHeight))
            ];
        }

        if (scene.LegendPlacement == CodeNavigationMapLegendBlockPlacement.BelowGraph
            && scene.LegendBlockTopY > 0
            && scene.LegendBlockTopY < viewportHeight)
        {
            var graphH = Math.Min(scene.LegendBlockTopY, viewportHeight);
            return
            [
                new CodeNavigationMapWorkspaceInstrumentBlockDescriptor(
                    CodeNavigationMapWorkspaceInstrumentBlockIds.Graph,
                    CodeNavigationMapWorkspaceInstrumentBlockKind.Graph,
                    new Rect(0, 0, viewportWidth, graphH)),
                new CodeNavigationMapWorkspaceInstrumentBlockDescriptor(
                    CodeNavigationMapWorkspaceInstrumentBlockIds.Legend,
                    CodeNavigationMapWorkspaceInstrumentBlockKind.Legend,
                    new Rect(0, scene.LegendBlockTopY, viewportWidth, viewportHeight - scene.LegendBlockTopY))
            ];
        }

        // Соседняя колонка: легенда справа от <see cref="CodeNavigationMapGraphSceneVm.LegendColumnLeft"/>
        var legendX = Math.Min(scene.LegendColumnLeft, viewportWidth);
        if (legendX >= viewportWidth - 1)
        {
            return
            [
                new CodeNavigationMapWorkspaceInstrumentBlockDescriptor(
                    CodeNavigationMapWorkspaceInstrumentBlockIds.Graph,
                    CodeNavigationMapWorkspaceInstrumentBlockKind.Graph,
                    new Rect(0, 0, viewportWidth, viewportHeight))
            ];
        }

        var graphW = Math.Max(0, legendX);
        return
        [
            new CodeNavigationMapWorkspaceInstrumentBlockDescriptor(
                CodeNavigationMapWorkspaceInstrumentBlockIds.Graph,
                CodeNavigationMapWorkspaceInstrumentBlockKind.Graph,
                new Rect(0, 0, graphW, viewportHeight)),
            new CodeNavigationMapWorkspaceInstrumentBlockDescriptor(
                CodeNavigationMapWorkspaceInstrumentBlockIds.Legend,
                CodeNavigationMapWorkspaceInstrumentBlockKind.Legend,
                new Rect(legendX, 0, viewportWidth - legendX, viewportHeight))
        ];
    }
}

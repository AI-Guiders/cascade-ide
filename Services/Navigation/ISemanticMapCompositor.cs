#nullable enable
using CascadeIDE.Models;
using CascadeIDE.ViewModels;
using CascadeIDE.Services.SkiaInstruments;

namespace CascadeIDE.Services.Navigation;

/// <summary>
/// Композитор Semantic Map: выбирает layout по уровню карты и возвращает сцену
/// вместе с рекомендуемой высотой viewport для читаемой отрисовки.
/// </summary>
public interface ISemanticMapCompositor
    : ISkiaInstrumentCompositor<SemanticMapCompositionIntent, SemanticMapCompositionResult>
{
    SemanticMapCompositionResult Compose(
        SemanticMapSubgraphDocument doc,
        string semanticMapLevel,
        double availableWidth,
        double availableHeight,
        SemanticMapDetailLevel detailLevel = SemanticMapDetailLevel.Normal);
}

public sealed record SemanticMapCompositionResult(SemanticMapGraphSceneVm Scene, double PreferredHeight);

/// <param name="DetailLevel">Политика Declutter + метрики Layout (ADR 0055); источник — <c>[semantic_map].detail_level</c>.</param>
public sealed record SemanticMapCompositionIntent(
    SemanticMapSubgraphDocument Subgraph,
    string SemanticMapLevel,
    SemanticMapDetailLevel DetailLevel = SemanticMapDetailLevel.Normal);
